using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace IDC
{
    public partial class IDCController : MonoBehaviour
    {
        string[] scenes;

        #region Settings cmds
        [IDCCmd]
        public void SetIDCSettings(IDCSettings idcSettings)
        {
            if (idcSettings == null)
            {
                Log("Trying to set IDCSettings to null. Ignored.", LogType.Error);
                return;
            }

            settings = idcSettings;
            ApplySettings();
        }

        //General options
        [IDCCmd]
        void SetMaxLogLines(int maxLines)
        {
            settings.maxLogLines = Mathf.Clamp(maxLines, IDCSettings.MinMaxLogLines, maxLines);
            ApplySettings();
        }

        [IDCCmd]
        void SetMaxSuggsToShow(int suggsToShow)
        {
            settings.maxSuggestionsToShow = suggsToShow < IDCSettings.MinMaxSuggsToShow ? IDCSettings.MinMaxSuggsToShow : suggsToShow;
        }

        [IDCCmd("", "Sets the open/close speed of the IDC")]
        void SetConsoleSpd(float spd = 0.6f)
        {
            settings.consoleSpd = spd < IDCSettings.MinConsoleSpd ? IDCSettings.MinConsoleSpd : spd;
        }

        [IDCCmd]
        void SetScrollSpd(float spd = 15f)
        {
            settings.scrollSpd = spd < IDCSettings.MinScrollSpd ? IDCSettings.MinScrollSpd : spd;
            ApplySettings();
        }

        [IDCCmd("", "How much of the screen the console will take when open. Size must be 0.1 - 1")]
        void SetConsoleSize([IDCParam(0.1f, 1, 0.1f, 1)] float size = 0.3f)
        {
            settings.openConsoleSize = Mathf.Clamp(size, 0.1f, 1);
        }

        [IDCCmd]
        void SetCatchUnityLogs(bool catchUnityLogs)
        {
            settings.catchUnityLogs = catchUnityLogs;
        }

        [IDCCmd("SetShortcutsInConsoleOnly", "Whether shortcuts only work when the console is open")]
        void SetShortcutsInConsoleOnly(bool onlyInConsole)
        {
            settings.shortcutsInConsoleOnly = onlyInConsole;
        }

        //Font options
        [IDCCmd]
        void SetLogAreaFont(TMP_FontAsset fontToUse)
        {
            if (!fontToUse)
                return;

            settings.logFont = fontToUse;
            ApplySettings();
        }

        [IDCCmd]
        void SetSuggestionAreaFont(TMP_FontAsset fontToUse)
        {
            if (!fontToUse)
                return;

            settings.suggestionsFont = fontToUse;
            ApplySettings();
        }

        [IDCCmd]
        void SetVarsWindowFont(TMP_FontAsset fontToUse)
        {
            if (!fontToUse)
                return;

            settings.varsWindowFont = fontToUse;
            ApplySettings();
        }

        [IDCCmd]
        void SetLogAreaFontSize(float size = 20)
        {
            settings.logFontSize = size;
            ApplySettings();
        }

        [IDCCmd]
        void SetSuggestionAreaFontSize(float size = 20)
        {
            settings.suggestionsFontSize = size;
            ApplySettings();
        }

        [IDCCmd]
        void SetVarsWindowFontSize(float size = 20)
        {
            settings.varsWindowFontSize = size;
            ApplySettings();
        }

        [IDCCmd]
        void SetLogAreaLineSpacing(float lineSpacing)
        {
            settings.logLineSpacing = lineSpacing;
            ApplySettings();
        }

        [IDCCmd]
        void SetSuggestionAreaLineSpacing(float lineSpacing)
        {
            settings.suggestionsLineSpacing = lineSpacing;
            ApplySettings();
        }

        [IDCCmd]
        void SetVarsWindowLineSpacing(float lineSpacing)
        {
            settings.varsWindowLineSpacing = lineSpacing;
            ApplySettings();
        }

        [IDCCmd]
        public void SetTabSize(byte tabSize = 4)
        {
            settings.tabSize = tabSize;
            ApplySettings();
        }

        //Prefix options
        [IDCCmd]
        void SetLogPrefix(bool state)
        {
            settings.showLogPrefix = state;
        }

        [IDCCmd]
        void SetWarningPrefix(bool state)
        {
            settings.showWarningPrefix = state;
        }

        [IDCCmd]
        void SetAssertPrefix(bool state)
        {
            settings.showAssertPrefix = state;
        }

        [IDCCmd]
        void SetErrorPrefix(bool state)
        {
            settings.showErrorPrefix = state;
        }

        [IDCCmd]
        void SetExceptionPrefix(bool state)
        {
            settings.showExceptionPrefix = state;
        }

        //Basic color options
        [IDCCmd]
        void SetCmdColor(Color c)
        {
            settings.cmdColor = c;
        }

        [IDCCmd("SetArgumentColor", "")]
        void SetArgColor(Color c)
        {
            settings.argumentColor = c;
        }

        [IDCCmd]
        void SetStringColor(Color c)
        {
            settings.stringColor = c;
        }

        [IDCCmd]
        void SetLogAreaColor(Color c)
        {
            settings.logAreaColor = c;
            ApplySettings();
        }

        [IDCCmd]
        void SetVarsWindowColor(Color c)
        {
            settings.VarsWindowColor = c;
            ApplySettings();
        }

        //Log color options
        [IDCCmd]
        void SetGeneralTextColor(Color c)
        {
            settings.generalTextColor = c;
        }

        [IDCCmd]
        void SetWarningColor(Color c)
        {
            settings.warningColor = c;
        }

        [IDCCmd]
        void SetAssertColor(Color c)
        {
            settings.assertColor = c;
        }

        [IDCCmd]
        void SetErrorColor(Color c)
        {
            settings.errorColor = c;
        }

        [IDCCmd]
        void SetExceptionColor(Color c)
        {
            settings.exceptionColor = c;
        }

        //Suggs color options
        [IDCCmd]
        void SetSuggestionsColor(Color c)
        {
            settings.suggestionsColor = c;
            ApplySettings();
        }

        [IDCCmd]
        void SetSelectedSuggestionColor(Color c)
        {
            settings.selectedSuggestionColor = c;
        }

        [IDCCmd]
        void SetSuggestionAreaColor(Color c)
        {
            settings.suggestionsAreaColor = c;
            ApplySettings();
        }
        #endregion

        [IDCCmd("Help", "Shows all IDC commands")]
        [IDCCmd("PrintCmds", "Shows all IDC commands")]
        /// <summary>
        /// Prints executable commands
        /// </summary>
        /// <param name="args"></param>
        public void PrintCommands()
        {
            sb.Length = 0;
            sb.Append(IDCUtils.ColorString(cmds.Count.ToString(), settings.argumentColor) + " Cmds:\n");
            foreach (var c in cmds.Values)
                sb.AppendLine("\t" + c.CmdSummary);

            Log(sb.ToString());
        }

        [IDCCmd("", "Shows the vars window and updates its values")]
        public void ShowVarsWindow()
        {
            varsWindow.gameObject.SetActive(true);
            varsWindow.UpdateWindow();
        }

        [IDCCmd("", "Clears the console window")]
        public void ClearConsole()
        {
            infiniteLog.Clear();
        }

        [IDCCmd("", "Loads a scene. Only scenes added to build settings can be loaded.")]
        void LoadScene(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadMode = UnityEngine.SceneManagement.LoadSceneMode.Single, bool async = true)
        {
            if (async)
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(int.Parse(sceneName.Split(' ')[0]), loadMode);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(int.Parse(sceneName.Split(' ')[0]), loadMode);
        }

        [IDCCmd("", "Unloads a scene. Only scenes added to build settings can be unloaded.")]
        void UnloadScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(int.Parse(sceneName.Split(' ')[0]));
        }

        [IDCCmd]
        void DestroyGameobject(GameObject go, float delay = 0)
        {
            Destroy(go, delay);
        }

        [IDCCmd]
        void SetGameobjectActive(GameObject go, bool active = true)
        {
            go.SetActive(active);
        }

        [IDCCmd]
        public void SetIDCShortcutProfile(IDCShortcutProfile idcShortcutProfile)
        {
            if (idcShortcutProfile != null)
                shortcutProfile = idcShortcutProfile;
        }
    }
}