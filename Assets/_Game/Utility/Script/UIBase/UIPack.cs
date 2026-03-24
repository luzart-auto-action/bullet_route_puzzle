////#define IAP_ENABLE
//namespace Luzart
//{
//#if IAP_ENABLE
//    using BG_Library.IAP;
//    using BG_Library.NET;
//    using UnityEngine.Purchasing;
//#endif
//    using System;
//    using System.Collections.Generic;
//    using System.Reflection;
//    using UnityEngine;
//    using UnityEngine.UI;
//#if FIREBASE_ENABLE
//    using Firebase.Analytics;
//#endif

//    public abstract class BaseUIPack : UIBase
//    {
//        [SerializeField]
//        protected DB_Pack db_Pack;
//        public Button btnBuy;
//        protected override void Setup()
//        {
//            isAnimBtnClose = true;
//            base.Setup();
//            GameUtil.ButtonOnClick(btnBuy, BuyIAP, true);
    
//        }
//        public override void Show(Action onHideDone)
//        {
//            base.Show(onHideDone);
    
//            RefreshUI();
//        }
//        public override void RefreshUI()
//        {
//            base.RefreshUI();
//            GetDBIAP();
//            if (IsHasBuyPack())
//            {
//                Hide();
//                return;
//            }
//            InitIAP();
//        }
//        public virtual bool IsHasBuyPack()
//        {
//            return PackManager.Instance.IsHasBuyPack(db_Pack.productId) && db_Pack.maxBuy <= PackManager.Instance.GetPackPurchaseCount(db_Pack.productId);
//        }
//        public virtual void GetDBIAP()
//        {
//            string where = db_Pack.where;

//            if (!String.IsNullOrEmpty(db_Pack.productId))
//            {
//                db_Pack = PackManager.Instance.dbPackSO.GetDBPack(db_Pack.productId);
//            }
//            else
//            {
//                db_Pack = PackManager.Instance.dbPackSO.GetDBPack(db_Pack.ePack);
//            }

//            db_Pack.where = where;
//        }
//        public virtual void InitIAP()
//        {
    
//        }
//        public virtual void BuyIAP()
//        {
//#if IAP_ENABLE
//            IAPManager.PurchaseResultListener += OnPurchaseComplete;
//            SetBuyProduct();
//#endif
//        }

//        private void SetBuyProduct()
//        {
//#if IAP_ENABLE
//            IAPManager.PurchaseProduct(db_Pack.where, db_Pack.productId);
//#endif
//        }
//#if IAP_ENABLE
//        private void OnPurchaseComplete(IAPPurchaseResult iappurchaseresult)
//        {
//            IAPManager.PurchaseResultListener -= OnPurchaseComplete;
//            switch (iappurchaseresult.Result)
//            {
//                case IAPPurchaseResult.EResult.Complete:
//                    OnCompleteBuy();
//                    FirebaseEvent.LogEvent($"IAP_success_custom",
//                    new Parameter("location", iappurchaseresult.Position),
//                    new Parameter("package_id", iappurchaseresult.Product.TrackingId),
//                    new Parameter("value", (float)iappurchaseresult.PurchasedProduct.metadata.localizedPrice),
//                    new Parameter("currency", iappurchaseresult.PurchasedProduct.metadata.isoCurrencyCode));
//                    new Parameter("level_id", UserLevel.GetCurrentLevel());
//                    break;
//                case IAPPurchaseResult.EResult.WrongInstance:
//                    break;
//                case IAPPurchaseResult.EResult.WrongProduct:
//                    break;
//                case IAPPurchaseResult.EResult.WrongStoreController:
//                    break;
//            }
//        }
//#endif
//        protected virtual void OnCompleteBuy()
//        {
//            PackManager.Instance.SaveBuyPack(db_Pack.productId);
//            UIManager.Instance.RefreshUI();
//        }
//    }
//    public class UIPack : BaseUIPack
//    {
    
//        public ListResUI listResUI;
//        public ResUI[] resOther;
//        public bool isAutoPushFirebaseIAPShow = true;
    
//        public override void InitIAP()
//        {
//            base.InitIAP();
//            if (db_Pack.gift.dataResources != null && db_Pack.gift.dataResources.Count > 0)
//            {
//                listResUI.InitResUI(db_Pack.gift.dataResources.ToArray());
//            }
    
//            if (db_Pack.resourcesOther.dataResources != null && db_Pack.resourcesOther.dataResources.Count > 0 &&
//                resOther != null && resOther.Length > 0)
//            {
//                int length = db_Pack.resourcesOther.dataResources.Count;
//                int lengthRes = resOther.Length;
//                int min = Mathf.Min(length, lengthRes);
//                for (int i = 0; i < min; i++)
//                {
//                    resOther[i].InitData(db_Pack.resourcesOther.dataResources[i]);
//                }
//            }
//            if (isAutoPushFirebaseIAPShow)
//            {
//                PushFirebaseShow();
//            }
    
