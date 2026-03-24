namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class SelectChangeBaseSelectCustomIndex : BaseSelect
    {
        public GroupBaseSelectCustom[] groupBaseSelectCustoms;
    
    
        public override void Select(int index)
        {
            base.Select(index);
            if(groupBaseSelectCustoms == null)
            {
                return;
            }
            int length = groupBaseSelectCustoms.Length;
            if (index >= length)
            {
                return;
            }
            if (groupBaseSelectCustoms[index] == null || groupBaseSelectCustoms[index].baseSelectCustom ==null)
            {
                return;
            }
            var arrayBaseSelect = groupBaseSelectCustoms[index].baseSelectCustom;
            int lengthArrayBaseSelect = arrayBaseSelect.Length;
            for (int j = 0; j < lengthArrayBaseSelect; j++)
            {
                BaseSelectCustom bsCustom = arrayBaseSelect[j];
                if (bsCustom != null && bsCustom.baseSelect != null)
                {
                    bsCustom.baseSelect.Select(bsCustom.index);
                    bsCustom.baseSelect.Select(bsCustom.isSelect);
                }
            }
    
        }
    }
    
    [System.Serializable]
    public class BaseSelectCustom
    {
        public BaseSelect baseSelect;
        public int index;
        public bool isSelect;
    }
    
    [System.Serializable]
    public class GroupBaseSelectCustom
    {
        public BaseSelectCustom[] baseSelectCustom;
    }
}
