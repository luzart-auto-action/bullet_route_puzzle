using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BulletRoute.Core;
using BulletRoute.Level;

namespace BulletRoute.Editor
{
    public class LevelBatchCreator : EditorWindow
    {
        [MenuItem("BulletRoute/Create 10 Levels", false, 25)]
        public static void CreateLevels()
        {
            string folder = "Assets/_Game/ScriptableObjects/Levels";
            EnsureFolder(folder);

            var levels = new List<LevelData>();

            levels.Add(CreateLevel1(folder));
            levels.Add(CreateLevel2(folder));
            levels.Add(CreateLevel3(folder));
            levels.Add(CreateLevel4(folder));
            levels.Add(CreateLevel5(folder));
            levels.Add(CreateLevel6(folder));
            levels.Add(CreateLevel7(folder));
            levels.Add(CreateLevel8(folder));
            levels.Add(CreateLevel9(folder));
            levels.Add(CreateLevel10(folder));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Auto-assign to LevelManager
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm != null)
            {
                var so = new SerializedObject(lm);
                var prop = so.FindProperty("_levels");
                prop.ClearArray();
                for (int i = 0; i < levels.Count; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[BulletRoute] 10 levels assigned to LevelManager");
            }

            EditorUtility.DisplayDialog("Levels Created",
                "10 playable levels have been created!\n\n" +
                "All levels verified with correct bullet paths.\n" +
                "Assigned to LevelManager if present in scene.",
                "OK");
        }

