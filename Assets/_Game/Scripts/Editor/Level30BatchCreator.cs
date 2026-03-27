using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using BulletRoute.Core;
using BulletRoute.Level;

namespace BulletRoute.Editor
{
    /// <summary>
    /// 30 levels: 5x5 → 10x10. Menu: BulletRoute > Create 30 Levels.
    /// CORNER bullet-travel: rot0:R→U,U→R | rot1:R→D,D→R | rot2:L→D,D→L | rot3:L→U,U→L
    /// MIRROR rot0(/): R→U,U→R,L→D,D→L
    /// SPLITTER: bullet-dir→perpendicular pair (Up→L+R, R→U+D, etc.)
    /// STRAIGHT: rot0=vertical(U/D) rot1=horizontal(L/R)
    /// </summary>
    public class Level30BatchCreator : EditorWindow
    {
        [MenuItem("BulletRoute/Create 30 Levels", false, 26)]
        public static void CreateLevels()
        {
            string folder = "Assets/_Game/ScriptableObjects/Levels";
            EnsureFolder(folder);
            var levels = new List<LevelData>();
            for (int i = 0; i < 30; i++)
                levels.Add(Build(folder, i));
            foreach (var level in levels)
                EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm != null) {
                var so = new SerializedObject(lm); var prop = so.FindProperty("_levels");
                prop.ClearArray();
                for (int i = 0; i < levels.Count; i++) { prop.InsertArrayElementAtIndex(i); prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i]; }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(lm);
                EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
                EditorSceneManager.SaveScene(lm.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Done", $"{levels.Count} levels created & assigned.\n\nScene saved!", "OK");
        }

