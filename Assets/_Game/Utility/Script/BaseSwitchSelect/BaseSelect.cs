namespace Luzart
{
    using UnityEngine;
    
    public abstract class BaseSelect : MonoBehaviour
    {
    #if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.PropertySpace(32)]
        [Sirenix.OdinInspector.Button]
    #endif
        public virtual void Select(bool isSelect) { }
    #if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
    #endif
        public virtual void Select(int index)
        {
        }
    }
    public interface ISelect
    {
        public abstract void Select();
        public abstract void UnSelect();
    }
}
