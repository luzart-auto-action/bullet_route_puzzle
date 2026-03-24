using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    public class SelectChangeIndexForBaseToggle : BaseSelect
    {
        public List<GroupBaseToggle> groupBaseToggles = new List<GroupBaseToggle>();
        public override void Select(int index)
        {
            base.Select(index);
            if (groupBaseToggles == null)
            {
                return;
            }
            int length = groupBaseToggles.Count;
            if (index >= length)
            {
                return;
            }
            if (groupBaseToggles[index] == null || groupBaseToggles[index].baseToggle == null)
            {
                return;
            }
            foreach (var groupBaseTogglFalse in groupBaseToggles)
            {
                if (groupBaseTogglFalse != null)
                {
                    groupBaseTogglFalse.Select(false);
                }
            }
            var groupBaseToggleTrue = groupBaseToggles[index];
            groupBaseToggleTrue.Select(true);
        }

        [Serializable]
        public class GroupBaseToggle
        {
            public List<BaseToggle> baseToggle;
            [SerializeField][ReadOnly] bool isOn;

            public void Select(bool value)
            {
                isOn = value;
                if (baseToggle != null)
                {
                    foreach (var toggle in baseToggle)
                    {
                        if (toggle != null)
                        {
                            toggle.Select(isOn);
                        }
                    }
                }
            }
        }
    }
}
