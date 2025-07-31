using UnityEditor;
using UnityEngine;

namespace IDC
{
    [CustomEditor(typeof(IDCShortcutProfile))]
    public class IDCShortcutProfileInspector : Editor
    {
        IDCShortcutProfile profile;
        int isCapturingForIndex = -1;

        private GUIStyle h1Style, h2Style;
        private GUIStyle shortcutButtonStyle;
        private GUIStyle commandStyle;
        private GUIStyle deleteButtonStyle;

        void OnEnable()
        {
            profile = (IDCShortcutProfile)target;
            if (profile.shortcuts == null)
                profile.shortcuts = new System.Collections.Generic.List<IDCShortcut>();
        }

        void InitializeStyles()
        {
            h1Style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                padding = new RectOffset(5, 5, 8, 8),
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 30,
            };

            h2Style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                padding = new RectOffset(5, 5, 8, 8),
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 25,
            };

            shortcutButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                //fixedHeight = 24,
                padding = new RectOffset(10, 10, 4, 4)
            };

            commandStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 6, 6),
                margin = new RectOffset(0, 0, 2, 2),
                fontSize = 12,
                fixedHeight = 28
            };

            deleteButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = new Color(1f, 0.2f, 0.2f) },
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                //padding = new RectOffset(4, 4, 8, 4)
            };
        }

        public override void OnInspectorGUI()
        {
            // This way is to avoid console errors when initing in OnEnable
            if (h1Style == null)
                InitializeStyles();

            EditorGUILayout.Space(10);

            // Header Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("IDC Shortcut Profile", h1Style);
            EditorGUILayout.Space(10);
            if (GUILayout.Button("+ Add New Shortcut", GUILayout.Height(25)))
            {
                profile.shortcuts.Add(new IDCShortcut());
                EditorUtility.SetDirty(profile);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Shortcuts Section
            for (int i = 0; i < profile.shortcuts.Count; i++)
            {
                DrawShortcut(i);
            }
        }

        void DrawShortcut(int index)
        {
            var shortcut = profile.shortcuts[index];
            bool isBeingCaptured = isCapturingForIndex == index;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
            string newName = EditorGUILayout.TextField(shortcut.name, GUILayout.ExpandWidth(true));
            if (newName != shortcut.name)
            {
                shortcut.name = newName;
                profile.shortcuts[index] = shortcut;
                EditorUtility.SetDirty(profile);
            }

            // Key combination button
            GUI.enabled = !isBeingCaptured;
            if (GUILayout.Button(GetKeyCombinationString(shortcut, index), shortcutButtonStyle, GUILayout.Width(180)))
            {
                isBeingCaptured= true;
                isCapturingForIndex = index;
            }
            GUI.enabled = true;

            GUILayout.Space(5);

            if (GUILayout.Button("X", deleteButtonStyle))
            {
                isBeingCaptured = false;
                isCapturingForIndex = -1;
                profile.shortcuts.RemoveAt(index);
                EditorUtility.SetDirty(profile);
                return;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (isBeingCaptured)
            {
                HandleKeyCapture(index);
            }

            // Commands Section
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Commands", h2Style);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Command", GUILayout.Width(120), GUILayout.Height(24)))
            {
                CommandSelectorWindow.Show((selectedCmd) =>
                {
                    if (shortcut.cmds == null)
                        shortcut.cmds = new System.Collections.Generic.List<IDCCmdsEnum>();

                    shortcut.cmds.Add(selectedCmd);
                    profile.shortcuts[index] = shortcut;
                    EditorUtility.SetDirty(profile);
                });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (shortcut.cmds != null && shortcut.cmds.Count > 0)
            {
                for (int i = 0; i < shortcut.cmds.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(commandStyle);
                    EditorGUILayout.LabelField(shortcut.cmds[i].ToString(), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X", deleteButtonStyle))
                    {
                        shortcut.cmds.RemoveAt(i);
                        profile.shortcuts[index] = shortcut;
                        EditorUtility.SetDirty(profile);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No commands assigned", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        string GetKeyCombinationString(IDCShortcut shortcut, int index)
        {
            if (isCapturingForIndex == index)
                return "Press combination...";

            if (shortcut.key == KeyCode.None)
                return "Click to set key";

            string combo = "";
            if (shortcut.modifiers != null)
            {
                foreach (var modifier in shortcut.modifiers)
                {
                    combo += modifier.ToString().Replace("Left", "").Replace("Right", "") + "+";
                }
            }

            return combo + shortcut.key.ToString();
        }

        void HandleKeyCapture(int index)
        {
            var e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    isCapturingForIndex = -1;
                    e.Use();
                    return;
                }

                if (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl ||
                    e.keyCode == KeyCode.LeftAlt || e.keyCode == KeyCode.RightAlt ||
                    e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift ||
                    e.keyCode == KeyCode.CapsLock)
                    return;

                var shortcut = profile.shortcuts[index];
                shortcut.key = e.keyCode;
                shortcut.modifiers = new System.Collections.Generic.List<IDCShortcut.ModKey>();

                if (e.control)
                    shortcut.modifiers.Add(IDCShortcut.ModKey.LeftCtrl);

                if (e.alt)
                    shortcut.modifiers.Add(IDCShortcut.ModKey.LeftAlt);

                if (e.shift)
                    shortcut.modifiers.Add(IDCShortcut.ModKey.LeftShift);

                if (e.capsLock)
                    shortcut.modifiers.Add(IDCShortcut.ModKey.CapsLock);

                profile.shortcuts[index] = shortcut;
                EditorUtility.SetDirty(profile);

                isCapturingForIndex = -1;
                e.Use();
            }
        }
    }
}