        static LevelData Build(string f, int i) {
            switch(i) {
                case 0:return L01(f);case 1:return L02(f);case 2:return L03(f);case 3:return L04(f);case 4:return L05(f);
                case 5:return L06(f);case 6:return L07(f);case 7:return L08(f);case 8:return L09(f);case 9:return L10(f);
                case 10:return L11(f);case 11:return L12(f);case 12:return L13(f);case 13:return L14(f);case 14:return L15(f);
                case 15:return L16(f);case 16:return L17(f);case 17:return L18(f);case 18:return L19(f);case 19:return L20(f);
                case 20:return L21(f);case 21:return L22(f);case 22:return L23(f);case 23:return L24(f);case 24:return L25(f);
                case 25:return L26(f);case 26:return L27(f);case 27:return L28(f);case 28:return L29(f);case 29:return L30(f);
                default:return L01(f);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  TUTORIAL (1-4)
        // ════════════════════════════════════════════════════════════════

        static LevelData L01(string f) {
            var d = MK(f,0,"First Steps",5,5,90,70,40);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Targets.Add(Tg(4,2));
            S(d,1,2,1,true); S(d,2,2,0,false); S(d,3,2,1,true);
            return d;
        }
        static LevelData L02(string f) {
            var d = MK(f,1,"Swap Intro",5,5,90,65,35);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Targets.Add(Tg(4,2));
            S(d,1,2,1,true); S(d,3,2,1,true);
            S(d,2,0,1,false);
            return d;
        }
        static LevelData L03(string f) {
            var d = MK(f,2,"Zigzag",5,5,85,55,30);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Targets.Add(Tg(0,2));
            C(d,1,4,3,false); S(d,1,3,0,true); C(d,1,2,0,false);
            Ab(d,2,4); Bl(d,0,3); Ab(d,1,1);
            return d;
        }
        static LevelData L04(string f) {
            var d = MK(f,3,"Drag & Solve",5,5,85,55,30);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Targets.Add(Tg(2,4));
            S(d,1,2,1,true); S(d,2,3,0,true);
            C(d,3,0,2,false); S(d,3,2,1,false); Bl(d,4,2);
            return d;
        }

        // ════════════════════════════════════════════════════════════════
        //  L05-L10: 5x5→6x6, ≥2 targets, splitter+portal, decoys, traps
        // ════════════════════════════════════════════════════════════════

        // 5: 5x5 Splitter→2T + portal pair as confusion. 12 tiles wrong + 3 traps
        static LevelData L05(string f) {
            var d = MK(f,4,"Split Portal",5,5,75,35,18);
            d.Turrets.Add(Tu(2,0,Direction.Up)); d.Targets.Add(Tg(0,4)); d.Targets.Add(Tg(4,4));
            S(d,2,1,1,false); Sp(d,2,2,true);
            // L path: L→C(1,2)rot3:L→U→S(1,3)→Tg(0,4)... need (1,4)? No target at (0,4). C(1,2)rot3:L→U→(1,3)S→(1,4)C rot3:U→L→Tg(0,4)
            C(d,1,2,1,false); S(d,1,3,1,false); C(d,1,4,0,false);
            // R path: R→C(3,2)rot0:R→U→S(3,3)→C(3,4)rot0:U→R→Tg(4,4)
            C(d,3,2,2,false); S(d,3,3,1,false); C(d,3,4,3,false);
            // Portal pair (decoy/confusion)
            Po(d,0,0,1,true); Po(d,4,0,1,true);
            // Decoys
            S(d,0,2,0,false); C(d,4,2,1,false); S(d,2,4,0,false); C(d,0,1,2,false);
            // Traps
            Ab(d,2,3); Ab(d,4,1); Bl(d,0,3);
            return d;
        }

        // 6: 6x6 Splitter+Portal→2T. Portal teleports one split bullet.
        static LevelData L06(string f) {
            var d = MK(f,5,"Portal Fork",6,6,70,30,15);
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Targets.Add(Tg(5,5)); d.Targets.Add(Tg(5,0));
            // R→S(1,3)→Sp(2,3)→U+D
            S(d,1,3,0,false); Sp(d,2,3,true);
            // U: S(2,4)→Po(2,5)[1]→Po(5,3)[1]→continues U→S(5,4)→Tg(5,5)
            S(d,2,4,1,false); Po(d,2,5,1,true); Po(d,5,3,1,true); S(d,5,4,1,false);
            // D: S(2,2)→S(2,1)→C(2,0)rot1:D→R→S(3,0)→S(4,0)→Tg(5,0)
            S(d,2,2,1,false); S(d,2,1,1,false); C(d,2,0,3,false); S(d,3,0,0,false); S(d,4,0,0,false);
            // Decoys
            C(d,0,0,1,false); S(d,1,0,0,false); C(d,4,5,2,false); S(d,3,5,0,false);
            C(d,0,5,0,false); S(d,4,3,0,false); C(d,3,2,1,false); S(d,5,2,0,false);
            C(d,1,1,2,false); S(d,3,4,1,false); C(d,4,1,3,false); S(d,0,4,0,false);
            // Traps
            Ab(d,3,3); Ab(d,1,2); Ab(d,4,4); Bl(d,3,1); Bl(d,1,5);
            return d;
        }

        // 7: 6x6 Mirror+Splitter→2T, 2 portal pairs for confusion
        static LevelData L07(string f) {
            var d = MK(f,6,"Mirror Split",6,6,65,28,14);
            d.Turrets.Add(Tu(0,5,Direction.Right)); d.Targets.Add(Tg(5,5)); d.Targets.Add(Tg(5,0));
            // R→Mi(1,5)rot0:R→U... wait mirror rot0: R→U? No. Header says rot0:R→D. Let me re-check.
            // Header: MIRROR: rot0:R→D,D→R,L→U,U→L | rot1:R→U,U→R,L→D,D→L
            // So mirror rot0: R→D. rot1: R→U.
            // R→Mi(1,5)rot0:R→D→S(1,4)→S(1,3)→Sp(1,2)→L+R(perpendicular to D=L+R)
            // L: S(0,2)→Tg?... need target reachable. L goes to (0,2).
            // R: S(2,2)→S(3,2)→Mi(4,2)rot0:R... bullet going R enters mirror. rot0:R→D→S(4,1)→S(4,0)→... Tg(5,0)? Need corner.
            // Fix: R→S(2,2)→S(3,2)→S(4,2)→C(5,2)rot1:R→D→S(5,1)→Tg(5,0)
            // L: S(0,2)→C(0,2)... can not, need tile at (0,2). L bullet goes L from (1,2) to (0,2). Need tile.
            // C(0,2)rot3:L→U→S(0,3)→S(0,4)→Tg(0,5)... change target.
            // Simpler approach:
            d.Targets.Clear(); d.Targets.Add(Tg(0,5)); d.Targets.Add(Tg(5,0));
            Mi(d,1,5,0,false); // rot0:R→D, start wrong=rot1
            S(d,1,4,1,false); S(d,1,3,1,false); Sp(d,1,2,true);
            // L: goes L from splitter → (0,2)→C rot3:L→U→(0,3)→(0,4)→Tg(0,5)
            C(d,0,2,1,false); S(d,0,3,1,false); S(d,0,4,1,false);
            // R: goes R → (2,2)→(3,2)→(4,2)→C rot1:R→D→(4,1)→Tg... wait C at (4,2)? But (5,2) better.
            // R: (2,2)→(3,2)→(4,2)→C(5,2)rot1:R→D→S(5,1)→Tg(5,0)
            S(d,2,2,0,false); S(d,3,2,0,false); S(d,4,2,0,false); C(d,5,2,3,false); S(d,5,1,1,false);
            // Portal pairs for confusion
            Po(d,3,5,1,true); Po(d,3,0,1,true);
            // Decoys
            C(d,2,4,2,false); S(d,4,4,0,false); C(d,2,0,0,false); S(d,4,5,1,false);
            C(d,5,4,1,false); S(d,3,4,0,false); C(d,0,0,3,false); S(d,1,0,0,false);
            // Traps
            Ab(d,3,3); Ab(d,3,1); Ab(d,2,5); Ab(d,1,1); Bl(d,5,5); Bl(d,2,1);
            return d;
        }

        // 8: 6x6 2 turrets→2 targets, Cross intersection, portal pair
        static LevelData L08(string f) {
            var d = MK(f,7,"Cross Portal",6,6,60,25,12);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Turrets.Add(Tu(3,0,Direction.Up));
            d.Targets.Add(Tg(5,2)); d.Targets.Add(Tg(3,5));
            // T1: R→S(1,2)→S(2,2)→X(3,2)→S(4,2)→Tg(5,2)
            S(d,1,2,0,false); S(d,2,2,0,false); X(d,3,2,true); S(d,4,2,0,false);
            // T2: U→S(3,1)→X(3,2)→U→S(3,3)→Po(3,4)[1]→Po(3,4)... hmm need portal to reach (3,5)
            // Simpler: U→S(3,1)→X(3,2)→U→S(3,3)→S(3,4)→Tg(3,5)
            S(d,3,1,1,false); S(d,3,3,1,false); S(d,3,4,1,false);
            // Portal pair elsewhere for extra puzzle
            Po(d,0,0,1,true); Po(d,5,5,1,true);
            // Decoys
            C(d,1,0,1,false); S(d,5,0,0,false); C(d,0,4,2,false); S(d,1,4,0,false);
            C(d,5,4,3,false); S(d,4,4,1,false); C(d,2,4,0,false); S(d,0,5,0,false);
            C(d,4,0,2,false); S(d,5,1,0,false); C(d,1,5,1,false); S(d,2,0,1,false);
            // Traps
            Ab(d,2,3); Ab(d,4,3); Ab(d,2,1); Ab(d,4,1); Bl(d,0,3); Bl(d,5,3);
            return d;
        }

        // 9: 6x6 Splitter+Mirror+Portal→2T, many decoys
        static LevelData L09(string f) {
            var d = MK(f,8,"Mirror Portal",6,6,55,22,10);
            d.Turrets.Add(Tu(3,0,Direction.Up)); d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(5,5));
            // U→S(3,1)→S(3,2)→Sp(3,3)→L+R
            S(d,3,1,1,false); S(d,3,2,1,false); Sp(d,3,3,true);
            // L: (2,3)→(1,3)→Tg(0,3)
            S(d,2,3,0,false); S(d,1,3,0,false);
            // R: (4,3)→Mi(5,3)rot0:R→D→S(5,2)→Po(5,1)[1]→Po(2,5)[1]→D continues→hmm portal preserves dir.
            // Bullet going R enters mirror rot0: R→D. Goes down. S(5,2)→Po(5,1)→enters going D→exits Po(2,5) going D→(2,5) but that's the portal pos.
            // After portal exit: continues D from (2,5) → (2,4)? But (2,4) is below (2,5). Wait y increases upward. D means y decreases.
            // Portal at (5,1), exits at (2,5). Bullet going D from (2,5) → next pos (2,4). Need tile there.
            // Hmm this gets complex. Simpler:
            // R: (4,3)→C(5,3)rot0:R→U→S(5,4)→Tg(5,5)
            S(d,4,3,0,false); C(d,5,3,2,false); S(d,5,4,1,false);
            // Portal pairs for confusion
            Po(d,0,0,1,true); Po(d,5,0,1,true); Po(d,0,5,2,true); Po(d,4,5,2,true);
            // Decoys
            Mi(d,1,5,1,false); S(d,2,5,0,false); C(d,3,5,2,false); S(d,1,0,0,false);
            C(d,2,0,1,false); S(d,4,0,0,false); C(d,0,2,3,false); S(d,0,4,0,false);
            C(d,5,2,0,false); S(d,4,1,1,false); C(d,1,1,2,false); S(d,2,1,0,false);
            // Traps
            Ab(d,3,4); Ab(d,4,4); Ab(d,2,2); Ab(d,4,2); Bl(d,1,2); Bl(d,5,1);
            return d;
        }

        // 10: 6x6 Bomb+Splitter+Portal→2T, block walls
        static LevelData L10(string f) {
            var d = MK(f,9,"Bomb Fortress",6,6,55,20,10);
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Targets.Add(Tg(5,5)); d.Targets.Add(Tg(5,0));
            // R→S(1,3)→Bo(2,3)→R→Sp(3,3)→U+D
            S(d,1,3,0,false); Bo(d,2,3,true); Sp(d,3,3,true);
            Bl(d,2,4); Bl(d,2,2); // bomb destroys these cosmetically
            // U: S(3,4)→C(3,5)rot0:U→R→S(4,5)→Tg(5,5)
            S(d,3,4,1,false); C(d,3,5,2,false); S(d,4,5,0,false);
            // D: S(3,2)→S(3,1)→C(3,0)rot1:D→R→S(4,0)→Tg(5,0)
            S(d,3,2,1,false); S(d,3,1,1,false); C(d,3,0,3,false); S(d,4,0,0,false);
            // Portal pair
            Po(d,0,0,1,true); Po(d,5,3,1,true);
            // Decoys
            C(d,1,5,0,false); S(d,0,5,1,false); C(d,5,4,1,false); S(d,5,2,0,false);
            C(d,1,0,2,false); S(d,5,1,0,false); C(d,4,3,3,false); S(d,4,2,1,false);
            C(d,0,1,0,false); S(d,1,1,0,false); C(d,0,4,1,false); S(d,1,4,0,false);
            // Traps
            Ab(d,4,4); Ab(d,4,1); Ab(d,2,5); Ab(d,2,0); Bl(d,5,3); Bl(d,0,2);
            return d;
        }

        // ════════════════════════════════════════════════════════════════
        //  L11-L15: 7x7, 2-3 targets, 2 portal pairs, complex combos
        // ════════════════════════════════════════════════════════════════

        // 11: 7x7 Splitter+2 portal pairs→3T
        static LevelData L11(string f) {
            var d = MK(f,10,"Triple Warp",7,7,55,20,10);
            d.Turrets.Add(Tu(3,0,Direction.Up)); d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(6,3)); d.Targets.Add(Tg(3,6));
            // U→S(3,1)→S(3,2)→Sp(3,3)→L+R. Need 3rd path via 2nd turret or chain split.
            // Use 2nd turret for 3rd target:
            d.Turrets.Add(Tu(3,6,Direction.Down)); // fires Down. But target at (3,6) is same pos!
            // Fix: turret at different pos
            d.Turrets.Clear();
            d.Turrets.Add(Tu(3,0,Direction.Up)); d.Turrets.Add(Tu(0,6,Direction.Right));
            d.Targets.Clear(); d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(6,3)); d.Targets.Add(Tg(6,6));
            // T1: U→S(3,1)→S(3,2)→Sp(3,3)→L+R
            S(d,3,1,1,false); S(d,3,2,1,false); Sp(d,3,3,true);
            // L: S(2,3)→S(1,3)→Tg(0,3)
            S(d,2,3,0,false); S(d,1,3,0,false);
            // R: S(4,3)→S(5,3)→Tg(6,3)
            S(d,4,3,0,false); S(d,5,3,0,false);
            // T2: R→S(1,6)→Po(2,6)[1]→Po(5,4)[1]→R→S(6,4)→C(6,5)... need R continues. Portal preserves dir=R.
            // From Po(5,4) going R → (6,4). C(6,4)rot0:R→U→(6,5)→Tg(6,6)
            S(d,1,6,0,false); Po(d,2,6,1,true); Bl(d,3,6); Bl(d,4,6); Po(d,5,4,1,true);
            C(d,6,4,2,false); S(d,6,5,1,false);
            // Portal pair 2 for confusion
            Po(d,0,0,2,true); Po(d,6,0,2,true);
            // Decoys
            C(d,1,0,1,false); S(d,5,0,0,false); C(d,0,4,0,false); S(d,1,4,1,false);
            C(d,5,6,2,false); S(d,4,1,0,false); C(d,2,1,3,false); S(d,6,2,0,false);
            C(d,0,1,2,false); S(d,6,1,0,false); C(d,4,5,1,false); S(d,2,5,0,false);
            // Traps
            Ab(d,3,4); Ab(d,3,5); Ab(d,1,1); Ab(d,5,1); Ab(d,1,5); Ab(d,5,5);
            return d;
        }

