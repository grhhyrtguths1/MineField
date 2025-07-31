using System.Collections.Generic;
using UnityEngine;

namespace IDC
{
    [System.Serializable]
    public class IDCShortcutProfile : ScriptableObject
    {
        public List<IDCShortcut> shortcuts = new List<IDCShortcut>();
    }
}