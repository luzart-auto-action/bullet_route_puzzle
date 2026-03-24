using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BulletRoute.Core;
using BulletRoute.Level;

namespace BulletRoute.Editor
{
    /// <summary>
    /// 30 levels: 5x5 → 10x10. All tile types. Menu: BulletRoute > Create 30 Levels.
    /// CORNER (bullet travel → new): rot0:Down→R,Left→Up | rot1:Up→R,Left→Down | rot2:Up→L,Right→Down | rot3:Right→Up,Down→Left
    /// MIRROR: rot0:R→D,D→R,L→U,U→L | rot1:R→U,U→R,L→D,D→L
    /// SPLITTER: Up→(R,L) Right→(D,U) Down→(L,R) Left→(U,D)
    /// STRAIGHT: rot0:vertical(Up/Down) | rot1:horizontal(Left/Right)
    /// </summary>
    public class Level30BatchCreator : EditorWindow
    {
        [MenuItem("BulletRoute/Create 30 Levels", false, 26)]
        public static void CreateLevels()
        {
            string folder = "Assets/_Game/ScriptableObjects/Levels";
            EnsureFolder(folder);
            var levels = new List<LevelData>();
            for (int i = 0; i < 30; i++) levels.Add(Build(folder, i));
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm != null) {
                var so = new SerializedObject(lm); var prop = so.FindProperty("_levels");
                prop.ClearArray();
                for (int i = 0; i < levels.Count; i++) { prop.InsertArrayElementAtIndex(i); prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i]; }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.DisplayDialog("Done", $"{levels.Count} levels created & assigned.", "OK");
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

        // ════════ EASY 5x5 (1-5) ════════

        // 1: Tutorial — straight line, all locked
        static LevelData L01(string f) {
            var d = MK(f,0,"First Steps",5,5,90,70,40);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Targets.Add(Tg(4,2));
            S(d,1,2,1,true); S(d,2,2,1,true); S(d,3,2,1,true);
            return d;
        }
        // 2: One corner. R→S(1,2)→C(2,2)[solve:rot3 R→Up]→S(2,3)→Tg(2,4)
        static LevelData L02(string f) {
            var d = MK(f,1,"First Turn",5,5,90,65,35);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Targets.Add(Tg(2,4));
            S(d,1,2,1,true); C(d,2,2,1,false); S(d,2,3,0,true);
            return d;
        }
        // 3: S-Curve. R→S→C(2,0)[rot3]→S→C(2,2)[rot1]→S→Tg
        static LevelData L03(string f) {
            var d = MK(f,2,"S-Curve",5,5,85,60,30);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Targets.Add(Tg(4,2));
            S(d,1,0,1,true); C(d,2,0,0,false); S(d,2,1,0,true); C(d,2,2,2,false); S(d,3,2,1,true);
            return d;
        }
        // 4: Mirror. Down→S(2,3)→Mi(2,2)[rot0:D→R]→S(3,2)[solve:rot1]→Tg(4,2)
        static LevelData L04(string f) {
            var d = MK(f,3,"Reflection",5,5,85,60,30);
            d.Turrets.Add(Tu(2,4,Direction.Down)); d.Targets.Add(Tg(4,2));
            S(d,2,3,0,true); Mi(d,2,2,0,true); S(d,3,2,0,false);
            return d;
        }
        // 5: Splitter. Up→S(2,1)→Sp(2,2)→R:S(3,2)→Tg(4,2) + L:S(1,2)[solve:rot1]→Tg(0,2)
        static LevelData L05(string f) {
            var d = MK(f,4,"Fork",5,5,80,55,25);
            d.Turrets.Add(Tu(2,0,Direction.Up)); d.Targets.Add(Tg(4,2)); d.Targets.Add(Tg(0,2));
            S(d,2,1,0,true); Sp(d,2,2,true); S(d,3,2,1,true); S(d,1,2,0,false);
            return d;
        }

        // ════════ MEDIUM-EASY 5x5/6x6 (6-10) ════════

