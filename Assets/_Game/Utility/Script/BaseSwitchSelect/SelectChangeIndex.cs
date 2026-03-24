namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class SelectChangeIndex : BaseSelect
    {
        public BaseSelect[] baseSelect;
    
        public override void Select(int index)
        {
            base.Select(index);
            int length = baseSelect.Length;
            for (int i = 0; i < length; i++)
            {
                bool status = index == i;
                baseSelect[i].Select(status);
            }
        }
    }
}
