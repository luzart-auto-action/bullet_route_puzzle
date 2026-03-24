namespace Luzart
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using Luzart;
    
    public class ButtonSelect : MonoBehaviour
    {
        public Button btn;
        public BaseSelect[] baseSelects; 
    
        public bool IsAnim = true;
        public int index;
        private Action<ButtonSelect> ActionSelect;
        public bool IsAutoInitialize = true;
        private string strKeyAds = null;
    
        protected virtual void Start()
        {
            if (IsAutoInitialize)
            {
                InitializeButton();
            }
        }
        private void InitializeButton()
        {
            GameUtil.ButtonOnClick(btn, ClickAction, IsAnim, strKeyAds);
        }
        public virtual void InitAction(int index ,Action<ButtonSelect> action, string strKeyAds = null)
        {
            this.index = index;
            this.ActionSelect = action;
            this.strKeyAds = strKeyAds;
            if (!IsAutoInitialize)
            {
                InitializeButton();
            }
            Select(false);
        }
        public void ClickAction()
        {
            ActionSelect?.Invoke(this);
            Select(true);
        }
        public void Select(bool isSelect)
        {
            if (baseSelects != null)
            {
                int length = baseSelects.Length;
                for (int i = 0; i < baseSelects.Length; i++)
                {
                    if (baseSelects[i] != null)
                    {
                        baseSelects[i].Select(isSelect);
                    }
                }
            }
        }
    }
}
