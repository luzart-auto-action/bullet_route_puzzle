namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    
    public class SelectChangeUnityEvent : BaseSelect
    {
        public UnityEvent eventSelect, eventUnSelect;
        public override void Select(bool isSelect)
        {
            if (isSelect)
            {
                eventSelect.Invoke();
            }
            else
            {
                eventUnSelect.Invoke();
            }
        }
    }
}