        // 6: Block detour. R→C(1,2)[solve:rot3 R→Up]→S→C(1,4)[solve:rot1 Up→R]→S→S→C(4,4)[solve:rot2 R→Down]→S→Tg
        static LevelData L06(string f) {
            var d = MK(f,5,"Detour",5,5,80,52,25);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Targets.Add(Tg(4,2));
            Bl(d,2,2); Bl(d,3,2);
            C(d,1,2,0,false); S(d,1,3,0,true); C(d,1,4,0,false);
            S(d,2,4,1,true); S(d,3,4,1,true);
            C(d,4,4,2,true); S(d,4,3,0,true); // LOCKED at rot2 for testing — R→Down
            return d;
        }
        // 7: Portal jump. R→S(1,2)[solve:rot1]→Po(2,2)→...→Po(4,2)→Tg(5,2)
        static LevelData L07(string f) {
            var d = MK(f,6,"Portal Jump",6,5,75,50,25);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Targets.Add(Tg(5,2));
            S(d,1,2,0,false); Po(d,2,2,0,true); Bl(d,3,2); Po(d,4,2,0,true);
            return d;
        }
        // 8: Cross — 2 turrets share intersection
        static LevelData L08(string f) {
            var d = MK(f,7,"Crossroads",5,5,75,50,25);
            d.Turrets.Add(Tu(0,2,Direction.Right)); d.Turrets.Add(Tu(2,0,Direction.Up));
            d.Targets.Add(Tg(4,2)); d.Targets.Add(Tg(2,4));
            S(d,1,2,1,true); X(d,2,2,true); S(d,3,2,0,false);
            S(d,2,1,0,true); S(d,2,3,0,true);
            return d;
        }
        // 9: Absorb trap. R→S→C(2,1)[rot3:R→Up]→S→S→C(2,4)[rot1:Up→R]→S→S→Tg
        static LevelData L09(string f) {
            var d = MK(f,8,"Danger Zone",6,6,70,45,20);
            d.Turrets.Add(Tu(0,1,Direction.Right)); d.Targets.Add(Tg(5,4));
            S(d,1,1,1,true); C(d,2,1,2,false); Ab(d,3,1);
            S(d,2,2,0,true); S(d,2,3,0,true); C(d,2,4,0,false);
            S(d,3,4,1,true); S(d,4,4,1,true);
            return d;
        }
        // 10: Bomb. R→S→Bomb(2,3)→S(3,3)[solve:rot1]→S→Tg. Bomb destroys Bl(2,4),Bl(2,2)
        static LevelData L10(string f) {
            var d = MK(f,9,"Bomb Squad",6,6,70,45,20);
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Targets.Add(Tg(5,3));
            S(d,1,3,1,true); Bo(d,2,3,true); Bl(d,2,4); Bl(d,2,2);
            S(d,3,3,0,false); S(d,4,3,1,true);
            return d;
        }

        // ════════ MEDIUM 6x6/7x7 (11-15) ════════

