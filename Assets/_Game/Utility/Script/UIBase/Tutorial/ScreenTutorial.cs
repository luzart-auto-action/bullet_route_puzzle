namespace Luzart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class ScreenTutorial : MonoBehaviour
    {
        public Action ActionClick;
        public Button btn;
        public virtual void Setup()
        {
            GameUtil.ButtonOnClick(btn, OnClick);
        }
        private void OnClick()
        {
            ActionClick?.Invoke();
            if (g0SetUp == null)
            {
                return;
            }
            var canvas = g0SetUp.GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
            Destroy(canvas);
        }
        public virtual void Show(Action ActionClick)
        {
            this.ActionClick = ActionClick;
        }
        public virtual void RefreshUI()
        {

        }
        public virtual void Hide()
        {
            actionHide?.Invoke();
        }
        public Action actionHide;
        private GameObject g0SetUp;
        public virtual void SpawnItem(GameObject gO)
        {
            if (gO == null)
            {
                return;
            }
            this.g0SetUp = gO;
            var canvas = gO.GetComponent<Canvas>();
            if(canvas == null)
            {
                canvas = gO.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            var tmps = canvas.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            int length = tmps.Length;
            for (int i = 0; i < length; i++)
            {
                var cpn = tmps[i];
                cpn.gameObject.CallOnEnable();
            }
            OnDoneSpawnItem(gO);
        }

        public virtual void OnDoneSpawnItem(GameObject gO)
        {

            btn.transform.SetAsLastSibling();
            RectTransform rectTransform = gO.GetComponent<RectTransform>();
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;

            if (parentRectTransform != null)
            {
                Vector2 parentSize = parentRectTransform.rect.size;
                Vector2 anchorMin = rectTransform.anchorMin;
                Vector2 anchorMax = rectTransform.anchorMax;

                // Kích thước thực tế của rectTransform khi anchors khác nhau
                Vector2 realSizeDelta = new Vector2(
                    (anchorMax.x - anchorMin.x) * parentSize.x + rectTransform.sizeDelta.x,
                    (anchorMax.y - anchorMin.y) * parentSize.y + rectTransform.sizeDelta.y
                );
                btn.image.rectTransform.sizeDelta = realSizeDelta;
            }

            btn.transform.position = gO.transform.position;
        }
    }

}