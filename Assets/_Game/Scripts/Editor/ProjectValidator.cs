using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
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

namespace BulletRoute.Editor
{
    public class ProjectValidator : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<ValidationResult> _results = new List<ValidationResult>();
        private int _errorCount;
        private int _warningCount;
        private int _passCount;

        private enum ResultType { Pass, Warning, Error }

        private class ValidationResult
        {
            public string Category;
            public string Message;
            public ResultType Type;
            public Object Context;
        }

        [MenuItem("BulletRoute/Project Validator", false, 30)]
        public static void ShowWindow()
        {
            var win = GetWindow<ProjectValidator>("Project Validator");
            win.minSize = new Vector2(450, 500);
            win.RunValidation();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("PROJECT VALIDATOR", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("RE-VALIDATE", GUILayout.Width(120), GUILayout.Height(25)))
                RunValidation();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Summary
            EditorGUILayout.BeginHorizontal("box");
            GUI.contentColor = Color.green;
            GUILayout.Label($"Pass: {_passCount}", EditorStyles.boldLabel);
            GUI.contentColor = Color.yellow;
            GUILayout.Label($"Warning: {_warningCount}", EditorStyles.boldLabel);
            GUI.contentColor = Color.red;
            GUILayout.Label($"Error: {_errorCount}", EditorStyles.boldLabel);
            GUI.contentColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            string currentCat = "";
            foreach (var r in _results)
            {
                if (r.Category != currentCat)
                {
                    currentCat = r.Category;
                    EditorGUILayout.Space(5);
                    GUILayout.Label(currentCat, EditorStyles.boldLabel);
                }

                Color c;
                string icon;
                switch (r.Type)
                {
                    case ResultType.Pass: c = Color.green; icon = "OK"; break;
                    case ResultType.Warning: c = Color.yellow; icon = "!!"; break;
                    default: c = Color.red; icon = "XX"; break;
                }

                EditorGUILayout.BeginHorizontal();
                GUI.contentColor = c;
                GUILayout.Label($"[{icon}]", GUILayout.Width(30));
                GUI.contentColor = Color.white;

                if (r.Context != null)
                {
                    if (GUILayout.Button(r.Message, EditorStyles.linkLabel))
                    {
                        Selection.activeObject = r.Context;
                        EditorGUIUtility.PingObject(r.Context);
                    }
                }
                else
                {
                    GUILayout.Label(r.Message);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            if (_errorCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Co {_errorCount} loi can fix truoc khi chay game.\n" +
                    "Dung 'BulletRoute > Scene Setup Wizard' de fix nhanh.",
                    MessageType.Error);
            }
            else if (_warningCount > 0)
            {
                EditorGUILayout.HelpBox("Game co the chay nhung se co 1 so feature thieu.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Tat ca da san sang! Bam Play de test.", MessageType.Info);
            }
        }

        private void RunValidation()
        {
            _results.Clear();
            _errorCount = 0;
            _warningCount = 0;
            _passCount = 0;

            ValidateManagers();
            ValidateCamera();
            ValidateGridCollider();
            ValidateTileFactory();
            ValidateLevelManager();
            ValidateBulletManager();
            ValidateUI();
            ValidateFX();
            ValidateAudio();
            ValidatePrefabs();

            Repaint();
        }

        private void Add(string cat, string msg, ResultType type, Object ctx = null)
        {
            _results.Add(new ValidationResult { Category = cat, Message = msg, Type = type, Context = ctx });
            switch (type)
            {
                case ResultType.Pass: _passCount++; break;
                case ResultType.Warning: _warningCount++; break;
                case ResultType.Error: _errorCount++; break;
            }
        }

        // ===================== VALIDATORS =====================

        private void ValidateManagers()
        {
            string cat = "MANAGERS";
            CheckComponent<GameManager>(cat, "GameManager");
            CheckComponent<GridManager>(cat, "GridManager");
            CheckComponent<TileFactory>(cat, "TileFactory");
            CheckComponent<LevelManager>(cat, "LevelManager");
            CheckComponent<BulletSimulator>(cat, "BulletSimulator");
            CheckComponent<BulletManager>(cat, "BulletManager");
            CheckComponent<GameStateManager>(cat, "GameStateManager");
            CheckComponent<InputManager>(cat, "InputManager");
            CheckComponent<FXManager>(cat, "FXManager");
            CheckComponent<AudioManager>(cat, "AudioManager");
        }

        private void ValidateCamera()
        {
            string cat = "CAMERA";
            var cam = Camera.main;
            if (cam == null)
            {
                Add(cat, "Main Camera khong tim thay!", ResultType.Error);
                return;
            }
            Add(cat, "Main Camera OK", ResultType.Pass, cam);

            var cc = cam.GetComponent<CameraController>();
            if (cc == null)
                Add(cat, "CameraController chua duoc add vao Camera", ResultType.Warning, cam);
            else
                Add(cat, "CameraController OK", ResultType.Pass, cc);
        }

        private void ValidateGridCollider()
        {
            string cat = "GRID COLLIDER";
            int gridLayer = LayerMask.NameToLayer("Grid");
            if (gridLayer < 0)
            {
                Add(cat, "Layer 'Grid' chua duoc tao!", ResultType.Error);
                return;
            }
            Add(cat, "Layer 'Grid' OK", ResultType.Pass);

            var collider = GameObject.Find("[GridCollider]");
            if (collider == null)
            {
                Add(cat, "[GridCollider] GameObject khong tim thay", ResultType.Error);
                return;
            }

            if (collider.GetComponent<BoxCollider>() == null)
                Add(cat, "[GridCollider] thieu BoxCollider", ResultType.Error, collider);
            else
                Add(cat, "[GridCollider] BoxCollider OK", ResultType.Pass, collider);

            if (collider.layer != gridLayer)
                Add(cat, "[GridCollider] layer KHONG phai 'Grid'!", ResultType.Error, collider);
            else
                Add(cat, "[GridCollider] layer OK", ResultType.Pass, collider);

            // Check InputManager has correct layer
            var input = Object.FindObjectOfType<InputManager>();
            if (input != null)
            {
                var so = new SerializedObject(input);
                var layerProp = so.FindProperty("_gridLayer");
                if (layerProp != null && layerProp.intValue == 0)
                    Add(cat, "InputManager.GridLayer chua duoc gan!", ResultType.Error, input);
                else
                    Add(cat, "InputManager.GridLayer OK", ResultType.Pass, input);
            }
        }

        private void ValidateTileFactory()
        {
            string cat = "TILE FACTORY";
            var factory = Object.FindObjectOfType<TileFactory>();
            if (factory == null)
            {
                Add(cat, "TileFactory khong tim thay!", ResultType.Error);
                return;
            }

            var so = new SerializedObject(factory);
            var prefabs = so.FindProperty("_tilePrefabs");

            TileType[] required = {
                TileType.Straight, TileType.Corner, TileType.Cross, TileType.Block,
                TileType.Mirror, TileType.Splitter, TileType.Portal,
                TileType.Bomb, TileType.Absorb, TileType.Turret, TileType.Target
            };

            var found = new HashSet<int>();
            for (int i = 0; i < prefabs.arraySize; i++)
            {
                var elem = prefabs.GetArrayElementAtIndex(i);
                int typeIdx = elem.FindPropertyRelative("Type").enumValueIndex;
                var prefab = elem.FindPropertyRelative("Prefab").objectReferenceValue;
                if (prefab != null) found.Add(typeIdx);
            }

            foreach (var type in required)
            {
                if (found.Contains((int)type))
                    Add(cat, $"Tile_{type} prefab OK", ResultType.Pass);
                else
                    Add(cat, $"Tile_{type} prefab THIEU!", ResultType.Warning, factory);
            }
        }

        private void ValidateLevelManager()
        {
            string cat = "LEVEL MANAGER";
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm == null)
            {
                Add(cat, "LevelManager khong tim thay!", ResultType.Error);
                return;
            }

            var so = new SerializedObject(lm);
            var levels = so.FindProperty("_levels");

            if (levels.arraySize == 0)
                Add(cat, "Khong co LevelData nao trong list!", ResultType.Error, lm);
            else
                Add(cat, $"{levels.arraySize} level(s) loaded", ResultType.Pass, lm);

            // Check for null entries
            for (int i = 0; i < levels.arraySize; i++)
            {
                if (levels.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    Add(cat, $"Level [{i}] bi null!", ResultType.Error, lm);
            }

            // Check GameConfig
            var gm = Object.FindObjectOfType<GameManager>();
            if (gm != null)
            {
                var gmSo = new SerializedObject(gm);
                var configProp = gmSo.FindProperty("_gameConfig");
                if (configProp.objectReferenceValue == null)
                    Add(cat, "GameManager.GameConfig chua duoc gan!", ResultType.Warning, gm);
                else
                    Add(cat, "GameConfig OK", ResultType.Pass, gm);
            }
        }

        private void ValidateBulletManager()
        {
            string cat = "BULLET";
            var bm = Object.FindObjectOfType<BulletManager>();
            if (bm == null)
            {
                Add(cat, "BulletManager khong tim thay!", ResultType.Error);
                return;
            }

            var so = new SerializedObject(bm);
            var prefab = so.FindProperty("_bulletPrefab");
            if (prefab.objectReferenceValue == null)
                Add(cat, "Bullet Prefab chua duoc gan!", ResultType.Error, bm);
            else
                Add(cat, "Bullet Prefab OK", ResultType.Pass, bm);
        }

        private void ValidateUI()
        {
            string cat = "UI";
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Add(cat, "Canvas khong tim thay!", ResultType.Error);
                return;
            }
            Add(cat, "Canvas OK", ResultType.Pass, canvas);

            var uiMgr = Object.FindObjectOfType<UIManager>();
            if (uiMgr == null)
            {
                Add(cat, "UIManager khong tim thay!", ResultType.Error);
                return;
            }

            var so = new SerializedObject(uiMgr);
            var panels = so.FindProperty("_panels");
            if (panels.arraySize == 0)
                Add(cat, "UIManager khong co panel nao!", ResultType.Warning, uiMgr);

            // Check panel names
            var winPanel = Object.FindObjectOfType<WinPanel>(true);
            if (winPanel != null)
            {
                var pso = new SerializedObject(winPanel);
                string name = pso.FindProperty("_panelName").stringValue;
                if (name != "WinPanel")
                    Add(cat, $"WinPanel.PanelName = '{name}' (phai la 'WinPanel')", ResultType.Error, winPanel);
                else
                    Add(cat, "WinPanel OK", ResultType.Pass, winPanel);
            }
            else
            {
                Add(cat, "WinPanel khong tim thay!", ResultType.Warning);
            }

            var failPanel = Object.FindObjectOfType<FailPanel>(true);
            if (failPanel != null)
            {
                var pso = new SerializedObject(failPanel);
                string name = pso.FindProperty("_panelName").stringValue;
                if (name != "FailPanel")
                    Add(cat, $"FailPanel.PanelName = '{name}' (phai la 'FailPanel')", ResultType.Error, failPanel);
                else
                    Add(cat, "FailPanel OK", ResultType.Pass, failPanel);
            }
            else
            {
                Add(cat, "FailPanel khong tim thay!", ResultType.Warning);
            }

            // GameplayUI
            var gpUI = Object.FindObjectOfType<GameplayUI>();
            if (gpUI == null)
                Add(cat, "GameplayUI khong tim thay!", ResultType.Warning);
            else
                Add(cat, "GameplayUI OK", ResultType.Pass, gpUI);

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                Add(cat, "EventSystem khong tim thay! (UI se khong nhan input)", ResultType.Error);
            else
                Add(cat, "EventSystem OK", ResultType.Pass);
        }

