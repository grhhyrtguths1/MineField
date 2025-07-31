using System;
using System.Reflection;

namespace IDC
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class IDCVarAttribute : Attribute
    {
        public string VarName { get; private set; }
        public string ClassName { get; private set; }
        public Type VarType { get; private set; }
        public FieldInfo Fi { get; private set; }
        public PropertyInfo Pi { get; private set; }

        public IDCVarAttribute(string name = "")
        {
            VarName = string.IsNullOrWhiteSpace(name) ? null : name;
            VarName = VarName.Trim();
        }

        public void SetVar(FieldInfo fi, string className)
        {
            Fi = fi;
            ClassName = className;
            VarType = fi.FieldType;

            if (VarName == null || VarName.Trim() == string.Empty)
                VarName = fi.Name.Trim();
        }

        public void SetVar(PropertyInfo pi, string className)
        {
            Pi = pi;
            ClassName = className;
            VarType = pi.PropertyType;

            if (VarName == null || VarName.Trim() == string.Empty)
                VarName = pi.Name.Trim();
        }

        public object GetVarValue(object classInstance)
        {
            if (Fi != null)
                return Fi.GetValue(classInstance);
            else if (Pi.CanRead)
                return Pi.GetValue(classInstance, null);
            else
            {
                IDCUtils.IDC.Log("IDCVar '" + VarName + "' Does not have a getter", UnityEngine.LogType.Error);
                return null;
            }
        }

        public void SetVarValue(object classInstance, object newValue)
        {
            if (Fi != null)
                Fi.SetValue(classInstance, newValue);
            else if (Pi.CanWrite)
                Pi.SetValue(classInstance, newValue, null);
            else
            {
                IDCUtils.IDC.Log("IDCVar '" + VarName + "' Does not have a setter", UnityEngine.LogType.Error);
            }
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