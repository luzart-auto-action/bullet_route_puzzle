using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    public class SelectChangeUnityEventIndex : BaseSelect
    {
        public List<UnityEngine.Events.UnityEvent> listEventSelect = new List<UnityEngine.Events.UnityEvent>();

        public override void Select(int index)
        {
            base.Select(index);
            if (index >= 0 && index < listEventSelect.Count)
            {
                listEventSelect[index]?.Invoke();
            }
        }
    }
}