        private void ValidateFX()
        {
            string cat = "FX SYSTEM";
            var fxMgr = Object.FindObjectOfType<FXManager>();
            if (fxMgr == null)
            {
                Add(cat, "FXManager khong tim thay!", ResultType.Warning);
                return;
            }

            var so = new SerializedObject(fxMgr);
            var lib = so.FindProperty("_fxLibrary");

            string[] requiredFX = {
                "BulletPass", "BulletHitTarget", "BulletStop", "MuzzleFlash",
                "TurretCharge", "MirrorReflect", "PortalIn", "PortalOut",
                "Explosion", "BlockHit", "TargetHit", "Confetti",
                "StarBurst", "BulletSplit", "BulletAbsorb"
            };

            var found = new HashSet<string>();
            for (int i = 0; i < lib.arraySize; i++)
            {
                string name = lib.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue;
                var prefab = lib.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab").objectReferenceValue;
                if (!string.IsNullOrEmpty(name)) found.Add(name);
                if (prefab == null && !string.IsNullOrEmpty(name))
                    Add(cat, $"FX '{name}' co ten nhung thieu prefab!", ResultType.Warning, fxMgr);
            }

            int missing = 0;
            foreach (var fx in requiredFX)
            {
                if (!found.Contains(fx)) missing++;
            }

            if (missing > 0)
                Add(cat, $"{missing}/{requiredFX.Length} FX entries thieu (game van chay, chi khong co hieu ung)", ResultType.Warning, fxMgr);
            else
                Add(cat, $"Tat ca {requiredFX.Length} FX entries OK", ResultType.Pass, fxMgr);
        }

