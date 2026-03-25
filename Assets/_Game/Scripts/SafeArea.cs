using UnityEngine;

namespace GraveyardHunter.UI
{
    /// <summary>
    /// Adjusts a RectTransform to fit within Screen.safeArea.
    ///
    /// USAGE:
    /// 1. Create an empty GameObject as a child of Canvas, name it "SafeArea"
    /// 2. Stretch its RectTransform to full canvas (anchor 0,0 → 1,1, offsets all 0)
    /// 3. Attach this script
    /// 4. Move all UI content inside this SafeArea object
    ///
    /// Hierarchy:
    ///   Canvas
    ///     └─ SafeArea  ← this script (adjusts anchors to safe region)
    ///         ├─ UIMainMenu
    ///         ├─ GameplayUI
    ///         └─ ... other panels
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            ApplySafeArea();
        }

        private void Update()
        {
            // Re-apply if screen size, orientation, or safe area changed
            if (_lastSafeArea != Screen.safeArea
                || _lastScreenSize.x != Screen.width
                || _lastScreenSize.y != Screen.height
                || _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;

            // Cache current state
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            // Guard against invalid screen dimensions
            if (Screen.width <= 0 || Screen.height <= 0) return;

            // Convert safe area from pixels to normalized anchor values (0..1)
            Vector2 anchorMin = new Vector2(
                safeArea.x / Screen.width,
                safeArea.y / Screen.height
            );
            Vector2 anchorMax = new Vector2(
                (safeArea.x + safeArea.width) / Screen.width,
                (safeArea.y + safeArea.height) / Screen.height
            );

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
