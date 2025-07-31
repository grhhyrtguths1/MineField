using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IDC
{
    public class IDCState
    {
        public enum ConsoleState
        {
            /// <summary>When input field is empty and no selection in progress</summary>
            SuggestingCmds,
            ShowingCmdSignature,
            SelectingCmds,
            SelectingFromHistory,
            SelectingParamValues,
        }

        public int PrevParamIndex { get; private set; }
        public int CurrParamIndex { get; private set; }
        public int CurrCtorParamIndex { get; private set; }

        ///<summary>The position of '-' for the current cmd parameter</summary>
        public int CurrParamPos { get; private set; }
        ///<summary>The position of '-' for the next cmd parameter. If no next param it has 'InputTxt.length'</summary>
        public int NextParamPos { get; private set; }

        ///<summary>Start trimmed input field text</summary>
        public string InputTxt { get; private set; }
        public ConsoleState State { get; private set; }

        public bool CanSelectSuggestions;

        bool selectingItems;
        TMP_InputField inputField;
        readonly List<string> rawSuggestions;

        public IDCState(TMP_InputField inputField, List<string> rawSuggestions)
        {
            this.inputField = inputField;
            this.rawSuggestions = rawSuggestions;
            CurrParamIndex = PrevParamIndex = -1;
        }

        public void UpdateState()
        {
            InputTxt = inputField.text.TrimStart();
            UpdateConsoleState();
            UpdateParamInfo();
            UpdateCtorParamIndex();
        }

        void UpdateConsoleState()
        {
            var prevState = State;
            bool upOrDown = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);

            bool wasSelecting = selectingItems;
            if (upOrDown)
                selectingItems = true;
            else if (selectingItems && Input.anyKey)
                selectingItems = false;

            //If we are selecting and no other input was given then maintain state
            if (selectingItems && wasSelecting)
                return;

            //If we just selected an item then maintain state until next update
            //to give systems time to use the input
            if (!selectingItems && wasSelecting && Input.GetKey(KeyCode.Tab))
                return;

            if (State != ConsoleState.SelectingParamValues && upOrDown && !CanSelectSuggestions)
                return;

            bool isWritingParams = HasParams();
            if (upOrDown)
            {
                if (isWritingParams)
                    State = ConsoleState.SelectingParamValues;
                else if (rawSuggestions.Count == 0)
                    State = ConsoleState.SelectingFromHistory;
                else
                    State = ConsoleState.SelectingCmds;
            }

            else
            {
                State = isWritingParams ? ConsoleState.ShowingCmdSignature : ConsoleState.SuggestingCmds;
            }

            //if (State != prevState)
            //    IDCUtils.IDC.Log("Change: " + prevState + "->" + State);
        }

        void UpdateParamInfo()
        {
            int currParamIndex = -1, currParamPos = -1, nextParamPos = InputTxt.Length;
            bool inString = false, gotCurrParam = false;
            for (int i = 0; i < InputTxt.Length; i++)
            {
                if (i == inputField.caretPosition)
                    gotCurrParam = true;

                if (InputTxt[i] == '"')
                    inString = !inString;
                else if (InputTxt[i] == '-' && !inString && !(i + 1 < InputTxt.Length && char.IsDigit(InputTxt[i + 1])))
                {
                    if (!gotCurrParam)
                    {
                        currParamIndex++;
                        currParamPos = i;
                    }

                    else
                    {
                        nextParamPos = i;
                        break;
                    }
                }
            }

            if (currParamIndex != CurrParamIndex)
                PrevParamIndex = CurrParamIndex;

            CurrParamIndex = currParamIndex;
            CurrParamPos = currParamPos;
            NextParamPos = nextParamPos;
        }

        void UpdateCtorParamIndex()
        {
            if (InputTxt == string.Empty)
            {
                CurrCtorParamIndex = -1;
                return;
            }

            int currCtorParamIndex = 0;
            int endDashIndex = Mathf.Clamp(CurrParamPos + 1, 0, InputTxt.Length - 1);
            while (endDashIndex <= InputTxt.Length - 1 && InputTxt[endDashIndex] != '-')
            {
                //If in string skip till we are outside or at reached end of string without exiting (open string)
                if (InputTxt[endDashIndex] == '"')
                {
                    while (endDashIndex < InputTxt.Length - 1 && InputTxt[++endDashIndex] != '"') ;

                    if (InputTxt[endDashIndex] == '"')
                    {
                        endDashIndex++;
                        continue;
                    }

                    //This means we have an open string so no point in continuing
                    else
                    {
                        break;
                    }
                }

                if (InputTxt[endDashIndex] == ',')
                    currCtorParamIndex++;

                endDashIndex++;
            }

            CurrCtorParamIndex = currCtorParamIndex;
        }

        bool HasParams()
        {
            bool insideLiteral = false;
            for (int i = 0; i < InputTxt.Length; i++)
            {
                if (InputTxt[i] == '"')
                {
                    insideLiteral = !insideLiteral;
                    continue;
                }

                if (!insideLiteral && (InputTxt[i] == '-' || InputTxt[i] == ' '))
                    return true;
            }

            return false;
        }
    }
}