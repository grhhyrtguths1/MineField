using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IDC
{
    public class CommandSelectorWindow : EditorWindow
    {
        static System.Action<IDCCmdsEnum> onCommandSelected;
        Vector2 scrollPosition;
        string searchText = "";
        IDCCmdsEnum[] allCommands;
        bool moveWithArrowKeys = false;
        bool shouldFocusSearchField = true;

        // For keyboard navigation
        int selectedIndex = 0;
        float selectedItemYMin = 0;
        System.Collections.Generic.List<IDCCmdsEnum> filteredCommands = new System.Collections.Generic.List<IDCCmdsEnum>();

        public static void Show(System.Action<IDCCmdsEnum> onSelect)
        {
            onCommandSelected = onSelect;
            var window = GetWindow<CommandSelectorWindow>(true, "Select Command");
            window.minSize = new Vector2(300, 400);
            window.maxSize = new Vector2(300, 400);
            window.Initialize();
        }

        void Initialize()
        {
            allCommands = System.Enum.GetValues(typeof(IDCCmdsEnum)).Cast<IDCCmdsEnum>().ToArray();
            UpdateFilteredList();
        }

        void UpdateFilteredList()
        {
            string search = searchText.ToLower();
            filteredCommands.Clear();

            foreach (var cmd in allCommands)
            {
                string cmdName = cmd.ToString();
                if (string.IsNullOrEmpty(search) || cmdName.ToLower().Contains(search))
                {
                    filteredCommands.Add(cmd);
                }
            }

            // Ensure selected index stays valid
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, filteredCommands.Count - 1));
        }

        void OnGUI()
        {
            // Handle keyboard input
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown)
            {
                switch (currentEvent.keyCode)
                {
                    case KeyCode.UpArrow:
                        moveWithArrowKeys = true;
                        selectedIndex = Mathf.Max(0, selectedIndex - 1);
                        currentEvent.Use();
                        Repaint();
                        break;

                    case KeyCode.DownArrow:
                        moveWithArrowKeys = true;
                        selectedIndex = Mathf.Min(filteredCommands.Count - 1, selectedIndex + 1);
                        currentEvent.Use();
                        Repaint();
                        break;

                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (filteredCommands.Count > 0)
                        {
                            onCommandSelected?.Invoke(filteredCommands[selectedIndex]);
                            Close();
                        }
                        currentEvent.Use();
                        break;

                    case KeyCode.Escape:
                        Close();
                        currentEvent.Use();
                        break;
                }
            }
            else if (currentEvent.type == EventType.ScrollWheel)
            {
                moveWithArrowKeys = false;
            }

            if (filteredCommands.Count > 0 && moveWithArrowKeys && selectedItemYMin != 0)
            {
                // Scroll down when selection is too low
                if (selectedItemYMin - scrollPosition.y > position.height * 0.75f)
                {
                    scrollPosition.y = selectedItemYMin - position.height * 0.75f;
                }
                // Scroll up when selection is too high
                else if (selectedItemYMin - scrollPosition.y < position.height * 0.25f)
                {
                    scrollPosition.y = Mathf.Max(0, selectedItemYMin - position.height * 0.25f);
                }
            }

            // Search bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (shouldFocusSearchField)
                GUI.SetNextControlName("CommandSearch");

            string newSearchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                UpdateFilteredList();
            }

            EditorGUILayout.EndHorizontal();

            // Commands list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < filteredCommands.Count; i++)
            {
                var cmd = filteredCommands[i];

                // Custom drawing for selected item
                bool isSelected = i == selectedIndex;
                var rect = EditorGUILayout.GetControlRect();
                bool isHover = rect.Contains(Event.current.mousePosition);

                if (isSelected)
                {
                    selectedItemYMin = rect.yMin;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    var style = new GUIStyle(EditorStyles.label);
                    if (isSelected)
                    {
                        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.32f, 0.39f, 0.49f) : new Color(0.24f, 0.48f, 0.90f));
                        style.normal.textColor = Color.white;
                    }

                    style.Draw(rect, cmd.ToString(), false, false, false, false);
                }

                // Handle mouse click
                if (isHover)
                {
                    if (Event.current.type == EventType.MouseUp)
                    {
                        onCommandSelected?.Invoke(cmd);
                        Close();
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (shouldFocusSearchField)
            {
                EditorGUI.FocusTextInControl("CommandSearch");
                shouldFocusSearchField = false;
            }
        }

        void OnLostFocus()
        {
            Close();
        }
    }
}
