using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using BulletRoute.Core;
using BulletRoute.Grid;
using BulletRoute.Tile;
using BulletRoute.Bullet;
using BulletRoute.Level;
using BulletRoute.GameState;
using BulletRoute.FX;
using BulletRoute.Audio;
using BulletRoute.Input;
using BulletRoute.CameraSystem;
using BulletRoute.UI;
using BulletRoute.Data;
using BulletRoute.Timer;

namespace BulletRoute.Editor
{
    public class SceneSetupWizard : EditorWindow
    {
        private bool _setupManagers = true;
        private bool _setupCamera = true;
        private bool _setupGridCollider = true;
        private bool _setupUI = true;
        private bool _setupLighting = true;
        private Vector2 _scrollPos;

        [MenuItem("BulletRoute/Scene Setup Wizard", false, 0)]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupWizard>("Scene Setup Wizard").minSize = new Vector2(350, 500);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(10);
            GUILayout.Label("BULLET ROUTE - SCENE SETUP", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tao tat ca GameObjects can thiet trong scene chi voi 1 click.", MessageType.Info);
            EditorGUILayout.Space(5);

            _setupManagers = EditorGUILayout.Toggle("Managers (Core Systems)", _setupManagers);
            _setupCamera = EditorGUILayout.Toggle("Camera + CameraController", _setupCamera);
            _setupGridCollider = EditorGUILayout.Toggle("Grid Collider (Raycast)", _setupGridCollider);
            _setupUI = EditorGUILayout.Toggle("UI Canvas + Panels", _setupUI);
            _setupLighting = EditorGUILayout.Toggle("Directional Light", _setupLighting);

            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("SETUP FULL SCENE", GUILayout.Height(40)))
            {
                SetupFullScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(15);
            GUILayout.Label("SETUP TUNG PHAN", EditorStyles.boldLabel);

            if (GUILayout.Button("Chi tao Managers")) SetupManagers();
            if (GUILayout.Button("Chi tao Camera")) SetupCamera();
            if (GUILayout.Button("Chi tao Grid Collider")) SetupGridCollider();
            if (GUILayout.Button("Chi tao UI Canvas + Panels")) SetupUI();

            EditorGUILayout.Space(15);
            GUILayout.Label("SCRIPTABLE OBJECTS", EditorStyles.boldLabel);

            if (GUILayout.Button("Tao GameConfig Asset"))
                CreateGameConfig();
            if (GUILayout.Button("Tao Level Data Asset (moi)"))
                CreateLevelData();

            EditorGUILayout.Space(15);
            GUILayout.Label("UTILITIES", EditorStyles.boldLabel);

            if (GUILayout.Button("Tu dong gan References"))
                AutoAssignReferences();
            if (GUILayout.Button("Tim va xoa node_modules"))
                CleanNodeModules();

            EditorGUILayout.EndScrollView();
        }

        private void SetupFullScene()
        {
            Undo.SetCurrentGroupName("BulletRoute Scene Setup");

            if (_setupLighting) SetupLighting();
            if (_setupCamera) SetupCamera();
            if (_setupGridCollider) SetupGridCollider();
            if (_setupManagers) SetupManagers();
            if (_setupUI) SetupUI();

            AutoAssignReferences();

            Debug.Log("[BulletRoute] Scene setup hoan tat!");
            EditorUtility.DisplayDialog("Setup Complete",
                "Scene da duoc setup thanh cong!\n\n" +
                "Tiep theo:\n" +
                "1. BulletRoute > Tile Prefab Creator -> tao prefabs\n" +
                "2. BulletRoute > Level Editor -> thiet ke levels\n" +
                "3. BulletRoute > Project Validator -> kiem tra",
                "OK");
        }