        // 11: Staircase 3 corners. R→S→C(2,0)[rot3]→S→S→C(2,3)[rot1]→S→C(4,3)[rot3]→S→C(4,5)[rot1]→Tg(5,5)
        static LevelData L11(string f) {
            var d = MK(f,10,"Staircase",6,6,65,42,20);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Targets.Add(Tg(5,5));
            S(d,1,0,1,true); C(d,2,0,2,false);
            S(d,2,1,0,true); S(d,2,2,0,true); C(d,2,3,3,false);
            S(d,3,3,1,true); C(d,4,3,0,false);
            S(d,4,4,0,true); C(d,4,5,3,false);
            return d;
        }
        // 12: Mirror bounce. R→S→Mi(2,5)[rot0:R→D]→S→S→S→Mi(2,1)[rot0:D→R]→S[solve:rot1]→S→Mi(5,1)[rot0:R→D]→Tg(5,0)
        static LevelData L12(string f) {
            var d = MK(f,11,"Mirror Bounce",6,6,65,42,20);
            d.Turrets.Add(Tu(0,5,Direction.Right)); d.Targets.Add(Tg(5,0));
            S(d,1,5,1,true); Mi(d,2,5,0,true);
            S(d,2,4,0,true); S(d,2,3,0,true); S(d,2,2,0,true); Mi(d,2,1,0,true);
            S(d,3,1,0,false); S(d,4,1,1,true); Mi(d,5,1,0,true);
            return d;
        }
        // 13: Portal+corner (7x7). R→S→S→Po(3,3,0)→wall→Po(5,3,0)→R→C(6,3)[rot3:R→Up]→S→S→Tg(6,6)
        static LevelData L13(string f) {
            var d = MK(f,12,"Portal Turn",7,7,60,38,18);
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Targets.Add(Tg(6,6));
            S(d,1,3,1,true); S(d,2,3,1,true);
            Po(d,3,3,0,true); Bl(d,4,3); Po(d,5,3,0,true); // portal continues Right
            C(d,6,3,0,false); // solve:rot3 (R→Up)
            S(d,6,4,0,true); S(d,6,5,0,true);
            return d;
        }
        // 14: Four-corner zigzag (7x7). R→S→C(2,0)[rot3]→S→C(2,2)[rot1]→S→C(4,2)[rot3]→S→C(4,4)[rot1]→S→Tg
        static LevelData L14(string f) {
            var d = MK(f,13,"Zigzag Pro",7,7,60,38,18);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Targets.Add(Tg(6,4));
            S(d,1,0,1,true); C(d,2,0,1,false);
            S(d,2,1,0,true); C(d,2,2,3,false);
            S(d,3,2,1,true); C(d,4,2,0,false);
            S(d,4,3,0,true); C(d,4,4,2,false);
            S(d,5,4,1,true);
            return d;
        }
        // 15: Splitter+Cross 3 targets. T1 Up→S→S→Sp(3,3)→R+L. T2 Down through X(5,3)
        static LevelData L15(string f) {
            var d = MK(f,14,"Split Cross",7,7,55,35,15);
            d.Turrets.Add(Tu(3,0,Direction.Up)); d.Turrets.Add(Tu(5,6,Direction.Down));
            d.Targets.Add(Tg(6,3)); d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(5,0));
            S(d,3,1,0,true); S(d,3,2,0,true); Sp(d,3,3,true);
            S(d,4,3,0,false); X(d,5,3,true);
            S(d,2,3,1,true); S(d,1,3,0,false);
            S(d,5,5,0,true); S(d,5,4,0,true); S(d,5,2,0,true); S(d,5,1,0,true);
            return d;
        }

        // ════════ MEDIUM-HARD 7x7/8x8 (16-20) ════════

        // 16: Block wall detour. R→C(1,3)[rot3]→S→S→C(1,6)[rot1]→S→S→S→C(5,6)[rot2]→S→S→C(5,3)[rot0]→Tg
        static LevelData L16(string f) {
            var d = MK(f,15,"The Wall",7,7,55,35,15);
            d.Turrets.Add(Tu(0,3,Direction.Right)); d.Targets.Add(Tg(6,3));
            Bl(d,2,3); Bl(d,3,3); Bl(d,3,2); Bl(d,3,4);
            C(d,1,3,0,false); S(d,1,4,0,true); S(d,1,5,0,true); C(d,1,6,3,false);
            S(d,2,6,1,true); S(d,3,6,1,true); S(d,4,6,1,true); C(d,5,6,0,false);
            S(d,5,5,0,true); S(d,5,4,0,true); C(d,5,3,1,false);
            return d;
        }
        // 17: 2 turrets, 2 targets. T1:R straight row. T2:R straight with unlocked straights
        static LevelData L17(string f) {
            var d = MK(f,16,"Double Trouble",7,7,55,35,15);
            // T1: straight row with 2 unlocked straights
            d.Turrets.Add(Tu(0,1,Direction.Right)); d.Targets.Add(Tg(6,1));
            S(d,1,1,1,true); S(d,2,1,0,false); S(d,3,1,1,true); S(d,4,1,0,false); S(d,5,1,1,true);
            // T2: straight row with 2 unlocked straights
            d.Turrets.Add(Tu(0,5,Direction.Right)); d.Targets.Add(Tg(6,5));
            S(d,1,5,1,true); S(d,2,5,0,false); S(d,3,5,1,true); S(d,4,5,0,false); S(d,5,5,1,true);
            return d;
        }
        // 18: Mirror chain (8x8). R→Mi[R→D]→S→S→Mi[D→R]→S[solve]→S→Mi[R→D]→S→S→Mi[D→R]→S[solve]→Mi[R→D]→Tg(7,0)
        static LevelData L18(string f) {
            var d = MK(f,17,"Mirror Chain",8,8,50,30,15);
            d.Turrets.Add(Tu(0,7,Direction.Right)); d.Targets.Add(Tg(7,0));
            S(d,1,7,1,true); Mi(d,2,7,0,true);
            S(d,2,6,0,true); S(d,2,5,0,true); Mi(d,2,4,0,false);
            S(d,3,4,0,false); S(d,4,4,1,true); Mi(d,5,4,0,true);
            S(d,5,3,0,true); S(d,5,2,0,true); Mi(d,5,1,0,false);
            S(d,6,1,0,false); Mi(d,7,1,0,true);
            return d;
        }
        // 19: Portal chain (8x8). R→S[solve]→Po(2,4,0)→wall→Po(6,1,0)→C(7,1)[rot3:R→Up]→S→...→Tg(7,7)
        static LevelData L19(string f) {
            var d = MK(f,18,"Portal Chain",8,8,50,30,15);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Targets.Add(Tg(7,7));
            S(d,1,4,0,false); Po(d,2,4,0,true);
            Bl(d,3,4); Bl(d,4,4); Bl(d,5,4);
            Po(d,6,1,0,true);
            C(d,7,1,0,false);
            S(d,7,2,0,true); S(d,7,3,0,true); S(d,7,4,0,true);
            S(d,7,5,0,true); S(d,7,6,0,true);
            return d;
        }
        // 20: Bomb path (8x8). R→S→Bomb(2,4)→S→S[solve]→C(5,4)[rot3:R→Up]→S→S→C(5,7)[rot1:Up→R]→S→Tg
        static LevelData L20(string f) {
            var d = MK(f,19,"Demolition",8,8,50,30,15);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Targets.Add(Tg(7,7));
            S(d,1,4,1,true); Bo(d,2,4,true); Bl(d,2,5); Bl(d,2,3);
            S(d,3,4,1,true); S(d,4,4,0,false);
            C(d,5,4,0,false); S(d,5,5,0,true); S(d,5,6,0,true); C(d,5,7,3,false);
            S(d,6,7,1,true);
            return d;
        }

