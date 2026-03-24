namespace Luzart
{
    using JetBrains.Annotations;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security;
    using Unity.VisualScripting;
    using UnityEngine;
    
    [CreateAssetMenu(menuName = "SO/SpriteResourcesSO", fileName = "SpriteResourcesSO")]
    public class SpriteResourcesSO : ScriptableObject
    {
        [SerializeField]
        private SpriteResource[] spriteRes;
    
        [SerializeField]
        private Sprite[] spBg;
        
        private Dictionary<DataTypeResource, Sprite[]> dictSpriteRes = new Dictionary<DataTypeResource, Sprite[]>();
    
        private void InitDictSprite()
        {
            if(dictSpriteRes == null || dictSpriteRes.Count == 0)
            {
                dictSpriteRes.Clear();
                for(int i = 0; i < spriteRes.Length; i++)
                {
                    var data = spriteRes[i];
                    for (int j = 0; j < spriteRes[i].res.Length; j++)
                    {
                        var res = spriteRes[i].res[j];
                        var dataType = new DataTypeResource(data.type, res.id);
                        if (!dictSpriteRes.ContainsKey(dataType))
                        {
                            dictSpriteRes.Add(dataType, res.spIcons);
                        }
                    }
                    
                }
            }
        }
        public Sprite GetSpriteIcon(DataResource data)
        {
            InitDictSprite();
            if(dictSpriteRes.TryGetValue(data.type, out var sprite))
            {
                if(sprite != null && sprite[data.idIcon]!=null)
                {
                    return sprite[data.idIcon];
                }
            }
            return null;
        }
        //public Sprite GetSpriteBg(DataResource data)
        //{
        //    return GetSpriteBG(data.idBg);
        //}
        public Sprite GetSpriteBG(int index)
        {
            if(index == 0)
            {
                return null;
            }
            if(index < spBg.Length)
            {
                return spBg[index];
            }
            return null;    
        }
    }
    
    [System.Serializable]
    public class SpriteResource
    {
        public RES_type type;
        public SpriteIDResource[] res;
    }
    [System.Serializable]
    public class SpriteIDResource
    {
        public int id;
        public Sprite[] spIcons;
    }
    
}