        private void ValidateAudio()
        {
            string cat = "AUDIO";
            var audio = Object.FindObjectOfType<AudioManager>();
            if (audio == null)
            {
                Add(cat, "AudioManager khong tim thay!", ResultType.Warning);
                return;
            }

            var so = new SerializedObject(audio);
            var sfxLib = so.FindProperty("_sfxLibrary");
            var musicLib = so.FindProperty("_musicLibrary");

            if (sfxLib.arraySize == 0)
                Add(cat, "Khong co SFX nao (game van chay, chi khong co am thanh)", ResultType.Warning, audio);
            else
                Add(cat, $"{sfxLib.arraySize} SFX entries", ResultType.Pass, audio);

            if (musicLib.arraySize == 0)
                Add(cat, "Khong co Music nao", ResultType.Warning, audio);
            else
                Add(cat, $"{musicLib.arraySize} Music entries", ResultType.Pass, audio);
        }

        private void ValidatePrefabs()
        {
            string cat = "PREFABS";
            string bulletPath = "Assets/_Game/Prefabs/Bullet/Bullet.prefab";
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
            if (bulletPrefab == null)
                Add(cat, "Bullet.prefab chua duoc tao!", ResultType.Error);
            else
                Add(cat, "Bullet.prefab OK", ResultType.Pass, bulletPrefab);
        }

        // ===================== HELPERS =====================
        private void CheckComponent<T>(string cat, string name) where T : Component
        {
            var obj = Object.FindObjectOfType<T>();
            if (obj == null)
                Add(cat, $"{name} khong tim thay trong scene!", ResultType.Error);
            else
                Add(cat, $"{name} OK", ResultType.Pass, obj);
        }
    }
}