        // ════════ HARD 8x8/9x9 (21-25) ════════

        // 21: Splitter + 2 targets (8x8). Up→S→S→Sp(4,3)→R:S[solve]→S→Tg(7,3) + L:S→S[solve]→Tg(1,3)
        static LevelData L21(string f) {
            var d = MK(f,20,"Split & Turn",8,8,48,28,12);
            d.Turrets.Add(Tu(4,0,Direction.Up)); d.Targets.Add(Tg(7,3)); d.Targets.Add(Tg(1,3));
            S(d,4,1,0,true); S(d,4,2,0,true); Sp(d,4,3,true);
            S(d,5,3,0,false); S(d,6,3,1,true);
            S(d,3,3,1,true); S(d,2,3,0,false);
            Ab(d,4,4); Bl(d,4,5);
            return d;
        }
        // 22: Spiral (8x8). R along bottom→C[rot3]→Up along right→C[rot2]→L along top→C[rot1]→D→C[rot0]→R→Tg
        static LevelData L22(string f) {
            var d = MK(f,21,"Spiral",8,8,48,28,12);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Targets.Add(Tg(4,4));
            S(d,1,0,1,true); S(d,2,0,1,true); S(d,3,0,1,true); S(d,4,0,1,true); S(d,5,0,1,true);
            C(d,6,0,2,false);
            S(d,6,1,0,true); S(d,6,2,0,true); S(d,6,3,0,true); S(d,6,4,0,true); S(d,6,5,0,true);
            C(d,6,6,0,false);
            S(d,5,6,1,true); S(d,4,6,0,false); S(d,3,6,1,true);
            C(d,2,6,1,false);
            S(d,2,5,0,true); C(d,2,4,1,false);
            S(d,3,4,1,true);
            return d;
        }
        // 23: 3 targets splitter+turret (9x9). T1 Up→Sp(4,3)→R/L. T2 Down→Tg(4,5)
        static LevelData L23(string f) {
            var d = MK(f,22,"Triple Threat",9,9,45,26,10);
            d.Turrets.Add(Tu(4,0,Direction.Up)); d.Turrets.Add(Tu(4,8,Direction.Down));
            d.Targets.Add(Tg(8,3)); d.Targets.Add(Tg(0,3)); d.Targets.Add(Tg(4,5));
            S(d,4,1,0,true); S(d,4,2,0,true); Sp(d,4,3,true);
            S(d,5,3,0,false); S(d,6,3,1,true); S(d,7,3,1,true);
            S(d,3,3,1,true); S(d,2,3,0,false); S(d,1,3,1,true);
            S(d,4,7,0,true); S(d,4,6,0,true);
            Bl(d,4,4);
            return d;
        }
        // 24: Portal maze (9x9). R→S[solve]→Po(2,4,0)→wall→Po(6,7,0)→S→C(8,7)[rot3:R→Up]→Tg(8,8)
        static LevelData L24(string f) {
            var d = MK(f,23,"Portal Maze",9,9,45,26,10);
            d.Turrets.Add(Tu(0,4,Direction.Right)); d.Targets.Add(Tg(8,8));
            S(d,1,4,0,false); Po(d,2,4,0,true);
            Bl(d,3,4); Bl(d,4,4); Bl(d,5,4);
            Po(d,6,7,0,true);
            S(d,7,7,1,true); C(d,8,7,0,false);
            return d;
        }
        // 25: All tiles (9x9). R→S→Mi[R→D]→S→Sp(2,2)→main L + split R. L→C(1,2)[rot2:L→Down]→S→C(1,0)[rot0:D→R]→...→Tg(8,1). R→S→...→Tg(8,2)
        static LevelData L25(string f) {
            var d = MK(f,24,"Full Arsenal",9,9,42,24,10);
            d.Turrets.Add(Tu(0,4,Direction.Right));
            d.Targets.Add(Tg(8,1)); d.Targets.Add(Tg(8,2));
            S(d,1,4,1,true); Mi(d,2,4,0,true); // R→D
            S(d,2,3,0,true); Sp(d,2,2,true); // D→(main:L, split:R)
            // L path→Tg(8,1): bullet Left at C(1,2) needs rot1 (Left→Down)
            C(d,1,2,0,false); // solve:rot1 (Left→Down, 1 tap)
            S(d,1,1,0,true); C(d,1,0,3,false); // solve:rot0 (D→R)
            S(d,2,0,1,true); S(d,3,0,1,true); S(d,4,0,1,true);
            S(d,5,0,1,true); S(d,6,0,1,true); S(d,7,0,1,true);
            C(d,8,0,0,false); // solve:rot3 (R→Up)
            // R path→Tg(8,2)
            S(d,3,2,0,false); S(d,4,2,1,true); S(d,5,2,1,true); S(d,6,2,0,false); S(d,7,2,1,true);
            // Traps
            Ab(d,3,4); Bl(d,4,4); Bo(d,6,4,true); Bl(d,6,5); Bl(d,6,3);
            return d;
        }

