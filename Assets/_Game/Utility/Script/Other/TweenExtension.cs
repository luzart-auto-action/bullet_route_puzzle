using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace Luzart
{
    public static class TweenExtension
    {
        // Hàm mở rộng để tạo hiệu ứng gõ chữ cho TMP_InputField
        public static Tween DOText(this TMP_InputField inputField, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
        {
            // Chúng ta tạo một Tween chạy trên thuộc tính .text của InputField
            // DOTween sẽ tự động cập nhật giá trị text qua các frame
            return DOTween.To(() => inputField.text, x => inputField.text = x, endValue, duration)
                .SetOptions(richTextEnabled, scrambleMode, scrambleChars)
                .SetTarget(inputField);
        }
        public static Tweener DOSetTextCharByChar(
        this TMP_InputField inputField,
        string content,
        float charPerSecond)
        {
            if (inputField == null)
            {
                Debug.LogError("TMP_InputField is null");
                return null;
            }

            if (charPerSecond <= 0f)
                charPerSecond = 1f;

            inputField.text = string.Empty;

            int totalChars = content.Length;
            float duration = totalChars / charPerSecond;

            int lastIndex = 0;

            return DOTween.To(
                    () => lastIndex,
                    x =>
                    {
                        if (x <= lastIndex) return;

                        lastIndex = x;
                        inputField.text = content.Substring(0, lastIndex);
                    },
                    totalChars,
                    duration
                )
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    inputField.text = content;
                });
        }
        public static Tweener DOSetTextCharByChar(
        this TMP_Text text,
        string content,
        float charPerSecond)
        {
            if (text == null)
            {
                Debug.LogError("TMP_Text is null");
                return null;
            }

            if (charPerSecond <= 0f)
                charPerSecond = 1f;

            text.text = string.Empty;

            int totalChars = content.Length;
            float duration = totalChars / charPerSecond;

            int lastIndex = 0;

            return DOTween.To(
                    () => lastIndex,
                    x =>
                    {
                        if (x <= lastIndex) return;

                        lastIndex = x;
                        text.text = content.Substring(0, lastIndex);
                    },
                    totalChars,
                    duration
                )
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    text.text = content;
                });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrollRect"></param>
        /// <param name="floatValue"> Bottom la 0, top la 1</param>
        /// <param name="duration"></param>
        /// <param name="ease"></param>
        /// <param name="ignoreTimeScale"></param>
        /// <returns></returns>
        public static Tween ScrollTo(
    this ScrollRect scrollRect,
    float floatValue = 0f,
    float duration = 0.5f,
    Ease ease = Ease.OutCubic,
    bool ignoreTimeScale = true
)
        {
            if (scrollRect == null)
                return null;

            scrollRect.StopScrollTween();

            return DOTween.To(() => scrollRect.verticalNormalizedPosition,
                    v => scrollRect.verticalNormalizedPosition = v,
                    floatValue,
                    duration
                )
                .SetEase(ease)
                .SetUpdate(ignoreTimeScale);
        }
        public static void StopScrollTween(this ScrollRect scrollRect)
        {
            DOTween.Kill(scrollRect);
        }
    }
}
