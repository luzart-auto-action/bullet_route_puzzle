namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class ListResUI : MonoBehaviour
    {
        public ResUI resUIPf;
        [SerializeField]
        private Transform _parentSpawn = null;
        public Transform parentSpawn
        {
            get
            {
                if (_parentSpawn == null)
                {
                    _parentSpawn = transform;
                }
                return _parentSpawn;
            }
        }
        public List<ResUI> listResUI = new List<ResUI>();
        public void InitResUI(params DataResource[] dataResources)
        {
            int length = dataResources.Length;
            MasterHelper.InitListObj(length, resUIPf, listResUI, parentSpawn, (item, index) =>
            {
                item.gameObject.SetActive(true);
                var data = dataResources[index];
                item.InitData(data);
            });
        }
    }
}
