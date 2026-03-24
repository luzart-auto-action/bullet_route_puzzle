namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class SelectChangeCanvasGroup : BaseSelect
    {
        public CanvasGroup canvasGroup;
        public float valueSelect = 1;
        public float valueUnSelect = 0.5f;
    
        public override void Select(bool isSelect)
        {
            base.Select(isSelect);
            if (isSelect)
            {
                canvasGroup.alpha = valueSelect;
            }
            else
            {
                canvasGroup.alpha = valueUnSelect;
            }
        }
    }
}
