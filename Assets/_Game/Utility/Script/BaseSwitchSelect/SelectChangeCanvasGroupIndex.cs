namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class SelectChangeCanvasGroupIndex : BaseSelect
    {
        public CanvasGroup[] groupCanvasGroup;
        public ValueGroupCanvasGroup[] valueGroups;
    
        public override void Select(int index)
        {
            base.Select(index);
            int length = groupCanvasGroup.Length;
    
            if (index >= valueGroups.Length)
            {
                Debug.LogError($"SelectChangeCanvasGroupIndex {gameObject}");
                return;
            }
            int lengthValueGroup = valueGroups[index].values.Length;
            for (int i = 0;i < length; i++)
            {
                float valueAlpha = 1;
                if (i < lengthValueGroup)
                {
                    valueAlpha = valueGroups[index].values[i];
                }
                groupCanvasGroup[i].alpha = valueAlpha;
            }
        }
    
        [System.Serializable]
        public class ValueGroupCanvasGroup
        {
            public float[] values;
        }
    }
}
