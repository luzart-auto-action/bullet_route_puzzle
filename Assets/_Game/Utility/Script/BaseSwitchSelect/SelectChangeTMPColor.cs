namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    
    public class SelectChangeTMPColor : BaseSelect
    {
        public TMP_Text[] txts;
        public Color[] colorsSelect;
        public Color[] colorsUnSelect;
    
        public override void Select(bool isSelect)
        {
            if (txts != null)
            {
                int length = txts.Length;
                for (int i = 0; i < length; i++)
                {
                    if (isSelect)
                    {
                        txts[i].color = colorsSelect[i];
                    }
                    else
                    {
                        txts[i].color = colorsUnSelect[i];
                    }
                }
            }
        }
    }
}
