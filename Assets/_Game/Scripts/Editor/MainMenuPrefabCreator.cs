using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BulletRoute.UI;

namespace BulletRoute.Editor
{
    /// <summary>
    /// One-click tool: BulletRoute > Create MainMenu Prefab.
    /// Creates a complete UIMainMenu prefab with all UI elements and wired references.
    /// </summary>
    public class MainMenuPrefabCreator
    {
        // ═══════════ COLORS ═══════════
        static readonly Color BG_DARK = new Color(0.08f, 0.08f, 0.15f, 0.95f);
        static readonly Color CARD_BG = new Color(0.12f, 0.14f, 0.22f, 1f);
        static readonly Color PLAY_BTN = new Color(0.2f, 0.75f, 0.3f, 1f);
        static readonly Color PLAY_BTN_TEXT = Color.white;
        static readonly Color SETTINGS_BTN = new Color(0.35f, 0.35f, 0.45f, 1f);
        static readonly Color TITLE_COLOR = new Color(1f, 0.9f, 0.3f, 1f);
        static readonly Color SUBTITLE_COLOR = new Color(0.7f, 0.75f, 0.85f, 1f);
        static readonly Color DIVIDER = new Color(1f, 1f, 1f, 0.08f);

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

            // ═══════════ BACKGROUND ═══════════
            var bg = CreateImage("Background", root.transform, BG_DARK);
            Stretch(bg.rectTransform);

            // ═══════════ HEADER ═══════════
            var header = CreateRect("Header", root.transform);
            header.anchorMin = new Vector2(0, 0.82f);
            header.anchorMax = new Vector2(1, 1f);
            header.offsetMin = new Vector2(30, 10);
            header.offsetMax = new Vector2(-30, -20);

            // Title
            var title = CreateText("TitleText", header.transform,
                "BULLET ROUTE", 48, FontStyles.Bold, TextAlignmentOptions.Center, TITLE_COLOR);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0, 0.35f);
            titleRt.anchorMax = new Vector2(1, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            // Subtitle
            var subtitle = CreateText("SubtitleText", header.transform,
                "SELECT A LEVEL", 18, FontStyles.Normal, TextAlignmentOptions.Center, SUBTITLE_COLOR);
            var subRt = subtitle.rectTransform;
            subRt.anchorMin = new Vector2(0, 0);
            subRt.anchorMax = new Vector2(1, 0.35f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;

            // Settings button (top-right gear)
            var settingsBtn = CreateButton("SettingsButton", header.transform,
                "\u2699", 28, SETTINGS_BTN, Color.white); // ⚙
            var settingsRt = settingsBtn.GetComponent<RectTransform>();
            settingsRt.anchorMin = new Vector2(1, 0.5f);
            settingsRt.anchorMax = new Vector2(1, 0.5f);
            settingsRt.pivot = new Vector2(1, 0.5f);
            settingsRt.anchoredPosition = new Vector2(0, 10);
            settingsRt.sizeDelta = new Vector2(60, 60);

            // ═══════════ DIVIDER ═══════════
            var divider = CreateImage("Divider", root.transform, DIVIDER);
            var divRt = divider.rectTransform;
            divRt.anchorMin = new Vector2(0.05f, 0.815f);
            divRt.anchorMax = new Vector2(0.95f, 0.82f);
            divRt.offsetMin = Vector2.zero;
            divRt.offsetMax = Vector2.zero;

            // ═══════════ LEVEL SCROLL AREA ═══════════
            var scrollArea = CreateRect("LevelScrollArea", root.transform);
            scrollArea.anchorMin = new Vector2(0, 0.14f);
            scrollArea.anchorMax = new Vector2(1, 0.81f);
            scrollArea.offsetMin = new Vector2(20, 10);
            scrollArea.offsetMax = new Vector2(-20, -5);

            // ScrollRect
            var scrollGo = scrollArea.gameObject;
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 30f;
            var scrollMask = scrollGo.AddComponent<Mask>();
            var scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.01f); // nearly invisible, needed for Mask
            scrollMask.showMaskGraphic = false;

            // Content container (inside scroll)
            var content = CreateRect("LevelGridContent", scrollArea.transform);
            Stretch(content);
            var gridLayout = content.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 140);
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = content;
            scrollRect.viewport = scrollArea;

            // ═══════════ FOOTER ═══════════
            var footer = CreateRect("Footer", root.transform);
            footer.anchorMin = new Vector2(0, 0);
            footer.anchorMax = new Vector2(1, 0.13f);
            footer.offsetMin = new Vector2(30, 15);
            footer.offsetMax = new Vector2(-30, -5);

            // Play button
            var playBtn = CreateButton("PlayButton", footer.transform,
                "PLAY LEVEL 1", 26, PLAY_BTN, PLAY_BTN_TEXT);
            var playBtnRt = playBtn.GetComponent<RectTransform>();
            Stretch(playBtnRt);
            playBtnRt.offsetMin = new Vector2(40, 5);
            playBtnRt.offsetMax = new Vector2(-40, -5);
            // Round the button a bit
            var playBtnImg = playBtn.GetComponent<Image>();
            playBtnImg.pixelsPerUnitMultiplier = 3f;

            // Play button text reference
            var playBtnText = playBtn.GetComponentInChildren<TextMeshProUGUI>();

            // ═══════════ WIRE REFERENCES ═══════════
            var so = new SerializedObject(menu);
            so.FindProperty("_panelName").stringValue = "MainMenu";
            so.FindProperty("_playButton").objectReferenceValue = playBtn.GetComponent<Button>();
            so.FindProperty("_settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_playButtonText").objectReferenceValue = playBtnText;
            so.FindProperty("_levelGridContainer").objectReferenceValue = content.transform;
            so.FindProperty("_levelScrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("_totalLevels").intValue = 30;
            // CanvasGroup on UIPanel base
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            // Show animation settings (fade in, no scale bounce for full-screen menu)
            so.FindProperty("_showFromScale").vector3Value = Vector3.one;
            so.FindProperty("_showFromAlpha").floatValue = 0f;
            so.FindProperty("_showDuration").floatValue = 0.4f;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false); // panels start inactive

            // ═══════════ SAVE PREFAB ═══════════
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existingPrefab != null) AssetDatabase.DeleteAsset(path);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            EditorUtility.DisplayDialog("MainMenu Prefab Created",
                $"Saved to: {path}\n\n" +
                "Drag it into your Canvas.\n" +
                "Make sure UIManager has it in _panels list with name \"MainMenu\".",
                "OK");
        }

        // ═══════════ HELPERS ═══════════

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent,
            string text, int fontSize, FontStyles style, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableAutoSizing = false;
            return tmp;
        }

        static GameObject CreateButton(string name, Transform parent,
            string label, int fontSize, Color bgColor, Color textColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button color tint
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

            // Label
            var txt = CreateText("Label", go.transform, label, fontSize,
                FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            Stretch(txt.rectTransform);

            return go;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void EnsureFolder(string path)
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
