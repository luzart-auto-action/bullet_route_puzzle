namespace Luzart
{
    using DG.Tweening;
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    public class UIToast : UIBase
    {
        public CanvasGroup canvasGroup;
        public TMP_Text txtNoti;
        private Sequence sq;
        public void Init(string str)
        {
            txtNoti.text = str;
            sq?.Kill();
            sq = DOTween.Sequence();
            sq.AppendInterval(1f);
            sq.Append(DOVirtual.Float(1, 0, 0.5f, (x) =>
            {
                canvasGroup.alpha = x;
            }));
            sq.AppendCallback(Hide);

        }
    }
    public static class KeyToast
    {
        public const string NoInternetLoadAds = "You are currently offline.\nPlease check your internet connection";
        public const string Expansion = "Your ship is in the extended version";
        public static string UnlockBooster(int levelUnlock)
        {
            return $"Reach to level {levelUnlock} to use this booster !";
        }
        public const string UnlockPreviousItem = "Please unlock previous item";
    }
}
