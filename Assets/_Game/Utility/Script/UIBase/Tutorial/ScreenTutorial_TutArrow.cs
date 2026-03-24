namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using UnityEngine;

    public class ScreenTutorial_TutArrow : ScreenTutorial
    {
        public GameObject muiTen;
        public override void OnDoneSpawnItem(GameObject gO)
        {
            base.OnDoneSpawnItem(gO);
            muiTen.SetActive(true);
            muiTen.transform.position = gO.transform.position;
            muiTen.transform.rotation = gO.transform.rotation;
        }
    }

}