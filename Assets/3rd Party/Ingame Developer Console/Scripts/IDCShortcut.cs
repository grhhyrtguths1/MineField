using System.Collections.Generic;
using UnityEngine;
using static IDC.IDCShortcut;

namespace IDC
{
    [System.Serializable]
    public struct IDCShortcut
    {
        public enum ModKey
        {
            LeftShift = KeyCode.LeftShift,
            RightShift = KeyCode.RightShift,
            LeftCtrl = KeyCode.LeftControl,
            RightCtrl = KeyCode.RightControl,
            LeftAlt = KeyCode.LeftAlt,
            RightAlt = KeyCode.RightAlt,
            CapsLock = KeyCode.CapsLock,
        }

        public string name;
        public KeyCode key;

        public List<IDCCmdsEnum> cmds;
        public List<ModKey> modifiers;
    }

    public static class MyEnumExtensions
    {
        /// <summary>
        /// Returns the other direction mod key if present, or the same key if no opposite is present.
        /// For example, if given modkey is LeftShift, it returns RightShift, and vice versa.
        /// If CapsLock, it returns CapsLock.
        /// </summary>
        /// <param name="mk"></param>
        /// <returns></returns>
        public static ModKey Opposite(this ModKey mk)
        {
            switch (mk)
            {
                case ModKey.LeftShift:
                    return ModKey.RightShift;
                case ModKey.RightShift:
                    return ModKey.LeftShift;

                case ModKey.LeftCtrl:
                    return ModKey.RightCtrl;
                case ModKey.RightCtrl:
                    return ModKey.LeftCtrl;

                case ModKey.LeftAlt:
                    return ModKey.RightAlt;
                case ModKey.RightAlt:
                    return ModKey.LeftAlt;

                case ModKey.CapsLock:
                    return ModKey.CapsLock;

                default:
                    Debug.Log("unhandled mod key: " + mk);
                    return mk;
            }
        }
    }
}