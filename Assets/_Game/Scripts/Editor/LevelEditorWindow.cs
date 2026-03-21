using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BulletRoute.Core;
using BulletRoute.Level;

namespace BulletRoute.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private LevelData _levelData;
        private Vector2 _scrollPos;
        private float _cellSize = 50f;
        private int _paintMode; // 0=Tile, 1=Turret, 2=Target, 3=Eraser
        private TileType _paintTileType = TileType.Straight;
        private int _paintRotation;
        private bool _paintLocked;
        private int _paintExtraData;
        private Direction _paintFireDirection = Direction.Right;
        private Vector2 _gridScrollPos;

        // Colors for tile types
        private static readonly Dictionary<TileType, Color> TileColors = new Dictionary<TileType, Color>
        {
            { TileType.Empty, new Color(0.2f, 0.2f, 0.2f) },
            { TileType.Straight, new Color(0.4f, 0.8f, 0.4f) },
            { TileType.Corner, new Color(0.8f, 0.6f, 0.2f) },
            { TileType.Cross, new Color(0.6f, 0.6f, 0.9f) },
            { TileType.Block, new Color(0.5f, 0.5f, 0.5f) },
            { TileType.Mirror, new Color(0.7f, 0.9f, 1f) },
            { TileType.Splitter, new Color(1f, 0.6f, 0.8f) },
            { TileType.Portal, new Color(0f, 1f, 1f) },
            { TileType.Bomb, new Color(1f, 0.3f, 0.3f) },
            { TileType.Absorb, new Color(0.3f, 0f, 0.5f) },
            { TileType.Turret, new Color(0.9f, 0.9f, 0.2f) },
            { TileType.Target, new Color(1f, 0.5f, 0f) },
        };

        private static readonly Dictionary<Direction, string> DirArrows = new Dictionary<Direction, string>
        {
            { Direction.Up, "\u2191" },
            { Direction.Right, "\u2192" },
            { Direction.Down, "\u2193" },
            { Direction.Left, "\u2190" },
        };

        private static readonly Dictionary<int, string> RotSymbols = new Dictionary<int, string>
        {
            { 0, "\u2191" }, { 1, "\u2192" }, { 2, "\u2193" }, { 3, "\u2190" },
        };

        [MenuItem("BulletRoute/Level Editor", false, 20)]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Level Editor").minSize = new Vector2(700, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            GUILayout.Label("LEVEL EDITOR", EditorStyles.boldLabel);

            // Level data selection
            EditorGUILayout.BeginHorizontal();
            _levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", _levelData, typeof(LevelData), false);
            if (GUILayout.Button("New", GUILayout.Width(50)))
            {
                CreateNewLevelData();
            }
            EditorGUILayout.EndHorizontal();

            if (_levelData == null)
            {
                EditorGUILayout.HelpBox("Chon hoac tao LevelData asset de bat dau thiet ke level.", MessageType.Warning);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Level info
            DrawLevelInfo();

            EditorGUILayout.Space(10);

            // Paint tools
            DrawPaintTools();

            EditorGUILayout.Space(10);

            // Grid
            DrawGrid();

            EditorGUILayout.Space(10);

            // Placed items summary
            DrawSummary();

            EditorGUILayout.Space(10);

            // Actions
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelInfo()
        {
            GUILayout.Label("THONG TIN LEVEL", EditorStyles.boldLabel);
            var so = new SerializedObject(_levelData);

            EditorGUILayout.PropertyField(so.FindProperty("LevelIndex"));
            EditorGUILayout.PropertyField(so.FindProperty("LevelName"));
            EditorGUILayout.PropertyField(so.FindProperty("WorldIndex"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("GridWidth"));
            EditorGUILayout.PropertyField(so.FindProperty("GridHeight"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(so.FindProperty("TimeLimit"), new GUIContent("Time Limit (s)"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("ThreeStarTime"), new GUIContent("3 Star Time >="));
            EditorGUILayout.PropertyField(so.FindProperty("TwoStarTime"), new GUIContent("2 Star Time >="));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("ThreeStar"), new GUIContent("3 Star Moves <="));
            EditorGUILayout.PropertyField(so.FindProperty("TwoStar"), new GUIContent("2 Star Moves <="));
            EditorGUILayout.PropertyField(so.FindProperty("OneStar"), new GUIContent("1 Star Moves <="));
            EditorGUILayout.EndHorizontal();

            so.ApplyModifiedProperties();
        }

        private void DrawPaintTools()
        {
            GUILayout.Label("PAINT TOOLS", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // Mode buttons
            string[] modeLabels = { "Tile", "Turret", "Target", "Eraser" };
            Color[] modeColors = {
                new Color(0.4f, 0.8f, 0.4f),
                new Color(0.9f, 0.9f, 0.2f),
                new Color(1f, 0.5f, 0f),
                new Color(0.8f, 0.2f, 0.2f)
            };

            for (int i = 0; i < modeLabels.Length; i++)
            {
                GUI.backgroundColor = _paintMode == i ? modeColors[i] : Color.gray;
                if (GUILayout.Button(modeLabels[i], GUILayout.Height(30)))
                    _paintMode = i;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Mode-specific options
            switch (_paintMode)
            {
                case 0: // Tile
                    EditorGUILayout.BeginHorizontal();
                    _paintTileType = (TileType)EditorGUILayout.EnumPopup("Type", _paintTileType);
                    _paintRotation = EditorGUILayout.IntSlider("Rot", _paintRotation, 0, 3);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    _paintLocked = EditorGUILayout.Toggle("Locked", _paintLocked);
                    if (_paintTileType == TileType.Portal || _paintTileType == TileType.Mirror)
                    {
                        string label = _paintTileType == TileType.Portal ? "Portal ID" : "0=/ 1=\\";
                        _paintExtraData = EditorGUILayout.IntField(label, _paintExtraData);
                    }
                    EditorGUILayout.EndHorizontal();
                    break;

                case 1: // Turret
                    _paintFireDirection = (Direction)EditorGUILayout.EnumPopup("Fire Direction", _paintFireDirection);
                    break;
            }
        }

        private void DrawGrid()
        {
            GUILayout.Label("GRID", EditorStyles.boldLabel);

            _cellSize = EditorGUILayout.Slider("Cell Size", _cellSize, 30, 80);

            int w = _levelData.GridWidth;
            int h = _levelData.GridHeight;

            float gridW = w * _cellSize + 40;
            float gridH = h * _cellSize + 40;

            _gridScrollPos = EditorGUILayout.BeginScrollView(_gridScrollPos,
                GUILayout.Height(Mathf.Min(gridH + 20, 500)));

            // Draw grid (Y from top to bottom, but grid Y=0 is bottom)
            for (int y = h - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{y}", GUILayout.Width(20));

                for (int x = 0; x < w; x++)
                {
                    DrawCell(x, y);
                }
                EditorGUILayout.EndHorizontal();
            }

            // X axis labels
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(22);
            for (int x = 0; x < w; x++)
            {
                GUILayout.Label($"{x}", GUILayout.Width(_cellSize));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawCell(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            Color cellColor = new Color(0.15f, 0.15f, 0.2f);
            string label = "";

            // Check turret
            var turret = FindTurret(pos);
            if (turret != null)
            {
                cellColor = TileColors[TileType.Turret];
                label = "T" + DirArrows[turret.FireDirection];
            }

            // Check target
            var target = FindTarget(pos);
            if (target != null)
            {
                cellColor = TileColors[TileType.Target];
                label = "X";
            }

            // Check tile
            var tile = FindTile(pos);
            if (tile != null)
            {
                cellColor = TileColors.ContainsKey(tile.Type) ? TileColors[tile.Type] : Color.white;
                label = GetTileLabel(tile);
            }

            GUI.backgroundColor = cellColor;
            if (GUILayout.Button(label, GUILayout.Width(_cellSize), GUILayout.Height(_cellSize)))
            {
                HandleCellClick(pos);
            }
            GUI.backgroundColor = Color.white;
        }

        private void HandleCellClick(Vector2Int pos)
        {
            Undo.RecordObject(_levelData, "Level Edit");

            switch (_paintMode)
            {
                case 0: // Paint tile
                    // Remove existing
                    RemoveTurret(pos);
                    RemoveTarget(pos);
                    RemoveTile(pos);

                    if (_paintTileType != TileType.Empty)
                    {
                        _levelData.Tiles.Add(new TilePlacement
                        {
                            Position = pos,
                            Type = _paintTileType,
                            Rotation = _paintRotation,
                            IsLocked = _paintLocked,
                            ExtraData = _paintExtraData
                        });
                    }
                    break;

                case 1: // Paint turret
                    RemoveTile(pos);
                    RemoveTarget(pos);
                    RemoveTurret(pos);

                    _levelData.Turrets.Add(new TurretPlacement
                    {
                        Position = pos,
                        FireDirection = _paintFireDirection
                    });
                    break;

                case 2: // Paint target
                    RemoveTile(pos);
                    RemoveTurret(pos);
                    RemoveTarget(pos);

                    _levelData.Targets.Add(new TargetPlacement
                    {
                        Position = pos
                    });
                    break;

                case 3: // Eraser
                    RemoveTile(pos);
                    RemoveTurret(pos);
                    RemoveTarget(pos);
                    break;
            }

            EditorUtility.SetDirty(_levelData);
        }

        private void DrawSummary()
        {
            GUILayout.Label("SUMMARY", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Tiles: {_levelData.Tiles.Count}");
            EditorGUILayout.LabelField($"Turrets: {_levelData.Turrets.Count}");
            EditorGUILayout.LabelField($"Targets: {_levelData.Targets.Count}");

            // Show portal pairs
            var portals = new Dictionary<int, int>();
            foreach (var t in _levelData.Tiles)
            {
                if (t.Type == TileType.Portal)
                {
                    if (!portals.ContainsKey(t.ExtraData))
                        portals[t.ExtraData] = 0;
                    portals[t.ExtraData]++;
                }
            }
            foreach (var kvp in portals)
            {
                Color c = kvp.Value == 2 ? Color.green : Color.red;
                GUI.contentColor = c;
                EditorGUILayout.LabelField($"  Portal ID {kvp.Key}: {kvp.Value}/2 {(kvp.Value == 2 ? "OK" : "THIEU!")}");
                GUI.contentColor = Color.white;
            }
        }

        private void DrawActions()
        {
            GUILayout.Label("ACTIONS", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("Xoa tat ca Tiles"))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Xoa tat ca tiles?", "Xoa", "Huy"))
                {
                    Undo.RecordObject(_levelData, "Clear Tiles");
                    _levelData.Tiles.Clear();
                    EditorUtility.SetDirty(_levelData);
                }
            }
            if (GUILayout.Button("Xoa tat ca"))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Xoa tat ca tiles, turrets, targets?", "Xoa", "Huy"))
                {
                    Undo.RecordObject(_levelData, "Clear All");
                    _levelData.Tiles.Clear();
                    _levelData.Turrets.Clear();
                    _levelData.Targets.Clear();
                    EditorUtility.SetDirty(_levelData);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dien Straight vao o trong"))
            {
                FillEmptyCells(TileType.Straight);
            }
            if (GUILayout.Button("Dien Cross vao o trong"))
            {
                FillEmptyCells(TileType.Cross);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Gan LevelData nay vao LevelManager"))
            {
                AssignToLevelManager();
            }
        }

        // ===================== DATA HELPERS =====================
        private TilePlacement FindTile(Vector2Int pos)
        {
            return _levelData.Tiles.Find(t => t.Position == pos);
        }

        private TurretPlacement FindTurret(Vector2Int pos)
        {
            return _levelData.Turrets.Find(t => t.Position == pos);
        }

        private TargetPlacement FindTarget(Vector2Int pos)
        {
            return _levelData.Targets.Find(t => t.Position == pos);
        }

        private void RemoveTile(Vector2Int pos)
        {
            _levelData.Tiles.RemoveAll(t => t.Position == pos);
        }

        private void RemoveTurret(Vector2Int pos)
        {
            _levelData.Turrets.RemoveAll(t => t.Position == pos);
        }

        private void RemoveTarget(Vector2Int pos)
        {
            _levelData.Targets.RemoveAll(t => t.Position == pos);
        }

        private string GetTileLabel(TilePlacement tile)
        {
            string typeChar;
            switch (tile.Type)
            {
                case TileType.Straight: typeChar = "S"; break;
                case TileType.Corner: typeChar = "C"; break;
                case TileType.Cross: typeChar = "+"; break;
                case TileType.Block: typeChar = "#"; break;
                case TileType.Mirror: typeChar = tile.ExtraData == 0 ? "/" : "\\"; break;
                case TileType.Splitter: typeChar = "Y"; break;
                case TileType.Portal: typeChar = $"P{tile.ExtraData}"; break;
                case TileType.Bomb: typeChar = "*"; break;
                case TileType.Absorb: typeChar = "O"; break;
                default: typeChar = "?"; break;
            }

            string rot = RotSymbols.ContainsKey(tile.Rotation) ? RotSymbols[tile.Rotation] : "";
            string locked = tile.IsLocked ? "L" : "";
            return $"{typeChar}{rot}\n{locked}";
        }

        private void FillEmptyCells(TileType type)
        {
            Undo.RecordObject(_levelData, "Fill Empty");
            for (int x = 0; x < _levelData.GridWidth; x++)
            {
                for (int y = 0; y < _levelData.GridHeight; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (FindTile(pos) == null && FindTurret(pos) == null && FindTarget(pos) == null)
                    {
                        _levelData.Tiles.Add(new TilePlacement
                        {
                            Position = pos,
                            Type = type,
                            Rotation = 0,
                            IsLocked = false,
                            ExtraData = 0
                        });
                    }
                }
            }
            EditorUtility.SetDirty(_levelData);
        }

        private void CreateNewLevelData()
        {
            string path = "Assets/_Game/ScriptableObjects/Levels";
            EnsureFolder(path);

            int index = 0;
            while (AssetDatabase.LoadAssetAtPath<LevelData>($"{path}/Level_{index + 1:D2}.asset") != null)
                index++;

            var data = ScriptableObject.CreateInstance<LevelData>();
            data.LevelIndex = index;
            data.LevelName = $"Level {index + 1}";
            data.GridWidth = 5;
            data.GridHeight = 5;
            data.ThreeStar = 3;
            data.TwoStar = 5;
            data.OneStar = 8;

            string assetPath = $"{path}/Level_{index + 1:D2}.asset";
            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();

            _levelData = data;
            Debug.Log($"[BulletRoute] Created {assetPath}");
        }

        private void AssignToLevelManager()
        {
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm == null)
            {
                Debug.LogWarning("[BulletRoute] LevelManager khong tim thay trong scene!");
                return;
            }

            var so = new SerializedObject(lm);
            var levelsProp = so.FindProperty("_levels");

            // Check if already in list
            for (int i = 0; i < levelsProp.arraySize; i++)
            {
                if (levelsProp.GetArrayElementAtIndex(i).objectReferenceValue == _levelData)
                {
                    Debug.Log("[BulletRoute] LevelData da co trong list!");
                    return;
                }
            }

            // Add at correct index
            int idx = _levelData.LevelIndex;
            while (levelsProp.arraySize <= idx)
            {
                levelsProp.InsertArrayElementAtIndex(levelsProp.arraySize);
            }
            levelsProp.GetArrayElementAtIndex(idx).objectReferenceValue = _levelData;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[BulletRoute] LevelData assigned to LevelManager at index {idx}");
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
    }
}
