namespace Luzart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Luzart;
    
    public class BoxInforMess : ClickToOnDisable
    {
        private RectTransform _rtMe = null;
        public RectTransform rectTransform
        {
            get
            {
                if (_rtMe == null)
                {
                    _rtMe = GetComponent<RectTransform>();
                }
                return _rtMe;
            }
        }
        public BaseSelect baseBox;
        public ListResUI listResUI;
        public void InitMess(EStateClaim state, params DataResource[] dataRes )
        {
            switch (state)
            {
                case EStateClaim.Chest:
                    {
                        baseBox?.Select(3);
                        if( listResUI != null )
                        {
                            listResUI.InitResUI(dataRes);
                        }
                        break;
                    }
                case EStateClaim.CanClaim:
                    {
                        baseBox?.Select(4);
                        break;
                    }
                case EStateClaim.WillClaim:
                    {
                        baseBox?.Select(1);
                        break;
                    }
                case EStateClaim.Claimed:
                    {
                        baseBox?.Select(0);
                        break;
                    }
                case EStateClaim.NeedIAP:
                    {
                        baseBox?.Select(2);
                        break;
                    }
    
    
            }
    
        }
       
    }
}
