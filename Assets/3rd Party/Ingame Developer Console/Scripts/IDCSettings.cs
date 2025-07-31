using TMPro;
using UnityEngine;

namespace IDC
{
    [System.Serializable]
    public class IDCSettings : ScriptableObject
    {
        #region Consts
        public const int MinMaxLogLines = 100, MinMaxSuggsToShow = 5;
        public const float MinConsoleSpd = 0.01f, MinScrollSpd = 1;
        #endregion

        
        [Header("General Options")]
        [Range(MinMaxLogLines, 1000000)] public int maxLogLines = 1000;
        [Tooltip("Max suggestions to show at one time"), Range(MinMaxSuggsToShow, 100)] public int maxSuggestionsToShow = 20;
        [Tooltip("Seconds taken for console to open or close"), Range(MinConsoleSpd, 5f)] public float consoleSpd = 0.6f;
        [Range(MinScrollSpd, 50)] public float scrollSpd = 15;
        [Tooltip("The amount of the screen taken by the console when opened. 0.5 covers half the screen."), Range(0.1f, 1)] public float openConsoleSize = 0.30f;


        [Space, Tooltip("Min size of the vars window as a percentage of the screen size")] public Vector2 MinVarsWindowSize = new Vector2(0.25f, 0.7f);
        [Tooltip("Default position of the vars window when reset as a percentage of the screen size. 0, 0 is bottom-left")] public Vector2 DefaultVarsWindowPos = new Vector2(0.75f, 0.4f);

        [Space]
        public bool catchUnityLogs = true;
        public bool includeStackTraceWithUnityLogs = true;

        [Tooltip("If this is ticked then shortcuts only work when the console window is open")] public bool shortcutsInConsoleOnly = false;
        [Tooltip("Regenerates IDC Cmd and Var enums whenever a script is changd and unity reloads")] public bool updateIDCEnumsOnScriptReload = true;
        [Tooltip("Sets where the console can be used. For example, EditorAndDevBuild will disable the console in production builds")] public AccessLevel consoleAccessLvl = AccessLevel.EditorAndDevBuild;

        [Header("Font Options")]
        public TMP_FontAsset logFont;
        public TMP_FontAsset suggestionsFont;
        public TMP_FontAsset varsWindowFont;

        [Space, Range(8, 120)] public float logFontSize = 20;
        [Range(8, 120)] public float suggestionsFontSize = 20, varsWindowFontSize = 20;

        [Space, Range(0.1f, 4)] public float logLineSpacing = 1.1f;
        [Range(0.1f, 4)] public float suggestionsLineSpacing = 1.1f, varsWindowLineSpacing = 1.1f;
        [Range(1, 20)] public byte tabSize = 4;

        [Header("Prefix Options")]
        [Tooltip("Adds \"[LOG]:\" Before Normal Logs")] public bool showLogPrefix = true;
        [Tooltip("Adds \"[WARNING]:\" Before Warning Logs")] public bool showWarningPrefix = true;
        [Tooltip("Adds \"[ASSERT]:\" Before Assertion Logs")] public bool showAssertPrefix = true;
        [Tooltip("Adds \"[ERROR]:\" Before Error Logs")] public bool showErrorPrefix = true;
        [Tooltip("Adds \"[EXCEPTION]:\" Before Exception Logs")] public bool showExceptionPrefix = true;

        //[Header("Key Bindings")]
        [HideInInspector] public KeyCode toggleKey = KeyCode.BackQuote;
        [HideInInspector] public KeyCode sizeModifierKey = KeyCode.LeftShift;

        [Header("Basic Color Options")]
        public Color cmdColor = Color.cyan;
        public Color argumentColor = new Color(141 / 255f, 122 / 255f, 255 / 255f);
        public Color stringColor = new Color(214 / 255f, 157 / 255f, 133 / 255f);
        public Color logAreaColor = new Color(30 / 255f, 30 / 255f, 30 / 255f, 240 / 255f);
        public Color VarsWindowColor = new Color(30 / 255f, 30 / 255f, 30 / 255f, 240 / 255f);

        [Header("Log Color Options")]
        public Color generalTextColor = Color.white;
        public Color warningColor = Color.yellow;
        public Color assertColor = new Color(230 / 255f, 50 / 255f, 50 / 255f);
        public Color errorColor = new Color(230 / 255f, 50 / 255f, 50 / 255f);
        public Color exceptionColor = new Color(230 / 255f, 50 / 255f, 50 / 255f);

        [Header("Suggestions Color Options")]
        public Color suggestionsColor = new Color(1, 1, 1);
        public Color selectedSuggestionColor = new Color(1, 215 / 255f, 0);
        public Color suggestionsAreaColor = new Color(30 / 255f, 30 / 255f, 30 / 255f, 240 / 255f);
    }
}