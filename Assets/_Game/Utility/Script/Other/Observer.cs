namespace Luzart
{
    using System.Collections.Generic;
    using UnityEngine;
    
    public class Observer : Singleton<Observer>
    {
        public delegate void CallBackObserver(object data);
    
        Dictionary<string, HashSet<CallBackObserver>> dictObserver = new Dictionary<string, HashSet<CallBackObserver>>();
        // Use this for initialization
        public void AddObserver(string topicName, CallBackObserver callbackObserver)
        {
            HashSet<CallBackObserver> listObserver = CreateListObserverForTopic(topicName);
            listObserver.Add(callbackObserver);
        }
    
        public void RemoveObserver(string topicName, CallBackObserver callbackObserver)
        {
            HashSet<CallBackObserver> listObserver = CreateListObserverForTopic(topicName);
            if (listObserver.Contains(callbackObserver))
            {
                listObserver.Remove(callbackObserver);
            }
        }
    
        public void Notify(string topicName, object Data)
        {
            HashSet<CallBackObserver> listObserver = CreateListObserverForTopic(topicName);
            HashSet<CallBackObserver> listObserverClone = new HashSet<CallBackObserver>(listObserver);
            foreach (CallBackObserver observer in listObserverClone)
            {
                observer(Data);
            }
        }
    
        public void Notify(string topicName)
        {
            Notify(topicName, null);
        }
    
        protected HashSet<CallBackObserver> CreateListObserverForTopic(string topicName)
        {
            if (!dictObserver.ContainsKey(topicName))
                dictObserver.Add(topicName, new HashSet<CallBackObserver>());
            return dictObserver[topicName];
        }
    }
    public static class ObserverKey
    {
        public const string TimeActionPerSecond = "TimeActionPerSecond";
        public const string CoinObserverNormal = "CoinObserverNormal";
        public const string CoinObserverTextRun = "CoinObserverTextRun";
        public const string CoinObserverDontAuto = "CoinObserverDontAuto";
        public const string OnNewDay = "OnNewDay";
        public const string QuestKey = "QuestKey";
        public const string OnDoneATrayTrue = "OnDoneATrayTrue";
        public const string OnCompleteStage = "OnCompleteStage";
        public const string OnTutorial = "OnTutorial";
        public const string OnChangeAmountBooster = "OnChangeAmountBooster";
        public const string RegisterBoosterRocket = "RegisterBoosterRocket";
        public const string OnAddTicket = "OnAddTicket";
        public const string OnCheckNotiTreasure = "OnCheckNotiTreasure";
        public const string OnChangeVisualEventRacing = "OnChangeVisualEventRacing";
        public const string BlockRaycast = "BlockRaycast";
        public const string PersonaDataChange = "PersonaDataChange";
        public const string OnUpdateNewChat = "OnUpdateNewChat";

    }
}
