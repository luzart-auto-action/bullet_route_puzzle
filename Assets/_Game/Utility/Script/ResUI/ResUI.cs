namespace Luzart
{
    using Luzart;
    using System.Collections;
    using System.Collections.Generic;
    using System.Resources;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class ResUI : MonoBehaviour
    {
        public BaseSelect bsItem;
        public Image imBg;
    
        [Space, Header("Item")]
        public Image imIcon;
        public TMP_Text txt;
        public string preStr = "";
        public string endStr = "";
        [Space, Header("Gold")]
        public Image imIconGold;
        public TMP_Text txtGold;
        [Space, Header("Heart")]
        public TMP_Text txtAmountHeart;
        [Space, Header("Heart Time")]
        public TMP_Text txtAmountHeartTime;
    
    
        private DataResource dataRes;
        public void InitData(DataResource _dataRes)
        {

            if (_dataRes == null)
            {
                return;
            }
            this.dataRes = _dataRes;
    
            SwitchItem();
    
        }
    
        private void SwitchItem()
        {
            switch (dataRes.type.type)
            {
                case RES_type.Gold:
                    {
                        SetSelect(1);
                        SetItemGold();
                        break;
                    }
                case RES_type.Heart:
                    {
                        SetSelect(2);
                        SetHeart();
                        break;
                    }
                case RES_type.HeartTime:
                    {
                        SetSelect(3);
                        SetHeartTime();
                        break;
                    }
                default:
                    {
                        SetSelect(0);
                        SetItem();
                        break;
                    }
            }
        }
    
        private void SetItem()
        {
            SetIcon();
            SetBg();
            SetTextPreEndString();
        }
        private void SetItemGold()
        {
            SetIconGold();
            SetBg();
            SetTextGold();
        }
        private void SetHeart()
        {
            SetTextHeart();
        }
        private void SetHeartTime()
        {
            SetTextHeartTime();
        }
        private void SetIcon()
        {
            var sp = Luzart.ResourcesManager.Instance.spriteResourcesSO.GetSpriteIcon(dataRes);
            SetImage(imIcon, sp);
        }
        private void SetIconGold()
        {
            var sp = Luzart.ResourcesManager.Instance.spriteResourcesSO.GetSpriteIcon(dataRes);
            SetImage(imIconGold, sp);
        }
        private void SetBg()
        {
            var sp = Luzart.ResourcesManager.Instance.spriteResourcesSO.GetSpriteBG(0);
            SetImage(imBg, sp);
        }
        private void SetTextPreEndString()
        {
            SetText(txt, $"{preStr}{dataRes.amount}{endStr}");
        }
        private void SetTextGold()
        {
            SetText(txtGold, $"{dataRes.amount}");
        }
        private void SetTextHeart()
        {
            SetText(txtAmountHeart, $"{dataRes.amount}");
        }
    
        private void SetTextHeartTime()
        {
            SetText(txtAmountHeartTime, GameUtil.FloatTimeSecondToUnixTime(dataRes.amount));
        }
        private void SetSelect(int index)
        {
            if (bsItem != null)
            {
                bsItem.Select(index);
            }
        }
    
        private void SetActive(GameObject ob, bool isStatus)
        {
            if (ob != null)
            {
                ob.SetActive(isStatus);
            }
        }
    
        private void SetText(TMP_Text txt, string str)
        {
            if (txt != null)
            {
                txt.text = str;
            }
        }
        private void SetImage(Image im, Sprite sprite)
        {
            if (im != null)
            {
                im.sprite = sprite;
            }
        }
    
    }
}
