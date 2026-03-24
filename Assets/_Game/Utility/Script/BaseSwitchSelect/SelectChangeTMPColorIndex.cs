namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UIElements;
    
    public class SelectChangeTMPColorIndex : BaseSelect
    {
        public TMP_Text[] txts;
        public GroupColor[] groupColor;
    
        public override void Select(int index)
        {
            if (groupColor == null)
            {
                return;
            }
            int lengthTxt = txts.Length;
            int lengthGroupColor = groupColor.Length;
            if(index >= lengthGroupColor)
            {
                return;
            }
            var arrayColor = groupColor[index].colors;
            if(arrayColor == null)
            {
                return;
            }
            int lengthColor = arrayColor.Length;
            int length = Mathf.Min(lengthTxt, lengthColor);
            for (int i = 0; i < length; i++)
            {
                txts[i].color = arrayColor[i];
            }
        }
    
        private void SetTextColor(TMP_Text txt, Color color)
        {
            if (txt != null)
            {
                txt.color = color;
            }
        }
    }
    [System.Serializable]
    public struct GroupColor
    {
        public Color[] colors;
    }
}
