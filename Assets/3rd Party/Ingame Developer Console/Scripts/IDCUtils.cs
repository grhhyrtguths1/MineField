using UnityEngine;

namespace IDC
{
    [DisallowMultipleComponent, RequireComponent(typeof(IDCController))]
    public class IDCUtils : MonoBehaviour
    {
        public static IDCController IDC { get; private set; }
        public const string colorEnd = "</color>";

        void Awake()
        {
            IDC = GetComponent<IDCController>();
        }

        /// <summary>
        /// Adds the rich text code for the wanted color around the passed string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string ColorString(string s, Color c)
        {
            return "<color=" + RGBAToHex(c) + ">" + s + colorEnd;
        }

        public static string BoldString(string s)
        {
            return "<b>" + s + "</b>";
        }

        public static string ItalicString(string s)
        {
            return "<i>" + s + "</i>";
        }

        public static string SetStringSize(string s, float newSize)
        {
            return "<size=" + newSize + ">" + s + "</size>";
        }

        /// <summary>
        /// RGBA to Hexadecimal(RRGGBBAA)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string RGBAToHex(Color c)
        {
            float r = c.r * 255, g = c.g * 255, b = c.b * 255, a = c.a * 255;   //Change from 0-1 to 0-255

            return "#" +
                DigitToHex(Mathf.FloorToInt(r / 16)) + DigitToHex((int)r % 16) +
                DigitToHex(Mathf.FloorToInt(g / 16)) + DigitToHex((int)g % 16) +
                DigitToHex(Mathf.FloorToInt(b / 16)) + DigitToHex((int)b % 16) +
                DigitToHex(Mathf.FloorToInt(a / 16)) + DigitToHex((int)a % 16);
        }

        /// <summary>
        /// Returns the hex representation of a digit between 0 and 15
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string DigitToHex(int n)
        {
            if (n < 10)
                return n.ToString();

            switch (n)
            {
                case 10:
                    return "A";
                case 11:
                    return "B";
                case 12:
                    return "C";
                case 13:
                    return "D";
                case 14:
                    return "E";
                case 15:
                    return "F";
                default:
                    return "0";
            }
        }

        /// <summary>
        /// Returns the opening rich text color tag of the passed color
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string GetColorStart(Color c)
        {
            return "<color=" + RGBAToHex(c) + ">";
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sets the value of the static IDC reference in IDCUtils.
        /// Only usable by editor tools that might need the reference to be available (e.g. editor time usage of reflection and the IDC in general).
        /// </summary>
        /// <param name="idcCont"></param>
        public static void SetIDCRef(IDCController idcCont)
        {
            IDC = idcCont;
        }
#endif
    }
}