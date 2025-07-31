using System;
using System.Collections.Generic;
using System.Reflection;

namespace IDC
{
    /// <summary>
    /// Decides which instances are used to run a command when the command is on a non-static method.
    /// </summary>
    [Flags]
    public enum CommandCallMode
    {
        /// <summary>
        /// Runs the command on all instances of the class
        /// </summary>
        AllInstances = 1 << 0,

        /// <summary>
        /// Runs the command on a single instance of the class.
        /// Multiple calls will use the same instance until it is deleted, in which case another instance will be selected.
        /// </summary>
        SingleInstance = 1 << 1,

        /// <summary>
        /// Runs the command on a random instance of the class.
        /// Multiple calls will use randomly selected instances.
        /// </summary>
        RandomInstance = 1 << 2,
    }

    /// <summary>
    /// Decides what is done with the return value of a command
    /// </summary>
    [Flags]
    public enum CommandReturnHandling
    {
        /// <summary>
        /// Log the returned value to the IDC console
        /// </summary>
        Log = 1 << 0,
        /// <summary>
        /// Don't log the returned value
        /// </summary>
        NoLog = 1 << 1,
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class IDCCmdAttribute : Attribute
    {
        public string CmdName { get; private set; }
        public string CmdSummary { get; private set; }
        public string ClassName { get; private set; }
        public int MinArgs { get; private set; }

        public MethodInfo Mi { get; private set; }
        public ParameterInfo[] ParamInfo { get; private set; }
        public string[] ParamTypes { get; private set; }
        public Dictionary<string, Func<string[]>> UpdateUserSuggFunc { get; private set; }

        public CommandType CmdType { get; private set; }
        public AccessLevel AccessLevel { get; private set; }
        public CommandCallMode CmdCallMode { get; private set; }
        public CommandReturnHandling CmdReturnHandling { get; private set; }

        Dictionary<string, string[]> userSugg;
        Dictionary<string, List<string>> paramAttribSugg;

        /// <summary>
        /// </summary>
        /// <param name="cmdName">Name to be used for running this cmd</param>
        /// <param name="cmdDesc">Description to show in the suggestions list of the IDC</param>
        /// <param name="accessLvl">Where the cmd is allowed to be run</param>
        /// <param name="callMode">Decides which instances of the class the command is run on. Does not affect static methods</param>
        /// <param name="commandType">When a cmd should be called</param>
        public IDCCmdAttribute(string cmdName = "", string cmdDesc = "", AccessLevel accessLvl = AccessLevel.Everywhere, CommandCallMode callMode = CommandCallMode.AllInstances, CommandType commandType = CommandType.Manual, CommandReturnHandling cmdReturnHandling = CommandReturnHandling.Log)
        {
            bool isCmdNameSet = !string.IsNullOrWhiteSpace(cmdName);

            //Null is required if cmd is not given a name
            CmdName = isCmdNameSet ? cmdName: null;
            AccessLevel = accessLvl;
            CmdType = commandType;
            CmdCallMode = callMode;
            CmdReturnHandling = cmdReturnHandling;

            string descString = string.Empty;
            if (!string.IsNullOrEmpty(cmdDesc))
                descString = isCmdNameSet ? ". " + cmdDesc : cmdDesc;

            cmdName = cmdName.Trim();
            CmdSummary = IDCUtils.ColorString(cmdName, IDCUtils.IDC.settings.cmdColor) + descString;
        }

        public void SetCmd(MethodInfo mi, string className)
        {
            Mi = mi;
            ClassName = className;

            //If no name was given use the methods name
            if (string.IsNullOrWhiteSpace(CmdName))
            {
                CmdName = mi.Name.Trim();
                CmdSummary = IDCUtils.ColorString(CmdName, IDCUtils.IDC.settings.cmdColor) + (string.IsNullOrEmpty(CmdSummary) ? string.Empty : ". " + CmdSummary);
            }

            ParamInfo = mi.GetParameters();
            ParamTypes = new string[ParamInfo.Length];
            paramAttribSugg = new Dictionary<string, List<string>>();
            userSugg = new Dictionary<string, string[]>();
            UpdateUserSuggFunc = new Dictionary<string, Func<string[]>>();

            MinArgs = 0;
            for (int i = 0; i < ParamInfo.Length; i++)
            {
                ParamTypes[i] = GetTypeName(ParamInfo[i].ParameterType);

                if (!ParamInfo[i].IsOptional)
                    MinArgs++;

                //Add IDCParam suggs
                var pas = (IDCParamAttribute[])ParamInfo[i].GetCustomAttributes(typeof(IDCParamAttribute), false);
                if (pas.Length == 0)
                    continue;

                List<string> paramSuggs = new List<string>(pas[0].Suggs);
                for (int j = 1; j < pas.Length; j++)
                    paramSuggs.AddRange(pas[j].Suggs);
                paramAttribSugg[ParamInfo[i].Name] = paramSuggs;
            }
        }

        public void Execute(object instance, object[] args)
        {
            object ret = Mi.Invoke(instance, args);
            if (CmdReturnHandling == CommandReturnHandling.NoLog || Mi.ReturnType == typeof(void))
                return;

            if (ret == null)
            {
                IDCUtils.IDC.Log($"null returned for type: '{Mi.ReturnType.Name}'");
                return;
            }

            IDCUtils.IDC.Log(ret.ToString());
        }

        public string GetCmdSignature(int currentParam)
        {
            var cmdColor = IDCUtils.IDC.settings.cmdColor;
            string cmdSignature = IDCUtils.ColorString(CmdName, cmdColor);
            for (int i = 0; i < ParamInfo.Length; i++)
            {
                //Print param type and name and bold if currently being worked on
                string defaultVal = "";
                if (ParamInfo[i].IsOptional)
                {
                    if (ParamInfo[i].DefaultValue == null)
                        defaultVal = " = null";
                    else if (ParamInfo[i].DefaultValue.ToString() == string.Empty)
                        defaultVal = " = " + (ParamTypes[i] == "string" ? "\"\"" : "new " + ParamInfo[i].ParameterType.Name + "()");
                    else
                        defaultVal = " = " + ParamInfo[i].DefaultValue;
                }

                string sig = IDCUtils.ColorString(ParamTypes[i], cmdColor) + " " + ParamInfo[i].Name + defaultVal;
                if (i == currentParam)
                    cmdSignature += " -" + IDCUtils.SetStringSize(IDCUtils.BoldString(sig), IDCUtils.IDC.settings.suggestionsFontSize + 3.5f);
                else
                    cmdSignature += " -" + sig;
            }

            return cmdSignature;
        }

        /// <summary>
        /// Sets the function to be called to generate suggestions for this cmd parameter everytime the suggestions are needed.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="f"></param>
        public void SetUpdateUserSuggFunc(string paramName, Func<string[]> f)
        {
            UpdateUserSuggFunc[paramName] = f;
            userSugg[paramName] = new string[0];
        }

        public void UpdateUserSugg(string paramName, string[] newSugg)
        {
            bool found = false;
            for (int i = 0; i < ParamInfo.Length; i++)
            {
                if (ParamInfo[i].Name == paramName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                IDCUtils.IDC.Log("Failed to update the '" + paramName + "' param of the " + CmdName + " cmd. No such parameter", UnityEngine.LogType.Error);
                return;
            }

            userSugg[paramName] = newSugg;
        }

        /// <summary>
        /// Returns the parameter suggestions set by the user
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public string[] GetUserParamSugg(string paramName)
        {
            if (!userSugg.ContainsKey(paramName))
                return new string[0];

            if (UpdateUserSuggFunc.ContainsKey(paramName))
                userSugg[paramName] = UpdateUserSuggFunc[paramName].Invoke();

            return userSugg[paramName];
        }

        /// <summary>
        /// Returns the parameter suggestions set by using the IDCParamAttribute
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public IEnumerable<string> GetParamAttribSugg(string paramName)
        {
            return paramAttribSugg.ContainsKey(paramName) ? paramAttribSugg[paramName] : System.Linq.Enumerable.Empty<string>();
        }

        public static string GetTypeName(Type t)
        {
            if (t.IsEnum)
                return t.Name;

            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Boolean: return "bool";
                case TypeCode.Char: return "char";
                case TypeCode.SByte: return "sbyte";
                case TypeCode.Byte: return "byte";
                case TypeCode.Int16: return "short";
                case TypeCode.UInt16: return "ushort";
                case TypeCode.Int32: return "int";
                case TypeCode.UInt32: return "uint";
                case TypeCode.Int64: return "long";
                case TypeCode.UInt64: return "ulong";
                case TypeCode.Single: return "float";
                case TypeCode.Double: return "double";
                case TypeCode.Decimal: return "decimal";
                case TypeCode.DateTime: return "DateTime";
                case TypeCode.String: return "string";
                default: return t.Name;
            }
        }
    }
}