        // 12: 7x7 Mirror chain+portal→2T
        static LevelData L12(string f) {
            var d = MK(f,11,"Mirror Warp",7,7,50,18,8);
            d.Turrets.Add(Tu(0,6,Direction.Right)); d.Targets.Add(Tg(6,0)); d.Targets.Add(Tg(0,0));
            // R→Mi(1,6)rot0:R→D→S(1,5)→S(1,4)→Sp(1,3)→L+R(perpendicular to D = L+R)
            Mi(d,1,6,1,false); S(d,1,5,1,false); S(d,1,4,1,false); Sp(d,1,3,true);
            // L: (0,3)→C(0,3)rot2:L→D→S(0,2)→S(0,1)→Tg(0,0)
            C(d,0,3,0,false); S(d,0,2,1,false); S(d,0,1,1,false);
            // R: (2,3)→Mi(3,3)rot0:R→D→S(3,2)→Po(3,1)[1]→Po(6,4)[1]→D continues→S(6,3)→S(6,2)→S(6,1)→Tg(6,0)
            Mi(d,3,3,1,false); S(d,3,2,1,false); Po(d,3,1,1,true); Po(d,6,4,1,true);
            S(d,6,3,1,false); S(d,6,2,1,false); S(d,6,1,1,false);
            S(d,2,3,0,false); // straight for R bullet to reach mirror
            // Portal pair 2
            Po(d,0,5,2,true); Po(d,5,0,2,true);
            // Decoys
            C(d,2,6,2,false); S(d,4,6,0,false); C(d,5,6,1,false); S(d,3,6,0,false);
            C(d,4,0,3,false); S(d,2,0,0,false); C(d,5,2,0,false); S(d,4,2,1,false);
            C(d,5,5,2,false); S(d,4,4,0,false); C(d,2,1,1,false); S(d,4,1,0,false);
            // Traps
            Ab(d,1,2); Ab(d,1,1); Ab(d,3,4); Ab(d,3,5); Ab(d,5,1); Ab(d,5,3); Bl(d,6,6); Bl(d,6,5);
            return d;
        }

        // 13: 7x7 2 turrets, splitter, 2 portals → 3T
        static LevelData L13(string f) {
            var d = MK(f,12,"Chaos Warp",7,7,50,16,8);
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Turrets.Add(Tu(6,3,Direction.Left));
            d.Targets.Add(Tg(3,6)); d.Targets.Add(Tg(0,6)); d.Targets.Add(Tg(6,6));
            // T1: R→S(1,3)→S(2,3)→Sp(3,3)→U+D
            S(d,1,3,0,false); S(d,2,3,0,false); Sp(d,3,3,true);
            // U: S(3,4)→S(3,5)→Tg(3,6)
            S(d,3,4,1,false); S(d,3,5,1,false);
            // D: S(3,2)→Po(3,1)[1]→Po(0,5)[1]→D continues→Tg(0,6)... wait D from (0,5)→(0,4). Need target at (0,6). Portal exits D, goes to (0,4). Wrong dir.
            // Fix: Po preserves dir=D. From (0,5) going D → (0,4)→(0,3)... misses target.
            // Use corner: D→S(3,2)→S(3,1)→C(3,0)rot2:D→L→S(2,0)→S(1,0)→C(0,0)rot3:L→U→S(0,1)→...too long
            // Simpler D path: S(3,2)→C(3,1)rot2:D→L→S(2,1)→S(1,1)→C(0,1)rot3:L→U→S(0,2)→...→Tg(0,6)
            // Too many tiles. Let me use portal smarter:
            // D: S(3,2)→S(3,1)→C(3,0)rot1:D→R→S(4,0)→S(5,0)→C(6,0)rot0:R→U→S(6,1)→S(6,2)→... but T2 uses (6,3) as turret.
            // Simplify: remove one target, use 2T + 2Tg
            d.Targets.Clear(); d.Targets.Add(Tg(3,6)); d.Targets.Add(Tg(6,6));
            // T1: R→S(1,3)→S(2,3)→C(3,3)rot0:R→U→S(3,4)→S(3,5)→Tg(3,6)
            d.Tiles.Clear();
            S(d,1,3,0,false); S(d,2,3,0,false); C(d,3,3,2,false); S(d,3,4,1,false); S(d,3,5,1,false);
            // T2: L→S(5,3)→Po(4,3)[1]→blocks→Po(4,5)[1]→L continues→S(3,5)... pos taken!
            // Fix T2: L→S(5,3)→S(4,3)→C(3,3)... pos taken by corner! Can not overlap.
            // Use cross: replace C(3,3) with X(3,3). But cross goes straight, not corner.
            // Redesign completely:
            d.Tiles.Clear();
            // T1: R→S(1,3)→S(2,3)→X(3,3)→S(4,3)→C(5,3)rot0:R→U→S(5,4)→S(5,5)→Tg... change target to (5,6)
            // T2: L→S(5,3)... pos taken by corner!
            // Just make 2 separate paths:
            d.Targets.Clear(); d.Targets.Add(Tg(6,6)); d.Targets.Add(Tg(0,6));
            S(d,1,3,0,false); S(d,2,3,0,false); C(d,3,3,2,false);
            S(d,3,4,1,false); S(d,3,5,1,false); C(d,3,6,0,false); // wait (3,6)... C rot0:U→R→goes to (4,6). Hmm
            // I'm overcomplicating. Let me just make clean level:
            d.Tiles.Clear(); d.Turrets.Clear(); d.Targets.Clear();
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Turrets.Add(Tu(6,3,Direction.Left));
            d.Targets.Add(Tg(3,0)); d.Targets.Add(Tg(3,6));
            // T1: R→S(1,3)→S(2,3)→C(3,3)rot0:R→U→S(3,4)→S(3,5)→Tg(3,6)
            S(d,1,3,0,false); S(d,2,3,0,false); C(d,3,3,2,false); S(d,3,4,1,false); S(d,3,5,1,false);
            // T2: L→S(5,3)→S(4,3)→C(3,3)... same pos! Use Cross instead:
            // Replace C(3,3) with approach: T1 goes through (3,3) differently.
            // T1: R→S(1,3)→C(2,3)rot0:R→U→S(2,4)→S(2,5)→C(2,6)rot0:U→R→Tg(3,6)... but target at col 3, need S(3,6) or just make target at (2,6)? No.
            // Final clean design:
            d.Tiles.Clear();
            // T1: R→S(1,3)→C(2,3)rot0:R→U→S(2,4)→S(2,5)→C(2,6)rot0:U→R→S(3,6)→Tg... wait target at (3,6). Place target: Tg(4,6)?
            // Scrap and simplify:
            d.Targets.Clear(); d.Targets.Add(Tg(0,0)); d.Targets.Add(Tg(6,0));
            // T1: R→C(1,3)rot1:R→D→S(1,2)→S(1,1)→C(1,0)rot1:D→R→S(2,0)→S(3,0)→... hmm complex
            // OK final approach - keep it simple and solvable:
            d.Tiles.Clear(); d.Turrets.Clear(); d.Targets.Clear();
            d.Turrets.Add(Tu(3,0,Direction.Up));
            d.Targets.Add(Tg(0,6)); d.Targets.Add(Tg(6,6));
            S(d,3,1,1,false); S(d,3,2,1,false); Sp(d,3,3,true);
            // L: S(2,3)→S(1,3)→C(0,3)rot3:L→U→S(0,4)→S(0,5)→Tg(0,6)
            S(d,2,3,0,false); S(d,1,3,0,false); C(d,0,3,1,false); S(d,0,4,1,false); S(d,0,5,1,false);
            // R: S(4,3)→S(5,3)→C(6,3)rot0:R→U→S(6,4)→S(6,5)→Tg(6,6)
            S(d,4,3,0,false); S(d,5,3,0,false); C(d,6,3,2,false); S(d,6,4,1,false); S(d,6,5,1,false);
            // Portals
            Po(d,1,0,1,true); Po(d,5,6,1,true); Po(d,5,0,2,true); Po(d,1,6,2,true);
            // Decoys
            C(d,2,5,0,false); S(d,4,5,1,false); C(d,2,1,2,false); S(d,4,1,0,false);
            C(d,3,5,1,false); S(d,3,6,0,false); C(d,0,0,3,false); S(d,6,0,0,false);
            C(d,0,6,0,false); S(d,6,6,1,false); // wait (6,6) is target! Can not place tile.
            d.Tiles.RemoveAll(t => t.Position == new Vector2Int(6,6));
            S(d,2,6,0,false); C(d,4,6,1,false);
            // Traps
            Ab(d,1,1); Ab(d,5,1); Ab(d,1,5); Ab(d,5,5); Ab(d,3,4); Ab(d,3,6,0,1,true);
            // Remove absorb at target pos
            d.Tiles.RemoveAll(t => t.Type == TileType.Absorb && t.Position == new Vector2Int(3,6));
            Bl(d,2,2); Bl(d,4,2);
            return d;
        }

        // 14-30: Increasing complexity. Due to space, using denser notation.
        static LevelData L14(string f) {
            var d = MK(f,13,"Portal Labyrinth",7,7,48,15,7);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Targets.Add(Tg(6,6)); d.Targets.Add(Tg(0,6));
            // T path splits via splitter, uses 2 portal pairs
            S(d,1,0,0,false); C(d,2,0,2,false); S(d,2,1,1,false); S(d,2,2,1,false); Sp(d,2,3,true);
            // U: S(2,4)→S(2,5)→C(2,6)rot0:U→R→S(3,6)→S(4,6)→S(5,6)→Tg(6,6)
            S(d,2,4,1,false); S(d,2,5,1,false); C(d,2,6,3,false); S(d,3,6,0,false); S(d,4,6,0,false); S(d,5,6,0,false);
            // D: S(2,2)→already placed. Split D goes from (2,3) down. Already S at (2,2). So D: (2,2)→(2,1) already S. C(2,0) already placed.
            // Split gives L+R from Up-moving bullet. L goes Left, R goes Right.
            // L: S(1,3)→C(0,3)rot3:L→U→S(0,4)→S(0,5)→Tg(0,6)
            S(d,1,3,0,false); C(d,0,3,1,false); S(d,0,4,1,false); S(d,0,5,1,false);
            // R: S(3,3)→Po(4,3)[1]→Po(6,1)[1]→R→S... portal preserves dir=R. From (6,1) going R → out of grid (6 is max in 7x7=0-6). So (6,1) is edge.
            // Fix: Po(4,3)[1]→Po(5,1)[1]→R→S(6,1)→C(6,1)... can not. S at (6,1).
            // Po(4,3)[1]→Po(4,5)[1]→R continues→S(5,5)→C(6,5)rot0:R→U→Tg(6,6)
            S(d,3,3,0,false); Po(d,4,3,1,true); Po(d,4,5,1,true); S(d,5,5,0,false); C(d,6,5,3,false);
            // Portal pair 2
            Po(d,6,0,2,true); Po(d,0,6,2,true);
            // Decoys
            C(d,4,0,1,false); S(d,5,0,0,false); C(d,6,2,2,false); S(d,5,2,0,false);
            C(d,4,4,3,false); S(d,6,4,1,false); C(d,1,5,0,false); S(d,3,4,0,false);
            C(d,5,4,1,false); S(d,1,1,0,false); C(d,3,1,2,false); S(d,5,1,1,false);
            // Traps
            Ab(d,1,2); Ab(d,3,2); Ab(d,5,3); Ab(d,1,6); Ab(d,3,5); Ab(d,6,3); Bl(d,4,1); Bl(d,4,2);
            return d;
        }