        // ════════ EXPERT 9x9/10x10 (26-30) ════════

        // 26: Mirror zigzag (9x9). R→Mi→D→D→D→Mi→R→R→Mi→D→D→D→Mi→R→R→Mi→D→Tg
        static LevelData L26(string f) {
            var d = MK(f,25,"Mirror Dimension",9,9,40,22,8);
            d.Turrets.Add(Tu(0,8,Direction.Right)); d.Targets.Add(Tg(8,0));
            S(d,1,8,1,true); Mi(d,2,8,0,true);
            S(d,2,7,0,true); S(d,2,6,0,true); Mi(d,2,5,0,false);
            S(d,3,5,0,false); S(d,4,5,1,true); Mi(d,5,5,0,true);
            S(d,5,4,0,true); S(d,5,3,0,true); Mi(d,5,2,0,false);
            S(d,6,2,0,false); S(d,7,2,1,true); Mi(d,8,2,0,true);
            S(d,8,1,0,true);
            return d;
        }
        // 27: Staircase (10x10). R→R→C[rot3]→Up→Up→C[rot1]→R→R→C[rot3]→Up→Up→C[rot1]→R→R→C[rot3]→Up→Up→Tg
        static LevelData L27(string f) {
            var d = MK(f,26,"The Gauntlet",10,10,40,22,8);
            d.Turrets.Add(Tu(0,0,Direction.Right)); d.Targets.Add(Tg(9,9));
            S(d,1,0,1,true); S(d,2,0,1,true); C(d,3,0,2,false);
            S(d,3,1,0,true); S(d,3,2,0,true); C(d,3,3,0,false);
            S(d,4,3,1,true); S(d,5,3,1,true); C(d,6,3,1,false);
            S(d,6,4,0,true); S(d,6,5,0,true); C(d,6,6,3,false);
            S(d,7,6,1,true); S(d,8,6,1,true); C(d,9,6,0,false);
            S(d,9,7,0,true); S(d,9,8,0,true);
            return d;
        }
        // 28: Double splitter 4 targets (10x10). T1 Up→Sp(5,3)→R/L direct. T2 Down→Sp(5,6)→R/L direct
        static LevelData L28(string f) {
            var d = MK(f,27,"Double Split",10,10,38,20,8);
            d.Turrets.Add(Tu(5,0,Direction.Up)); d.Turrets.Add(Tu(5,9,Direction.Down));
            d.Targets.Add(Tg(9,3)); d.Targets.Add(Tg(1,3));
            d.Targets.Add(Tg(9,6)); d.Targets.Add(Tg(1,6));
            // T1 Up→Sp(5,3)→R(6,3)→(7,3)→(8,3)→Tg(9,3) + L(4,3)→(3,3)→(2,3)→Tg(1,3)
            S(d,5,1,0,true); S(d,5,2,0,true); Sp(d,5,3,true);
            S(d,6,3,0,false); S(d,7,3,1,true); S(d,8,3,1,true);
            S(d,4,3,1,true); S(d,3,3,0,false); S(d,2,3,1,true);
            // T2 Down→Sp(5,6)→L(4,6)→(3,6)→(2,6)→Tg(1,6) + R(6,6)→(7,6)→(8,6)→Tg(9,6)
            S(d,5,8,0,true); S(d,5,7,0,true); Sp(d,5,6,true);
            S(d,4,6,1,true); S(d,3,6,0,false); S(d,2,6,1,true);
            S(d,6,6,0,false); S(d,7,6,1,true); S(d,8,6,1,true);
            // Wall between the two splitter zones
            Bl(d,5,4); Bl(d,5,5);
            return d;
        }
        // 29: Portal+corners (10x10). R→S[solve]→Po(2,5,0)→wall→Po(6,8,0)→S→C(8,8)[rot3]→C(8,9)[rot1:Up→R]→Tg(9,9)
        static LevelData L29(string f) {
            var d = MK(f,28,"Portal Network",10,10,38,20,8);
            d.Turrets.Add(Tu(0,5,Direction.Right)); d.Targets.Add(Tg(9,9));
            S(d,1,5,0,false); Po(d,2,5,0,true);
            Bl(d,3,5); Bl(d,4,5); Bl(d,5,5);
            Po(d,6,8,0,true);
            S(d,7,8,1,true); C(d,8,8,0,false);
            C(d,8,9,3,false);
            return d;
        }
        // 30: Grand Master (10x10). T1:R→Mi→D→C→R through X→C→Up→C→R→Tg(9,5). T2:Up through X→straight up→Tg(5,9)
        static LevelData L30(string f) {
            var d = MK(f,29,"Grand Master",10,10,35,18,6);
            d.Turrets.Add(Tu(0,5,Direction.Right)); d.Turrets.Add(Tu(5,0,Direction.Up));
            d.Targets.Add(Tg(9,5)); d.Targets.Add(Tg(5,9));
            // T1: R→Mi(2,5)[R→D]→S→S→C(2,2)[rot0:D→R]→S→S[solve]→X(5,2)→S[solve]→S→C(8,2)[rot3:R→Up]→S→S→C(8,5)[rot1:Up→R]→Tg(9,5)
            S(d,1,5,1,true); Mi(d,2,5,0,true);
            S(d,2,4,0,true); S(d,2,3,0,true); C(d,2,2,1,false);
            S(d,3,2,1,true); S(d,4,2,0,false); X(d,5,2,true);
            S(d,6,2,0,false); S(d,7,2,1,true); C(d,8,2,0,false);
            S(d,8,3,0,true); S(d,8,4,0,true); C(d,8,5,2,false);
            // T2: Up→S(5,1)→X(5,2) passes Up→S→S→S→S→S→S→Tg(5,9)
            S(d,5,1,0,true);
            S(d,5,3,0,true); S(d,5,4,0,true); S(d,5,5,0,true);
            S(d,5,6,0,true); S(d,5,7,0,true); S(d,5,8,0,true);
            // Traps
            Ab(d,9,2); Bl(d,9,3); Ab(d,3,5); Bl(d,4,5);
            return d;
        }

        // ════════ HELPERS ════════

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
