using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace IDC
{
    [RequireComponent(typeof(ScrollRect))]
    public class InfiniteLog : MonoBehaviour
    {
        const byte requiredScrollbarPushes = 3;

        [HideInInspector] public bool skipViewUpdates;

        Scrollbar scrollBar;
        ScrollRect scrollrect;
        RectTransform logRectTr;
        TMP_Text expandingText, logText;

        public List<string> LogBuffer { get; private set; }

        int bufferWritePos, maxLogLines;
        bool isMaintainingScrollPos;
        byte scrollbarPushesToBot;

        public void Init(int maxLines)
        {
            scrollrect = GetComponent<ScrollRect>();
            logRectTr = GetComponent<RectTransform>();
            expandingText = transform.GetChild(0).GetComponent<TMP_Text>();
            logText = transform.GetChild(1).GetComponent<TMP_Text>();
            scrollBar = transform.GetChild(2).GetComponent<Scrollbar>();

            SetMaxLogLines(maxLines);
            LogBuffer = new List<string>(maxLogLines);
        }

        public void SetFont(TMP_FontAsset font) => logText.font = font;

        public void SetFontSize(float size) => logText.fontSize = size;

        public void SetMaxLogLines(int maxLines) => maxLogLines = maxLines;

        public void SetLineSpacing(float spacing) => logText.lineSpacing = spacing;

        public void SetTabSize(byte tabSize) => logText.font.tabSize = tabSize;

        public void Clear()
        {
            LogBuffer = new List<string>(maxLogLines);
            logText.text = "";
            expandingText.text = "";
            StartCoroutine(ResetScrollBarSize());
        }

        IEnumerator ResetScrollBarSize()
        {
            scrollBar.Select();
            yield return null;

            scrollBar.size = 1;
            scrollrect.verticalNormalizedPosition = 1;
        }

        /// <summary>
        /// Updates the log area size and text. Called internally and by the scrollrect
        /// </summary>
        /// <param name="scrollRectPos"></param>
        public void UpdateView(Vector2 scrollRectPos)
        {
            if (skipViewUpdates)
                return;

            logText.text = "";
            UpdateLogAreaSize();

            if (LogBuffer.Count == 0)
                return;

            //Show needed messages
            int start = GetStartLineIndex(), end = start + GetViewSize();
            int startOverflow = start > LogBuffer.Count ? start - LogBuffer.Count : 0, endOverflow = end > LogBuffer.Count ? end - LogBuffer.Count : 0;
            start -= startOverflow;
            end -= endOverflow;

            if (startOverflow > 0 && start <= bufferWritePos && bufferWritePos - start < end)
                end = bufferWritePos;

            //PERF: Use StringBuilder
            for (int i = start; i < end; i++)
                logText.text += LogBuffer[i];

            for (int i = startOverflow; i < bufferWritePos && i < endOverflow; i++)
                logText.text += LogBuffer[i];
        }

        /// <summary>
        /// Gets the logBuffer index of the first line that should be shown in the log area
        /// The index can be greater than maxLogLines and must be normalized first
        /// </summary>
        /// <returns></returns>
        int GetStartLineIndex()
        {
            int start = Mathf.FloorToInt((LogBuffer.Count + GetViewSize() - Mathf.CeilToInt(GetViewSize() / logText.lineSpacing)) * Mathf.Clamp01(1 - scrollBar.value));
            return start + GetBufferIndex(0);
        }

        public void UpdateLogAreaSize()
        {
            expandingText.rectTransform.sizeDelta = new Vector2(expandingText.rectTransform.sizeDelta.x, LogBuffer.Count * logText.fontSize);
        }

        /// <summary>
        /// Returns the approximate number of lines that can be shown on screen
        /// </summary>
        /// <returns></returns>
        public int GetViewSize()
        {
            //At small line counts, viewSize isn't accurate and not all the view is used for display, so we calculate what it would be at large line counts and use that
            float testScrollbarSize = Mathf.Clamp01(logRectTr.rect.height / (100000 * logText.fontSize));
            return Mathf.FloorToInt(100000 * testScrollbarSize);
        }

        public void Log(string s, Color c, bool maintainPos)
        {
            int start = GetStartLineIndex();

            var split = s.Split('\n');
            if (maxLogLines - LogBuffer.Count > 0)
            {
                bufferWritePos = LogBuffer.Count;
                LogBuffer.AddRange(new string[Mathf.Clamp(split.Length, 0, maxLogLines - LogBuffer.Count)]);
            }

            for (int i = 0; i < split.Length; i++)
            {
                LogBuffer[bufferWritePos++] = split[i] == "" ? "\n" : IDCUtils.ColorString(split[i], c) + "\n";

                if (bufferWritePos == LogBuffer.Count)
                    bufferWritePos = 0;
            }

            if (!maintainPos)
                return;

            //If the user is not scrolled up more than a view size of text push him down
            if (start > LogBuffer.Count - GetViewSize() * 2)
            {
                scrollbarPushesToBot = 0;
                StartCoroutine(HandleViewUpdates());
            }
            //If he is scrolled up far enough make sure he still sees the same lines
            else
            {
                //Fix for the 'MaintainScrollPos' not working well when pos at the very top 
                if (start <= 20)
                    UpdateLogAreaSize();

                if (!isMaintainingScrollPos)
                    StartCoroutine(MaintainScrollPos(start));
            }
        }

        int GetBufferIndex(int i)
        {
            return LogBuffer.Count == 0 ? 0 : (i + bufferWritePos) % Mathf.Clamp(LogBuffer.Count, 1, LogBuffer.Count);
        }

        IEnumerator HandleViewUpdates()
        {
            while (scrollbarPushesToBot < requiredScrollbarPushes)
            {
                UpdateLogAreaSize();
                scrollBar.value = Mathf.Abs(GetViewSize() * 0.8f / Mathf.Clamp(LogBuffer.Count, 1, LogBuffer.Count));

                if (++scrollbarPushesToBot == requiredScrollbarPushes)
                    UpdateView(Vector2.zero);

                yield return null;
            }
        }

        IEnumerator MaintainScrollPos(int index)
        {
            isMaintainingScrollPos = true;
            yield return null;

            UpdateLogAreaSize();
            int ticks = 0;
            while (ticks++ < 100 && GetStartLineIndex() > index)
                scrollBar.value += scrollBar.size * 0.05f;

            isMaintainingScrollPos = false;
        }
    }
}