namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class UIBase : MonoBehaviour
    {
        public UIName uiName = UIName.None;
        public Button closeBtn;
        public bool isAnimBtnClose = false;
        public bool isCache = false;


        protected bool isSetup = false;
        protected System.Action onHideDone;

        protected virtual void Setup()
        {
            if (closeBtn != null)
            {
                GameUtil.ButtonOnClick(closeBtn, OnClickClose, isAnimBtnClose);
                // closeBtn.onClick.AddListener(OnClickClose);
            }
        }
        public virtual void Show(System.Action onHideDone)
        {
            if (!isSetup)
            {
                isSetup = true;
                Setup();
            }
            gameObject.SetActive(true);
            InitActionOnHideDone(onHideDone);
        }
        public void InitActionOnHideDone(System.Action onHideDone)
        {
            this.onHideDone = onHideDone;
        }

        public virtual void RefreshUI()
        {
        }

        public virtual void Hide()
        {
            UIManager.Instance.RemoveActiveUI(uiName);
            if (gameObject == null)
            {
                return;
            }
            if (!isCache)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
            onHideDone?.Invoke();
            onHideDone = null;
        }

        public virtual void OnAnimHideDone()
        {
            if (!isCache)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }

            onHideDone?.Invoke();

            onHideDone = null;
        }

        private IEnumerator DelayAnimHide(float dur)
        {
            yield return new WaitForSeconds(dur);
            OnAnimHideDone();
        }
        private IEnumerator DelayShowOut()
        {
            yield return new WaitForSeconds(0.5f);
            gameObject.SetActive(false);
        }

        public virtual void OnClickClose()
        {
            Hide();
            //AudioManager.Instance.PlayClickBtnFx();
        }
    }
}