        static LevelData L15(string f) {
            var d = MK(f,14,"Bomb Split Portal",7,7,45,14,6);
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Targets.Add(Tg(6,6)); d.Targets.Add(Tg(6,0));
            S(d,1,3,0,false); Bo(d,2,3,true); Bl(d,2,4); Bl(d,2,2); Sp(d,3,3,true);
            S(d,3,4,1,false); S(d,3,5,1,false); C(d,3,6,2,false); S(d,4,6,0,false); S(d,5,6,0,false);
            S(d,3,2,1,false); S(d,3,1,1,false); C(d,3,0,3,false); S(d,4,0,0,false); S(d,5,0,0,false);
            Po(d,0,0,1,true); Po(d,6,3,1,true); Po(d,0,6,2,true); Po(d,6,3,2,true);
            // Fix duplicate portal pos: change
            d.Tiles.RemoveAll(t => t.Type == TileType.Portal && t.Position == new Vector2Int(6,3) && t.ExtraData == 2);
            Po(d,5,3,2,true);
            C(d,1,0,1,false); S(d,5,1,0,false); C(d,1,6,0,false); S(d,5,5,0,false);
            C(d,4,3,3,false); S(d,4,4,1,false); C(d,4,2,0,false); S(d,6,2,0,false);
            C(d,0,1,2,false); S(d,0,5,0,false); C(d,1,5,1,false); S(d,1,1,0,false);
            Ab(d,4,5); Ab(d,4,1); Ab(d,2,6); Ab(d,2,0); Ab(d,6,4); Ab(d,6,1); Bl(d,5,4); Bl(d,5,2);
            return d;
        }

        // ════════════════════════════════════════════════════════════════
        //  L16-L20: 8x8, 3 targets, 2-3 portal pairs
        // ════════════════════════════════════════════════════════════════

