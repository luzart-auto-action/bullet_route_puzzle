namespace Luzart
{
    using Luzart;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    
    public static class DataWrapperGame
    {
        public static int CurrentLevel
        {
            get
            {
                int level = 100;
                return level;
            }
        }
        public static Difficulty diff
        {
            get
            {
                Debug.LogError($"[DataWrapperGame] Don't have level difficult to check ");
                return Difficulty.Normal;
            }
        }
        public static string NamePlayer
        {
            get
            {
                Debug.LogError($"[DataWrapperGame] Don't have name player to check ");
                return "Player";
            }
        }
    
        public static int IDAvatarPlayer
        {
            get
            {
                Debug.LogError($"[DataWrapperGame] Don't have id avatar player to check ");
                return 0;
            }
        }
        public static Sprite[] AllSpriteAvatars
        {
            get
            {
                Debug.LogError($"[DataWrapperGame] Don't have all sprite avatars to check ");
                return new Sprite[0];
            }
        }
        public static void ReceiveReward(string where = null, params DataResource[] dataResource)
        {
            foreach (var data in dataResource)
            {
                // Xử lý tài nguyên
                int valueAdd = Mathf.Abs(data.amount);
                ChangeResourceAmount(data, valueAdd);

                // Log sự kiện Firebase
                LogFirebaseEvent(data);
            }

            static void LogFirebaseEvent(DataResource data)
            {
#if FIREBASE_ENABLE
                FirebaseEvent.LogEvent(KeyFirebase.ResEarn, new Firebase.Analytics.Parameter[]
                {
            new Firebase.Analytics.Parameter(TypeFirebase.LevelID, UserLevel.GetCurrentLevel().ToString()),
            new Firebase.Analytics.Parameter(TypeFirebase.Type, data.type.ToString()),
            new Firebase.Analytics.Parameter(TypeFirebase.Amount, data.amount.ToString())
                });
#endif
            }
        }
        public static void ReceiveRewardShowPopUp(string where = null, Action onDone = null ,params DataResource [] dataResource)
        {
            ReceiveReward(where, dataResource);
            var ui = Luzart.UIManager.Instance.ShowUI<UIReceiveRes>(UIName.ReceiveRes, onDone);
            ui.Initialize(dataResources: dataResource);
    
        }
        public static void ReceiveRewardShowPopUpAnim(string where = null, Action onDone = null,bool isAnim = false, Vector3 posSpawn = default, params DataResource[] dataResource)
        {
            ReceiveReward(where, dataResource);
            var ui = Luzart.UIManager.Instance.ShowUI<UIReceiveRes>(UIName.ReceiveRes, onDone);
            ui.Initialize(isAnim,posSpawn, dataResource);

        }
        public static void SubtractResources(DataResource dataRes, Action onDone = null, string where = null)
        {
            int amount = GetResource(dataRes.type);
            if(amount >= dataRes.amount)
            {
                SubtractResources(dataRes);
                onDone?.Invoke();
            }
            else 
            {
                Luzart.UIManager.Instance.ShowToast("You need to earn enough to buy it !");
            }
        }
        private static void SubtractResources(DataResource data)
        {
            int valueSub = -Mathf.Abs(data.amount);
            ChangeResourceAmount(data, valueSub);
            static void LogFirebaseEvent(DataResource data)
            {
#if FIREBASE_ENABLE
                FirebaseEvent.LogEvent(KeyFirebase.ResSpend, new Firebase.Analytics.Parameter[]
                {
            new Firebase.Analytics.Parameter(TypeFirebase.LevelID, UserLevel.GetCurrentLevel().ToString()),
            new Firebase.Analytics.Parameter(TypeFirebase.Type, data.type.ToString()),
            new Firebase.Analytics.Parameter(TypeFirebase.Amount, data.amount.ToString())
                });
#endif
            }
        }

        // Phương thức lấy tài nguyên
        public static int GetResource(DataTypeResource data)
        {
            return 0;
        }
        // Phương thức thay đổi tài nguyên
        public static void ChangeResourceAmount(DataResource data, int amount)
        {
           
        }
        // Phương thức lấy tên tài nguyên
        public static string GetStringAtlas(DataTypeResource data)
        {
            return null;
        }

    }
    [System.Serializable]
    public enum EStateClaim
    {
        CanClaimDontClaimed,
        CanClaim,
        Claimed,
        WillClaim,
        NeedIAP,
        Chest,
    }
    [System.Serializable]
    public class DataResource
    {
        public DataResource()
        {
    
        }
        public DataResource(DataTypeResource type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }
        public DataTypeResource type;
        public int amount;
        [System.NonSerialized]
        public int idIcon = 0;
        [System.NonSerialized]
        public Sprite spIcon;
        public DataResource Clone()
        {
            return new DataResource(this.type, this.amount);
        }
    }
    [System.Serializable]
    public struct DataTypeResource
    {
        public DataTypeResource(RES_type type, int id = 0)
        {
            this.type = type;
            this.id = id;
        }
        public RES_type type;
        public int id;
        public bool Compare(DataTypeResource dataOther)
        {
            if (type == dataOther.type && id == dataOther.id)
            {
                return true;
            }
            return false;
        }
        public string GetKeyString(RES_type _type, int _id)
        {
            return $"{_type}_{_id}";
        }
        public override string ToString()
        {
            return $"{type}_{id}";
        }
        // Định nghĩa toán tử == để so sánh các instance của struct
        public static bool operator ==(DataTypeResource left, DataTypeResource right)
        {
            return left.type == right.type && left.id == right.id;
        }
        public static bool operator !=(DataTypeResource left, DataTypeResource right)
        {
            return left.type != right.type || left.id != right.id;
        }
    
    
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public enum RES_type
    {
        None = 0,
        Gold = 1,
        Heart = 2,
        HeartTime = 3,
        Booster = 4, // 0: Scale, 1: ShuffleTool, 2: VIP, 3: Sort
        Chest = 5, // 0: None, start for 1, 2 , 3,4
        Spin = 6,
    }
    [System.Serializable]
    public struct GroupDataResources
    {
        public List<DataResource> dataResources;
        public DataTypeResource typeChest;
        public bool IsHasChest
        {
            get
            {
                return typeChest.type == RES_type.Chest && typeChest.id != 0;
            }
        }
    }
    public enum Difficulty
    {
        Normal = 0,
        Hard = 1,
        SuperHard = 2,
    }
}
