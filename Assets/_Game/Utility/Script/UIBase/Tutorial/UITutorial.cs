namespace Luzart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class UITutorial : UIBase
    {
        public override void Show(Action onHideDone)
        {
            base.Show(onHideDone);
            UIManager.Instance.ShowScenario(UIName.Tut1);
        }
    }

}