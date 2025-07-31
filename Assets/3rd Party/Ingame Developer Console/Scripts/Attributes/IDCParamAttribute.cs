using System;

namespace IDC
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public class IDCParamAttribute : Attribute
    {
        public string[] Suggs { get; private set; }

        public IDCParamAttribute(params string[] extraSuggestions)
        {
            Suggs = extraSuggestions;
        }

        /// <summary>
        /// Generates values between start (inclusive) and end (exclusive) with a given increment.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="increment"></param>
        public IDCParamAttribute(int start, int end, int increment = 1)
        {
            GenVals(start, end, increment);
        }

        /// <summary>
        /// Generates values between start (inclusive) and end (exclusive) with a given increment and a certain number of decimal places to display.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="increment"></param>
        public IDCParamAttribute(float start, float end, float increment = 1, int decimalPlaces = 1)
        {
            GenVals(start, end, increment, decimalPlaces);
        }

        void GenVals(int start, int end, int increment)
        {
            if (Math.Abs(end - start) <= Math.Abs(end - (start + increment)))
            {
                Suggs = new string[0];
                IDCUtils.IDC.Log("Generating suggestions for IDCParam failed. Values must not result in an infinite loop", UnityEngine.LogType.Error);
                return;
            }

            Suggs = new string[UnityEngine.Mathf.RoundToInt(Math.Abs((end - start) / increment))];
            for (int i = 0; i < Suggs.Length; i++)
            {
                Suggs[i] = start.ToString();
                start += increment;
            }
        }

        void GenVals(float start, float end, float increment, int decimalPlaces)
        {
            if (Math.Abs(end - start) <= Math.Abs(end - (start + increment)))
            {
                Suggs = new string[0];
                IDCUtils.IDC.Log("Generating suggestions for IDCParam failed. Values must not result in an infinite loop", UnityEngine.LogType.Error);
                return;
            }

            string format = "0.";
            for (int i = 0; i < decimalPlaces; i++)
                format += "0";

            Suggs = new string[UnityEngine.Mathf.RoundToInt(Math.Abs((end - start) / increment))];
            for (int i = 0; i < Suggs.Length; i++)
            {
                Suggs[i] = start.ToString(format);
                start += increment;
            }
        }
    }
}