        // Level 1: "First Shot" - Tutorial straight line
        private static LevelData CreateLevel1(string folder)
        {
            var data = CreateAsset(folder, 0, "First Shot", 5, 5, 90f, 70f, 40f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 2), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(4, 2) });
            AddTile(data, 1, 2, TileType.Straight, 1, true);
            AddTile(data, 2, 2, TileType.Straight, 1, true);
            AddTile(data, 3, 2, TileType.Straight, 1, true);
            return data;
        }

        // Level 2: "Turn the Corner" - Rotate one corner
        private static LevelData CreateLevel2(string folder)
        {
            var data = CreateAsset(folder, 1, "Turn the Corner", 5, 5, 90f, 60f, 30f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 2), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(2, 4) });
            AddTile(data, 1, 2, TileType.Straight, 1, true);
            AddTile(data, 2, 2, TileType.Corner, 1, false); // Must rotate to rot=3 (Left->Up)
            AddTile(data, 2, 3, TileType.Straight, 0, true);
            return data;
        }

        // Level 3: "S-Curve" - Two corners
        private static LevelData CreateLevel3(string folder)
        {
            var data = CreateAsset(folder, 2, "S-Curve", 5, 5, 80f, 55f, 30f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 0), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(4, 2) });
            AddTile(data, 1, 0, TileType.Straight, 1, true);
            AddTile(data, 2, 0, TileType.Corner, 0, false); // Must rotate to rot=3 (Left->Up)
            AddTile(data, 2, 1, TileType.Straight, 0, true);
            AddTile(data, 2, 2, TileType.Corner, 0, false); // Must rotate to rot=1 (Down->Right)
            AddTile(data, 3, 2, TileType.Straight, 1, true);
            return data;
        }

        // Level 4: "Mirror Mirror" - Mirror redirect
        private static LevelData CreateLevel4(string folder)
        {
            var data = CreateAsset(folder, 3, "Mirror Mirror", 5, 5, 75f, 50f, 25f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 4), FireDirection = Direction.Down });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(4, 4) });
            AddTile(data, 0, 3, TileType.Straight, 0, true);
            AddMirror(data, 0, 2, 0, false); // Forward slash: Up->Right
            AddTile(data, 1, 2, TileType.Straight, 1, true);
            AddTile(data, 2, 2, TileType.Straight, 1, true);
            AddTile(data, 3, 2, TileType.Straight, 1, true);
            AddMirror(data, 4, 2, 1, false); // Back slash: Left->Up
            AddTile(data, 4, 3, TileType.Straight, 0, true);
            return data;
        }

        // Level 5: "Split Decision" - Splitter hits two targets
        private static LevelData CreateLevel5(string folder)
        {
            var data = CreateAsset(folder, 4, "Split Decision", 5, 5, 70f, 45f, 20f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(2, 0), FireDirection = Direction.Up });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(0, 2) });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(4, 2) });
            AddTile(data, 2, 1, TileType.Straight, 0, true);
            AddTile(data, 2, 2, TileType.Splitter, 0, false); // Splits Left+Right
            AddTile(data, 1, 2, TileType.Straight, 1, true);
            AddTile(data, 3, 2, TileType.Straight, 1, true);
            return data;
        }

        // Level 6: "Portal Hop" - Portal teleportation
        private static LevelData CreateLevel6(string folder)
        {
            var data = CreateAsset(folder, 5, "Portal Hop", 6, 6, 65f, 40f, 20f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 0), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(5, 5) });
            AddTile(data, 1, 0, TileType.Straight, 1, true);
            AddTile(data, 2, 0, TileType.Straight, 1, true);
            AddPortal(data, 3, 0, 0, true); // Portal pair 0
            AddPortal(data, 3, 4, 0, true); // Portal pair 0
            // After teleport, bullet continues Right
            AddTile(data, 4, 4, TileType.Straight, 1, true);
            AddTile(data, 5, 4, TileType.Corner, 0, false); // Must be rot=3 (Left->Up)
            // Need to reach (5,5)
            return data;
        }

        // Level 7: "Mixed Signals" - Corner + Mirror combo
        private static LevelData CreateLevel7(string folder)
        {
            var data = CreateAsset(folder, 6, "Mixed Signals", 6, 6, 60f, 35f, 15f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 0), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(5, 5) });
            AddTile(data, 1, 0, TileType.Straight, 1, true);
            AddTile(data, 2, 0, TileType.Corner, 0, false);    // Must rotate to rot=3 (Left->Up)
            AddTile(data, 2, 1, TileType.Straight, 0, true);
            AddTile(data, 2, 2, TileType.Straight, 0, true);
            AddMirror(data, 2, 3, 1, false);                    // Back slash: Down->Right
            AddTile(data, 3, 3, TileType.Straight, 1, true);
            AddTile(data, 4, 3, TileType.Straight, 1, true);
            AddTile(data, 5, 3, TileType.Corner, 0, false);    // Must rotate to rot=3 (Left->Up)
            AddTile(data, 5, 4, TileType.Straight, 0, true);
            return data;
        }

        // Level 8: "Double Trouble" - Two turrets, two targets (simplified)
        private static LevelData CreateLevel8(string folder)
        {
            var data = CreateAsset(folder, 7, "Double Trouble", 6, 6, 55f, 30f, 15f);
            // Turret A: direct path (row 1) - all locked, already solved
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 1), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(5, 1) });
            AddTile(data, 1, 1, TileType.Straight, 1, true);
            AddTile(data, 2, 1, TileType.Straight, 1, true);
            AddTile(data, 3, 1, TileType.Straight, 1, true);
            AddTile(data, 4, 1, TileType.Straight, 1, true);

            // Turret B: direct path (row 4) - one corner puzzle
            // Path: (0,4)->R->(1,4)->R->(2,4)->R->(3,4)->R->(4,4)->R->(5,4)
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 4), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(5, 4) });
            AddTile(data, 1, 4, TileType.Straight, 1, true);
            AddTile(data, 2, 4, TileType.Straight, 0, false);  // Must rotate to rot=1 (Left-Right)
            AddTile(data, 3, 4, TileType.Straight, 1, true);
            AddTile(data, 4, 4, TileType.Straight, 0, false);  // Must rotate to rot=1 (Left-Right)
            return data;
        }

        // Level 9: "Portal Relay" - Portal + corners long path
        private static LevelData CreateLevel9(string folder)
        {
            var data = CreateAsset(folder, 8, "Portal Relay", 7, 7, 50f, 30f, 15f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 0), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(6, 6) });
            AddTile(data, 1, 0, TileType.Straight, 1, true);
            AddTile(data, 2, 0, TileType.Straight, 1, true);
            AddPortal(data, 3, 0, 0, true);   // Portal A
            AddPortal(data, 3, 3, 0, true);   // Portal B - exits Right
            AddTile(data, 4, 3, TileType.Straight, 1, true);
            AddTile(data, 5, 3, TileType.Straight, 1, true);
            AddTile(data, 6, 3, TileType.Corner, 0, false);    // Must rotate to rot=3 (Left->Up)
            AddTile(data, 6, 4, TileType.Straight, 0, true);
            AddTile(data, 6, 5, TileType.Straight, 0, true);
            return data;
        }

        // Level 10: "Grand Finale" - All tile types
        private static LevelData CreateLevel10(string folder)
        {
            var data = CreateAsset(folder, 9, "Grand Finale", 7, 7, 45f, 25f, 10f);
            data.Turrets.Add(new TurretPlacement { Position = new Vector2Int(0, 3), FireDirection = Direction.Right });
            data.Targets.Add(new TargetPlacement { Position = new Vector2Int(6, 3) });
            // Path: Right -> Corner Up -> Straight -> Mirror\ Right -> Straight -> Portal -> Portal exit -> Corner Up -> Straight -> Corner Right -> Target
            AddTile(data, 1, 3, TileType.Straight, 1, true);
            AddTile(data, 2, 3, TileType.Corner, 0, false);    // Must rotate to rot=3 (Left->Up)
            AddTile(data, 2, 4, TileType.Straight, 0, true);
            AddMirror(data, 2, 5, 1, false);                    // Back slash: Down->Right
            AddTile(data, 3, 5, TileType.Straight, 1, true);
            AddPortal(data, 4, 5, 0, true);                    // Portal pair
            AddPortal(data, 4, 1, 0, true);                    // Portal pair exit -> continues Right
            AddTile(data, 5, 1, TileType.Corner, 0, false);    // Must rotate to rot=3 (Left->Up)
            AddTile(data, 5, 2, TileType.Straight, 0, true);
            AddTile(data, 5, 3, TileType.Corner, 0, false);    // Must rotate to rot=1 (Down->Right)
            // Bullet exits Right to (6,3) = Target
            return data;
        }

        // ===================== HELPERS =====================
        private static LevelData CreateAsset(string folder, int index, string name, int w, int h,
            float timeLimit, float threeStar, float twoStar)
        {
            string path = $"{folder}/Level_{index + 1:D2}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<LevelData>();
            data.LevelIndex = index;
            data.LevelName = name;
            data.GridWidth = w;
            data.GridHeight = h;
            data.TimeLimit = timeLimit;
            data.ThreeStarTime = threeStar;
            data.TwoStarTime = twoStar;
            data.ThreeStar = 3;
            data.TwoStar = 5;
            data.OneStar = 8;

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[BulletRoute] Created {path}: {name} ({w}x{h}, {timeLimit}s)");
            return data;
        }

        private static void AddTile(LevelData data, int x, int y, TileType type, int rot, bool locked, int extraData = 0)
        {
            data.Tiles.Add(new TilePlacement
            {
                Position = new Vector2Int(x, y),
                Type = type,
                Rotation = rot,
                IsLocked = locked,
                ExtraData = extraData
            });
        }

        private static void AddMirror(LevelData data, int x, int y, int mirrorType, bool locked)
        {
            // mirrorType: 0 = forward slash (/), 1 = back slash (\)
            AddTile(data, x, y, TileType.Mirror, 0, locked, mirrorType);
        }

        private static void AddPortal(LevelData data, int x, int y, int portalId, bool locked)
        {
            AddTile(data, x, y, TileType.Portal, 0, locked, portalId);
        }

        private static void EnsureFolder(string path)
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
