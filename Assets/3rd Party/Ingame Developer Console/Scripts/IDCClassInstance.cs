namespace IDC
{
    public class IDCClassInstance
    {
        public object Instance { get { return wr.Target; } }
        public readonly string ClassName;
        public readonly int Hash;
        public readonly bool IsStatic;

        readonly System.WeakReference wr;

        public IDCClassInstance(string name, int hash, object classInstance, bool isStatic)
        {
            ClassName = name;
            Hash = hash;
            IsStatic = isStatic;
            wr = new System.WeakReference(classInstance);
        }
    }
}