        // ===================== MANAGERS =====================
        private void SetupManagers()
        {
            // GameManager
            var gm = FindOrCreate<GameManager>("[GameManager]");

            // GridManager
            var grid = FindOrCreate<GridManager>("[GridManager]");

            // TileFactory
            var factory = FindOrCreate<TileFactory>("[TileFactory]");

            // LevelManager
            var level = FindOrCreate<LevelManager>("[LevelManager]");

            // BulletSimulator
            var sim = FindOrCreate<BulletSimulator>("[BulletSimulator]");

            // BulletManager
            var bullet = FindOrCreate<BulletManager>("[BulletManager]");

            // GameStateManager
            var state = FindOrCreate<GameStateManager>("[GameStateManager]");

            // InputManager
            var input = FindOrCreate<InputManager>("[InputManager]");

            // FXManager
            var fx = FindOrCreate<FXManager>("[FXManager]");

            // AudioManager
            var audio = FindOrCreate<AudioManager>("[AudioManager]");

            // LevelTimer
            var timer = FindOrCreate<LevelTimer>("[LevelTimer]");

            Debug.Log("[BulletRoute] Managers created: 11 systems");
        }

        // ===================== CAMERA =====================
        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(go, "Create Camera");
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                go.tag = "MainCamera";
            }

            cam.transform.position = new Vector3(0, 10, -5);
            cam.transform.rotation = Quaternion.Euler(60, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.18f);

            if (cam.GetComponent<CameraController>() == null)
            {
                Undo.AddComponent<CameraController>(cam.gameObject);
            }

