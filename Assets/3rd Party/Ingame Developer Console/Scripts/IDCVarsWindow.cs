using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IDC
{
    [RequireComponent(typeof(InfiniteLog))]
    public class IDCVarsWindow : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        enum UpdateMode
        {
            Manual,
            Auto
        }

        readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();

        List<IDCGO> gosInOrder;
        Dictionary<IDCGO, List<IDCClassInstance>> classesInOrder;

        HashSet<IDCGO> gosWithVars;
        Dictionary<string, List<IDCClassInstance>> classInstances;
        Dictionary<string, Dictionary<string, IDCVarAttribute>> classVars;

        Vector2 MinSize;
        Scrollbar scrollbar;
        ScrollRect scrollRect;
        InfiniteLog infiniteLog;
        RectTransform rt, canvasRt;

        TMP_Dropdown updateModeDropdown, updateIntervalDropdown;
        //TMP_InputField updateIntervalInputField;

        UpdateMode updateMode;
        float updateInterval = 2f;
        bool isAutoUpdatingWindow;

        float startScrollbarValue;
        bool resizing;

        public void Init(HashSet<IDCGO> gosWithVars, Dictionary<string, List<IDCClassInstance>> classInstances, Dictionary<string, Dictionary<string, IDCVarAttribute>> classVars, int maxLines)
        {
            //Get needed components
            rt = GetComponent<RectTransform>();
            canvasRt = transform.parent.GetComponent<RectTransform>();

            infiniteLog = GetComponent<InfiniteLog>();
            infiniteLog.Init(maxLines);

            scrollbar = transform.GetChild(2).GetComponent<Scrollbar>();
            scrollbar.value = 1;

            scrollRect = GetComponent<ScrollRect>();

            //Get option panel components
            updateModeDropdown = transform.GetChild(4).GetChild(0).GetComponent<TMP_Dropdown>();
            updateIntervalDropdown = transform.GetChild(4).GetChild(1).GetComponent<TMP_Dropdown>();

            IDCUtils.IDC.AddClass(this);
            ResetWindow();

            this.gosWithVars = gosWithVars;
            this.classVars = classVars;
            this.classInstances = classInstances;

            gosInOrder = new List<IDCGO>(gosWithVars.Count);
            classesInOrder = new Dictionary<IDCGO, List<IDCClassInstance>>(gosWithVars.Count);
        }

        public void ApplySettings(IDCSettings settings)
        {
            infiniteLog.SetFont(settings.varsWindowFont);
            infiniteLog.SetMaxLogLines(settings.maxLogLines);
            infiniteLog.SetTabSize(settings.tabSize);
            infiniteLog.SetFontSize(settings.varsWindowFontSize);
            infiniteLog.SetLineSpacing(settings.varsWindowLineSpacing);
        }

        void OnEnable() => UpdateIntervalChanged();

        public void UpdateWindow()
        {
            //If the window was empty then ensure the scrollbar is set at the top before adding content.
            //If this isn't done then the scrollbar will be pushed to the bottom after the first update
            if (infiniteLog.LogBuffer.Count == 0)
                scrollbar.value = 1;

            IDCUtils.IDC.Clean();
            sb.Length = 0;
            sb.AppendLine();

            if (gosInOrder.Count < gosWithVars.Count)
                gosInOrder.AddRange(new IDCGO[gosWithVars.Count - gosInOrder.Count]);

            int i = 0, j = 0;
            foreach (var go in gosWithVars)
            {
                gosInOrder[i] = go;
                sb.Append("[")
                  .Append(IDCUtils.ColorString(go.GOName, IDCUtils.IDC.settings.cmdColor))
                  .AppendLine(":" + i++ + "]");

                var classes = new List<IDCClassInstance>(new IDCClassInstance[go.Classes.Count]);
                foreach (var c in go.Classes.Values)
                {
                    classes[j] = c;

                    sb.Append("  (")
                      .Append(IDCUtils.ColorString(c.ClassName, IDCUtils.IDC.settings.selectedSuggestionColor))
                      .AppendLine(":" + j++ + ")");

                    var vars = classVars[c.ClassName];
                    foreach (var v in vars.Values)
                    {
                        object val = v.GetVarValue(c.Instance);
                        if (val != null)
                        {
                            sb.Append("    <i>")
                              .Append(v.VarName)
                              .Append("</i>: ")
                              .AppendLine(IDCUtils.ColorString(val.ToString(), IDCUtils.IDC.settings.argumentColor));
                        }

                        else
                        {
                            sb.Append("    <i>")
                              .Append(v.VarName)
                              .Append("</i>: ")
                              .AppendLine(IDCUtils.ColorString("null", IDCUtils.IDC.settings.argumentColor));
                        }
                    }
                }

                j = 0;
                sb.AppendLine();
                classesInOrder[go] = classes;
            }

            float oldScrollbarPos = scrollbar.value;
            infiniteLog.Clear();
            infiniteLog.Log(sb.ToString(), Color.white, false);
            StartCoroutine(SetScrollbarPos(oldScrollbarPos));
        }

        [IDCCmd("SetVarValue", "Sets the value of an IDCVar. If no object and class are selected, then all instances of the variable are updated")]
        void SetVarValue(IDCVarsEnum varToChange, string newVal, string goName = "", int classIndex = -1)
        {
            string[] split = varToChange.ToString().Split(new char[] { '_' }, 2);
            string className = split[0];
            string varName = split[1];

            if (!classInstances.ContainsKey(className) || classInstances[className].Count == 0)
            {
                IDCUtils.IDC.Log("No live class instances found", LogType.Error);
                return;
            }

            IDCVarAttribute var = classVars[className][varName];
            if (var == null)
            {
                IDCUtils.IDC.Log("Variable not found", LogType.Error);
                return;
            }

            //Parse input to object
            bool isClass = var.VarType.IsClass && var.VarType != typeof(string);
            bool isStruct = !var.VarType.IsPrimitive && var.VarType.IsValueType && !var.VarType.IsEnum;

            bool success;
            object parsedObj;
            if (var.VarType.IsEnum)
                parsedObj = IDCUtils.IDC.ParseToEnum(newVal, var.VarType, out success);
            else if (isClass || isStruct)
                parsedObj = IDCUtils.IDC.ParseToObject(newVal, var.VarType, isStruct, out success);
            else
                parsedObj = IDCUtils.IDC.ParseToPrimitive(newVal, var.VarType, out success);

            if (!success)
            {
                IDCUtils.IDC.Log("New variable value is invalid. Maybe you put a string but needed a number?", LogType.Error);
                return;
            }

            //Update variable on all GOs
            if (goName == "")
            {
                List<IDCClassInstance> varClasses = classInstances[className];
                for (int i = 0; i < varClasses.Count; i++)
                    var.SetVarValue(varClasses[i].Instance, parsedObj);
            }

            //Update variable on specific GO
            else
            {
                int goIndex = int.Parse(goName.Split(':')[1]);
                if (goIndex < 0 || goIndex >= gosInOrder.Count)
                {
                    IDCUtils.IDC.Log("GameObject index out of range", LogType.Error);
                    return;
                }

                IDCGO idcGo = gosInOrder[goIndex];
                if (!classesInOrder.ContainsKey(idcGo) || classIndex < 0 || classIndex >= classesInOrder[idcGo].Count)
                {
                    IDCUtils.IDC.Log("Class index out of range", LogType.Error);
                    return;
                }

                var.SetVarValue(classesInOrder[idcGo][classIndex].Instance, parsedObj);
            }
        }

        public void UpdateModeChanged()
        {
            updateMode = (UpdateMode)updateModeDropdown.value;
            StartCoroutine(DoAutoUpdate());
        }

        public void UpdateIntervalChanged()
        {
            string selectedText = updateIntervalDropdown.options[updateIntervalDropdown.value].text;
            if (!float.TryParse(selectedText, out updateInterval))
            {
                Debug.LogError("Invalid Vars Window interval value: " + selectedText);
                return;
            }

            StartCoroutine(DoAutoUpdate());
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //Prevent jitter while dragging/resizing
            infiniteLog.skipViewUpdates = true;
            startScrollbarValue = scrollbar.value;
            scrollbar.interactable = false;

            Vector3 localMousePos = transform.InverseTransformPoint(eventData.position);
            resizing = localMousePos.x > rt.rect.width * 0.5f * 0.85f && localMousePos.y < -rt.rect.height * 0.5f * 0.85f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (resizing)
            {
                Vector3 oldPos = rt.position;

                Vector3 localMousePos = transform.InverseTransformPoint(eventData.position);
                Vector2 sizeOffset = new Vector2(localMousePos.x - rt.rect.width * 0.5f, localMousePos.y + rt.rect.height * 0.5f);

                rt.offsetMin += Vector2.up * sizeOffset.y;
                rt.offsetMax += Vector2.right * sizeOffset.x;

                //Ensure min size, and in that case also undo movement compensation
                if (rt.sizeDelta.x < MinSize.x)
                {
                    rt.sizeDelta = new Vector2(MinSize.x, rt.sizeDelta.y);
                    rt.position = new Vector3(oldPos.x, rt.position.y);
                }

                if (rt.sizeDelta.y < MinSize.y)
                {
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, MinSize.y);
                    rt.position = new Vector3(rt.position.x, oldPos.y);
                }
            }

            else
            {
                transform.position += (Vector3)eventData.delta;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            scrollbar.interactable = true;
            infiniteLog.skipViewUpdates = false;
            scrollbar.value = startScrollbarValue;
        }

        public void CloseBtnClicked()
        {
            gameObject.SetActive(false);
        }

        [IDCCmd("ResetVarsWindow", "Resets the size and position of the vars window")]
        /// <summary>
        /// Reset the windows size and position
        /// </summary>
        public void ResetWindow()
        {
            UpdateMinSize();
            rt.sizeDelta = MinSize;

            Vector2 pos = IDCUtils.IDC.settings.DefaultVarsWindowPos - Vector2.one * 0.5f;
            rt.localPosition = new Vector3(pos.x * canvasRt.sizeDelta.x, pos.y * canvasRt.sizeDelta.y);
        }

        public void UpdateMinSize()
        {
            MinSize.x = -canvasRt.sizeDelta.x + canvasRt.sizeDelta.x * IDCUtils.IDC.settings.MinVarsWindowSize.x;
            MinSize.y = -canvasRt.sizeDelta.y + canvasRt.sizeDelta.y * IDCUtils.IDC.settings.MinVarsWindowSize.y;
        }

        void OnDisable() => isAutoUpdatingWindow = false;

        IEnumerator DoAutoUpdate()
        {
            //The check for the bool in the while loop is in case we got disabled
            isAutoUpdatingWindow = true;
            while (isAutoUpdatingWindow && updateMode == UpdateMode.Auto)
            {
                UpdateWindow();
                yield return new WaitForSeconds(updateInterval);
            }
            isAutoUpdatingWindow = false;
        }

        IEnumerator SetScrollbarPos(float pos)
        {
            infiniteLog.UpdateView(Vector2.zero);
            //Without a value change the scrollbar will become of max size
            scrollbar.value -= 0.0001f;
            yield return null;

            scrollRect.verticalNormalizedPosition = pos;
        }
    }
}