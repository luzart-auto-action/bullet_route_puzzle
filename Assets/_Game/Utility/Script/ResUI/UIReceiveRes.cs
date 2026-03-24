namespace Luzart
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class UIReceiveRes : UIBase
    {
        //public GameObject fxFirework;
        //public ListResUI listResUI;
        private Transform parentItemReceive = null;
        public void Initialize(bool isAnim = false, Vector3 posSpawn = default ,params DataResource[] dataResources)
        {

            //if (listResUI != null)
            //    listResUI.InitResUI(dataResources);
            //Sequence sq = DOTween.Sequence();
            //sq.AppendInterval(0.05f);
            //sq.AppendCallback(() => fxFirework.SetActive(true));
            //for (int i = 0; i < listResUI.listResUI.Count; i++)
            //{
            //    int index = i;
            //    listResUI.listResUI[index].gameObject.SetActive(false);
            //    sq.AppendCallback(() => listResUI.listResUI[index].gameObject.SetActive(true));
            //    sq.AppendInterval(0.1f);
            //}
            //sq.OnComplete(() => closeBtn.gameObject.SetActive(true));

        }
        public void Initialize( params DataResource[] dataResources)
        {

        }
    }

}