namespace Luzart
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using System;
    using UnityEngine.UI;
    using Unity.VisualScripting;

    public class UIManager : Singleton<UIManager>
    {
        private const string PATH_UI = "";
        //public UITop topUI;
        public Transform[] rootOb;
        public UIBase[] listSceneCache;

        public Canvas canvas;
        public GraphicRaycaster graphicRaycaster;

        private List<UIBase> listScreenActive = new List<UIBase>();
        private Dictionary<UIName, UIBase> cacheScreen = new Dictionary<UIName, UIBase>();
        /// <summary>
        /// 0: UiController, shop, event, main, guild, free (on tab)
        /// 1: General (down top)
        /// 2: top, changeScene,login, ....
        /// 3: loading, NotiUpdate
        /// top: -1: not action || -2: Hide top || >=0: Show top
        /// </summary>
        private Dictionary<UIName, string> dir = new Dictionary<UIName, string>
    {
        // UIName, rootIdx,topIdx,loadPath
            {UIName.MainMenu,"0,0,UIMainMenu" },
            {UIName.Gameplay, "0,0,UIGameplay"},
            {UIName.Settings,"1,0,UISettings" },
            {UIName.WinClassic,"1,0,UIWinClassic" },
            {UIName.LoseClassic,"1,0,UILoseClassic" },
            {UIName.Splash,"0,0,UISplash" },
            {UIName.LoadScene,"3,0,UILoadScene" },
            {UIName.Toast,"4,0,UIToast" },
            {UIName.Noti,"4,0,UINoti" },

            {UIName.Tutorial,"1,0,UITutorial" },
            {UIName.Level1,"1,0,UILevel1" },
            {UIName.Level2,"1,0,UILevel2" },
            {UIName.Level3,"1,0,UILevel3" },
            {UIName.Level4,"1,0,UILevel4" },
            {UIName.Loading,"2,0,UILoading" },
            {UIName.Profile,"2,0,UIProfile" },


            {UIName.Tut1,"1,0,Level/Tutorial/UITut1" },
            {UIName.Tut2,"1,0,Level/Tutorial/UITut2" },
            {UIName.Tut3,"1,0,Level/Tutorial/UITut3" },
            {UIName.Tut4,"1,0,Level/Tutorial/UITut4" },
            {UIName.Tut5,"1,0,Level/Tutorial/UITut5" },
            {UIName.Tut5_1,"1,0,Level/Tutorial/UITut5_1" },
            {UIName.Tut6,"1,0,Level/Tutorial/UITut6" },
            {UIName.Level1_0,"1,0,Level/Level1/UILevel1_0" },
            {UIName.Level1_1,"1,0,Level/Level1/UILevel1_1" },
            {UIName.Level1_1_1,"1,0,Level/Level1/UILevel1_1_1" },
            {UIName.Level1_1_2,"1,0,Level/Level1/UILevel1_1_2" },
            {UIName.Level1_1_3,"1,0,Level/Level1/UILevel1_1_3" },
            {UIName.Level1_2,"1,0,Level/Level1/UILevel1_2" },
            {UIName.Level1_2_1,"1,0,Level/Level1/UILevel1_2_1" },
            {UIName.Level1_2_2,"1,0,Level/Level1/UILevel1_2_2" },
            {UIName.Level1_2_3,"1,0,Level/Level1/UILevel1_2_3" },
            {UIName.Level1_2_4,"1,0,Level/Level1/UILevel1_2_4" },
            {UIName.Level1_3,"1,0,Level/Level1/UILevel1_3" },
            {UIName.Level1_3_1,"1,0,Level/Level1/UILevel1_3_1" },
            {UIName.Level1_3_2,"1,0,Level/Level1/UILevel1_3_2" },
            {UIName.Level1_3_3,"1,0,Level/Level1/UILevel1_3_3" },
            {UIName.Level1_3_4,"1,0,Level/Level1/UILevel1_3_4" },
            {UIName.Level1_3_5,"1,0,Level/Level1/UILevel1_3_5" },
            {UIName.Level1_3_6,"1,0,Level/Level1/UILevel1_3_6" },
            {UIName.Level1_4,"1,0,Level/Level1/UILevel1_4" },
            {UIName.Level1_4_1,"1,0,Level/Level1/UILevel1_4_1" },
            {UIName.Level1_5,"1,0,Level/Level1/UILevel1_5" },
            {UIName.Level1_6,"1,0,Level/Level1/UILevel1_6" },
            {UIName.Level2_1,"1,0,Level/Level2/UILevel2_1" },
            {UIName.Level2_1_1,"1,0,Level/Level2/UILevel2_1_1" },
            {UIName.Level2_2,"1,0,Level/Level2/UILevel2_2" },
            {UIName.Level2_2_1,"1,0,Level/Level2/UILevel2_2_1" },
            {UIName.Level2_3,"1,0,Level/Level2/UILevel2_3" },
            {UIName.Level2_3_1,"1,0,Level/Level2/UILevel2_3_1" },
            {UIName.Level2_4,"1,0,Level/Level2/UILevel2_4" },
            {UIName.Level2_5,"1,0,Level/Level2/UILevel2_5" },
            {UIName.Level2_6,"1,0,Level/Level2/UILevel2_6" },
            {UIName.Level3_1,"1,0,Level/Level3/UILevel3_1" },
            {UIName.Level3_1_1,"1,0,Level/Level3/UILevel3_1_1" },
            {UIName.Level3_2,"1,0,Level/Level3/UILevel3_2" },
            {UIName.Level3_3,"1,0,Level/Level3/UILevel3_3" },
            {UIName.Level3_4,"1,0,Level/Level3/UILevel3_4" },
            {UIName.Level3_5,"1,0,Level/Level3/UILevel3_5" },
            {UIName.Level3_6,"1,0,Level/Level3/UILevel3_6" },
            {UIName.Level3_7,"1,0,Level/Level3/UILevel3_7" },
            {UIName.Level4_1,"1,0,Level/Level4/UILevel4_1" },
            {UIName.Level4_1_1,"1,0,Level/Level4/UILevel4_1_1" },
            {UIName.Level4_1_2,"1,0,Level/Level4/UILevel4_1_2" },
            {UIName.Level4_1_3,"1,0,Level/Level4/UILevel4_1_3" },
            {UIName.Level4_2,"1,0,Level/Level4/UILevel4_2" },
            {UIName.Level4_2_1,"1,0,Level/Level4/UILevel4_2_1" },
            {UIName.Level4_2_2,"1,0,Level/Level4/UILevel4_2_2" },
            {UIName.Level4_3,"1,0,Level/Level4/UILevel4_3" },
            {UIName.Level4_3_1,"1,0,Level/Level4/UILevel4_3_1" },
            {UIName.Level4_3_2,"1,0,Level/Level4/UILevel4_3_2" },
            {UIName.Level4_3_3,"1,0,Level/Level4/UILevel4_3_3" },
            {UIName.Level4_4,"1,0,Level/Level4/UILevel4_4" },
            {UIName.Level4_4_1,"1,0,Level/Level4/UILevel4_4_1" },
            {UIName.Level4_5,"1,0,Level/Level4/UILevel4_5" },


    };
        private List<UIName> listScenario = new List<UIName>()
        {
            UIName.Tut1,
            UIName.Tut2,
            UIName.Tut3,
            UIName.Tut4,
            UIName.Tut5,
            UIName.Tut5_1,
            UIName.Tut6,


            UIName.Level1_0,
            UIName.Level1_1,
            UIName.Level1_1_1,
            UIName.Level1_1_2,
            UIName.Level1_1_3,
            UIName.Level1_2,
            UIName.Level1_2_1,
            UIName.Level1_2_2,
            UIName.Level1_2_3,
            UIName.Level1_2_4,
            UIName.Level1_3,
            UIName.Level1_3_1,
            UIName.Level1_3_2,
            UIName.Level1_3_3,
            UIName.Level1_3_4,
            UIName.Level1_3_5,
            UIName.Level1_3_6,
            UIName.Level1_4,
            UIName.Level1_4_1,
            UIName.Level1_5,
            UIName.Level1_6,



            UIName.Level2_1,
            UIName.Level2_1_1,
            UIName.Level2_2,
            UIName.Level2_2_1,
            UIName.Level2_3,
            UIName.Level2_3_1,
            UIName.Level2_4,
            UIName.Level2_5,
            UIName.Level2_6,


            UIName.Level3_1,
            UIName.Level3_1_1,
            UIName.Level3_2,
            UIName.Level3_3,
            UIName.Level3_4,
            UIName.Level3_5,
            UIName.Level3_6,
            UIName.Level3_7,


            UIName.Level4_1,
            UIName.Level4_1_1,
            UIName.Level4_1_2,
            UIName.Level4_1_3,
            UIName.Level4_2,
            UIName.Level4_2_1,
            UIName.Level4_2_2,
            UIName.Level4_3,
            UIName.Level4_3_1,
            UIName.Level4_3_2,
            UIName.Level4_3_3,
            UIName.Level4_4,
            UIName.Level4_4_1,
            UIName.Level4_5
        };
        private UIName currentScenario = UIName.Tut1;
        public void ShowScenario(UIName uiName)
        {
            var ui = ShowScenario<UIBase>(uiName);
        }
        public T ShowScenario<T>(UIName uiName) where T : UIBase
        {
            currentScenario = uiName;
            return ShowUI<T>(currentScenario);
        }
        public T ShowNextScenario<T>() where T : UIBase
        {
            var idx = listScenario.FindIndex(x => x == currentScenario);
            var nextIdx = idx + 1;
            currentScenario = listScenario[nextIdx];
            return ShowScenario<T>(currentScenario);
        }
        public T ShowBackScenario<T>() where T : UIBase
        {
            var idx = listScenario.FindIndex(x => x == currentScenario);
            var nextIdx = idx - 1;
            currentScenario = listScenario[nextIdx];
            return ShowScenario<T>(currentScenario);
        }
        public void ShowNextScenario()
        {
            HideUiActive(currentScenario);
            var ui = ShowNextScenario<UIBase>();
        }
        public void ShowBackScenario()
        {
            HideUiActive(currentScenario);
            var ui = ShowBackScenario<UIBase>();
        }
        private Dictionary<UIName, DataUIBase> dic2;

        public UIName CurrentName { get; private set; }
        public bool IsAction { get; set; }
        private void Awake()
        {
            canvas ??= GetComponent<Canvas>();
            graphicRaycaster ??= GetComponent<GraphicRaycaster>();
            dic2 = new Dictionary<UIName, DataUIBase>();
            foreach (var i in dir)
            {
                if (!dic2.ContainsKey(i.Key))
                {
                    var t = i.Value.Split(',');
                    dic2.Add(i.Key, new DataUIBase(int.Parse(t[0]), int.Parse(t[1]), t[2]));
                }
            }
            for (int i = 0; i < listSceneCache.Length; i++)
            {
                if (!cacheScreen.ContainsKey(listSceneCache[i].uiName))
                {
                    cacheScreen.Add(listSceneCache[i].uiName, listSceneCache[i]);
                }
            }
            Observer.Instance.AddObserver(ObserverKey.BlockRaycast, BlockRaycast);
            //if (SdkUtil.isiPad())
            //{
            //    GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;
            //}
            //else
            //{
            //    GetComponent<CanvasScaler>().matchWidthOrHeight = 0f;
            //}
            IsAction = false;
        }
        private void OnDestroy()
        {
            Observer.Instance.RemoveObserver(ObserverKey.BlockRaycast, BlockRaycast);
        }
        public void ShowUI(UIName uIScreen, Action onHideDone = null, bool isNeedCheck = true)
        {
            ShowUI<UIBase>(uIScreen, onHideDone, isNeedCheck);
        }
        public T ShowUI<T>(UIName uIScreen, Action onHideDone = null, bool isNeedCheck = true) where T : UIBase
        {

            UIBase current = listScreenActive.Find(x => x.uiName == uIScreen);
            if (!current)
            {
                current = LoadUI(uIScreen);
                current.uiName = uIScreen;
                AddScreenActive(current, true);
            }
            current.transform.SetAsLastSibling();
            current.Show(onHideDone);
            CurrentName = uIScreen;
            return current as T;
        }
        public void ShowToast(string toast)
        {
            var ui = ShowUI<UIToast>(UIName.Toast, isNeedCheck: false);
            ui.Init(toast);
        }
        private void AddScreenActive(UIBase current, bool isTop)
        {
            var idx = listScreenActive.FindIndex(x => x.uiName == current.uiName);
            if (isTop)
            {
                if (idx >= 0)
                {
                    listScreenActive.RemoveAt(idx);
                }
                listScreenActive.Add(current);
            }
            else
            {
                if (idx < 0)
                {
                    listScreenActive.Add(current);
                }
            }
        }
        //public void LoadScene(Action onLoad, Action onDone, float timeLoad = 0.75f, float timeHide = 0.25f)
        //{
        //    UILoadScene uILoadScene = ShowUI<UILoadScene>(UIName.LoadScene);
        //    uILoadScene.LoadSceneCloud(onLoad, onDone, timeLoad, timeHide);
        //}

        private static Action actionRefreshUI = null;
        public static void AddActionRefreshUI(Action callBack)
        {
            actionRefreshUI += callBack;
        }
        public static void RemoveActionRefreshUI(Action callBack)
        {
            actionRefreshUI -= callBack;
        }
        public void RefreshUI()
        {
            var idx = 0;
            while (listScreenActive.Count > idx)
            {
                listScreenActive[idx].RefreshUI();
                idx++;
            }
            actionRefreshUI?.Invoke();
            //topUI.RefreshUI();
            //GameManager.OnRefreshUI?.Invoke();
        }

        //private UIToast _uiToast;
        //public UIToast UIToast()
        //{
        //    if (_uiToast == null)
        //    {
        //        _uiToast = GetComponentInChildren<UIToast>();
        //    }
        //    return _uiToast;
        //}

        public T GetUI<T>(UIName uIScreen) where T : UIBase
        {
            var c = LoadUI(uIScreen);
            return c as T;
        }

        public UIBase GetUI(UIName uIScreen)
        {
            return LoadUI(uIScreen);
        }

        public UIBase GetUiActive(UIName uIScreen)
        {
            return listScreenActive.Find(x => x.uiName == uIScreen);
        }

        public T GetUiActive<T>(UIName uIScreen) where T : UIBase
        {
            var ui = listScreenActive.Find(x => x.uiName == uIScreen);
            if (ui)
            {
                return ui as T;
            }
            else
            {
                return default;
            }
        }

        private UIBase LoadUI(UIName uIScreen)
        {
            UIBase current = null;
            if (cacheScreen.ContainsKey(uIScreen))
            {
                current = cacheScreen[uIScreen];
                if (current == null)
                {
                    var idx = dic2[uIScreen].rootIdx;
                    var pf = Resources.Load<UIBase>(PATH_UI + dic2[uIScreen].loadPath);
                    current = Instantiate(pf, rootOb[idx]);
                    cacheScreen[uIScreen] = current;
                }
            }
            else
            {
                var idx = dic2[uIScreen].rootIdx;
                var pf = Resources.Load<UIBase>(PATH_UI + dic2[uIScreen].loadPath);
                current = Instantiate(pf, rootOb[idx]);
                cacheScreen.Add(uIScreen, current);
            }
            return current;
        }

        public void RemoveActiveUI(UIName uiName)
        {
            var idx = listScreenActive.FindIndex(x => x.uiName == uiName);
            if (idx >= 0)
            {
                var ui = listScreenActive[idx];
                listScreenActive.RemoveAt(idx);
                if (!ui.isCache && cacheScreen.ContainsKey(uiName))
                {
                    cacheScreen[uiName] = null;
                }
            }
        }

        public void HideAllUIIgnore(UIName uiName = UIName.LoadScene)
        {
            int length = listScreenActive.Count;
            for (int i = 0; i < length; i++)
            {
                if (listScreenActive.Count == 0)
                {
                    continue;
                }
                HideUIIgnore(listScreenActive[0]);
            }
            void HideUIIgnore(UIBase uiBase)
            {
                if (uiBase.uiName != uiName)
                {
                    uiBase.Hide();
                }
            }
        }

        public void HideAll()
        {
            while (listScreenActive.Count > 0)
            {
                listScreenActive[0].Hide();
            }
            //topUI.Hide();
        }
        public void HideAllUiActive()
        {
            while (listScreenActive.Count > 0)
            {
                listScreenActive[0].Hide();
            }
        }

        public void HideAllUiActive(params UIName[] ignoreUI)
        {
            for (int i = 0; i < listScreenActive.Count; i++)
            {
                for (int j = 0; j < ignoreUI.Length; j++)
                {
                    if (listScreenActive[i].uiName != ignoreUI[j])
                    {
                        listScreenActive[i].Hide();
                    }
                }
            }
        }

        public void HideUiActive(UIName uiName)
        {
            var ui = listScreenActive.Find(x => x.uiName == uiName);
            if (ui)
            {
                ui.Hide();
            }
        }
        public void ShowToastInternet()
        {
            ShowToast(KeyToast.NoInternetLoadAds);
        }

        public UIBase GetLastUiActive()
        {
            if (listScreenActive.Count == 0) return null;
            return listScreenActive.Last();
        }

        private void BlockRaycast(object data = null)
        {
            if (data == null)
            {
                return;
            }
            bool isBlock = (bool)data;
            graphicRaycaster.enabled = !isBlock;
        }
        public void BlockRaycast(bool isBlock)
        {
            graphicRaycaster.enabled = !isBlock;
        }
        public void ShowLoading()
        {
            ShowUI(UIName.Loading, isNeedCheck: false);
        }
        public void HideLoading()
        {
            HideUiActive(UIName.Loading);
        }
    }

    public enum UIName
    {
        None = 0,
        Gameplay = 1,
        Settings = 2,
        MainMenu = 3,
        WinClassic = 4,
        LoseClassic = 5,
        Splash = 6,
        LoadScene = 7,
        Toast = 8,
        Noti = 9,
        ReceiveRes = 10,
        Level1 = 11,
        Level2 = 12,
        Level3 = 13,
        Tutorial = 14,
        Loading = 15,
        Level4 = 16,
        Profile = 17,

        //Tutorial =50,
        //Tutorial
        Tut1 = 100,
        Tut2 = 200,
        Tut3 = 300,
        Tut4 = 400,
        Tut5 = 500,
        Tut5_1 = 501,
        Tut6 = 600,
        
        Level1_0 = 1000,
        Level1_1 = 1100,
        Level1_1_1 = 1110,
        Level1_1_2 = 1120,
        Level1_1_3 = 1130,
        Level1_2 = 1200,
        Level1_2_1 = 1210,
        Level1_2_2 = 1220,
        Level1_2_3 = 1230,
        Level1_2_4 = 1240,
        Level1_3 = 1300,
        Level1_3_1 = 1310,
        Level1_3_2 = 1320,
        Level1_3_3 = 1330,
        Level1_3_4 = 1340,
        Level1_3_5 = 1350,
        Level1_3_6 = 1360,
        Level1_4 = 1400,
        Level1_4_1 = 1410,
        Level1_5 = 1500,
        Level1_6 = 1600,
        Level2_1 = 2100,
        Level2_1_1 = 2110,
        Level2_2 = 2200,
        Level2_2_1 = 2210,
        Level2_3 = 2300,
        Level2_3_1 = 2310,
        Level2_4 = 2400,
        Level2_5 = 2500,
        Level2_6 = 2600,
        Level3_1 = 3100,
        Level3_1_1 = 3110,
        Level3_2 = 3200,
        Level3_3 = 3300,
        Level3_4 = 3400,
        Level3_5 = 3500,
        Level3_6 = 3600,
        Level3_7 = 3700,
        Level4_1 = 4100,
        Level4_1_1 = 4110,
        Level4_1_2 = 4120,
        Level4_1_3 = 4130,
        Level4_2 = 4200,
        Level4_2_1 = 4210,
        Level4_2_2 = 4220,
        Level4_3 = 4300,
        Level4_3_1 = 4310,
        Level4_3_2 = 4320,
        Level4_3_3 = 4330,
        Level4_4 = 4400,
        Level4_4_1 = 4410,
        Level4_5 = 4500,
    }
    public class DataUIBase
    {
        public int rootIdx;
        public int topIdx;
        public string loadPath;

        public DataUIBase(int rootIdx, int topIdx, string loadPath)
        {
            this.rootIdx = rootIdx;
            this.topIdx = topIdx;
            this.loadPath = loadPath;
        }
    }

}