//        }
//        protected override void OnCompleteBuy()
//        {
//            List<DataResource> listReward = new List<DataResource>();
//            if (db_Pack.gift.dataResources != null)
//            {
//                listReward.AddRange(db_Pack.gift.dataResources);
//            }
//            if (db_Pack.resourcesOther.dataResources != null)
//            {
//                listReward.AddRange(db_Pack.resourcesOther.dataResources);
//            }
//            if (listReward.Count > 0)
//            {
//                DataWrapperGame.ReceiveRewardShowPopUp(db_Pack.productId, OnCompleteBuyIAP, dataResource: listReward.ToArray());
//            }
//            if (db_Pack.isRemoveAds)
//            {
//                AdsWrapperManager.PurchaseRemoveAds();
//            }
//            base.OnCompleteBuy();
//        }
//        protected virtual void OnCompleteBuyIAP()
//        {
//            PackManager.Instance.CalculateShowPack();
//            UIManager.Instance.RefreshUI();
//            var ui = UIManager.Instance.GetUiActive(uiName);
//            if (ui != null)
//            {
//                Hide();
//            }
    
    
//        }
//        public override void Hide()
//        {
//            base.Hide();
//            HideCallAnimFly();
    
//        }
//        public virtual void HideCallAnimFly()
//        {
//            //var ui = UIManager.Instance.GetUIMainMenuHome();
//            //if (ui != null)
//            //{
//            //    ui.ShowDataCache();
//            //}
//        }
    
//        public virtual void PushFirebaseShow()
//        {
//            if (db_Pack == null)
//            {
//                return;
//            }
//            //string strPack = FirebaseWrapperLog.GetStringIAP(db_Pack.ePack);
//            //FirebaseWrapperLog.LogIAPShow(TypeFirebase.TypeShowIAP.PopUp, strPack);
//        }
    
//    }
    
//    public class PackItem : MonoBehaviour
//    {
//        public DB_Pack db_Pack;
//        public Button btnBuy;
//        protected virtual void Awake()
//        {
//            GameUtil.ButtonOnClick(btnBuy, BuyIAP, true);
//        }
//        public virtual void Initialize()
//        {
//            GetDBIAP();
//            if (IsHasBuyPack())
//            {
//                Hide();
//                return;
//            }
//            InitIAP();
//        }
//        public virtual bool IsHasBuyPack()
//        {
//            return PackManager.Instance.IsHasBuyPack(db_Pack.productId) && db_Pack.maxBuy <= PackManager.Instance.GetPackPurchaseCount(db_Pack.productId);
//        }
//        public virtual void GetDBIAP()
//        {
//            string where = db_Pack.where;

//            if (!String.IsNullOrEmpty(db_Pack.productId))
//            {
//                db_Pack = PackManager.Instance.dbPackSO.GetDBPack(db_Pack.productId);
//            }
//            else
//            {
//                db_Pack = PackManager.Instance.dbPackSO.GetDBPack(db_Pack.ePack);
//            }

//            db_Pack.where = where;

//        }
//        public virtual void InitIAP()
//        {
    
//        }
//        public virtual void BuyIAP()
//        {
//#if IAP_ENABLE
//            IAPManager.PurchaseResultListener += OnPurchaseComplete;
//            SetBuyProduct();
//#endif
//        }
//        private void SetBuyProduct()
//        {
//#if IAP_ENABLE
//            IAPManager.PurchaseProduct(db_Pack.where, db_Pack.productId);
//#endif
//        }
//#if IAP_ENABLE
//        private void OnPurchaseComplete(IAPPurchaseResult iappurchaseresult)
//        {
//            IAPManager.PurchaseResultListener -= OnPurchaseComplete;
//            switch (iappurchaseresult.Result)
//            {
//                case IAPPurchaseResult.EResult.Complete:
//                    OnCompleteBuy();
//                    FirebaseEvent.LogEvent($"IAP_success_custom",
//                    new Parameter("location", iappurchaseresult.Position),
//                    new Parameter("package_id", iappurchaseresult.Product.TrackingId),
//                    new Parameter("value", (float)iappurchaseresult.PurchasedProduct.metadata.localizedPrice),
//                    new Parameter("currency", iappurchaseresult.PurchasedProduct.metadata.isoCurrencyCode));
//                    new Parameter("level_id", UserLevel.GetCurrentLevel());
//                    // iappurchaseresult.Product.Reward - Reward setup in stats
//                    // iappurchaseresult.Product.Reward.PackRewardValue - give reward amount
//                    // iappurchaseresult.Product.Reward.Reward - Type Reward > REMOVE_AD, CURRENCY (CASH OR GOLD), CUSTOM (Item Or Tool)
//                    // iappurchaseresult.Product.Reward.atlas - Reward give Currency Id or Item, Tool Id (example: CASH, GOLD, TOOL_1...)
//                    // todo give product reward
//                    break;
//                case IAPPurchaseResult.EResult.WrongInstance:
//                    // Purchase faield: IAP Manager instance null (Read Setup IAP)  
//                    break;
//                case IAPPurchaseResult.EResult.WrongProduct:
//                    // Purchase faield: can't find product with id 
//                    break;
//                case IAPPurchaseResult.EResult.WrongStoreController:
//                    // Purchase faield: IAP initialized faield
//                    break;
//            }
//        }
//#endif
//        protected virtual void OnCompleteBuy()
//        {
//            if (db_Pack.isRemoveAds)
//            {
//                AdsWrapperManager.PurchaseRemoveAds();
//            }
//            UIManager.Instance.RefreshUI();
//        }
//        protected virtual void Hide()
//        {
//            gameObject.SetActive(false);
//        }
//    }
//}
