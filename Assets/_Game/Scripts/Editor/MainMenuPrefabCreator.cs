using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BulletRoute.UI;

namespace BulletRoute.Editor
{
    /// <summary>
    /// One-click: BulletRoute > Create MainMenu Prefab.
    /// Creates vertical path level map (Candy Crush style).
    /// </summary>
    public class MainMenuPrefabCreator
    {
        static readonly Color BG_TOP = new Color(0.55f, 0.82f, 0.95f, 1f);
        static readonly Color BG_BOT = new Color(0.4f, 0.7f, 0.9f, 1f);
        static readonly Color PLAY_BTN = new Color(0.3f, 0.8f, 0.25f, 1f);
        static readonly Color SETTINGS_BTN = new Color(0.35f, 0.35f, 0.45f, 0.9f);
        static readonly Color HEADER_BG = new Color(0, 0, 0, 0.15f);

        [MenuItem("BulletRoute/Create MainMenu Prefab", false, 30)]
        public static void Create()
        {
            string folder = "Assets/_Game/Prefabs/UI";
            EnsureFolder(folder);
            string path = $"{folder}/UIMainMenu.prefab";

            // ═══════════ ROOT ═══════════
            var root = new GameObject("UIMainMenu");
            var rootRt = root.AddComponent<RectTransform>();
            var cg = root.AddComponent<CanvasGroup>();
            var menu = root.AddComponent<UIMainMenu>();
            Stretch(rootRt);

            // ═══════════ BACKGROUND (gradient feel) ═══════════
            var bg = MakeImage("Background", root.transform, BG_TOP);
            Stretch(bg.rectTransform);

            // ═══════════ HEADER BAR ═══════════
            var header = MakeImage("Header", root.transform, HEADER_BG);
            var headerRt = header.rectTransform;
            headerRt.anchorMin = new Vector2(0, 0.92f);
            headerRt.anchorMax = Vector2.one;
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;

            // Settings button (top-right)
            var settingsBtn = MakeButton("SettingsButton", header.transform, "\u2699", 26, SETTINGS_BTN);
            var settingsRt = settingsBtn.GetComponent<RectTransform>();
            settingsRt.anchorMin = new Vector2(1, 0);
            settingsRt.anchorMax = new Vector2(1, 1);
            settingsRt.pivot = new Vector2(1, 0.5f);
            settingsRt.sizeDelta = new Vector2(70, 0);
            settingsRt.anchoredPosition = new Vector2(-10, 0);

            // ═══════════ SCROLL AREA (level path) ═══════════
            var scrollGo = new GameObject("LevelScroll");
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0.12f);
            scrollRt.anchorMax = new Vector2(1, 0.92f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.1f;

            // Mask
            var scrollMask = scrollGo.AddComponent<Mask>();
            var scrollBgImg = scrollGo.AddComponent<Image>();
            scrollBgImg.color = new Color(0, 0, 0, 0.01f);
            scrollMask.showMaskGraphic = false;

            // Content (where level nodes go)
            var contentGo = new GameObject("PathContent");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(1, 0);
            contentRt.pivot = new Vector2(0.5f, 0);
            contentRt.sizeDelta = new Vector2(0, 5000); // will be recalculated in code

            scrollRect.content = contentRt;
            scrollRect.viewport = scrollRt;

            // ═══════════ FOOTER (Play button) ═══════════
            var footer = MakeImage("Footer", root.transform, new Color(0, 0, 0, 0.2f));
            var footerRt = footer.rectTransform;
            footerRt.anchorMin = new Vector2(0, 0);
            footerRt.anchorMax = new Vector2(1, 0.12f);
            footerRt.offsetMin = Vector2.zero;
            footerRt.offsetMax = Vector2.zero;

            // Play button
            var playBtn = MakeButton("PlayButton", footer.transform, "Play Level 1", 30, PLAY_BTN);
            var playRt = playBtn.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.1f, 0.1f);
            playRt.anchorMax = new Vector2(0.9f, 0.9f);
            playRt.offsetMin = Vector2.zero;
            playRt.offsetMax = Vector2.zero;
            var playBtnImg = playBtn.GetComponent<Image>();
            playBtnImg.pixelsPerUnitMultiplier = 3f;

            var playBtnText = playBtn.GetComponentInChildren<TextMeshProUGUI>();

            // ═══════════ WIRE REFERENCES ═══════════
            var so = new SerializedObject(menu);
            so.FindProperty("_panelName").stringValue = "MainMenu";
            so.FindProperty("_playButton").objectReferenceValue = playBtn.GetComponent<Button>();
            so.FindProperty("_settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
            so.FindProperty("_playButtonText").objectReferenceValue = playBtnText;
            so.FindProperty("_levelPathContainer").objectReferenceValue = contentRt.transform;
            so.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("_totalLevels").intValue = 30;
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_showFromScale").vector3Value = Vector3.one;
            so.FindProperty("_showFromAlpha").floatValue = 0f;
            so.FindProperty("_showDuration").floatValue = 0.35f;

            // Auto-find LevelNode prefab
            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/LevelNode.prefab");
            if (nodePrefab != null)
            {
                var nodeUI = nodePrefab.GetComponent<LevelNodeUI>();
                so.FindProperty("_levelNodePrefab").objectReferenceValue = nodeUI;
            }
            else
            {
                Debug.LogWarning("[MainMenuPrefabCreator] LevelNode.prefab not found! Run 'Create LevelNode Prefab' first.");
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);

            // ═══════════ SAVE ═══════════
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            EditorUtility.DisplayDialog("MainMenu Prefab Created",
                $"Saved: {path}\n\n" +
                "1. Drag into Canvas\n" +
                "2. Add to UIManager _panels list with name \"MainMenu\"\n" +
                "3. Edit LevelNode.prefab to customize level node look",
                "OK");
        }

        // ═══════════ HELPERS ═══════════

        static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI MakeText(string name, Transform parent,
            string text, int fontSize, FontStyles style, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.fontStyle = style;
            tmp.alignment = align; tmp.color = color; tmp.raycastTarget = false;
            return tmp;
        }

        static GameObject MakeButton(string name, Transform parent, string label, int fontSize, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            var txt = MakeText("Label", go.transform, label, fontSize,
                FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(txt.rectTransform);
            return go;
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
