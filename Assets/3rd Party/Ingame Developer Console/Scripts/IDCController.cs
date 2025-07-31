using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IDC
{
    [Flags]
    public enum CommandType
    {
        Manual = 1,
        OnConsoleOpen = 2,
        OnConsoleClose = 4,
        OnOpenOrClose = OnConsoleOpen | OnConsoleClose
    }

    [DisallowMultipleComponent]
    public partial class IDCController : MonoBehaviour
    {
        enum ConsoleMovementOption
        {
            ToggleSmall,
            ToggleMinimal
        }

        #region Consts
        /// <summary>GoHash to use for static and non-MonoBehaviour classes</summary>
        const int StaticClassGoHash = 1;
        const string StaticClassesGoName = "Static Classes", NonMonoClassesGoName = "Normal C# Classes";
        const string LogPrefix = "[LOG]: ", WarningPrefix = "[WARNING]: ", AssertPrefix = "[ASSERT]: ", ErrorPrefix = "[ERROR]: ", ExceptionPrefix = "[EXCEPTION]: ";
        const char ArrayOpenChar = '[', ArrayCloseChar = ']', ObjectCtorOpenChar = '(', ObjectCtorCloseChar = ')', ObjectCtorParamsSeparator = ',';
        public const BindingFlags MethodBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        #endregion

        #region Public
        public IDCSettings settings;
        public IDCShortcutProfile shortcutProfile;
        #endregion

        #region UI
        Scrollbar scrollBar;
        ScrollRect scrollrect;
        InfiniteLog infiniteLog;
        TMP_InputField inputField;
        ContentSizeFitter contentSizeFitter;
        TMP_Text expandingText, logText, suggestionsText;
        RectTransform canvas, panel, logRectTr, suggestionsBgRectTr, suggestionsRectTr, inputFieldRect;

        /// <summary>Last scrollpos before closing</summary>
        float prevScrollPos;
        #endregion

        #region Command Related
        Application.LogCallback logCallback;

        readonly Dictionary<string, IDCCmdAttribute> cmds = new Dictionary<string, IDCCmdAttribute>();
        readonly List<string> onOpenCmds = new List<string>(), onCloseCmds = new List<string>(), onOpenOrCloseCmds = new List<string>();
        readonly List<string> inputHistory = new List<string>(), formattedSuggestions = new List<string>(), rawSuggestions = new List<string>();

        /// <summary>Maps IDC calculated go hashes to IDCGO objects</summary>
        readonly Dictionary<int, IDCGO> gos = new Dictionary<int, IDCGO>();
        /// <summary>Contains GOs with classes that use var attributes</summary>
        readonly HashSet<IDCGO> gosWithVars = new HashSet<IDCGO>();
        /// <summary>Maps class names to active class instances</summary>
        readonly Dictionary<string, List<IDCClassInstance>> classInstances = new Dictionary<string, List<IDCClassInstance>>();
        /// <summary>Maps class names to a dictionary mapping var names to var attribs</summary>
        readonly Dictionary<string, Dictionary<string, IDCVarAttribute>> classVars = new Dictionary<string, Dictionary<string, IDCVarAttribute>>();

        readonly Dictionary<string, HideFlags> typeHideFlags = new Dictionary<string, HideFlags>();
        readonly Dictionary<string, TypeSuggestion> typeSuggs = new Dictionary<string, TypeSuggestion>();
        readonly Dictionary<string, Dictionary<string, UnityEngine.Object>> unityObjectSuggs = new Dictionary<string, Dictionary<string, UnityEngine.Object>>();
        #endregion

        IDCState state;
        IDCVarsWindow varsWindow;
        ConsoleMovementOption consoleMoveOption;
        readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();

        int suggestionsIndex, suggestionIndexOffset, inputHistoryIndex, prevCaretPos;

        /// <summary>Whether enough time has passed for selecting items when the arrow keys are held</summary>
        bool SelectionHasCooledDown { get { return keyHoldTimer > 0.2f; } }
        float keyHoldTimer;

        bool isMoving, IsConsoleClosed = true;

        void Awake()
        {
            //References
            canvas = GetComponent<RectTransform>();
            panel = transform.GetChild(0).GetComponent<RectTransform>();
            inputField = panel.GetChild(0).GetComponent<TMP_InputField>();
            inputFieldRect = inputField.GetComponent<RectTransform>();
            logRectTr = panel.GetChild(1).GetComponent<RectTransform>();
            expandingText = panel.GetChild(1).GetChild(0).GetComponent<TMP_Text>();
            logText = panel.GetChild(1).GetChild(1).GetComponent<TMP_Text>();
            scrollBar = panel.GetChild(1).GetChild(2).GetComponent<Scrollbar>();
            scrollrect = panel.GetChild(1).GetComponent<ScrollRect>();
            infiniteLog = panel.GetChild(1).GetComponent<InfiniteLog>();
            suggestionsBgRectTr = panel.GetChild(2).GetComponent<RectTransform>();
            suggestionsRectTr = suggestionsBgRectTr.GetChild(0).GetComponent<RectTransform>();
            suggestionsText = suggestionsRectTr.GetComponent<TMP_Text>();
            contentSizeFitter = suggestionsRectTr.GetComponent<ContentSizeFitter>();
            varsWindow = transform.GetChild(1).GetComponent<IDCVarsWindow>();

            state = new IDCState(inputField, rawSuggestions);

            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<IDCSettings>();
                Log("IDCSettings asset not set in inspector. Using default settings", LogType.Warning);
            }

            if (!shortcutProfile)
            {
                shortcutProfile = ScriptableObject.CreateInstance<IDCShortcutProfile>();
                Log("IDCShortcutProfile asset not set in inspector. Using default shortcut profile", LogType.Warning);
            }

            //Make entry for static classes so we don't have to check on every addition
            gos.Add(StaticClassGoHash, new IDCGO(StaticClassesGoName));
            infiniteLog.Init(settings.maxLogLines);
        }

        void Start()
        {
            AddClass(this);
            SetUnityTypeHideFlags(typeof(Font), HideFlags.NotEditable);

            scenes = new string[UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings];
            for (int i = 0; i < scenes.Length; i++)
                scenes[i] = i + " " + UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);

            SetParamSuggestions(IDCCmdsEnum.LoadScene, "sceneName", scenes);
            SetParamSuggestions(IDCCmdsEnum.UnloadScene, "sceneName", scenes);

            //Init log and vars window and apply settings
            varsWindow.Init(gosWithVars, classInstances, classVars, settings.maxLogLines);
            ApplySettings();

            //Catch unity logs
            logCallback = new Application.LogCallback(UnityLogs);
            Application.logMessageReceived += logCallback;
        }

        void ApplySettings()
        {
            varsWindow.ApplySettings(settings);
            infiniteLog.SetMaxLogLines(settings.maxLogLines);

            //Font and linespacing and tab sizes
            infiniteLog.SetFont(settings.logFont);
            infiniteLog.SetFontSize(settings.logFontSize);
            infiniteLog.SetTabSize(settings.tabSize);
            infiniteLog.SetLineSpacing(settings.logLineSpacing);

            suggestionsText.font = settings.suggestionsFont;
            suggestionsText.font.tabSize = settings.tabSize;
            suggestionsText.fontSize = settings.suggestionsFontSize;
            suggestionsText.lineSpacing = settings.suggestionsLineSpacing;

            //Colors
            panel.GetChild(1).GetComponent<Image>().color = settings.logAreaColor;
            suggestionsBgRectTr.GetComponent<Image>().color = settings.suggestionsAreaColor;
            suggestionsText.color = settings.suggestionsColor;
            varsWindow.GetComponent<Image>().color = settings.VarsWindowColor;

            //Scrollbar spds
            scrollrect.scrollSensitivity = settings.scrollSpd;
            varsWindow.GetComponent<ScrollRect>().scrollSensitivity = settings.scrollSpd;

            if (!IsConsoleClosed && !AccessLvlIsAllowed(settings.consoleAccessLvl))
                StartCoroutine(ForceCloseConsole());
        }

        #region Adding Classes
        public void AddClass(object classInstance)
        {
            if (classInstance == null)
                Debug.LogError("Class not added to IDC because it is null");

            //Add Gameobject entry
            int goHash = GetGOHash(classInstance);
            if (!gos.ContainsKey(goHash))
                gos.Add(goHash, new IDCGO(classInstance is MonoBehaviour ? ((MonoBehaviour)classInstance).gameObject.name : NonMonoClassesGoName));

            Type t = classInstance.GetType();
            int classHash = GetClassHash(classInstance);
            if (gos[goHash].Classes.ContainsKey(classHash))
                return;

            gos[goHash].Classes.Add(classHash, new IDCClassInstance(t.Name, classHash, classInstance, false));

            //Add class to instances list
            if (!classInstances.ContainsKey(t.Name))
                classInstances.Add(t.Name, new List<IDCClassInstance>() { gos[goHash].Classes[classHash] });
            else
                classInstances[t.Name].Add(gos[goHash].Classes[classHash]);

            AddClassMethods(t, MethodBindingFlags);
            AddClassVars(t, MethodBindingFlags, goHash);
        }

        public void AddStaticClass(Type classType)
        {
            if (classType == null)
                Debug.LogError("Static class not added to IDC because it is null");

            int classHash = GetClassHash(classType);
            if (gos[StaticClassGoHash].Classes.ContainsKey(classHash))
                return;

            gos[StaticClassGoHash].Classes.Add(classHash, new IDCClassInstance(classType.Name, classHash, null, true));

            if (!classInstances.ContainsKey(classType.Name))
                classInstances.Add(classType.Name, new List<IDCClassInstance>() { gos[StaticClassGoHash].Classes[classHash] });

            AddClassMethods(classType, MethodBindingFlags);
            AddClassVars(classType, MethodBindingFlags, StaticClassGoHash);
        }

        void AddClassMethods(Type t, BindingFlags bf)
        {
            //Handle method attributes
            MethodInfo[] mis = t.GetMethods(bf);
            for (int i = 0; i < mis.Length; i++)
            {
                IDCCmdAttribute attrib;
                IDCCmdAttribute[] idcAttribs = (IDCCmdAttribute[])mis[i].GetCustomAttributes(typeof(IDCCmdAttribute), false);
                for (int j = 0; j < idcAttribs.Length; j++)
                {
                    attrib = idcAttribs[j];
                    attrib.SetCmd(mis[i], t.Name);
                    if (cmds.ContainsKey(attrib.CmdName))
                        continue;

                    //Error checking
                    if (!IsValidName(attrib.CmdName))
                    {
                        Debug.LogError("Invalid Command Name '" + attrib.CmdName + "'. Name empty, already in use or invalid.\n Note that only alphanumeric characters and underscores are accepted.");
                        continue;
                    }

                    var methodParams = mis[i].GetParameters();
                    if (attrib.CmdType != CommandType.Manual && methodParams.Length > 0)
                    {
                        bool allOptional = true;
                        for (int k = 0; k < methodParams.Length; k++)
                            if (!methodParams[k].IsOptional)
                            {
                                allOptional = false;
                                break;
                            }

                        if (!allOptional)
                        {
                            Debug.LogError("Error in command '" + attrib.CmdName + "'. Automatic commands can NOT have any parameters");
                            continue;
                        }
                    }

                    AddCmd(attrib);
                }
            }
        }

        void AddClassVars(Type t, BindingFlags bf, int goHash)
        {
            //Handle var attributes
            FieldInfo[] fis = t.GetFields(bf);
            PropertyInfo[] pis = t.GetProperties(bf);
            int count = fis.Length;
            for (int selector = 0; selector < 2; selector++)
            {
                for (int i = 0; i < count; i++)
                {
                    IDCVarAttribute[] varAttribs = selector == 0 ?
                        (IDCVarAttribute[])fis[i].GetCustomAttributes(typeof(IDCVarAttribute), false) :
                        (IDCVarAttribute[])pis[i].GetCustomAttributes(typeof(IDCVarAttribute), false);

                    if (varAttribs.Length == 0)
                        continue;

                    gosWithVars.Add(gos[goHash]);
                    for (int j = 0; j < varAttribs.Length; j++)
                    {
                        var attrib = varAttribs[j];

                        if (selector == 0)
                            attrib.SetVar(fis[i], t.Name);
                        else
                            attrib.SetVar(pis[i], t.Name);

                        if (!IsValidName(attrib.VarName))
                        {
                            Debug.LogError("Invalid Var Name '" + attrib.VarName + "'. Name empty, already in use or invalid.\n Note that only alphanumeric characters and underscores are accepted.");
                            continue;
                        }

                        if (!classVars.ContainsKey(attrib.ClassName))
                        {
                            classVars.Add(attrib.ClassName, new Dictionary<string, IDCVarAttribute>());
                            classVars[attrib.ClassName].Add(attrib.VarName, attrib);
                        }
                        else if (!classVars[attrib.ClassName].ContainsKey(attrib.VarName))
                            classVars[attrib.ClassName].Add(attrib.VarName, attrib);
                    }
                }

                count = pis.Length;
            }

            UpdateSetVarValueSugg();
        }

        /// <summary>
        /// Returns 0 if object is null
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        int GetClassHash(object o)
        {
            if (o == null)
                return 0;

            return o is MonoBehaviour ? ((MonoBehaviour)o).GetInstanceID() : o.GetHashCode();
        }

        /// <summary>
        /// Returns 0 if object is null or not a monobehavior
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        int GetGOHash(object o)
        {
            if (o == null || !(o is MonoBehaviour))
                return 0;

            return ((MonoBehaviour)o).gameObject.GetInstanceID();
        }

        /// <summary>
        /// Validates a given cmd or var name
        /// Only letters, numbers and underscores are accepted
        /// </summary>
        /// <param name="idcObjectName"></param>
        /// <returns></returns>
        bool IsValidName(string idcObjectName)
        {
            if (idcObjectName == null || idcObjectName.Trim() == string.Empty)
                return false;

            idcObjectName = idcObjectName.Trim();
            for (int i = 0; i < idcObjectName.Length; i++)
                if (!char.IsLetterOrDigit(idcObjectName[i]) && idcObjectName[i] != '_')
                    return false;

            return true;
        }

        void AddCmd(IDCCmdAttribute cmd)
        {
            cmds.Add(cmd.CmdName, cmd);

            if (cmd.CmdType == CommandType.OnConsoleClose)
                onCloseCmds.Add(cmd.CmdName);
            else if (cmd.CmdType == CommandType.OnConsoleOpen)
                onOpenCmds.Add(cmd.CmdName);
            else if (cmd.CmdType == CommandType.OnOpenOrClose)
                onOpenOrCloseCmds.Add(cmd.CmdName);
        }

        /// <summary>
        /// Removes garbage collected classes and their variables from the IDC. Called internally.
        /// </summary>
        public void Clean()
        {
            List<int> gosToRemove = new List<int>();
            List<IDCClassInstance> classesToRemove = null;

            foreach (var go in gos)
            {
                if (go.Key == StaticClassGoHash)
                    continue;

                //Get classes to remove
                classesToRemove = new List<IDCClassInstance>();
                foreach (var c in go.Value.Classes.Values)
                    if (c.Instance == null || c.Instance.ToString() == "null")
                        classesToRemove.Add(c);

                //If we are removing all classes from GO then directly add GO to remove list
                if (go.Value.Classes.Count - classesToRemove.Count == 0)
                {
                    gosToRemove.Add(go.Key);
                    continue;
                }

                for (int i = 0; i < classesToRemove.Count; i++)
                    go.Value.Classes.Remove(classesToRemove[i].Hash);
            }

            for (int i = 0; i < gosToRemove.Count; i++)
            {
                gosWithVars.Remove(gos[gosToRemove[i]]);
                gos.Remove(gosToRemove[i]);
            }

            //Clean classInstances as well
            foreach (var cil in classInstances.Values)
                if (cil.Count > 0 && !cil[0].IsStatic)
                    cil.RemoveAll(x => x.Instance == null);

            UpdateSetVarValueSugg();
        }
        #endregion

        #region Update and Input
        void Update()
        {
            if (!AccessLvlIsAllowed(settings.consoleAccessLvl))
                return;

            if (!isMoving && Input.GetKeyDown(settings.toggleKey))
                HandleToggleKey();

            HandleShortcuts();
            if (isMoving || IsConsoleClosed)
                return;

            state.UpdateState();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                HandleReturnKey();

            HandleUpDownKeys();

            //If we are giving cmd params then force an update if the caret is moved
            if ((state.State == IDCState.ConsoleState.ShowingCmdSignature || state.State == IDCState.ConsoleState.SelectingParamValues)
                && (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.Home) || Input.GetKeyUp(KeyCode.End) || Input.GetMouseButtonDown(0)))
                UpdateSuggestions();

            //Fix disappearing double quote when the last character
            if (inputField.text.Length > 0 && inputField.text[inputField.text.Length - 1] == '"')
                inputField.text += " ";

            inputField.ActivateInputField();
            prevCaretPos = inputField.caretPosition;
        }

        void HandleToggleKey()
        {
            if (inputField.text != "")
            {
                int index = inputField.text.LastIndexOf('`');
                if (index >= 0)
                    inputField.text = inputField.text.Remove(index, 1);

                index = inputField.text.LastIndexOf('~');
                if (index >= 0)
                    inputField.text = inputField.text.Remove(index, 1);
            }

            if (Input.GetKey(settings.sizeModifierKey))
                consoleMoveOption = ConsoleMovementOption.ToggleMinimal;
            else
                consoleMoveOption = ConsoleMovementOption.ToggleSmall;

            StartCoroutine(MoveConsole());
        }

        void HandleReturnKey()
        {
            //Check that the input field is selected before processing input
            if (state.InputTxt.Length == 0 || UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != inputField.gameObject)
                return;

            ProcessInput(inputField.text);
            inputField.text = string.Empty;
        }

        void HandleShortcuts()
        {
            if (settings.shortcutsInConsoleOnly && (isMoving || IsConsoleClosed))
                return;

            var shortcuts = shortcutProfile.shortcuts;

            for (int i = 0; i < shortcuts.Count; i++)
            {
                var s = shortcuts[i];

                if (!Input.GetKeyDown(s.key))
                    continue;

                bool modsPressed = true;
                for (int j = 0; j < s.modifiers.Count; j++)
                {
                    var modKey = s.modifiers[j];

                    // Accept mod keys from any position (e.g. LeftCtrl and RightCtrl are both accepted)
                    if (!Input.GetKey((KeyCode)modKey) && !Input.GetKey((KeyCode)modKey.Opposite()))
                    {
                        modsPressed = false;
                        break;
                    }
                }

                if (modsPressed)
                    for (int j = 0; j < s.cmds.Count; j++)
                        RunCmd(s.cmds[j]);
            }
        }

        void HandleUpDownKeys()
        {
            //Tick selection cooldown timer and check arrow key input
            keyHoldTimer += Time.deltaTime;
            if (!state.CanSelectSuggestions)
                return;

            bool down = Input.GetKeyDown(KeyCode.DownArrow) || (SelectionHasCooledDown && Input.GetKey(KeyCode.DownArrow));
            bool upOrDown = down || Input.GetKeyDown(KeyCode.UpArrow) || (SelectionHasCooledDown && Input.GetKey(KeyCode.UpArrow));

            keyHoldTimer = upOrDown ? 0 : keyHoldTimer;

            switch (state.State)
            {
                case IDCState.ConsoleState.SelectingCmds:
                case IDCState.ConsoleState.SuggestingCmds:
                    UpDownSelectingCmds(down, upOrDown);
                    break;
                case IDCState.ConsoleState.SelectingFromHistory:
                    UpDownSelectingFromHistory(down, upOrDown);
                    break;
                case IDCState.ConsoleState.ShowingCmdSignature:
                case IDCState.ConsoleState.SelectingParamValues:
                    UpDownSelectingParamValues(down, upOrDown);
                    break;
            }

            //If we are not actively selecting from history then just make sure the index is reset
            if (state.State != IDCState.ConsoleState.SelectingFromHistory)
                inputHistoryIndex = inputHistory.Count + 1;

            //All required to stop the caret moving (especially when holding arrow keys then releasing)
            if (state.State != IDCState.ConsoleState.SelectingFromHistory
                && (upOrDown || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow)))
                inputField.caretPosition = prevCaretPos;
        }

        void UpDownSelectingCmds(bool down, bool upOrDown)
        {
            if (upOrDown)
            {
                suggestionsIndex = down ? suggestionsIndex + 1 : suggestionsIndex - 1;
                suggestionsIndex = Mathf.Clamp(suggestionsIndex, 0, rawSuggestions.Count - 1);
                state.CanSelectSuggestions = true;
                ShowSuggestions();
            }

            else if (rawSuggestions.Count > 0 && Input.GetKeyDown(KeyCode.Tab))
            {
                WriteToInputField(rawSuggestions[suggestionsIndex] + " ");
                StartCoroutine(DeselectText());
            }
        }

        void UpDownSelectingFromHistory(bool down, bool upOrDown)
        {
            if (!upOrDown || inputHistory.Count < 1)
                return;

            inputHistoryIndex = down ? inputHistoryIndex + 1 : inputHistoryIndex - 1;
            inputHistoryIndex = Mathf.Clamp(inputHistoryIndex, 0, inputHistory.Count - 1);
            WriteToInputField(inputHistory[inputHistoryIndex]);
            inputField.caretPosition = int.MaxValue;    //Place caret at the end
        }

        void UpDownSelectingParamValues(bool down, bool upOrDown)
        {
            if (rawSuggestions.Count == 0 && formattedSuggestions.Count == 0)
                return;

            var listToUse = rawSuggestions.Count == 0 ? formattedSuggestions : rawSuggestions;

            if (upOrDown)
            {
                suggestionsIndex = down ? suggestionsIndex + 1 : suggestionsIndex - 1;
                suggestionsIndex = Mathf.Clamp(suggestionsIndex, 0, listToUse.Count - suggestionIndexOffset - 1);
                state.CanSelectSuggestions = true;
                ShowSuggestions(rawSuggestions.Count > 0);
            }

            else if (state.CanSelectSuggestions && Input.GetKeyDown(KeyCode.Tab))
            {
                string valToInsert = listToUse[suggestionsIndex + suggestionIndexOffset];
                int paramStart = state.CurrParamPos, length = valToInsert.Length + 2;

                //Add quotes if suggestions contains '-' and isn't a number
                if (valToInsert.Contains("-"))
                {
                    bool suggIsDigit = true;
                    for (int i = 0; i < valToInsert.Length; i++)
                        if (!char.IsDigit(valToInsert[i]) && valToInsert[i] != '-')
                        {
                            suggIsDigit = false;
                            break;
                        }

                    if (!suggIsDigit)
                    {
                        valToInsert = "\"" + valToInsert + "\"";
                        length += 2;
                    }
                }

                inputField.text = state.InputTxt
                    .Remove(state.CurrParamPos, state.NextParamPos - state.CurrParamPos)
                    .Insert(state.CurrParamPos, "- " + valToInsert + " ");

                StartCoroutine(SetCaretPos(paramStart + length + 1));
                UpdateSuggestions();
            }
        }

        IEnumerator SetCaretPos(int pos)
        {
            inputField.caretWidth = 0;
            yield return null;
            inputField.caretPosition = pos;
            inputField.caretWidth = 2;
        }
        #endregion

        #region Input Processing & CMD Exec
        void ProcessInput(string cmdInput)
        {
            string output = string.Empty;
            inputHistory.Add(cmdInput);
            var input = new List<string> { "" };
            int currParamIndex = 0;

            bool insideLiteral = false, needsArgColorEnd = false;
            for (int i = 0; i < cmdInput.Length; i++)
            {
                switch (cmdInput[i])
                {
                    case '"':
                        //If we are not inside a literal means this is the first double quote so add a start, otherwise an end
                        if (!insideLiteral)
                            output += IDCUtils.GetColorStart(settings.stringColor) + cmdInput[i];
                        else
                            output += cmdInput[i] + IDCUtils.colorEnd;

                        insideLiteral = !insideLiteral;
                        continue;

                    case '-':
                        if (insideLiteral || (i + 1 < cmdInput.Length && char.IsDigit(cmdInput[i + 1])))
                        {
                            output += '-';
                        }

                        else
                        {
                            if (needsArgColorEnd)
                            {
                                output += IDCUtils.colorEnd;
                                needsArgColorEnd = !needsArgColorEnd;
                            }

                            output += cmdInput[i] + IDCUtils.GetColorStart(settings.argumentColor);
                            needsArgColorEnd = true;

                            //Start a new parameter
                            input.Add("");
                            currParamIndex++;
                            continue;
                        }
                        break;

                    //Escape quotes
                    case '\\':
                        if (i + 1 < cmdInput.Length && cmdInput[i + 1] == '"')
                        {
                            i++;
                            output += cmdInput[i];
                            input[currParamIndex] += cmdInput[i];
                            continue;
                        }
                        break;

                    default:
                        output += cmdInput[i];
                        break;
                }

                input[currParamIndex] += cmdInput[i];
            }

            //If still in literal means we have an open string, so make sure to close the color for it
            if (insideLiteral)
                output += IDCUtils.colorEnd;
            //In case this is the last argument then we make sure to close it
            if (needsArgColorEnd)
                output += IDCUtils.colorEnd;

            //Error checking
            string cmdName = input[0].Trim();
            if (!cmds.ContainsKey(cmdName))
            {
                Log(cmdInput);
                Log("Command '" + cmdName + "' does not exist", settings.errorColor, LogType.Error);
                return;
            }

            var cmd = cmds[cmdName];
            if (!AccessLvlIsAllowed(cmd.AccessLevel))
            {
                Log(cmdInput.Replace(input[0], IDCUtils.ColorString(input[0], settings.cmdColor)));
                Log("Command not accessible. Access Level is: " + cmd.AccessLevel, settings.errorColor, LogType.Error);
                return;
            }

            if (input.Count - 1 < cmd.MinArgs)
            {
                Log(cmdInput.Replace(input[0], IDCUtils.ColorString(input[0], settings.cmdColor)));
                Log("Not enough arguments. Min: " + cmd.MinArgs, settings.errorColor, LogType.Error);
                return;
            }

            for (int i = 0; i < input.Count; i++)
                if (input[i] == string.Empty)
                {
                    Log(cmdInput.Replace(input[0], IDCUtils.ColorString(input[0], settings.cmdColor)));
                    Log("Empty arguments are not allowed", settings.errorColor, LogType.Error);
                    return;
                }

            Log("> " + output.Replace(input[0], IDCUtils.ColorString(input[0], settings.cmdColor)));
            var args = ValidateArgs(input);
            if (args != null)
                RunCmd(cmd.CmdName, args);
        }

        public bool AccessLvlIsAllowed(AccessLevel cmdAccessLevel)
        {
            switch (cmdAccessLevel)
            {
                case AccessLevel.EditorOnly:
                    return Application.isEditor;
                case AccessLevel.ProductionBuildOnly:
                    return !Debug.isDebugBuild && !Application.isEditor;
                case AccessLevel.DevBuildOnly:
                    return Debug.isDebugBuild && !Application.isEditor;
                case AccessLevel.EditorAndDevBuild:
                    return Application.isEditor || (Debug.isDebugBuild && !Application.isEditor);
                case AccessLevel.AnyBuild:
                    return !Application.isEditor;
                case AccessLevel.Everywhere:
                    return true;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Validates arg types and converts the arg strings to an object array of the correct types and values.
        /// Returns null when an error is encourted.
        /// </summary>
        /// <param name="input"></param>
        object[] ValidateArgs(List<string> input)
        {
            IDCCmdAttribute cmd = cmds[input[0].Trim()];
            object[] args = new object[cmd.ParamInfo.Length];

            bool success = true;
            for (int i = 0; i < cmd.ParamInfo.Length && i < input.Count - 1; i++)
            {
                var pt = cmd.ParamInfo[i].ParameterType;
                if (pt == typeof(object))
                    continue;

                string param = input[i + 1].Trim();
                bool isClass = pt.IsClass && pt != typeof(string);
                bool isStruct = !pt.IsPrimitive && pt.IsValueType && !pt.IsEnum;

                if (pt.IsArray)
                    args[i] = ParseToArray(param, pt, out success);
                else if (pt.IsEnum)
                    args[i] = ParseToEnum(param, pt, out success);
                else if (isClass || isStruct)
                    args[i] = ParseToObject(param, pt, isStruct, out success);
                else
                    args[i] = ParseToPrimitive(param, pt, out success);

                if (!success)
                    break;
            }

            if (!success)
            {
                Log("Wrong arg types or invalid input", LogType.Error);
                return null;
            }

            //For optional variables that weren't provided by the user, place their default values in the args
            for (int i = input.Count - 1; i < cmd.ParamInfo.Length; i++)
                args[i] = cmd.ParamInfo[i].RawDefaultValue;

            return args;
        }

        public object ParseToArray(string arrValuesParam, Type arrParamType, out bool success)
        {
            if (!arrValuesParam.StartsWith(ArrayOpenChar.ToString()) || !arrValuesParam.EndsWith(ArrayCloseChar.ToString()))
            {
                success = false;
                return null;
            }

            //Remove opening square bracket
            arrValuesParam = arrValuesParam.Substring(1, arrValuesParam.Length - 1);

            //Get individual array values by splittling at commas, but check they are not in a string
            List<string> arrValues = new List<string>();

            int start = 0;
            bool inString = false, inObject = false;
            for (int j = 0; j < arrValuesParam.Length; j++)
            {
                //If inside a string skip chars but detect when we reach string end
                if (arrValuesParam[j] == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                //If inside an object skip chars but detect when we reach object end
                if (arrValuesParam[j] == ObjectCtorOpenChar)
                {
                    inObject = true;
                    continue;
                }
                else if (arrValuesParam[j] == ObjectCtorCloseChar)
                {
                    inObject = false;
                    continue;
                }

                if (inObject)
                    continue;

                //If not inside an object and not inside a string we can process params
                if (arrValuesParam[j] == ObjectCtorParamsSeparator || arrValuesParam[j] == ArrayCloseChar)
                {
                    if (arrValuesParam[j] == ArrayCloseChar)
                    {
                        string s = arrValuesParam.Substring(start, j - start).Trim();
                        if (s != string.Empty)
                            arrValues.Add(s);
                    }

                    else
                    {
                        arrValues.Add(arrValuesParam.Substring(start, j - start).Trim());
                        start = j + 1;
                    }
                }
            }

            //Parse individual values depending on the array's element type
            Type elementType = arrParamType.GetElementType();
            Array outArr = Array.CreateInstance(elementType, arrValues.Count);

            //Handle empty array
            if (arrValues.Count == 0 || arrValues.Count == 1 && arrValues[0] == "")
            {
                success = true;
                return Array.CreateInstance(elementType, 0);
            }

            bool isClass = elementType.IsClass && elementType != typeof(string);
            bool isStruct = !elementType.IsPrimitive && elementType.IsValueType && !elementType.IsEnum;
            if (elementType.IsEnum)
            {
                for (int i = 0; i < arrValues.Count; i++)
                {
                    outArr.SetValue(ParseToEnum(arrValues[i], elementType, out success), i);

                    if (!success)
                        return null;
                }
            }
            else if (isClass || isStruct)
            {
                for (int i = 0; i < arrValues.Count; i++)
                {
                    outArr.SetValue(ParseToObject(arrValues[i], elementType, isStruct, out success), i);

                    if (!success)
                        return null;
                }
            }
            else
            {
                for (int i = 0; i < arrValues.Count; i++)
                {
                    outArr.SetValue(ParseToPrimitive(arrValues[i], elementType, out success), i);

                    if (!success)
                        return null;
                }
            }

            success = true;
            return outArr;
        }

        public object ParseToEnum(string param, Type paramType, out bool success)
        {
            success = true;
            try
            {
                return Enum.Parse(paramType, param);
            }

            catch (Exception x)
            {
                success = false;
                Log(x.Message, LogType.Error);
                return 0;
            }
        }

        public object ParseToObject(string constructorParams, Type paramType, bool isStruct, out bool success)
        {
            if (paramType.IsClass && constructorParams == "null")
            {
                success = true;
                return null;
            }

            if (unityObjectSuggs.ContainsKey(paramType.Name) && unityObjectSuggs[paramType.Name].ContainsKey(constructorParams))
            {
                success = true;
                return unityObjectSuggs[paramType.Name][constructorParams];
            }

            //Handle creation through static properties
            if (constructorParams[0] != ObjectCtorOpenChar)
            {
                var staticProperties = paramType.GetProperties(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < staticProperties.Length; i++)
                {
                    if (staticProperties[i].Name == constructorParams)
                    {
                        success = true;
                        return staticProperties[i].GetValue(null, null);
                    }
                }
            }

            if (constructorParams[0] != ObjectCtorOpenChar)
            {
                success = false;
                return null;
            }

            //Get constructor args by splittling at commas, but check they are not in a string
            constructorParams = constructorParams.Remove(0, 1);
            List<string> constructorInputs = new List<string>();

            int start = 0;
            bool inString = false;
            for (int j = 0; j < constructorParams.Length; j++)
            {
                if (constructorParams[j] == '"')
                    inString = !inString;
                else if (!inString && (constructorParams[j] == ObjectCtorParamsSeparator || constructorParams[j] == ObjectCtorCloseChar))
                {
                    if (constructorParams[j] == ObjectCtorCloseChar)
                    {
                        string s = constructorParams.Substring(start, j - start).Trim();
                        if (s != string.Empty)
                            constructorInputs.Add(s);
                    }

                    else
                    {
                        constructorInputs.Add(constructorParams.Substring(start, j - start).Trim());
                        start = j + 1;
                    }
                }
            }

            //Try to find the correct constructor for passed args
            ConstructorInfo[] ci = paramType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            List<object> constructorArgs = new List<object>();
            success = false;
            for (int j = 0; j < ci.Length; j++)
            {
                var cp = ci[j].GetParameters();
                if (cp.Length == constructorInputs.Count)
                    for (int k = 0; k < constructorInputs.Count; k++)
                    {
                        constructorArgs.Add(ParseToPrimitive(constructorInputs[k], cp[k].ParameterType, out success));
                        if (!success)
                            break;
                    }

                if (success)
                    break;
            }

            //Call default constructor if providing no inputs for the class type
            if (success)
                return Activator.CreateInstance(paramType, constructorArgs.ToArray());
            else if (constructorInputs.Count == 0 && isStruct || paramType.GetConstructor(Type.EmptyTypes) != null)
            {
                success = true;
                return Activator.CreateInstance(paramType);
            }

            //No default constructor
            else
            {
                success = false;
                return null;
            }
        }

        public object ParseToPrimitive(string s, Type t, out bool success)
        {
            if (t.IsPrimitive)
            {
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Boolean:
                        bool b = false;
                        success = bool.TryParse(s, out b);
                        return b;
                    case TypeCode.Char:
                        char c = ' ';
                        success = char.TryParse(s, out c);
                        return c;
                    case TypeCode.SByte:
                        sbyte sb = 0;
                        success = sbyte.TryParse(s, out sb);
                        return sb;
                    case TypeCode.Byte:
                        byte by = 0;
                        success = byte.TryParse(s, out by);
                        return by;
                    case TypeCode.Int16:
                        short sh = 0;
                        success = short.TryParse(s, out sh);
                        return sh;
                    case TypeCode.UInt16:
                        ushort ush = 0;
                        success = ushort.TryParse(s, out ush);
                        return ush;
                    case TypeCode.Int32:
                        int i = 0;
                        success = int.TryParse(s, out i);
                        return i;
                    case TypeCode.UInt32:
                        uint ui = 0;
                        success = uint.TryParse(s, out ui);
                        return ui;
                    case TypeCode.Int64:
                        long l = 0;
                        success = long.TryParse(s, out l);
                        return l;
                    case TypeCode.UInt64:
                        ulong ul = 0;
                        success = ulong.TryParse(s, out ul);
                        return ul;
                    case TypeCode.Single:
                        float f = 0;
                        success = float.TryParse(s, out f);
                        return f;
                    case TypeCode.Double:
                        double db = 0;
                        success = double.TryParse(s, out db);
                        return db;
                    case TypeCode.Decimal:
                        decimal d = 0;
                        success = decimal.TryParse(s, out d);
                        return d;
                    case TypeCode.String:
                        success = true;
                        return s;
                    default:
                        object o = t.IsValueType ? Activator.CreateInstance(t) : null;
                        success = o != null;
                        return o;
                }
            }

            else
            {
                if (t == typeof(string))
                {
                    success = true;
                    return s;
                }

                success = false;
                return null;
            }
        }

        void RunCmd(string cmdName, object[] args)
        {
            IDCCmdAttribute cmd = cmds[cmdName];
            if (cmd.Mi.IsStatic)
            {
                cmd.Execute(null, args);
                return;
            }

            List<IDCClassInstance> instances = classInstances[cmd.ClassName];

            for (int i = 0; i < instances.Count; i++)
                if (instances[i].Instance == null)
                    instances.RemoveAt(i--);

            if (instances.Count == 0)
            {
                Log("Failed to run the command because there are no active class instances", LogType.Error);
                return;
            }

            if (cmd.CmdCallMode == CommandCallMode.AllInstances)
            {
                for (int i = 0; i < instances.Count; i++)
                    cmd.Execute(instances[i].Instance, args);
            }
            else if (cmd.CmdCallMode == CommandCallMode.SingleInstance)
            {
                cmd.Execute(instances[0].Instance, args);
            }
            else if (cmd.CmdCallMode == CommandCallMode.RandomInstance)
            {
                cmd.Execute(instances[UnityEngine.Random.Range(0, instances.Count)].Instance, args);
            }
        }

        /// <summary>
        /// Give the IDC cmd and all its arguments just as you would normally in the UI<para />
        /// 
        /// Example:<para />
        /// RunCmd(IDCCmdsEnum.SetCmdColor, " - (1, 0, 0)");<para />
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        public void RunCmd(IDCCmdsEnum cmd, string args = "")
        {
            ProcessInput(cmd.ToString() + args);
        }
        #endregion

        #region Suggestion Methods
        /// <summary>
        /// Decides if suggestions need updating and runs the appropriate method to show suggestions depending on the state.
        /// <para />Called internally and by the input field.
        /// </summary>
        public void UpdateSuggestions()
        {
            //This might be called after an update and so the state might be outdated.
            //Normally a frame delay might be fine, but this function requires an up to date
            //state to function, so we force an update
            state.UpdateState();

            //If any keyboard key has been pressed reset the suggestions index
            if (Input.anyKey && !Input.GetMouseButton(0))
                suggestionsIndex = 0;

            //If we are selecting params or showing a signature and we are still on the same param as before no need to update suggestions
            bool needUpdate = !(state.State == IDCState.ConsoleState.SelectingParamValues && state.PrevParamIndex == state.CurrParamIndex);
            if (needUpdate)
            {
                suggestionIndexOffset = 0;
                rawSuggestions.Clear();
                formattedSuggestions.Clear();
            }

            switch (state.State)
            {
                case IDCState.ConsoleState.SelectingCmds:
                case IDCState.ConsoleState.SuggestingCmds:
                    SuggestCmds();
                    break;
                case IDCState.ConsoleState.SelectingParamValues:
                case IDCState.ConsoleState.ShowingCmdSignature:
                    SuggestCmdParams(needUpdate);
                    break;
                case IDCState.ConsoleState.SelectingFromHistory:
                    break;
            }
        }

        void SuggestCmds()
        {
            if (state.InputTxt == string.Empty)
            {
                state.CanSelectSuggestions = true;
                ShowSuggestions();
                return;
            }

            //Add accessible cmds that match input
            foreach (var c in cmds.Keys)
                if (AccessLvlIsAllowed(cmds[c].AccessLevel) && c.IndexOf(state.InputTxt, StringComparison.CurrentCultureIgnoreCase) != -1)
                    rawSuggestions.Add(c);

            //Sort the suggestions and then add cmd summaries
            SortSuggestions(rawSuggestions, state.InputTxt);
            for (int i = 0; i < rawSuggestions.Count; i++)
                formattedSuggestions.Add(cmds[rawSuggestions[i]].CmdSummary);

            state.CanSelectSuggestions = true;
            ShowSuggestions();
        }

        void SuggestCmdParams(bool update)
        {
            int index = state.InputTxt.IndexOf('-') - 1;
            if (index < 0)
            {
                index = state.InputTxt.IndexOf(' ');
                if (index < 0)
                    return;
            }

            string cmdName = state.InputTxt.Substring(0, index).Trim();
            if (!cmds.ContainsKey(cmdName))
                return;

            IDCCmdAttribute cmd = cmds[cmdName];
            if (!AccessLvlIsAllowed(cmd.AccessLevel))
                return;

            if (state.CurrParamIndex == -1 || state.CurrParamIndex >= cmd.ParamTypes.Length)
            {
                formattedSuggestions.Add(cmd.GetCmdSignature(state.CurrParamIndex));
                state.CanSelectSuggestions = false;
                ShowSuggestions();
                return;
            }

            if (!update)
            {
                SortForCurrParam(cmd.ParamInfo[state.CurrParamIndex].ParameterType.IsEnum ? rawSuggestions : formattedSuggestions);
                ShowSuggestions(cmd.ParamInfo[state.CurrParamIndex].ParameterType.IsEnum);
                return;
            }

            Type currParamType = cmd.ParamInfo[state.CurrParamIndex].ParameterType;
            if (currParamType.IsArray)
                currParamType = currParamType.GetElementType();

            //Enums
            if (currParamType.IsEnum)
            {
                SuggestEnumParam(cmd, currParamType);
            }

            //Structs and classes
            else if (cmd.ParamTypes[state.CurrParamIndex] != "string" && (currParamType.IsClass || (currParamType.IsValueType && !currParamType.IsPrimitive)))
            {
                SuggestClassOrStructParam(cmd, currParamType);
            }

            //Primitives
            else
            {
                SuggestPrimitiveParam(cmd, currParamType);
            }
        }

        void SuggestEnumParam(IDCCmdAttribute cmd, Type currParamType)
        {
            suggestionIndexOffset = 1;
            rawSuggestions.Add(cmd.GetCmdSignature(state.CurrParamIndex));
            rawSuggestions.AddRange(cmd.GetUserParamSugg(cmd.ParamInfo[state.CurrParamIndex].Name));
            rawSuggestions.AddRange(cmd.GetParamAttribSugg(cmd.ParamInfo[state.CurrParamIndex].Name));

            //Add type suggs
            if (typeSuggs.ContainsKey(currParamType.Name))
            {
                rawSuggestions.AddRange(typeSuggs[currParamType.Name].suggs);
                rawSuggestions.AddRange(typeSuggs[currParamType.Name].suggsFunc.Invoke());
            }

            rawSuggestions.AddRange(Enum.GetNames(currParamType));
            SortForCurrParam(rawSuggestions);

            state.CanSelectSuggestions = true;
            ShowSuggestions(true);
        }

        void SuggestClassOrStructParam(IDCCmdAttribute cmd, Type currParamType)
        {
            //Show the original cmd signature at the top of the suggestions and default constructor if available
            formattedSuggestions.Add(cmd.GetCmdSignature(state.CurrParamIndex));

            //Show empty constructor for structs
            if (currParamType.IsValueType)
                formattedSuggestions.Add(IDCUtils.ColorString(cmd.ParamTypes[state.CurrParamIndex], settings.cmdColor) + "()");

            //Get signatures for all constructors
            ConstructorInfo[] ctorInfo = currParamType.GetConstructors();
            string ctorBase = IDCUtils.ColorString(cmd.ParamTypes[state.CurrParamIndex], settings.cmdColor) + "(";
            for (int i = 0; i < ctorInfo.Length; i++)
            {
                string ctor = ctorBase;
                ParameterInfo[] ctorParams = ctorInfo[i].GetParameters();

                if (ctorParams.Length == 0)
                {
                    ctor += ")";
                    formattedSuggestions.Add(ctor);
                    continue;
                }

                //Create constructor signature and bold current param
                for (int j = 0; j < ctorParams.Length - 1; j++)
                {
                    if (j == state.CurrCtorParamIndex)
                    {
                        ctor +=
                            IDCUtils.BoldString(
                                IDCUtils.ColorString(IDCUtils.SetStringSize(IDCCmdAttribute.GetTypeName(ctorParams[j].ParameterType), settings.suggestionsFontSize + 2.5f), settings.cmdColor)
                                + " " + ctorParams[j].Name + ", ");
                    }

                    else
                    {
                        ctor += IDCUtils.ColorString(
                        IDCCmdAttribute.GetTypeName(ctorParams[j].ParameterType), settings.cmdColor)
                        + " "
                        + ctorParams[j].Name
                        + ", ";
                    }
                }

                //Handle last param separately so we can close it
                if (state.CurrCtorParamIndex == ctorParams.Length - 1)
                    ctor += IDCUtils.BoldString(IDCUtils.ColorString(IDCCmdAttribute.GetTypeName(ctorParams[ctorParams.Length - 1].ParameterType), settings.cmdColor) + " " + ctorParams[ctorParams.Length - 1].Name) + ")";
                else
                    ctor += IDCUtils.ColorString(IDCCmdAttribute.GetTypeName(ctorParams[ctorParams.Length - 1].ParameterType), settings.cmdColor) + " " + ctorParams[ctorParams.Length - 1].Name + ")";

                formattedSuggestions.Add(ctor);
            }

            suggestionIndexOffset = formattedSuggestions.Count;
            formattedSuggestions.AddRange(cmd.GetUserParamSugg(cmd.ParamInfo[state.CurrParamIndex].Name));
            formattedSuggestions.AddRange(cmd.GetParamAttribSugg(cmd.ParamInfo[state.CurrParamIndex].Name));

            //Add type suggs
            if (typeSuggs.ContainsKey(currParamType.Name))
            {
                formattedSuggestions.AddRange(typeSuggs[currParamType.Name].suggs);
                formattedSuggestions.AddRange(typeSuggs[currParamType.Name].suggsFunc.Invoke());
            }

            //Suggest static properties (e.g. Color.red)
            var staticProperties = currParamType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < staticProperties.Length; i++)
                if (staticProperties[i].PropertyType == currParamType)
                    formattedSuggestions.Add(staticProperties[i].Name);

            //Suggest unity objects matching this params type
            if (currParamType.IsSubclassOf(typeof(UnityEngine.Object)))
                AddUnityObjSuggs(currParamType);

            SortForCurrParam(formattedSuggestions);

            state.CanSelectSuggestions = formattedSuggestions.Count > suggestionIndexOffset;
            ShowSuggestions();
        }

        void SuggestPrimitiveParam(IDCCmdAttribute cmd, Type currParamType)
        {
            formattedSuggestions.Add(cmd.GetCmdSignature(state.CurrParamIndex));
            formattedSuggestions.AddRange(cmd.GetUserParamSugg(cmd.ParamInfo[state.CurrParamIndex].Name));
            formattedSuggestions.AddRange(cmd.GetParamAttribSugg(cmd.ParamInfo[state.CurrParamIndex].Name));

            //Add type suggs
            if (typeSuggs.ContainsKey(currParamType.Name))
            {
                formattedSuggestions.AddRange(typeSuggs[currParamType.Name].suggs);
                formattedSuggestions.AddRange(typeSuggs[currParamType.Name].suggsFunc.Invoke());
            }

            if (cmd.ParamTypes[state.CurrParamIndex] == "bool")
            {
                formattedSuggestions.Add("true");
                formattedSuggestions.Add("false");
            }

            suggestionIndexOffset = 1;
            SortForCurrParam(formattedSuggestions);

            state.CanSelectSuggestions = formattedSuggestions.Count > 1;
            ShowSuggestions();
        }

        /// <summary>
        /// Sorts a suggestions list based on the value of the currently selected input field parameter
        /// </summary>
        /// <param name="suggList"></param>
        void SortForCurrParam(List<string> suggList)
        {
            int index = state.CurrParamPos < state.InputTxt.Length - 1 ? state.CurrParamPos + 1 : state.CurrParamPos;
            SortSuggestions(suggList, state.InputTxt.Substring(index, state.NextParamPos - index).Trim());
        }

        /// <summary>
        /// Uses the comparator string to sort suggestions based on how close
        /// they are to it
        /// </summary>
        void SortSuggestions(List<string> suggs, string comparator)
        {
            //Happens when param input is empty
            if (comparator == string.Empty || comparator == "-" || suggs.Count == 0)
                return;

            //Calculate similarities
            string lowerComp = comparator.ToLower(), lowerSugg = "";
            List<int> similarities = new List<int>(suggs.Count - suggestionIndexOffset);
            for (int i = suggestionIndexOffset; i < suggs.Count; i++)
            {
                lowerSugg = suggs[i].ToLower();
                int matchStartIndex = lowerSugg.IndexOf(lowerComp[0]);
                if (matchStartIndex == -1 || lowerComp.Length - 1 > lowerSugg.Length - matchStartIndex)
                {
                    similarities.Add(int.MinValue);
                    continue;
                }

                int score = -matchStartIndex * 2;
                for (int j = 1; j < comparator.Length && j + matchStartIndex < suggs[i].Length; j++)
                {
                    if (comparator[j] == suggs[i][j + matchStartIndex])
                        score += 2 * j;
                    else if (lowerComp[j] == lowerSugg[j + matchStartIndex])
                        score += j;
                    else
                    {
                        int oldIndex = matchStartIndex;
                        matchStartIndex = lowerSugg.Substring(matchStartIndex + 1).IndexOf(lowerComp[0]);
                        if (matchStartIndex == -1 || lowerComp.Length - 1 > lowerSugg.Length - (oldIndex + matchStartIndex + 1))
                        {
                            score = int.MinValue;
                            break;
                        }

                        matchStartIndex = oldIndex + matchStartIndex + 1;
                        score = -matchStartIndex * 2;
                        j = 0;
                    }
                }

                similarities.Add(score);
            }

            if (suggs.Count == 0)
                return;

            QuickSort(similarities, suggs, 0, similarities.Count - 1);
        }

        void QuickSort(List<int> similarities, List<string> suggs, int start, int end)
        {
            if (start >= end)
                return;

            //Use insertion sort for <16 elements to improve performance
            if (end + 1 - start < 16)
            {
                InsertionSort(similarities, suggs, start, end);
                return;
            }

            int pivot = Partition(similarities, suggs, start, end);
            QuickSort(similarities, suggs, start, pivot - 1);
            QuickSort(similarities, suggs, pivot + 1, end);
        }

        int Partition(List<int> similarities, List<string> suggs, int start, int end)
        {
            //Random pivot is not used to make the algorithm stable
            int currIndex = start, pivotVal = similarities[end];
            int tempSim = 0;
            string tempSugg = "";

            for (int i = start; i < end; i++)
            {
                if (similarities[i] > pivotVal)
                {
                    tempSim = similarities[currIndex];
                    similarities[currIndex] = similarities[i];
                    similarities[i] = tempSim;

                    tempSugg = suggs[currIndex + suggestionIndexOffset];
                    suggs[currIndex + suggestionIndexOffset] = suggs[i + suggestionIndexOffset];
                    suggs[i + suggestionIndexOffset] = tempSugg;

                    currIndex++;
                }
            }

            tempSim = similarities[currIndex];
            similarities[currIndex] = similarities[end];
            similarities[end] = tempSim;

            tempSugg = suggs[currIndex + suggestionIndexOffset];
            suggs[currIndex + suggestionIndexOffset] = suggs[end + suggestionIndexOffset];
            suggs[end + suggestionIndexOffset] = tempSugg;

            return currIndex;
        }

        void InsertionSort(List<int> similarities, List<string> suggs, int start, int end)
        {
            for (int i = start + 1; i < end + 1; i++)
            {
                int tempSim = similarities[i];
                string tempSug = suggs[i + suggestionIndexOffset];

                int j = i - 1;
                for (; j >= 0 && tempSim > similarities[j]; j--)
                {
                    similarities[j + 1] = similarities[j];
                    suggs[j + suggestionIndexOffset + 1] = suggs[j + suggestionIndexOffset];
                }

                similarities[j + 1] = tempSim;
                suggs[j + suggestionIndexOffset + 1] = tempSug;
            }
        }

        /// <summary>
        /// Shows suggestions and highlights the selected one
        /// </summary>
        /// <param name="suggestionsAreCmdNames">Whether suggestions are simply CMD names.
        /// If this is set then CMD descriptions are added and selected item is highlighted</param>
        void ShowSuggestions(bool useRawSuggestions = false)
        {
            //Update suggestions text
            var sugg = useRawSuggestions ? rawSuggestions : formattedSuggestions;

            //Allows user to scroll through suggestion pages
            sb.Length = 0;
            int start = Mathf.RoundToInt((suggestionsIndex + suggestionIndexOffset) / settings.maxSuggestionsToShow * settings.maxSuggestionsToShow);
            for (int i = start; i < sugg.Count && i < start + settings.maxSuggestionsToShow; i++)
            {
                if (state.CanSelectSuggestions && i == suggestionsIndex + suggestionIndexOffset)
                    sb.AppendLine(IDCUtils.SetStringSize(
                            IDCUtils.BoldString(IDCUtils.ColorString(sugg[i], settings.selectedSuggestionColor)),
                            settings.suggestionsFontSize + 3.5f));
                else
                    sb.AppendLine(sugg[i]);
            }

            sb.Append(IDCUtils.ColorString(".", Color.clear)); // This is needed because on Unity 6+ the size fitter started trimming :)
            suggestionsText.text = IDCUtils.ColorString(sb.ToString(), settings.suggestionsColor);

            //Update size and position
            contentSizeFitter.SetLayoutVertical();  //Force an update
            suggestionsBgRectTr.sizeDelta += Vector2.up * suggestionsRectTr.sizeDelta.y;

            Vector2 v = suggestionsBgRectTr.anchoredPosition;
            v.y = -suggestionsBgRectTr.sizeDelta.y / 2;
            suggestionsBgRectTr.anchoredPosition = v;
        }

        /// <summary>
        /// Sets the user suggestions of a single param of a given cmd (overrides the 'IDCParam' attribute).
        /// Automatic suggestions are still shown.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="paramName"></param>
        /// <param name="newSugg"></param>
        public void SetParamSuggestions(IDCCmdsEnum cmd, string paramName, params string[] newSugg)
        {
            if (!cmds.ContainsKey(cmd.ToString()) || newSugg == null)
                return;

            cmds[cmd.ToString()].UpdateUserSugg(paramName, newSugg);
        }

        /// <summary>
        /// Takes a function that will be called when suggestions are needed for a parameter (overrides the 'IDCParam' attribute).
        /// Automatic suggestions are still shown.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="paramName"></param>
        /// <param name="func"></param>
        public void SetParamSuggestionsFunc(IDCCmdsEnum cmd, string paramName, Func<string[]> func)
        {
            if (!cmds.ContainsKey(cmd.ToString()) || func == null)
                return;

            cmds[cmd.ToString()].SetUpdateUserSuggFunc(paramName, func);
        }

        /// <summary>
        /// Shows the given suggestions whenever a parameter of the given type is being suggested
        /// </summary>
        /// <param name="t"></param>
        /// <param name="suggs"></param>
        public void SetTypeSuggestions(Type t, string[] suggs, Func<string[]> suggsFunc = null)
        {
            if (t == null)
            {
                Log("SetTypeSuggestions failed. Type can't be null", LogType.Error);
                return;
            }

            if (suggs == null)
            {
                Log("SetTypeSuggestions failed. Suggestions array can't be null", LogType.Error);
                return;
            }

            typeSuggs[t.Name] = new TypeSuggestion(suggs, suggsFunc);
        }

        /// <summary>
        /// Adds suggestions for the passed unity type to the formatted suggs list type will be shown for parameters of the same type.
        /// <para/> Only objects currently loaded by Unity are shown.
        /// </summary>
        /// <param name="objType">Object type to load</param>
        void AddUnityObjSuggs(Type objType)
        {
            //Remove the entry if it no longer has any loaded objects
            var foundObjs = UnityEngine.Resources.FindObjectsOfTypeAll(objType);
            if (foundObjs == null || foundObjs.Length == 0)
            {
                if (unityObjectSuggs.ContainsKey(objType.Name))
                    unityObjectSuggs.Remove(objType.Name);
                return;
            }

            HideFlags acceptedHideFlags = HideFlags.None;
            if (typeHideFlags.ContainsKey(objType.Name))
                acceptedHideFlags = typeHideFlags[objType.Name];
            else
                typeHideFlags[objType.Name] = HideFlags.None;

            var objsDict = new Dictionary<string, UnityEngine.Object>(foundObjs.Length);
            for (int i = 0; i < foundObjs.Length; i++)
                if (foundObjs[i].hideFlags == acceptedHideFlags)
                {
#if UNITY_EDITOR
                    //Don't show prefabs
                    if (objType == typeof(GameObject) && UnityEditor.EditorUtility.IsPersistent(foundObjs[i]))
                        continue;
#endif
                    string n = objsDict.ContainsKey(foundObjs[i].name) ? foundObjs[i].name + "*" + foundObjs[i].GetInstanceID() : foundObjs[i].name;
                    objsDict.Add(n, foundObjs[i]);
                    formattedSuggestions.Add(n);
                }

            unityObjectSuggs[objType.Name] = objsDict;
        }

        /// <summary>
        /// The accepted HideFlags when suggesting the given Unity object type
        /// </summary>
        /// <param name="t"></param>
        /// <param name="hf"></param>
        public void SetUnityTypeHideFlags(Type t, HideFlags hf)
        {
            typeHideFlags[t.Name] = hf;
        }

        /// <summary>
        /// Updates the suggestions that will be shown for the 'SetVarValue' cmd
        /// </summary>
        void UpdateSetVarValueSugg()
        {
            string[] gs = new string[gosWithVars.Count];

            int i = 0;
            foreach (var go in gosWithVars)
                gs[i] = go.GOName + ":" + i++;

            SetParamSuggestions(IDCCmdsEnum.SetVarValue, "goName", gs);
        }

        /// <summary>
        /// Deselects the text of the input field and moves the caret to the end
        /// </summary>
        /// <returns></returns>
        IEnumerator DeselectText()
        {
            yield return null;
            inputField.MoveTextEnd(false);
        }
        #endregion

        #region Log Area
        /// <summary>
        /// Handles unity debug.log callbacks
        /// </summary>
        /// <param name="logString"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        void UnityLogs(string logString, string stackTrace, LogType type)
        {
            if (!settings.catchUnityLogs)
                return;

            if (settings.includeStackTraceWithUnityLogs)
                Log(logString + "\nStack Trace:\n" + stackTrace, type);
            else
                Log(logString, type);
        }

        /// <summary>
        /// Log a message to the IDC console with normal text color
        /// </summary>
        /// <param name="msg"></param>
        public void Log(string msg)
        {
            infiniteLog.Log(msg, settings.generalTextColor, !(isMoving || IsConsoleClosed));
        }

        /// <summary>
        /// Log a message to the IDC console with the specified color
        /// </summary>
        /// <param name="msg"></param>
        public void Log(string msg, Color c)
        {
            infiniteLog.Log(msg, c, !(isMoving || IsConsoleClosed));
        }

        /// <summary>
        /// Log a message to the IDC console with prefix according to LogType and color as per color settings
        /// </summary>
        /// <param name="msg"></param>
        public void Log(string msg, LogType logType)
        {
            Color c = settings.generalTextColor;
            switch (logType)
            {
                case LogType.Log:
                    msg = (settings.showLogPrefix ? LogPrefix : string.Empty) + msg;
                    break;
                case LogType.Warning:
                    msg = (settings.showWarningPrefix ? WarningPrefix : string.Empty) + msg;
                    c = settings.warningColor;
                    break;
                case LogType.Assert:
                    msg = (settings.showAssertPrefix ? AssertPrefix : string.Empty) + msg;
                    c = settings.assertColor;
                    break;
                case LogType.Error:
                    msg = (settings.showErrorPrefix ? ErrorPrefix : string.Empty) + msg;
                    c = settings.errorColor;
                    break;
                case LogType.Exception:
                    msg = (settings.showExceptionPrefix ? ExceptionPrefix : string.Empty) + msg;
                    c = settings.exceptionColor;
                    break;
            }

            infiniteLog.Log(msg, c, !(isMoving || IsConsoleClosed));
        }

        /// <summary>
        /// Log a message to the IDC console with prefix according to LogType and the passed color
        /// </summary>
        /// <param name="msg"></param>
        public void Log(string msg, Color c, LogType logType)
        {
            switch (logType)
            {
                case LogType.Log:
                    msg = (settings.showLogPrefix ? LogPrefix : string.Empty) + msg;
                    break;
                case LogType.Warning:
                    msg = (settings.showWarningPrefix ? WarningPrefix : string.Empty) + msg;
                    break;
                case LogType.Assert:
                    msg = (settings.showAssertPrefix ? AssertPrefix : string.Empty) + msg;
                    break;
                case LogType.Error:
                    msg = (settings.showErrorPrefix ? ErrorPrefix : string.Empty) + msg;
                    break;
                case LogType.Exception:
                    msg = (settings.showExceptionPrefix ? ExceptionPrefix : string.Empty) + msg;
                    break;
            }

            infiniteLog.Log(msg, c, !(isMoving || IsConsoleClosed));
        }
        #endregion

        void WriteToInputField(string text, bool append = false)
        {
            inputField.DeactivateInputField();
            inputField.text = append ? inputField.text + text : text;
            inputField.ActivateInputField();
        }

        IEnumerator ForceCloseConsole()
        {
            while (isMoving)
                yield return null;

            if (!IsConsoleClosed)
                StartCoroutine(MoveConsole());
        }

        IEnumerator MoveConsole()
        {
            isMoving = true;
            inputField.readOnly = true;

            bool wasClosed = IsConsoleClosed;
            float finalSizeY = 0, finalPosY = 0;
            int dir = 1;

            switch (consoleMoveOption)
            {
                case ConsoleMovementOption.ToggleSmall:
                    if (panel.sizeDelta.y / (canvas.sizeDelta.y * settings.openConsoleSize) <= 0.9f)
                    {
                        finalSizeY = canvas.sizeDelta.y * settings.openConsoleSize;
                        finalPosY = -finalSizeY * 0.5f;    //The position is the center of the panel, therefore half its size
                        dir = -1;
                    }
                    break;

                case ConsoleMovementOption.ToggleMinimal:
                    if (panel.sizeDelta.y < inputFieldRect.sizeDelta.y || panel.sizeDelta.y > inputFieldRect.sizeDelta.y)
                    {
                        finalSizeY = inputFieldRect.sizeDelta.y;
                        finalPosY = -finalSizeY * 0.5f;

                        if (panel.sizeDelta.y < inputFieldRect.sizeDelta.y)
                            dir = -1;
                    }
                    break;
            }

            if (wasClosed)
                inputField.gameObject.SetActive(true);
            else
                suggestionsBgRectTr.gameObject.SetActive(false);

            if (finalSizeY == 0)
                prevScrollPos = scrollBar.value;

            //Smoothly move into position
            float time = 0;
            float panelSizeStart = panel.sizeDelta.y, panelSizeDiff = finalSizeY - panel.sizeDelta.y;
            float panelPosStart = panel.anchoredPosition.y, panelPosDiff = finalPosY - panel.anchoredPosition.y;
            while ((dir == 1 && panel.sizeDelta.y > finalSizeY) || (dir == -1 && panel.sizeDelta.y < finalSizeY))
            {
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, EaseOutCubic(time, panelSizeStart, panelSizeDiff, settings.consoleSpd));
                panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, EaseOutCubic(time, panelPosStart, panelPosDiff, settings.consoleSpd));
                scrollBar.value = prevScrollPos;

                yield return null;
                time += Time.deltaTime;
            }

            //Snap to final positions
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, finalSizeY);
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, finalPosY);

            //Opened or size change
            if (finalSizeY != 0)
            {
                inputField.readOnly = false;
                suggestionsBgRectTr.gameObject.SetActive(true);
                StartCoroutine(DeselectText());

                //Open commands are only when opening from a complete close, not when changing size
                if (wasClosed)
                {
                    List<string> cmdList = new List<string> { "" };
                    for (int i = 0; i < onOpenCmds.Count; i++)
                    {
                        cmdList[0] = onOpenCmds[i];
                        RunCmd(onOpenCmds[i], ValidateArgs(cmdList));
                    }

                    for (int i = 0; i < onOpenOrCloseCmds.Count; i++)
                    {
                        cmdList[0] = onOpenOrCloseCmds[i];
                        RunCmd(onOpenOrCloseCmds[i], ValidateArgs(cmdList));
                    }
                }

                IsConsoleClosed = false;
            }

            //Closed
            else
            {
                inputField.gameObject.SetActive(false);
                List<string> cmdList = new List<string> { "" };
                for (int i = 0; i < onCloseCmds.Count; i++)
                {
                    cmdList[0] = onCloseCmds[i];
                    RunCmd(onCloseCmds[i], ValidateArgs(cmdList));
                }

                for (int i = 0; i < onOpenOrCloseCmds.Count; i++)
                {
                    cmdList[0] = onOpenOrCloseCmds[i];
                    RunCmd(onOpenOrCloseCmds[i], ValidateArgs(cmdList));
                }

                IsConsoleClosed = true;
            }

            //Update for any text from auto cmds
            infiniteLog.skipViewUpdates = true;
            yield return null;

            //Adjust size but don't update log text
            infiniteLog.UpdateLogAreaSize();
            yield return null;
            infiniteLog.skipViewUpdates = false;

            //Reset pos since it could have moved after resize
            scrollBar.value = prevScrollPos;

            isMoving = false;
        }

        float EaseOutCubic(float t, float startVal, float maxChange, float duration)
        {
            t = (t / duration) - 1;
            return startVal + maxChange * (t * t * t + 1);
        }

        //void OnGUI()
        //{
        //    GUI.Box(new Rect(100, 250, 500, 150),
        //        state.state.tostring()
        //        + "\ncaret: " + inputfield.caretposition
        //        + "\ncurr param: " + state.currparampos
        //        + "\nnext param: " + state.nextparampos
        //        + "\nsuggestions index: " + suggestionsindex
        //        + "\nsuggestions index offset: " + suggestionindexoffset
        //        + "\nscrollbar value: " + scrollbar.value
        //        + "\nscrollbar size: " + scrollbar.size
        //        + "\nsize delta: " + expandingtext.recttransform.sizedelta);
        //}
    }
}