namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class SelectChangeImageIndex : BaseSelect
    {
        public Image[] imSelects;
        public GroupSprite[] groupSprite;
    
        public override void Select(int index)
        {
            if (imSelects != null && groupSprite != null)
            {
                int lengthImage = imSelects.Length;
                int lengthGroupSprite = groupSprite.Length;
                if(index >= lengthGroupSprite || groupSprite[index] == null || groupSprite[index].sp == null || groupSprite[index].sp.Length == 0)
                {
                    return;
                }
                var spAll = groupSprite[index].sp;
                int lengthGroup = spAll.Length;
                int length = Mathf.Min(lengthImage, lengthGroup);
                for (int i = 0; i < length; i++)
                {
                    if(i > lengthGroup)
                    {
                        continue;
                    }
                    imSelects[i].sprite = spAll[i];
                }
            }
        }
        [System.Serializable]
        public class GroupSprite
        {
            public Sprite[] sp;
        }
    }
    
}
