using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace IDC
{
    [CustomEditor(typeof(IDCController))]
    public class IDCControllerInspector : Editor
    {
        static IDCController idcCont;
        static readonly System.Text.RegularExpressions.Regex alphanumericRegex = new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9_]*$");

        void OnEnable()
        {
            idcCont = (IDCController)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);
            if (GUILayout.Button("Create New IDC Settings"))
                CreateNewIDCSettings();

            if (GUILayout.Button("Create New IDC Shortcut Profile"))
                CreateNewShortcutProfile();

            if (GUILayout.Button("Update IDC Enums"))
                GenEnums();
        }

        void CreateNewIDCSettings()
        {
            string settingsFolderPath = AssetDatabase
                .GetAssetPath(MonoScript.FromMonoBehaviour(idcCont).GetInstanceID())
                .Replace("Scripts/IDCController.cs", "Settings");

            string assetPath = EditorUtility.SaveFilePanelInProject("Create IDC Settings", "DefaultIDCSettings", "asset", "", settingsFolderPath);

            //Happens on cancel as well
            if (System.IO.Path.GetFileNameWithoutExtension(assetPath).Trim() == string.Empty)
                return;

            //Load default fonts
            var newSettings = CreateInstance<IDCSettings>();
            newSettings.logFont = UnityEngine.Resources.Load<TMPro.TMP_FontAsset>("Fonts/Hack-Bold SDF");
            newSettings.suggestionsFont = UnityEngine.Resources.Load<TMPro.TMP_FontAsset>("Fonts/Hack-Regular SDF");
            newSettings.varsWindowFont = UnityEngine.Resources.Load<TMPro.TMP_FontAsset>("Fonts/Hack-Bold SDF");

            AssetDatabase.CreateAsset(newSettings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void CreateNewShortcutProfile()
        {
            string settingsFolderPath = AssetDatabase
                .GetAssetPath(MonoScript.FromMonoBehaviour(idcCont).GetInstanceID())
                .Replace("Scripts/IDCController.cs", "Shortcut Profiles");

            string assetPath = EditorUtility.SaveFilePanelInProject("Create IDC Shortcut Profile", "DefaultIDCShortcutProfile", "asset", "", settingsFolderPath);

            //Happens on cancel as well
            if (System.IO.Path.GetFileNameWithoutExtension(assetPath).Trim() == string.Empty)
                return;

            //Load default fonts
            var newProfile = CreateInstance<IDCShortcutProfile>();

            AssetDatabase.CreateAsset(newProfile, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void AutoUpdateIDCEnums()
        {
            idcCont = FindObjectOfType<IDCController>();

            if (!idcCont || Application.isPlaying)
                return;

            if (idcCont.settings)
            {
                if (idcCont.settings.updateIDCEnumsOnScriptReload)
                {
                    var prevSelection = Selection.activeGameObject;

                    // For some reason this hack avoids a no-effect error by unity when
                    // the IDC is open in the inspector and Play is pressed.
                    //
                    // The final reload before the game is started has Application.isPlaying=false, so it can't be differentiated it seems from a normal script reload.
                    if (prevSelection == null)
                        Selection.activeGameObject = idcCont.gameObject;

                    GenEnums();
                    Selection.activeGameObject = prevSelection;
                }
            }
            else
                Debug.Log("No IDCSettings object attached to IDC Canvas. Will not update IDC enums.");
        }

        static void GenEnums()
        {
            //Getting methods runs the attribute constructor, so we make sure that the ref is valid
            IDCUtils.SetIDCRef(idcCont);

            string platformLineEnd = System.Environment.NewLine;

            //Loop through all public types in the assembly and and get the names of IDCCmds
            System.Text.StringBuilder cmdsSb = new System.Text.StringBuilder($"namespace IDC{platformLineEnd}{{{platformLineEnd}");
            cmdsSb.AppendLine($"\tpublic enum IDCCmdsEnum{platformLineEnd}\t{{");

            System.Text.StringBuilder varsSb = new System.Text.StringBuilder($"namespace IDC{platformLineEnd}{{{platformLineEnd}");
            varsSb.AppendLine($"\tpublic enum IDCVarsEnum{platformLineEnd}\t{{");

            int hash;
            var cmdHashesSet = new System.Collections.Generic.HashSet<int>();
            var cmdNamesSet = new System.Collections.Generic.HashSet<string>();
            var varHashesSet = new System.Collections.Generic.HashSet<int>();
            var varNamesSet = new System.Collections.Generic.HashSet<string>();

            //NOTE: Will not catch some attributes if multiple assemblies are used
            var exportedTypes = Assembly.GetAssembly(typeof(IDCController)).GetExportedTypes();
            for (int i = 0; i < exportedTypes.Length; i++)
            {
                //Get all IDC cmds
                var typeCmds = exportedTypes[i].GetMethods(IDCController.MethodBindingFlags);
                for (int j = 0; j < typeCmds.Length; j++)
                {
                    var cmds = (IDCCmdAttribute[])typeCmds[j].GetCustomAttributes(typeof(IDCCmdAttribute), false);
                    for (int k = 0; k < cmds.Length; k++)
                    {
                        string cmdName = cmds[k].CmdName ?? typeCmds[j].Name;
                        cmdName = cmdName.Trim();
                        if (!alphanumericRegex.IsMatch(cmdName))
                        {
                            Debug.LogError($"Failed to add IDC Cmd '<b>{cmdName}</b>' based on the method <b>{exportedTypes[i].Name}.{typeCmds[j].Name}</b> because the name contains spaces or symbols. Please only use letters, numbers and underscore.");
                            continue;
                        }

                        if (cmdNamesSet.Contains(cmdName))
                        {
                            Debug.LogError($"Failed to add IDC Cmd '<b>{cmdName}</b>' based on the method <b>{exportedTypes[i].Name}.{typeCmds[j].Name}</b> because the name is already used on another cmd.");
                            continue;
                        }

                        //In case of a hash collision
                        hash = cmdName.GetHashCode();
                        while (cmdHashesSet.Contains(hash))
                            hash++;

                        cmdNamesSet.Add(cmdName);
                        cmdsSb.AppendLine("\t\t" + cmdName + " = " + hash + ",");
                    }
                }

                //Get all IDC vars
                var typeFields = exportedTypes[i].GetFields(IDCController.MethodBindingFlags);
                for (int j = 0; j < typeFields.Length; j++)
                {
                    var vars = (IDCVarAttribute[])typeFields[j].GetCustomAttributes(typeof(IDCVarAttribute), false);
                    for (int k = 0; k < vars.Length; k++)
                    {
                        string varName = vars[k].VarName ?? typeFields[j].Name;
                        varName = varName.Trim();
                        if (!alphanumericRegex.IsMatch(varName))
                        {
                            Debug.LogError($"Failed to add IDC Var '<b>{varName}</b>' based on the variable <b>{exportedTypes[i].Name}.{typeFields[j].Name}</b> because the name contains spaces or symbols. Please only use letters, numbers and underscore.");
                            continue;
                        }

                        if (varNamesSet.Contains(varName))
                        {
                            Debug.LogError($"Trying to add IDC Var '<b>{varName}</b>' based on the variable <b>{exportedTypes[i].Name}.{typeFields[j].Name}</b> failed because the name is already used on another variable.");
                            continue;
                        }

                        //In case of a hash collision
                        hash = varName.GetHashCode();
                        while (varHashesSet.Contains(hash))
                            hash++;

                        varNamesSet.Add(varName);
                        varsSb.AppendLine("\t\t" + exportedTypes[i].Name + "_" + varName + " = " + hash + ",");
                    }
                }

                var typeProperties = exportedTypes[i].GetProperties(IDCController.MethodBindingFlags);
                for (int j = 0; j < typeProperties.Length; j++)
                {
                    var vars = (IDCVarAttribute[])typeProperties[j].GetCustomAttributes(typeof(IDCVarAttribute), false);
                    for (int k = 0; k < vars.Length; k++)
                    {
                        string varName = vars[k].VarName ?? typeProperties[j].Name;
                        varName = varName.Trim();
                        if (varNamesSet.Contains(varName))
                        {
                            Debug.LogError("Trying to add IDC Var <b>" + varName + "</b> based on the variable <b>" + exportedTypes[i].Name + "." + typeProperties[j].Name + "</b> failed. IDC Var with name <b>" + varName + "</b> already exists.");
                            continue;
                        }

                        //In case of a hash collision
                        hash = varName.GetHashCode();
                        while (varHashesSet.Contains(hash))
                            hash++;

                        varNamesSet.Add(varName);
                        varsSb.AppendLine("\t\t" + exportedTypes[i].Name + "_" + varName + " = " + hash + ",");
                    }
                }
            }

            cmdsSb.AppendLine($"\t}}{platformLineEnd}}}");
            varsSb.AppendLine($"\t}}{platformLineEnd}}}");

            //Write to disk and save
            string idcControllerPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(idcCont).GetInstanceID()).Replace("Assets/", "");

            string relativePath = idcControllerPath.Replace("IDCController.cs", "IDCCmdsEnum.cs");
            System.IO.File.WriteAllText(Application.dataPath + "/" + relativePath, cmdsSb.ToString());

            relativePath = idcControllerPath.Replace("IDCController.cs", "IDCVarsEnum.cs");
            System.IO.File.WriteAllText(Application.dataPath + "/" + relativePath, varsSb.ToString());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}