            Debug.Log("[BulletRoute] Camera setup done");
        }

        // ===================== GRID COLLIDER =====================
        private void SetupGridCollider()
        {
            // Ensure layer exists
            EnsureLayerExists("Grid");

            var existing = GameObject.Find("[GridCollider]");
            if (existing != null)
            {
                Debug.Log("[BulletRoute] GridCollider already exists");
                return;
            }

            var go = new GameObject("[GridCollider]");
            Undo.RegisterCreatedObjectUndo(go, "Create GridCollider");
            var col = go.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = new Vector3(20f, 0.1f, 20f);

            int gridLayer = LayerMask.NameToLayer("Grid");
            if (gridLayer >= 0)
                go.layer = gridLayer;

            Debug.Log("[BulletRoute] Grid Collider created. IMPORTANT: Set layer to 'Grid' in Inspector if not auto-set.");
        }

        // ===================== UI =====================
        private void SetupUI()
        {
            // Find or create Canvas
            var canvas = Object.FindObjectOfType<Canvas>();
            GameObject canvasGO;

            if (canvas == null)
            {
                canvasGO = new GameObject("[UICanvas]");
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;

                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGO.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasGO = canvas.gameObject;
            }

            // Ensure EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // UIManager
            var uiMgr = canvasGO.GetComponentInChildren<UIManager>();
            if (uiMgr == null)
            {
                var uiMgrGO = new GameObject("[UIManager]");
                Undo.RegisterCreatedObjectUndo(uiMgrGO, "Create UIManager");
                uiMgrGO.transform.SetParent(canvasGO.transform, false);
                uiMgr = uiMgrGO.AddComponent<UIManager>();
            }

            // MainMenu
            CreateUIPanel_MainMenu(canvasGO.transform);

            // GameplayUI
            CreateUIPanel_Gameplay(canvasGO.transform);

            // WinPanel
            CreateUIPanel_Win(canvasGO.transform);

            // FailPanel
            CreateUIPanel_Fail(canvasGO.transform);

            // PopupSettings
            CreateUIPanel_PopupSettings(canvasGO.transform);

            // PopupPause
            CreateUIPanel_PopupPause(canvasGO.transform);

            Debug.Log("[BulletRoute] UI setup done (Canvas + 6 panels)");
        }

        private void CreateUIPanel_Gameplay(Transform canvasRoot)
        {
            if (canvasRoot.Find("GameplayUI") != null) return;

            var go = CreateUIElement("GameplayUI", canvasRoot, true);
            go.AddComponent<GameplayUI>();
            SetFullStretch(go.GetComponent<RectTransform>());

            // Top bar
            var topBar = CreateUIElement("TopBar", go.transform);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.9f);
            topRect.anchorMax = Vector2.one;
            topRect.offsetMin = new Vector2(20, 0);
            topRect.offsetMax = new Vector2(-20, -20);

            var levelText = CreateTMPText("LevelText", topBar.transform, "Level 1", 36, TextAlignmentOptions.Left);
            var timerText = CreateTMPText("TimerText", topBar.transform, "01:00", 36, TextAlignmentOptions.Center);
            var moveText = CreateTMPText("MoveCountText", topBar.transform, "0", 36, TextAlignmentOptions.Right);

            // Stars
            for (int i = 0; i < 3; i++)
            {
                var star = CreateUIElement($"Star_{i}", topBar.transform);
                var img = star.AddComponent<Image>();
                img.color = new Color(1f, 0.85f, 0f);
                var rt = star.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(50, 50);
            }

            // Bottom bar
            var bottomBar = CreateUIElement("BottomBar", go.transform);
            var botRect = bottomBar.GetComponent<RectTransform>();
            botRect.anchorMin = Vector2.zero;
            botRect.anchorMax = new Vector2(1, 0.1f);
            botRect.offsetMin = new Vector2(20, 20);
            botRect.offsetMax = new Vector2(-20, 0);

            var hlg = bottomBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;

            CreateButton("FireButton", bottomBar.transform, "FIRE", new Color(0.9f, 0.3f, 0.2f));
            CreateButton("ResetButton", bottomBar.transform, "RESET", new Color(0.8f, 0.4f, 0.2f));
            CreateButton("UndoButton", bottomBar.transform, "UNDO", new Color(0.4f, 0.6f, 0.9f));
            CreateButton("HintButton", bottomBar.transform, "HINT", new Color(1f, 0.8f, 0.2f));
            CreateButton("PauseButton", topBar.transform, "II", new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateUIPanel_Win(Transform canvasRoot)
        {
            if (canvasRoot.Find("WinPanel") != null) return;

            var go = CreateUIElement("WinPanel", canvasRoot, true);
            SetFullStretch(go.GetComponent<RectTransform>());
            var panel = go.AddComponent<WinPanel>();
            go.AddComponent<CanvasGroup>();

            // Set panel name via SerializedObject
            var so = new SerializedObject(panel);
            so.FindProperty("_panelName").stringValue = "WinPanel";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Background overlay
            var bg = CreateUIElement("Background", go.transform);
            SetFullStretch(bg.GetComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);

            // Panel card
            var card = CreateUIElement("Panel", go.transform);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = Color.white;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(800, 900);

            CreateTMPText("LevelCompleteText", card.transform, "Level Complete!", 48, TextAlignmentOptions.Center);
            CreateTMPText("TimeRemainingText", card.transform, "Time: 00:00", 32, TextAlignmentOptions.Center);
            CreateTMPText("MoveCountText", card.transform, "Moves: 0", 32, TextAlignmentOptions.Center);

            for (int i = 0; i < 3; i++)
            {
                var star = CreateUIElement($"Star_{i}", card.transform);
                star.AddComponent<Image>().color = new Color(1f, 0.85f, 0f);
                star.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
            }

            CreateButton("NextButton", card.transform, "NEXT", new Color(0.2f, 0.8f, 0.3f));
            CreateButton("RetryButton", card.transform, "RETRY", new Color(0.8f, 0.4f, 0.2f));
            CreateButton("HomeButton", card.transform, "HOME", new Color(0.5f, 0.5f, 0.5f));

            go.SetActive(false);
        }

        private void CreateUIPanel_Fail(Transform canvasRoot)
        {
            if (canvasRoot.Find("FailPanel") != null) return;

            var go = CreateUIElement("FailPanel", canvasRoot, true);
            SetFullStretch(go.GetComponent<RectTransform>());
            var panel = go.AddComponent<FailPanel>();
            go.AddComponent<CanvasGroup>();

            var so = new SerializedObject(panel);
            so.FindProperty("_panelName").stringValue = "FailPanel";
            so.ApplyModifiedPropertiesWithoutUndo();

            var bg = CreateUIElement("Background", go.transform);
            SetFullStretch(bg.GetComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.5f, 0, 0, 0.6f);

            var card = CreateUIElement("Panel", go.transform);
            card.AddComponent<Image>().color = Color.white;
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 600);

            CreateTMPText("FailText", card.transform, "Level Failed!", 48, TextAlignmentOptions.Center);

            var icon = CreateUIElement("FailIcon", card.transform);
            icon.AddComponent<Image>().color = Color.red;
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);

            CreateButton("RetryButton", card.transform, "RETRY", new Color(0.8f, 0.4f, 0.2f));
            CreateButton("HomeButton", card.transform, "HOME", new Color(0.5f, 0.5f, 0.5f));

            go.SetActive(false);
        }

        private void CreateUIPanel_MainMenu(Transform canvasRoot)
        {
            if (canvasRoot.Find("MainMenu") != null) return;

            var go = CreateUIElement("MainMenu", canvasRoot, true);
            var panel = go.AddComponent<UIMainMenu>();
            go.AddComponent<CanvasGroup>();
            SetFullStretch(go.GetComponent<RectTransform>());

            var so = new SerializedObject(panel);
            so.FindProperty("_panelName").stringValue = "MainMenu";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Background
            var bg = CreateUIElement("Background", go.transform);
            SetFullStretch(bg.GetComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.15f);

            // Title
            var title = CreateTMPText("TitleText", go.transform, "BULLET ROUTE", 64, TextAlignmentOptions.Center);
            title.color = Color.white;
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);

            // Level text
            var levelTxt = CreateTMPText("LevelText", go.transform, "Level 1", 32, TextAlignmentOptions.Center);
            levelTxt.color = new Color(0.8f, 0.8f, 0.8f);
            levelTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

            // Buttons
            var playBtn = CreateButton("PlayButton", go.transform, "PLAY", new Color(0.2f, 0.8f, 0.3f));
            playBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            playBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 100);

            var settingsBtn = CreateButton("SettingsButton", go.transform, "SETTINGS", new Color(0.4f, 0.4f, 0.6f));
            settingsBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
            settingsBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 80);
        }

        private void CreateUIPanel_PopupSettings(Transform canvasRoot)
        {
            if (canvasRoot.Find("PopupSettings") != null) return;

            var go = CreateUIElement("PopupSettings", canvasRoot, true);
            var panel = go.AddComponent<PopupSettings>();
            go.AddComponent<CanvasGroup>();
            SetFullStretch(go.GetComponent<RectTransform>());

            var so = new SerializedObject(panel);
            so.FindProperty("_panelName").stringValue = "PopupSettings";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Background overlay
            var bg = CreateUIElement("Background", go.transform);
            SetFullStretch(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // Card
            var card = CreateUIElement("Panel", go.transform);
            card.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 600);

            CreateTMPText("Title", card.transform, "SETTINGS", 40, TextAlignmentOptions.Center);

            // SFX Slider
            CreateTMPText("SFXLabel", card.transform, "SFX Volume", 24, TextAlignmentOptions.Left);
            var sfxSliderGO = CreateUIElement("SFXSlider", card.transform);
            var sfxSlider = sfxSliderGO.AddComponent<Slider>();
            sfxSliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 30);
            CreateTMPText("SFXValueText", card.transform, "100%", 20, TextAlignmentOptions.Right);

            // Music Slider
            CreateTMPText("MusicLabel", card.transform, "Music Volume", 24, TextAlignmentOptions.Left);
            var musicSliderGO = CreateUIElement("MusicSlider", card.transform);
            var musicSlider = musicSliderGO.AddComponent<Slider>();
            musicSliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 30);
            CreateTMPText("MusicValueText", card.transform, "50%", 20, TextAlignmentOptions.Right);

            CreateButton("CloseButton", card.transform, "CLOSE", new Color(0.5f, 0.5f, 0.5f));

            go.SetActive(false);
        }

        private void CreateUIPanel_PopupPause(Transform canvasRoot)
        {
            if (canvasRoot.Find("PopupPause") != null) return;

            var go = CreateUIElement("PopupPause", canvasRoot, true);
            var panel = go.AddComponent<PopupPause>();
            go.AddComponent<CanvasGroup>();
            SetFullStretch(go.GetComponent<RectTransform>());

            var so = new SerializedObject(panel);
            so.FindProperty("_panelName").stringValue = "PopupPause";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Background overlay
            var bg = CreateUIElement("Background", go.transform);
            SetFullStretch(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // Card
            var card = CreateUIElement("Panel", go.transform);
            card.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 500);

            CreateTMPText("Title", card.transform, "PAUSED", 48, TextAlignmentOptions.Center);

            CreateButton("ResumeButton", card.transform, "RESUME", new Color(0.2f, 0.8f, 0.3f));
            CreateButton("SettingsButton", card.transform, "SETTINGS", new Color(0.4f, 0.4f, 0.6f));
            CreateButton("HomeButton", card.transform, "HOME", new Color(0.8f, 0.3f, 0.3f));

            go.SetActive(false);
        }

        // ===================== SCRIPTABLE OBJECTS =====================
        private void CreateGameConfig()
        {
            string path = "Assets/_Game/ScriptableObjects/Configs";
            EnsureFolder(path);

            string assetPath = $"{path}/GameConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(assetPath) != null)
            {
                Debug.Log("[BulletRoute] GameConfig already exists!");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameConfig>(assetPath);
                return;
            }

            var config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            Debug.Log("[BulletRoute] GameConfig created at " + assetPath);
        }

        private void CreateLevelData()
        {
            string path = "Assets/_Game/ScriptableObjects/Levels";
            EnsureFolder(path);

            // Find next available index
            int index = 0;
            while (AssetDatabase.LoadAssetAtPath<LevelData>($"{path}/Level_{index + 1:D2}.asset") != null)
                index++;

            string assetPath = $"{path}/Level_{index + 1:D2}.asset";
            var data = ScriptableObject.CreateInstance<LevelData>();
            data.LevelIndex = index;
            data.LevelName = $"Level {index + 1}";

            // Set grid size based on progression
            if (index < 3) { data.GridWidth = 5; data.GridHeight = 5; }
            else if (index < 6) { data.GridWidth = 6; data.GridHeight = 6; }
            else if (index < 9) { data.GridWidth = 7; data.GridHeight = 7; }
            else if (index < 12) { data.GridWidth = 8; data.GridHeight = 8; }
            else if (index < 15) { data.GridWidth = 9; data.GridHeight = 9; }
            else { data.GridWidth = 10; data.GridHeight = 10; }

            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
            Debug.Log($"[BulletRoute] Level_{index + 1:D2} created at {assetPath} (Grid: {data.GridWidth}x{data.GridHeight})");
        }

        // ===================== AUTO ASSIGN =====================
        private void AutoAssignReferences()
        {
            // TileFactory: auto-assign all tile prefabs
            AutoAssignTileFactory();

            // BulletManager: auto-assign bullet prefab
            var bm = Object.FindObjectOfType<BulletManager>();
            if (bm != null)
            {
                var bmSo = new SerializedObject(bm);
                var bulletProp = bmSo.FindProperty("_bulletPrefab");
                if (bulletProp != null && bulletProp.objectReferenceValue == null)
                {
                    var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Bullet/Bullet.prefab");
                    if (bulletPrefab != null)
                    {
                        var bc = bulletPrefab.GetComponent<BulletRoute.Bullet.BulletController>();
                        if (bc != null)
                        {
                            bulletProp.objectReferenceValue = bc;
                            bmSo.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                }
            }

            // InputManager: assign camera + layer
            var input = Object.FindObjectOfType<InputManager>();
            if (input != null)
            {
                var so = new SerializedObject(input);
                var camProp = so.FindProperty("_mainCamera");
                if (camProp != null && camProp.objectReferenceValue == null)
                {
                    camProp.objectReferenceValue = Camera.main;
                }
                var layerProp = so.FindProperty("_gridLayer");
                if (layerProp != null)
                {
                    int gridLayer = LayerMask.NameToLayer("Grid");
                    if (gridLayer >= 0)
                        layerProp.intValue = 1 << gridLayer;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // GameManager: assign GameConfig
            var gm = Object.FindObjectOfType<GameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var configProp = so.FindProperty("_gameConfig");
                if (configProp != null && configProp.objectReferenceValue == null)
                {
                    var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Game/ScriptableObjects/Configs/GameConfig.asset");
                    if (config != null)
                    {
                        configProp.objectReferenceValue = config;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            // UIManager: auto-find panels
            var uiMgr = Object.FindObjectOfType<UIManager>();
            if (uiMgr != null)
            {
                var panels = Object.FindObjectsOfType<UIPanel>(true);
                var so = new SerializedObject(uiMgr);
                var panelsProp = so.FindProperty("_panels");
                panelsProp.ClearArray();
                foreach (var p in panels)
                {
                    panelsProp.InsertArrayElementAtIndex(panelsProp.arraySize);
                    panelsProp.GetArrayElementAtIndex(panelsProp.arraySize - 1).objectReferenceValue = p;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // GameplayUI: auto-assign buttons and texts
            var gameplayUI = Object.FindObjectOfType<GameplayUI>();
            if (gameplayUI != null)
            {
                AutoAssignGameplayUI(gameplayUI);
            }

            // WinPanel: auto-assign
            var winPanel = Object.FindObjectOfType<WinPanel>();
            if (winPanel != null)
            {
                AutoAssignWinPanel(winPanel);
            }

            // FailPanel: auto-assign
            var failPanel = Object.FindObjectOfType<FailPanel>();
            if (failPanel != null)
            {
                AutoAssignFailPanel(failPanel);
            }

            // MainMenu: auto-assign
            var mainMenu = Object.FindObjectOfType<UIMainMenu>(true);
            if (mainMenu != null)
            {
                AutoAssignMainMenu(mainMenu);
            }

            // PopupSettings: auto-assign
            var popupSettings = Object.FindObjectOfType<PopupSettings>(true);
            if (popupSettings != null)
            {
                AutoAssignPopupSettings(popupSettings);
            }

            // PopupPause: auto-assign
            var popupPause = Object.FindObjectOfType<PopupPause>(true);
            if (popupPause != null)
            {
                AutoAssignPopupPause(popupPause);
            }

            Debug.Log("[BulletRoute] Auto-assign references done");
        }

        private void AutoAssignGameplayUI(GameplayUI ui)
        {
            var so = new SerializedObject(ui);
            AutoFindButton(so, "_fireButton", "FireButton", ui.transform);
            AutoFindButton(so, "_resetButton", "ResetButton", ui.transform);
            AutoFindButton(so, "_undoButton", "UndoButton", ui.transform);
            AutoFindButton(so, "_pauseButton", "PauseButton", ui.transform);
            AutoFindButton(so, "_hintButton", "HintButton", ui.transform);
            AutoFindTMP(so, "_levelText", "LevelText", ui.transform);
            AutoFindTMP(so, "_moveCountText", "MoveCountText", ui.transform);
            AutoFindTMP(so, "_timerText", "TimerText", ui.transform);

            // Star slots
            var starsProp = so.FindProperty("_starSlots");
            if (starsProp != null)
            {
                var stars = new System.Collections.Generic.List<Transform>();
                FindChildrenByPrefix(ui.transform, "Star_", stars);
                starsProp.ClearArray();
                foreach (var s in stars)
                {
                    starsProp.InsertArrayElementAtIndex(starsProp.arraySize);
                    starsProp.GetArrayElementAtIndex(starsProp.arraySize - 1).objectReferenceValue = s;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AutoAssignWinPanel(WinPanel panel)
        {
            var so = new SerializedObject(panel);
            // Canvas group
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                var cgProp = so.FindProperty("_canvasGroup");
                if (cgProp != null) cgProp.objectReferenceValue = cg;
            }
            AutoFindTMP(so, "_levelCompleteText", "LevelCompleteText", panel.transform);
            AutoFindTMP(so, "_timeRemainingText", "TimeRemainingText", panel.transform);
            AutoFindTMP(so, "_moveCountText", "MoveCountText", panel.transform);
            AutoFindButton(so, "_nextButton", "NextButton", panel.transform);
            AutoFindButton(so, "_retryButton", "RetryButton", panel.transform);
            AutoFindButton(so, "_homeButton", "HomeButton", panel.transform);

            var starsProp = so.FindProperty("_stars");
            if (starsProp != null)
            {
                var stars = new System.Collections.Generic.List<Transform>();
                FindChildrenByPrefix(panel.transform, "Star_", stars);
                starsProp.ClearArray();
                foreach (var s in stars)
                {
                    starsProp.InsertArrayElementAtIndex(starsProp.arraySize);
                    starsProp.GetArrayElementAtIndex(starsProp.arraySize - 1).objectReferenceValue = s;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AutoAssignFailPanel(FailPanel panel)
        {
            var so = new SerializedObject(panel);
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                var cgProp = so.FindProperty("_canvasGroup");
                if (cgProp != null) cgProp.objectReferenceValue = cg;
            }
            AutoFindTMP(so, "_failText", "FailText", panel.transform);
            AutoFindButton(so, "_retryButton", "RetryButton", panel.transform);
            AutoFindButton(so, "_homeButton", "HomeButton", panel.transform);
            AutoFindTransform(so, "_failIcon", "FailIcon", panel.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AutoAssignTileFactory()
        {
            var factory = Object.FindObjectOfType<TileFactory>();
            if (factory == null) return;

            var so = new SerializedObject(factory);
            var prefabsProp = so.FindProperty("_tilePrefabs");

            // Only auto-assign if list is empty (don't override manual assignments)
            if (prefabsProp.arraySize > 0) return;

            TileType[] types = {
                TileType.Straight, TileType.Corner, TileType.Cross, TileType.Block,
                TileType.Mirror, TileType.Splitter, TileType.Portal,
                TileType.Bomb, TileType.Absorb, TileType.Turret, TileType.Target
            };

            int assigned = 0;
            foreach (var type in types)
            {
                string folder;
                switch (type)
                {
                    case TileType.Turret: folder = "Assets/_Game/Prefabs/Turret"; break;
                    case TileType.Target: folder = "Assets/_Game/Prefabs/Target"; break;
                    default: folder = "Assets/_Game/Prefabs/Tiles"; break;
                }
                string path = $"{folder}/Tile_{type}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var tileComp = prefab.GetComponent<TileBase>();
                if (tileComp == null) continue;

                prefabsProp.InsertArrayElementAtIndex(prefabsProp.arraySize);
                var element = prefabsProp.GetArrayElementAtIndex(prefabsProp.arraySize - 1);
                element.FindPropertyRelative("Type").enumValueIndex = (int)type;
                element.FindPropertyRelative("Prefab").objectReferenceValue = tileComp;
                assigned++;
            }

            if (assigned > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[BulletRoute] TileFactory: {assigned} prefabs auto-assigned");
            }
        }

        private void AutoAssignMainMenu(UIMainMenu menu)
        {
            var so = new SerializedObject(menu);
            var cg = menu.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                var cgProp = so.FindProperty("_canvasGroup");
                if (cgProp != null) cgProp.objectReferenceValue = cg;
            }
            AutoFindButton(so, "_playButton", "PlayButton", menu.transform);
            AutoFindButton(so, "_settingsButton", "SettingsButton", menu.transform);
            AutoFindTMP(so, "_levelText", "LevelText", menu.transform);
            AutoFindTMP(so, "_titleText", "TitleText", menu.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AutoAssignPopupSettings(PopupSettings settings)
        {
            var so = new SerializedObject(settings);
            var cg = settings.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                var cgProp = so.FindProperty("_canvasGroup");
                if (cgProp != null) cgProp.objectReferenceValue = cg;
            }
            AutoFindButton(so, "_closeButton", "CloseButton", settings.transform);

            // Find sliders
            var sfxSlider = FindDeepChild(settings.transform, "SFXSlider");
            if (sfxSlider != null)
            {
                var prop = so.FindProperty("_sfxSlider");
                if (prop != null) prop.objectReferenceValue = sfxSlider.GetComponent<Slider>();
            }
            var musicSlider = FindDeepChild(settings.transform, "MusicSlider");
            if (musicSlider != null)
            {
                var prop = so.FindProperty("_musicSlider");
                if (prop != null) prop.objectReferenceValue = musicSlider.GetComponent<Slider>();
            }
            AutoFindTMP(so, "_sfxValueText", "SFXValueText", settings.transform);
            AutoFindTMP(so, "_musicValueText", "MusicValueText", settings.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AutoAssignPopupPause(PopupPause pause)
        {
            var so = new SerializedObject(pause);
            var cg = pause.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                var cgProp = so.FindProperty("_canvasGroup");
                if (cgProp != null) cgProp.objectReferenceValue = cg;
            }
            AutoFindButton(so, "_resumeButton", "ResumeButton", pause.transform);
            AutoFindButton(so, "_homeButton", "HomeButton", pause.transform);
            AutoFindButton(so, "_settingsButton", "SettingsButton", pause.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ===================== HELPERS =====================
        private T FindOrCreate<T>(string name) where T : Component
        {
            var existing = Object.FindObjectOfType<T>();
            if (existing != null) return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go.AddComponent<T>();
        }

        private GameObject CreateUIElement(string name, Transform parent, bool withRect = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            return go;
        }

        private void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = CreateUIElement(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.black;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 60);
            return tmp;
        }

        private Button CreateButton(string name, Transform parent, string label, Color color)
        {
            var go = CreateUIElement(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);

            var txt = CreateTMPText("Label", go.transform, label, 28, TextAlignmentOptions.Center);
            txt.color = Color.white;
            SetFullStretch(txt.GetComponent<RectTransform>());

            return btn;
        }

        private void AutoFindButton(SerializedObject so, string propName, string goName, Transform root)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.objectReferenceValue == null)
            {
                var t = FindDeepChild(root, goName);
                if (t != null) prop.objectReferenceValue = t.GetComponent<Button>();
            }
        }

        private void AutoFindTMP(SerializedObject so, string propName, string goName, Transform root)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.objectReferenceValue == null)
            {
                var t = FindDeepChild(root, goName);
                if (t != null) prop.objectReferenceValue = t.GetComponent<TextMeshProUGUI>();
            }
        }

        private void AutoFindTransform(SerializedObject so, string propName, string goName, Transform root)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.objectReferenceValue == null)
            {
                var t = FindDeepChild(root, goName);
                if (t != null) prop.objectReferenceValue = t;
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private void FindChildrenByPrefix(Transform parent, string prefix, System.Collections.Generic.List<Transform> results)
        {
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith(prefix)) results.Add(child);
                FindChildrenByPrefix(child, prefix, results);
            }
        }

        private void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private void EnsureLayerExists(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0) return;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[BulletRoute] Layer '{layerName}' created at index {i}");
                    return;
                }
            }
            Debug.LogWarning($"[BulletRoute] Could not create layer '{layerName}' - no empty slots!");
        }

        private void CleanNodeModules()
        {
            string path = "Assets/_Game/node_modules";
            if (AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.DeleteAsset(path);
                // Also clean up orphan files
                AssetDatabase.DeleteAsset("Assets/_Game/package.json");
                AssetDatabase.DeleteAsset("Assets/_Game/package-lock.json");
                AssetDatabase.Refresh();
                Debug.Log("[BulletRoute] node_modules cleaned!");
            }
            else
            {
                Debug.Log("[BulletRoute] No node_modules found - clean!");
            }
        }

        private void SetupLighting()
        {
            if (Object.FindObjectOfType<Light>() != null) return;

            var go = new GameObject("Directional Light");
            Undo.RegisterCreatedObjectUndo(go, "Create Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.9f);
            light.intensity = 1.2f;
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
    }
}
