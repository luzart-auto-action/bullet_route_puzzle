namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    
    public class SelectChangeTMPPresetIndex : BaseSelect
    {
        public TMP_Text txt;
        public Material[] materials;
        public override void Select(int index)
        {
            base.Select(index);
            int length = materials.Length;
            if(index >= length)
            {
                return;
            }
            else
            {
                txt.fontMaterial = materials[index];
            }
        }
    }
}
