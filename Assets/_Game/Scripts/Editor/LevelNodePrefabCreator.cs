using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BulletRoute.UI;

namespace BulletRoute.Editor
{
    /// <summary>
    /// BulletRoute > Create LevelNode Prefab.
    /// Creates a single level node item that UIMainMenu will spawn per level.
    /// Edit the prefab in Inspector to customize look.
    /// </summary>
    public class LevelNodePrefabCreator
    {
        [MenuItem("BulletRoute/Create LevelNode Prefab", false, 31)]
        public static void Create()
        {
            string folder = "Assets/_Game/Prefabs/UI";
            EnsureFolder(folder);
            string path = $"{folder}/LevelNode.prefab";

            // ═══════════ ROOT ═══════════
            var root = new GameObject("LevelNode");
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(110, 110);

            // ═══════════ BACKGROUND ═══════════
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            Stretch(bgRt);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.65f, 1f, 1f);
            bgImg.pixelsPerUnitMultiplier = 3f;

            // Button (on background so it catches clicks)
            var btn = bgGo.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.85f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = Color.white;
            btn.colors = colors;

            // ═══════════ NUMBER TEXT ═══════════
            var numGo = new GameObject("NumberText");
            numGo.transform.SetParent(root.transform, false);
            var numRt = numGo.AddComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0, 0.3f);
            numRt.anchorMax = new Vector2(1, 0.95f);
            numRt.offsetMin = Vector2.zero;
            numRt.offsetMax = Vector2.zero;
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = "1";
            numTmp.fontSize = 36;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;
            numTmp.raycastTarget = false;

            // ═══════════ STARS CONTAINER ═══════════
            var starsGo = new GameObject("StarsContainer");
            starsGo.transform.SetParent(root.transform, false);
            var starsRt = starsGo.AddComponent<RectTransform>();
            starsRt.anchorMin = new Vector2(0, 0);
            starsRt.anchorMax = new Vector2(1, 0.3f);
            starsRt.offsetMin = new Vector2(5, 2);
            starsRt.offsetMax = new Vector2(-5, 0);
            var layout = starsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var star1 = CreateStar("Star1", starsGo.transform);
            var star2 = CreateStar("Star2", starsGo.transform);
            var star3 = CreateStar("Star3", starsGo.transform);

            // ═══════════ LOCK ICON ═══════════
            var lockGo = new GameObject("LockIcon");
            lockGo.transform.SetParent(root.transform, false);
            var lockRt = lockGo.AddComponent<RectTransform>();
            lockRt.anchorMin = new Vector2(0, 0.1f);
            lockRt.anchorMax = new Vector2(1, 0.9f);
            lockRt.offsetMin = Vector2.zero;
            lockRt.offsetMax = Vector2.zero;
            var lockTmp = lockGo.AddComponent<TextMeshProUGUI>();
            lockTmp.text = "\U0001F512"; // 🔒
            lockTmp.fontSize = 30;
            lockTmp.alignment = TextAlignmentOptions.Center;
            lockTmp.color = new Color(1, 1, 1, 0.6f);
            lockTmp.raycastTarget = false;
            lockGo.SetActive(false); // hidden by default

            // ═══════════ ADD LevelNodeUI + WIRE ═══════════
            var nodeUI = root.AddComponent<LevelNodeUI>();
            var so = new SerializedObject(nodeUI);
            so.FindProperty("_background").objectReferenceValue = bgImg;
            so.FindProperty("_numberText").objectReferenceValue = numTmp;
            so.FindProperty("_starsContainer").objectReferenceValue = starsGo;
            so.FindProperty("_star1").objectReferenceValue = star1;
            so.FindProperty("_star2").objectReferenceValue = star2;
            so.FindProperty("_star3").objectReferenceValue = star3;
            so.FindProperty("_lockIcon").objectReferenceValue = lockGo;
            so.FindProperty("_button").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ═══════════ SAVE ═══════════
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            EditorUtility.DisplayDialog("LevelNode Prefab Created",
                $"Saved: {path}\n\n" +
                "Edit this prefab to customize the look of each level node.\n" +
                "UIMainMenu will spawn copies of this prefab.",
                "OK");
        }

        static TextMeshProUGUI CreateStar(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "\u2605"; // ★
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.85f, 0f, 1f);
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void EnsureFolder(string p)
        {
            if (AssetDatabase.IsValidFolder(p)) return;
            var s = p.Split('/'); string c = s[0];
            for (int i = 1; i < s.Length; i++)
            { string n = c + "/" + s[i]; if (!AssetDatabase.IsValidFolder(n)) AssetDatabase.CreateFolder(c, s[i]); c = n; }
        }
    }
}
