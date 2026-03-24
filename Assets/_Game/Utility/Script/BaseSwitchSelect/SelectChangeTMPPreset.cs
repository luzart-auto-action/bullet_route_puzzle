
namespace Luzart
{
    using System;
    using TMPro;
    using UnityEngine;
    public class SelectChangeTMPPreset : BaseSelect
    {
        public TMP_Text txt;
        public Material materialSelect;
        public Material materialUnSelect;
        public override void Select(bool isSelect)
        {
            base.Select(isSelect);
            if (isSelect)
            {
                txt.fontMaterial = materialSelect;
            }
            else
            {
                txt.fontMaterial = materialUnSelect;
            }
        }
    }
}