        static LevelData L16(string f) {
            var d = MK(f,15,"Triple Portal",8,8,45,14,6);
            d.Turrets.Add(Tu(4,0,Direction.Up)); d.Targets.Add(Tg(0,4)); d.Targets.Add(Tg(7,4)); d.Targets.Add(Tg(4,7));
            d.Turrets.Add(Tu(4,7,Direction.Down));
            d.Targets.Clear(); d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(7,3)); d.Targets.Add(Tg(4,7));
            S(d,4,1,1,false); S(d,4,2,1,false); Sp(d,4,3,true);
            S(d,3,3,0,false); S(d,2,3,0,false); S(d,1,3,0,false);
            S(d,5,3,0,false); S(d,6,3,0,false);
            S(d,4,6,1,false); S(d,4,5,1,false); S(d,4,4,1,false);
            Po(d,0,0,1,true); Po(d,7,0,1,true); Po(d,0,7,2,true); Po(d,7,7,2,true);
            C(d,1,0,1,false); S(d,2,0,0,false); C(d,6,0,2,false); S(d,5,0,0,false);
            C(d,1,7,0,false); S(d,2,7,1,false); C(d,6,7,3,false); S(d,5,7,0,false);
            C(d,0,1,2,false); S(d,0,5,0,false); C(d,7,1,1,false); S(d,7,5,0,false);
            C(d,2,5,0,false); S(d,6,5,1,false); C(d,2,1,3,false); S(d,6,1,0,false);
            Ab(d,3,4); Ab(d,5,4); Ab(d,3,2); Ab(d,5,2); Ab(d,1,4); Ab(d,6,4);
            Bl(d,3,1); Bl(d,5,1); Bl(d,3,5); Bl(d,5,5);
            return d;
        }

        static LevelData L17(string f) {
            var d = MK(f,16,"Mirror Portal Storm",8,8,42,12,5);
            d.Turrets.Add(Tu(0,7,Direction.Right)); d.Targets.Add(Tg(7,0)); d.Targets.Add(Tg(0,0)); d.Targets.Add(Tg(7,7));
            Mi(d,1,7,1,false); S(d,1,6,1,false); S(d,1,5,1,false); Sp(d,1,4,true);
            // L: C(0,4)rot2:L→D→S(0,3)→S(0,2)→S(0,1)→Tg(0,0)
            C(d,0,4,0,false); S(d,0,3,1,false); S(d,0,2,1,false); S(d,0,1,1,false);
            // R: S(2,4)→S(3,4)→Po(4,4)[1]→Po(7,2)[1]→R→C(7,2)... portal exits R from (7,2)→out of grid.
            // Fix: Po(4,4)[1]→Po(5,2)[1]→R→S(6,2)→C(7,2)rot1:R→D→S(7,1)→Tg(7,0)
            S(d,2,4,0,false); S(d,3,4,0,false); Po(d,4,4,1,true); Po(d,5,2,1,true);
            S(d,6,2,0,false); C(d,7,2,3,false); S(d,7,1,1,false);
            // 3rd target Tg(7,7): from T via separate route. Add Mi at (2,7): R→... but turret at (0,7).
            // The turret bullet first hits Mi(1,7). So only 1 bullet from T. Split gives 2. Need 3rd.
            // Chain: from R path after Po, split again? No, it's already split.
            // Use 2nd turret:
            d.Turrets.Add(Tu(7,7,Direction.Left)); // fires L. But target at (7,7)! Same pos.
            d.Turrets.RemoveAt(d.Turrets.Count-1);
            // Change 3rd target to something reachable:
            d.Targets.Clear(); d.Targets.Add(Tg(7,0)); d.Targets.Add(Tg(0,0));
            // Portal pair 2
            Po(d,3,7,2,true); Po(d,6,0,2,true);
            // Decoys
            C(d,2,6,2,false); S(d,4,6,0,false); C(d,5,6,1,false); S(d,3,6,0,false);
            C(d,4,0,3,false); S(d,2,0,0,false); C(d,5,0,0,false); S(d,7,4,1,false);
            C(d,7,6,2,false); S(d,6,6,0,false); C(d,2,2,1,false); S(d,4,2,0,false);
            C(d,6,4,0,false); S(d,3,2,1,false); C(d,5,4,3,false); S(d,0,6,0,false);
            Ab(d,1,3); Ab(d,1,2); Ab(d,3,5); Ab(d,3,3); Ab(d,5,5); Ab(d,5,1); Ab(d,7,3); Ab(d,7,5);
            return d;
        }

        static LevelData L18(string f) {
            var d = MK(f,17,"Portal Chain",8,8,40,12,5);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Targets.Add(Tg(7,7)); d.Targets.Add(Tg(7,0));
            // R→S(1,4)→Sp(2,4)→U+D
            S(d,1,4,0,false); Sp(d,2,4,true);
            // U: S(2,5)→Po(2,6)[1]→Po(5,6)[1]→U→S(5,7)... wait U from portal. Portal preserves dir. Bullet going U enters Po at (2,6), exits (5,6) still going U. (5,7) next. C(5,7)rot0:U→R? Target at (7,7). S(5,7)→C(6,7)rot0:... hmm.
            // U exits (5,6) going U → (5,7). Place C(5,7)rot0:U→R→S(6,7)→Tg(7,7)
            S(d,2,5,1,false); Po(d,2,6,1,true); Po(d,5,6,1,true); C(d,5,7,3,false); S(d,6,7,0,false);
            // D: S(2,3)→S(2,2)→Po(2,1)[2]→Po(5,1)[2]→D→S(5,0)→C(5,0)... pos conflict. D from (5,1)→(5,0).
            // C(5,0)rot1:D→R→S(6,0)→Tg(7,0)
            S(d,2,3,1,false); S(d,2,2,1,false); Po(d,2,1,2,true); Po(d,5,1,2,true); C(d,5,0,3,false); S(d,6,0,0,false);
            // Portal pair 3
            Po(d,0,0,3,true); Po(d,7,4,3,true);
            // Decoys (fill grid)
            C(d,0,7,0,false); S(d,1,7,1,false); C(d,3,7,2,false); S(d,4,7,0,false); C(d,7,6,1,false);
            C(d,0,2,3,false); S(d,1,2,0,false); C(d,3,2,0,false); S(d,4,2,1,false); C(d,6,2,2,false);
            S(d,3,5,0,false); C(d,4,5,1,false); S(d,6,5,0,false); C(d,7,3,3,false); S(d,7,2,0,false);
            C(d,0,6,0,false); S(d,1,6,1,false); C(d,3,0,2,false); S(d,4,0,0,false);
            Ab(d,3,4); Ab(d,4,4); Ab(d,3,3); Ab(d,4,3); Ab(d,6,4); Ab(d,1,0); Ab(d,6,6); Ab(d,1,1);
            Bl(d,3,6); Bl(d,4,6); Bl(d,3,1); Bl(d,4,1);
            return d;
        }

        static LevelData L19(string f) {
            var d = MK(f,18,"Mirror Split Chain",8,8,38,10,5);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Targets.Add(Tg(7,7)); d.Targets.Add(Tg(7,0)); d.Targets.Add(Tg(0,7));
            // 3 targets! R→Mi(1,0)rot0:R→D... header says rot0:R→D. But y=0 is bottom. D goes to y=-1 = out of grid!
            // Use rot1: R→U. R→Mi(1,0)rot1:R→U→S(1,1)→S(1,2)→Sp(1,3)→L+R
            Mi(d,1,0,0,false); S(d,1,1,1,false); S(d,1,2,1,false); Sp(d,1,3,true);
            // L: C(0,3)rot2:L→D→S(0,2)→S(0,1)→... out of useful range. Target at (0,7). Need to go UP not down.
            // L bullet goes L. C rot3: L→U. C(0,3)rot3:L→U→S(0,4)→S(0,5)→S(0,6)→Tg(0,7)
            C(d,0,3,1,false); S(d,0,4,1,false); S(d,0,5,1,false); S(d,0,6,1,false);
            // R: S(2,3)→S(3,3)→Po(4,3)[1]→Po(7,5)[1]→R→out. Hmm.
            // R: S(2,3)→S(3,3)→S(4,3)→S(5,3)→C(6,3)rot0:R→U→S(6,4)→S(6,5)→S(6,6)→C(7,6)rot0:... hmm
            // Simpler: R→S(2,3)→S(3,3)→Po(4,3)[1]→Po(6,6)[1]→R→S(7,6)→C(7,6)... same pos
            // R: (2,3)→(3,3)→Po(4,3)[1]→Po(4,6)[1]→R→S(5,6)→S(6,6)→C(7,6)rot0:R→U→Tg(7,7)
            S(d,2,3,0,false); S(d,3,3,0,false); Po(d,4,3,1,true); Po(d,4,6,1,true);
            S(d,5,6,0,false); S(d,6,6,0,false); C(d,7,6,2,false);
            // Target (7,0): need separate route. Use 2nd turret? Or chain from main path.
            // Add 2nd turret:
            d.Turrets.Add(Tu(7,0,Direction.Left)); // target at (7,0) same pos! Fix target:
            d.Targets.Clear(); d.Targets.Add(Tg(7,7)); d.Targets.Add(Tg(0,7));
            // Just 2 targets is fine for L19.
            // Portal pair 2
            Po(d,2,0,2,true); Po(d,5,7,2,true);
            // Decoys
            C(d,3,0,1,false); S(d,5,0,0,false); C(d,7,0,2,false); S(d,7,2,0,false);
            C(d,2,7,3,false); S(d,3,7,0,false); C(d,6,7,1,false); S(d,7,4,0,false);
            C(d,2,5,0,false); S(d,3,5,1,false); C(d,5,5,2,false); S(d,6,4,0,false);
            C(d,2,1,1,false); S(d,4,1,0,false); C(d,6,1,3,false); S(d,7,3,0,false);
            Ab(d,1,4); Ab(d,1,5); Ab(d,3,4); Ab(d,5,4); Ab(d,5,2); Ab(d,3,2); Ab(d,0,2); Ab(d,0,1);
            Bl(d,4,5); Bl(d,4,2); Bl(d,6,3); Bl(d,6,2);
            return d;
        }

        static LevelData L20(string f) {
            var d = MK(f,19,"Bomb Portal Split",8,8,35,8,4);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Targets.Add(Tg(7,7)); d.Targets.Add(Tg(7,0));
            S(d,1,4,0,false); Bo(d,2,4,true); Bl(d,2,5); Bl(d,2,3); Sp(d,3,4,true);
            // U: S(3,5)→S(3,6)→Po(3,7)[1]→Po(6,7)[1]→U→out. Portal preserves dir=U. From (6,7) U→(6,8)... out. Hmm.
            // Fix: Po preserves U. But (6,7) going U → y=8 out of 8x8 grid (0-7). So (6,7) is top row!
            // Change: U path without portal.
            // U: S(3,5)→S(3,6)→C(3,7)rot0:U→R→S(4,7)→S(5,7)→S(6,7)→Tg(7,7)
            S(d,3,5,1,false); S(d,3,6,1,false); C(d,3,7,2,false); S(d,4,7,0,false); S(d,5,7,0,false); S(d,6,7,0,false);
            // D: S(3,3)→S(3,2)→S(3,1)→C(3,0)rot1:D→R→S(4,0)→S(5,0)→S(6,0)→Tg(7,0)
            S(d,3,3,1,false); S(d,3,2,1,false); S(d,3,1,1,false); C(d,3,0,3,false); S(d,4,0,0,false); S(d,5,0,0,false); S(d,6,0,0,false);
            // 3 portal pairs for chaos
            Po(d,0,0,1,true); Po(d,7,4,1,true); Po(d,0,7,2,true); Po(d,7,3,2,true); Po(d,5,4,3,true); Po(d,5,3,3,true);
            // Decoys
            C(d,1,0,1,false); S(d,1,7,0,false); C(d,6,4,2,false); S(d,6,3,0,false);
            C(d,4,4,3,false); S(d,4,3,1,false); C(d,5,6,0,false); S(d,6,6,1,false);
            C(d,5,1,2,false); S(d,6,1,0,false); C(d,1,1,0,false); S(d,1,6,0,false);
            C(d,4,5,1,false); S(d,4,2,0,false); C(d,7,6,3,false); S(d,7,1,0,false);
            Ab(d,2,6); Ab(d,2,1); Ab(d,4,6); Ab(d,4,1); Ab(d,6,5); Ab(d,6,2);
            Bl(d,5,5); Bl(d,5,2); Bl(d,1,3); Bl(d,1,5);
            return d;
        }

        // ════════════════════════════════════════════════════════════════
        //  L21-L25: 9x9, 3-4 targets, 3 portal pairs
        // ════════════════════════════════════════════════════════════════

        static LevelData L21(string f) {
            var d = MK(f,20,"Triple Warp Storm",9,9,40,10,4);
            d.Turrets.Add(Tu(4,0,Direction.Up)); d.Turrets.Add(Tu(4,8,Direction.Down));
            d.Targets.Add(Tg(0,4)); d.Targets.Add(Tg(8,4)); d.Targets.Add(Tg(4,4));
            S(d,4,1,1,false); S(d,4,2,1,false); Sp(d,4,3,true);
            S(d,3,3,0,false); S(d,2,3,0,false); Po(d,1,3,1,true); Po(d,1,5,1,true);
            // Portal preserves dir=L. From (1,5) going L→(0,5). Need to reach (0,4). C(0,5)rot2:L→D→Tg(0,4)
            C(d,0,5,0,false); // need rot2:L→D
            S(d,5,3,0,false); S(d,6,3,0,false); Po(d,7,3,2,true); Po(d,7,5,2,true);
            C(d,8,5,3,false); // need rot0:R→? No. Bullet going R from (7,5) enters (8,5). But wait, portal preserves R. From (7,5) going R → (8,5)... hmm portal at (7,3) and (7,5). Bullet going R enters (7,3)? No, bullet going R from (6,3) enters (7,3) portal → exits (7,5) going R → (8,5). But we need to reach (8,4).
            // C(8,5) is wrong approach. Let portal pair go differently:
            // R: S(5,3)→S(6,3)→S(7,3)→S(8,3)→... direct to Tg(8,4)? Need corner: C(8,3)rot0:R→U→Tg(8,4)
            d.Tiles.RemoveAll(t => t.Type == TileType.Portal && t.ExtraData == 2);
            d.Tiles.RemoveAll(t => t.Position == new Vector2Int(8,5));
            C(d,8,3,2,false); // need rot0
            // T2: D→S(4,7)→S(4,6)→S(4,5)→Tg(4,4)
            S(d,4,7,1,false); S(d,4,6,1,false); S(d,4,5,1,false);
            // Portals
            Po(d,0,0,2,true); Po(d,8,8,2,true); Po(d,8,0,3,true); Po(d,0,8,3,true);
            // Decoys
            C(d,1,1,1,false); S(d,2,1,0,false); C(d,6,1,2,false); S(d,7,1,0,false);
            C(d,1,7,0,false); S(d,2,7,1,false); C(d,6,7,3,false); S(d,7,7,0,false);
            C(d,3,5,1,false); S(d,5,5,0,false); C(d,3,1,2,false); S(d,5,1,0,false);
            C(d,1,4,0,false); S(d,2,4,1,false); C(d,6,4,3,false); S(d,7,4,0,false);
            C(d,3,7,1,false); S(d,5,7,0,false); C(d,3,0,2,false); S(d,5,0,0,false);
            Ab(d,3,4); Ab(d,5,4); Ab(d,4,3); Ab(d,2,2); Ab(d,6,2); Ab(d,2,6); Ab(d,6,6);
            Bl(d,3,6); Bl(d,5,6); Bl(d,3,2); Bl(d,5,2);
            return d;
        }

        static LevelData L22(string f) {
            var d = MK(f,21,"Quad Split",9,9,38,8,3);
            d.Turrets.Add(Tu(4,0,Direction.Up)); d.Turrets.Add(Tu(4,8,Direction.Down));
            d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(8,3)); d.Targets.Add(Tg(0,5)); d.Targets.Add(Tg(8,5));
            S(d,4,1,1,false); S(d,4,2,1,false); Sp(d,4,3,true);
            S(d,3,3,0,false); S(d,2,3,0,false); S(d,1,3,0,false);
            S(d,5,3,0,false); S(d,6,3,0,false); S(d,7,3,0,false);
            S(d,4,7,1,false); S(d,4,6,1,false); Sp(d,4,5,true);
            S(d,3,5,0,false); S(d,2,5,0,false); S(d,1,5,0,false);
            S(d,5,5,0,false); S(d,6,5,0,false); S(d,7,5,0,false);
            Po(d,0,0,1,true); Po(d,8,0,1,true); Po(d,0,8,2,true); Po(d,8,8,2,true);
            Po(d,4,4,3,true); Po(d,0,4,3,true); // portal at (4,4) blocks center
            C(d,1,1,1,false); S(d,2,1,0,false); C(d,6,1,2,false); S(d,7,1,0,false);
            C(d,1,7,0,false); S(d,2,7,1,false); C(d,6,7,3,false); S(d,7,7,0,false);
            C(d,3,4,0,false); S(d,5,4,1,false); C(d,3,0,2,false); S(d,5,0,0,false);
            C(d,3,8,1,false); S(d,5,8,0,false); C(d,1,4,3,false); S(d,7,4,0,false);
            Ab(d,2,2); Ab(d,6,2); Ab(d,2,6); Ab(d,6,6); Ab(d,3,1); Ab(d,5,1); Ab(d,3,7); Ab(d,5,7);
            Bl(d,3,2); Bl(d,5,2); Bl(d,3,6); Bl(d,5,6);
            return d;
        }

        static LevelData L23(string f) {
            var d = MK(f,22,"Mirror Portal Maze",9,9,35,8,3);
            d.Turrets.Add(Tu(0,8,Direction.Right)); d.Targets.Add(Tg(8,0)); d.Targets.Add(Tg(0,0)); d.Targets.Add(Tg(8,8));
            Mi(d,1,8,1,false); S(d,1,7,1,false); S(d,1,6,1,false); Sp(d,1,5,true);
            C(d,0,5,1,false); S(d,0,4,1,false); S(d,0,3,1,false); S(d,0,2,1,false); S(d,0,1,1,false);
            S(d,2,5,0,false); S(d,3,5,0,false); Po(d,4,5,1,true); Po(d,6,2,1,true);
            S(d,7,2,0,false); C(d,8,2,2,false); S(d,8,1,1,false);
            // 3rd target (8,8): need route. Add 2nd turret.
            d.Turrets.Add(Tu(8,8,Direction.Left));
            d.Targets.RemoveAt(d.Targets.Count-1); d.Targets.Add(Tg(4,8));
            S(d,7,8,0,false); S(d,6,8,0,false); S(d,5,8,0,false);
            Po(d,0,6,2,true); Po(d,4,0,2,true); Po(d,6,6,3,true); Po(d,2,0,3,true);
            C(d,2,8,2,false); S(d,3,8,0,false); C(d,4,2,1,false); S(d,5,2,0,false);
            C(d,6,0,0,false); S(d,7,0,1,false); C(d,2,2,3,false); S(d,3,2,0,false);
            C(d,4,7,0,false); S(d,5,7,1,false); C(d,6,4,2,false); S(d,7,4,0,false);
            C(d,2,4,1,false); S(d,3,4,0,false); C(d,8,6,3,false); S(d,8,4,0,false);
            Ab(d,1,4); Ab(d,1,3); Ab(d,3,6); Ab(d,3,7); Ab(d,5,6); Ab(d,5,1); Ab(d,7,6); Ab(d,7,1);
            Ab(d,4,4); Ab(d,4,3); Bl(d,3,1); Bl(d,5,3); Bl(d,3,3); Bl(d,7,7);
            return d;
        }

        static LevelData L24(string f) {
            var d = MK(f,23,"Portal Nightmare",9,9,32,6,3);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Targets.Add(Tg(8,8)); d.Targets.Add(Tg(8,0)); d.Targets.Add(Tg(4,8));
            S(d,1,4,0,false); S(d,2,4,0,false); Sp(d,3,4,true);
            // U: S(3,5)→S(3,6)→Po(3,7)[1]→Po(7,7)[1]→U→(7,8)→Tg? Grid 0-8. C(7,8)rot0:U→R→Tg(8,8)
            S(d,3,5,1,false); S(d,3,6,1,false); Po(d,3,7,1,true); Po(d,7,7,1,true); C(d,7,8,2,false);
            // D: S(3,3)→S(3,2)→Po(3,1)[2]→Po(7,1)[2]→D→(7,0). C(7,0)rot1:D→R→Tg(8,0)
            S(d,3,3,1,false); S(d,3,2,1,false); Po(d,3,1,2,true); Po(d,7,1,2,true); C(d,7,0,3,false);
            // 3rd target: add 2nd turret
            d.Turrets.Add(Tu(4,8,Direction.Down));
            d.Targets.RemoveAt(d.Targets.Count-1); d.Targets.Add(Tg(4,0));
            S(d,4,7,1,false); S(d,4,6,1,false); S(d,4,5,1,false); S(d,4,3,1,false); S(d,4,2,1,false); S(d,4,1,1,false);
            // Portal pair 3
            Po(d,0,0,3,true); Po(d,8,4,3,true); Po(d,0,8,4,true); Po(d,8,5,4,true);
            C(d,1,0,1,false); S(d,2,0,0,false); C(d,6,0,2,false); S(d,5,0,0,false);
            C(d,1,8,0,false); S(d,2,8,1,false); C(d,6,8,3,false); S(d,5,8,0,false);
            C(d,0,2,2,false); S(d,0,6,0,false); C(d,8,2,1,false); S(d,8,6,0,false);
            C(d,5,4,0,false); S(d,6,4,1,false); C(d,1,6,3,false); S(d,2,6,0,false);
            C(d,5,6,1,false); S(d,6,6,0,false); C(d,1,2,0,false); S(d,2,2,1,false);
            Ab(d,4,4); Ab(d,3,8); Ab(d,5,5); Ab(d,5,3); Ab(d,6,7); Ab(d,6,1); Ab(d,2,7); Ab(d,2,1);
            Bl(d,3,0); Bl(d,5,0); Bl(d,0,4); Bl(d,8,3);
            return d;
        }

        static LevelData L25(string f) {
            var d = MK(f,24,"Full Arsenal",9,9,30,6,3);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Turrets.Add(Tu(8,4,Direction.Left));
            d.Targets.Add(Tg(0,8)); d.Targets.Add(Tg(8,8)); d.Targets.Add(Tg(0,0)); d.Targets.Add(Tg(8,0));
            // T1: R→S(1,4)→S(2,4)→Sp(3,4)→U+D
            S(d,1,4,0,false); S(d,2,4,0,false); Sp(d,3,4,true);
            S(d,3,5,1,false); S(d,3,6,1,false); C(d,3,7,2,false); S(d,2,7,0,false); S(d,1,7,0,false); C(d,0,7,1,false); // rot3:L→U
            S(d,3,3,1,false); S(d,3,2,1,false); C(d,3,1,0,false); S(d,2,1,0,false); S(d,1,1,0,false); C(d,0,1,2,false); // rot3:? Need D→L=rot2. Actually rot2:D→L
            // Fix corners: for U path going U from split, last corner to reach Tg(0,8):
            // C(3,7)rot0:U→R→? No need L. Change:
            // U: S(3,5)→S(3,6)→S(3,7)→C(3,8)rot... 3,8 is valid in 9x9. rot2:U→L? No. Corner rot3:U→L.
            // But originally: C(3,7) should be: bullet going U, need to turn L to reach (0,8). C rot3:U→L.
            // C(3,7) was rot2, change to initial wrong rot that player must fix.
            // D path: bullet going D from split. S(3,3)→S(3,2)→S(3,1)→C(3,0) need D→L=rot2. Start wrong.
            // T2: L→S(7,4)→S(6,4)→Sp(5,4)→U+D
            S(d,7,4,0,false); S(d,6,4,0,false); Sp(d,5,4,true);
            S(d,5,5,1,false); S(d,5,6,1,false); C(d,5,7,3,false); S(d,6,7,0,false); S(d,7,7,0,false); C(d,8,7,0,false);
            S(d,5,3,1,false); S(d,5,2,1,false); C(d,5,1,1,false); S(d,6,1,0,false); S(d,7,1,0,false); C(d,8,1,3,false);
            // Portals for chaos
            Po(d,4,0,1,true); Po(d,4,8,1,true); Po(d,0,4,2,true); Po(d,8,4,2,true);
            // Decoys
            Mi(d,4,4,1,false); C(d,4,6,0,false); S(d,4,2,1,false); C(d,2,6,1,false); S(d,6,6,0,false);
            C(d,2,2,2,false); S(d,6,2,0,false); C(d,4,3,3,false); S(d,4,5,0,false);
            // Traps
            Ab(d,1,3); Ab(d,7,3); Ab(d,1,5); Ab(d,7,5); Ab(d,2,8); Ab(d,6,8); Ab(d,2,0); Ab(d,6,0);
            Bl(d,0,3); Bl(d,8,3); Bl(d,0,5); Bl(d,8,5);
            return d;
        }

        // ════════════════════════════════════════════════════════════════
        //  L26-L30: 10x10, 4 targets, 3+ portal pairs, NIGHTMARE
        // ════════════════════════════════════════════════════════════════

        static LevelData L26(string f) {
            var d = MK(f,25,"Portal Dimension",10,10,35,8,3);
            d.Turrets.Add(Tu(5,0,Direction.Up)); d.Turrets.Add(Tu(5,9,Direction.Down));
            d.Targets.Add(Tg(0,4)); d.Targets.Add(Tg(9,4)); d.Targets.Add(Tg(0,5)); d.Targets.Add(Tg(9,5));
            S(d,5,1,1,false); S(d,5,2,1,false); Sp(d,5,3,true);
            S(d,4,3,0,false); S(d,3,3,0,false); Po(d,2,3,1,true); Po(d,2,5,1,true);
            S(d,1,5,0,false); C(d,0,5,0,false); // actual solution rot varies
            S(d,6,3,0,false); S(d,7,3,0,false); Po(d,8,3,2,true); Po(d,8,5,2,true);
            C(d,9,5,3,false);
            S(d,5,8,1,false); S(d,5,7,1,false); Sp(d,5,6,true);
            S(d,4,6,0,false); S(d,3,6,0,false); S(d,2,6,0,false); S(d,1,6,0,false); C(d,0,6,1,false);
            // Actually (0,6) going L needs to reach (0,5) or (0,4). Corner at (0,6) wrong.
            // Simplify: just make L/R from each splitter reach targets directly
            d.Tiles.RemoveAll(t => t.Position == new Vector2Int(0,6));
            S(d,6,6,0,false); S(d,7,6,0,false); S(d,8,6,0,false); C(d,9,6,2,false);
            // Fix targets reachability... this is getting very complex.
            // Let me just make it compile and be playable:
            d.Targets.Clear(); d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(9,3)); d.Targets.Add(Tg(0,6)); d.Targets.Add(Tg(9,6));
            d.Tiles.Clear();
            S(d,5,1,1,false); S(d,5,2,1,false); Sp(d,5,3,true);
            S(d,4,3,0,false); S(d,3,3,0,false); S(d,2,3,0,false); S(d,1,3,0,false);
            S(d,6,3,0,false); S(d,7,3,0,false); S(d,8,3,0,false);
            S(d,5,8,1,false); S(d,5,7,1,false); Sp(d,5,6,true);
            S(d,4,6,0,false); S(d,3,6,0,false); S(d,2,6,0,false); S(d,1,6,0,false);
            S(d,6,6,0,false); S(d,7,6,0,false); S(d,8,6,0,false);
            Bl(d,5,4); Bl(d,5,5);
            Po(d,0,0,1,true); Po(d,9,0,1,true); Po(d,0,9,2,true); Po(d,9,9,2,true); Po(d,4,4,3,true); Po(d,6,5,3,true);
            C(d,1,1,1,false); S(d,2,1,0,false); C(d,8,1,2,false); S(d,7,1,0,false);
            C(d,1,8,0,false); S(d,2,8,1,false); C(d,8,8,3,false); S(d,7,8,0,false);
            C(d,3,4,0,false); S(d,4,5,1,false); C(d,7,4,2,false); S(d,6,4,0,false);
            C(d,3,5,1,false); S(d,4,4,0,false); C(d,7,5,3,false);
            // Remove tiles at portal positions
            d.Tiles.RemoveAll(t => t.Position == new Vector2Int(4,4) && t.Type != TileType.Portal);
            Ab(d,4,2); Ab(d,6,2); Ab(d,4,7); Ab(d,6,7); Ab(d,2,4); Ab(d,8,4); Ab(d,2,5); Ab(d,8,5);
            Ab(d,3,1); Ab(d,7,1); Ab(d,3,8); Ab(d,7,8);
            return d;
        }

        static LevelData L27(string f) {
            var d = MK(f,26,"The Gauntlet",10,10,32,6,3);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Turrets.Add(Tu(9,9,Direction.Left));
            d.Targets.Add(Tg(9,0)); d.Targets.Add(Tg(0,9)); d.Targets.Add(Tg(0,5)); d.Targets.Add(Tg(9,4));
            // T1: R→ zigzag corners to (9,0). T2: L→ zigzag to (0,9). Other 2 targets via portals.
            // T1 path:
            S(d,1,0,0,false); S(d,2,0,0,false); C(d,3,0,2,false); S(d,3,1,1,false); S(d,3,2,1,false);
            C(d,3,3,3,false); S(d,4,3,0,false); S(d,5,3,0,false); C(d,6,3,1,false);
            S(d,6,2,1,false); S(d,6,1,1,false); C(d,6,0,0,false); S(d,7,0,0,false); S(d,8,0,0,false);
            // T2 path:
            S(d,8,9,0,false); S(d,7,9,0,false); C(d,6,9,1,false); S(d,6,8,1,false); S(d,6,7,1,false);
            C(d,6,6,2,false); S(d,5,6,0,false); S(d,4,6,0,false); C(d,3,6,0,false);
            S(d,3,7,1,false); S(d,3,8,1,false); C(d,3,9,3,false); S(d,2,9,0,false); S(d,1,9,0,false);
            // Targets (0,5) and (9,4) via portal detours
            Po(d,0,1,1,true); Po(d,0,4,1,true); // bullet somehow reaches. Just place as confusion.
            Po(d,9,8,2,true); Po(d,9,5,2,true);
            // Actually let me just make 2 targets (simpler, still hard):
            d.Targets.Clear(); d.Targets.Add(Tg(9,0)); d.Targets.Add(Tg(0,9));
            Po(d,0,4,1,true); Po(d,9,4,1,true); Po(d,0,5,2,true); Po(d,9,5,2,true);
            Po(d,4,4,3,true); Po(d,5,5,3,true);
            // Decoys
            C(d,1,3,0,false); S(d,2,3,1,false); C(d,8,6,3,false); S(d,7,6,0,false);
            C(d,1,6,1,false); S(d,2,6,0,false); C(d,8,3,2,false); S(d,7,3,0,false);
            C(d,4,1,3,false); S(d,5,1,0,false); C(d,4,8,0,false); S(d,5,8,1,false);
            C(d,1,5,2,false); S(d,8,4,0,false); C(d,0,7,1,false); S(d,9,2,0,false);
            Ab(d,4,0); Ab(d,5,0); Ab(d,4,9); Ab(d,5,9); Ab(d,2,2); Ab(d,7,7); Ab(d,2,7); Ab(d,7,2);
            Bl(d,4,2); Bl(d,5,7); Bl(d,0,3); Bl(d,9,6);
            return d;
        }

        static LevelData L28(string f) {
            var d = MK(f,27,"Double Split Warp",10,10,30,6,3);
            d.Turrets.Add(Tu(5,0,Direction.Up)); d.Turrets.Add(Tu(5,9,Direction.Down));
            d.Targets.Add(Tg(9,3)); d.Targets.Add(Tg(1,3)); d.Targets.Add(Tg(9,6)); d.Targets.Add(Tg(1,6));
            S(d,5,1,1,false); S(d,5,2,1,false); Sp(d,5,3,true);
            S(d,6,3,0,false); S(d,7,3,0,false); S(d,8,3,0,false);
            S(d,4,3,0,false); S(d,3,3,0,false); S(d,2,3,0,false);
            S(d,5,8,1,false); S(d,5,7,1,false); Sp(d,5,6,true);
            S(d,4,6,0,false); S(d,3,6,0,false); S(d,2,6,0,false);
            S(d,6,6,0,false); S(d,7,6,0,false); S(d,8,6,0,false);
            Bl(d,5,4); Bl(d,5,5);
            // 3 portal pairs
            Po(d,0,0,1,true); Po(d,9,0,1,true); Po(d,0,9,2,true); Po(d,9,9,2,true);
            Po(d,0,4,3,true); Po(d,9,4,3,true);
            // Decoys (fill grid)
            C(d,1,1,1,false); S(d,3,1,0,false); C(d,7,1,2,false); S(d,8,1,0,false);
            C(d,1,8,0,false); S(d,3,8,1,false); C(d,7,8,3,false); S(d,8,8,0,false);
            C(d,1,4,2,false); S(d,2,4,0,false); C(d,8,4,1,false); S(d,7,4,0,false);
            C(d,1,5,3,false); S(d,2,5,1,false); C(d,8,5,0,false); S(d,7,5,0,false);
            C(d,3,0,0,false); S(d,7,0,1,false); C(d,3,9,1,false); S(d,7,9,0,false);
            C(d,4,4,2,false); S(d,6,4,0,false); C(d,4,5,3,false); S(d,6,5,1,false);
            C(d,0,2,0,false); S(d,0,7,1,false); C(d,9,2,1,false); S(d,9,7,0,false);
            Ab(d,4,2); Ab(d,6,2); Ab(d,4,7); Ab(d,6,7); Ab(d,2,1); Ab(d,8,1); Ab(d,2,8); Ab(d,8,8);
            Ab(d,3,4); Ab(d,7,4); Ab(d,3,5); Ab(d,7,5);
            return d;
        }

        static LevelData L29(string f) {
            var d = MK(f,28,"Portal Network",10,10,28,5,2);
            d.Turrets.Add(Tu(0,5,Direction.Right)); d.Targets.Add(Tg(9,9)); d.Targets.Add(Tg(9,0)); d.Targets.Add(Tg(5,9));
            S(d,1,5,0,false); S(d,2,5,0,false); Sp(d,3,5,true);
            // U: S(3,6)→S(3,7)→Po(3,8)[1]→Po(8,8)[1]→U→(8,9). C(8,9)rot0:U→R→Tg(9,9)
            S(d,3,6,1,false); S(d,3,7,1,false); Po(d,3,8,1,true); Po(d,8,8,1,true); C(d,8,9,2,false);
            // D: S(3,4)→S(3,3)→Po(3,2)[2]→Po(8,2)[2]→D→(8,1)→C(8,0)rot1:D→R→Tg(9,0)
            S(d,3,4,1,false); S(d,3,3,1,false); Po(d,3,2,2,true); Po(d,8,2,2,true); S(d,8,1,1,false); C(d,8,0,3,false);
            // 3rd target (5,9): via separate route from R bullet of splitter
            // R from split goes R from (3,5). But exits are perpendicular to bullet dir (R→U+D). So split going R gives U+D, not L+R.
            // Wait: bullet going R enters splitter. Splitter: R→U+D (perpendicular). So exits are U and D, not L and R!
            // That means my U/D paths above are correct for a Right-moving bullet hitting the splitter.
            // For 3rd target, use 2nd turret:
            d.Turrets.Add(Tu(5,9,Direction.Down));
            d.Targets.RemoveAt(d.Targets.Count-1); d.Targets.Add(Tg(5,0));
            S(d,5,8,1,false); S(d,5,7,1,false); S(d,5,6,1,false); S(d,5,4,1,false); S(d,5,3,1,false); S(d,5,2,1,false); S(d,5,1,1,false);
            Po(d,0,0,3,true); Po(d,9,5,3,true); Po(d,0,9,4,true); Po(d,9,4,4,true);
            C(d,1,0,1,false); S(d,2,0,0,false); C(d,7,0,2,false); S(d,6,0,0,false);
            C(d,1,9,0,false); S(d,2,9,1,false); C(d,7,9,3,false); S(d,6,9,0,false);
            C(d,0,3,2,false); S(d,1,3,0,false); C(d,9,6,1,false); S(d,8,6,0,false);
            C(d,0,7,3,false); S(d,1,7,0,false); C(d,9,3,0,false); S(d,8,4,0,false);
            C(d,4,8,0,false); S(d,6,8,1,false); C(d,4,1,1,false); S(d,6,1,0,false);
            C(d,2,6,2,false); S(d,7,6,0,false); C(d,2,3,3,false); S(d,7,3,0,false);
            Ab(d,4,5); Ab(d,6,5); Ab(d,4,4); Ab(d,6,4); Ab(d,2,7); Ab(d,2,2); Ab(d,7,7); Ab(d,7,2);
            Ab(d,1,8); Ab(d,8,8); Ab(d,1,1); Ab(d,8,1);
            Bl(d,4,6); Bl(d,6,6); Bl(d,4,3); Bl(d,6,3); Bl(d,0,4); Bl(d,9,7);
            return d;
        }

        static LevelData L30(string f) {
            var d = MK(f,29,"Grand Master",10,10,25,4,2);
            d.Turrets.Add(Tu(0,5,Direction.Right)); d.Turrets.Add(Tu(9,4,Direction.Left));
            d.Targets.Add(Tg(9,9)); d.Targets.Add(Tg(0,0)); d.Targets.Add(Tg(9,0)); d.Targets.Add(Tg(0,9));
            // T1: R→S→S→Sp(3,5)→U+D
            S(d,1,5,0,false); S(d,2,5,0,false); Sp(d,3,5,true);
            // U: S(3,6)→S(3,7)→S(3,8)→C(3,9)rot0:U→R→S(4,9)→...→Tg not reachable easily.
            // U: S(3,6)→Po(3,7)[1]→Po(8,7)[1]→U→S(8,8)→C(8,9)rot0:... nah (8,9) going U→R. Tg(9,9).
            S(d,3,6,1,false); Po(d,3,7,1,true); Po(d,8,7,1,true); S(d,8,8,1,false); C(d,8,9,2,false);
            // D: S(3,4)→Po(3,3)[2]→Po(1,3)[2]→D→S(1,2)→S(1,1)→C(1,0)rot1:D→R? No need L. Target (0,0). C rot2:D→L→Tg(0,0)
            S(d,3,4,1,false); Po(d,3,3,2,true); Po(d,1,3,2,true); S(d,1,2,1,false); S(d,1,1,1,false); C(d,1,0,0,false);
            // T2: L→S→S→Sp(6,4)→U+D
            S(d,8,4,0,false); S(d,7,4,0,false); Sp(d,6,4,true);
            // U: S(6,5)→Po(6,6)[3]→Po(1,6)[3]→U→S(1,7)→S(1,8)→C(1,9)rot0:... need U→R? Tg(0,9). C rot3:U→L→Tg(0,9)
            S(d,6,5,1,false); Po(d,6,6,3,true); Po(d,1,6,3,true); S(d,1,7,1,false); S(d,1,8,1,false); C(d,1,9,1,false);
            // D: S(6,3)→Po(6,2)[4]→Po(8,2)[4]→D→S(8,1)→C(8,0)rot1:D→R→Tg(9,0)
            S(d,6,3,1,false); Po(d,6,2,4,true); Po(d,8,2,4,true); S(d,8,1,1,false); C(d,8,0,3,false);
            // Massive decoys
            C(d,0,2,1,false); S(d,2,0,0,false); C(d,9,7,2,false); S(d,7,9,0,false);
            C(d,0,7,0,false); S(d,2,9,1,false); C(d,9,2,3,false); S(d,7,0,0,false);
            C(d,4,1,2,false); S(d,5,1,0,false); C(d,4,8,1,false); S(d,5,8,0,false);
            C(d,0,4,3,false); S(d,0,6,0,false); C(d,9,5,0,false); S(d,9,3,0,false);
            Mi(d,4,4,1,false); Mi(d,5,5,1,false); Mi(d,4,5,0,false); Mi(d,5,4,0,false);
            C(d,2,2,0,false); S(d,7,7,1,false); C(d,2,7,1,false); S(d,7,2,0,false);
            C(d,4,6,2,false); S(d,5,3,0,false); C(d,4,3,3,false); S(d,5,6,1,false);
            // Traps — ring of death around center
            Ab(d,3,2); Ab(d,6,7); Ab(d,3,8); Ab(d,6,1);
            Ab(d,2,4); Ab(d,7,5); Ab(d,2,6); Ab(d,7,3);
            Ab(d,0,1); Ab(d,9,8); Ab(d,0,8); Ab(d,9,1);
            Bl(d,4,7); Bl(d,5,2); Bl(d,3,1); Bl(d,6,8); Bl(d,0,3); Bl(d,9,6);
            return d;
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        static LevelData MK(string folder,int idx,string name,int w,int h,float time,float s3,float s2) {
            string path=$"{folder}/Level_{idx+1:D2}.asset";
            var ex=AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if(ex!=null)AssetDatabase.DeleteAsset(path);
            var d=ScriptableObject.CreateInstance<LevelData>();
            d.LevelIndex=idx;d.LevelName=name;d.GridWidth=w;d.GridHeight=h;
            d.TimeLimit=time;d.ThreeStarTime=s3;d.TwoStarTime=s2;
            d.ThreeStar=3;d.TwoStar=5;d.OneStar=8;
            AssetDatabase.CreateAsset(d,path); return d;
        }
        static TurretPlacement Tu(int x,int y,Direction d)=>new TurretPlacement{Position=new Vector2Int(x,y),FireDirection=d};
        static TargetPlacement Tg(int x,int y)=>new TargetPlacement{Position=new Vector2Int(x,y)};
        static void S(LevelData d,int x,int y,int r,bool l)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Straight,Rotation=r,IsLocked=l});
        static void C(LevelData d,int x,int y,int r,bool l)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Corner,Rotation=r,IsLocked=l});
        static void X(LevelData d,int x,int y,bool l)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Cross,Rotation=0,IsLocked=l});
        static void Bl(LevelData d,int x,int y)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Block,Rotation=0,IsLocked=true});
        static void Mi(LevelData d,int x,int y,int r,bool l)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Mirror,Rotation=r,IsLocked=l});
        static void Sp(LevelData d,int x,int y,bool l)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Splitter,Rotation=0,IsLocked=l});
        static void Po(LevelData d,int x,int y,int id,bool l)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Portal,Rotation=0,IsLocked=l,ExtraData=id});
        static void Bo(LevelData d,int x,int y,bool l)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Bomb,Rotation=0,IsLocked=l});
        static void Ab(LevelData d,int x,int y)=>d.Tiles.Add(new TilePlacement{Position=new Vector2Int(x,y),Type=TileType.Absorb,Rotation=0,IsLocked=true});
        static void EnsureFolder(string p){if(AssetDatabase.IsValidFolder(p))return;var s=p.Split('/');string c=s[0];for(int i=1;i<s.Length;i++){string n=c+"/"+s[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(c,s[i]);c=n;}}
    }
}
