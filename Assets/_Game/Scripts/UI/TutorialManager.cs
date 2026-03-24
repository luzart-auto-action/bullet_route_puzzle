using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Data;
using BulletRoute.Grid;

namespace BulletRoute.UI
{
    /// <summary>
    /// Self-contained tutorial for Level 1. Automatically shows on first play.
    /// Creates its own UI overlay — just add this component to any GameObject in the scene.
    /// Saves completion to PlayerPrefs so it only shows once.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        private const string PREF_KEY = "BulletRoute_TutorialComplete";

        [Header("Settings")]
        [SerializeField] private float _typingSpeed = 0.03f;
        [SerializeField] private Color _overlayColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color _highlightColor = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _instructionBgColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);

        // Auto-created UI
        private Canvas _canvas;
        private Image _overlay;
        private RectTransform _instructionPanel;
        private TextMeshProUGUI _instructionText;
        private TextMeshProUGUI _tapPrompt;
        private RectTransform _pointer;
        private TextMeshProUGUI _pointerText;

        private int _currentStep;
        private bool _isActive;
        private bool _waitingForTap;
        private bool _waitingForFire;
        private Camera _mainCamera;
        private Sequence _pointerAnim;
        private Sequence _typingSequence;

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<PlayButtonPressedEvent>(OnFirePressed);
            EventBus.Subscribe<BulletHitTargetEvent>(OnTargetHit);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<PlayButtonPressedEvent>(OnFirePressed);
            EventBus.Unsubscribe<BulletHitTargetEvent>(OnTargetHit);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            Cleanup();
        }

        private void Update()
        {
            if (!_isActive) return;

            if (_waitingForTap && UnityEngine.Input.GetMouseButtonDown(0))
            {
                _waitingForTap = false;
                NextStep();
            }
        }

        // ════════════════════════════════════════
        //  EVENT HANDLERS
        // ════════════════════════════════════════

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            // Only show tutorial for Level 1 (index 0) and if not completed
            if (evt.LevelIndex != 0) return;
            if (PlayerPrefs.GetInt(PREF_KEY, 0) == 1) return;

            // Small delay to let the level appear first
            DOVirtual.DelayedCall(0.8f, () => StartTutorial());
        }

        private void OnFirePressed(PlayButtonPressedEvent evt)
        {
            if (!_isActive || !_waitingForFire) return;
            _waitingForFire = false;
            ShowStep_BulletFlying();
        }

        private void OnTargetHit(BulletHitTargetEvent evt)
        {
            if (!_isActive) return;
            DOVirtual.DelayedCall(0.5f, () => ShowStep_Success());
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            if (_isActive) CompleteTutorial();
        }

        // ════════════════════════════════════════
        //  TUTORIAL FLOW
        // ════════════════════════════════════════

        private void StartTutorial()
        {
            _isActive = true;
            _currentStep = 0;
            CreateUI();
            ShowStep_Welcome();
        }

        private void NextStep()
        {
            _currentStep++;

            switch (_currentStep)
            {
                case 1: ShowStep_Turret(); break;
                case 2: ShowStep_Target(); break;
                case 3: ShowStep_Pipes(); break;
                case 4: ShowStep_FireButton(); break;
                default: CompleteTutorial(); break;
            }
        }

        // ── Step 0: Welcome ──
        private void ShowStep_Welcome()
        {
            ShowOverlay(true);
            HidePointer();
            TypeInstruction("Welcome to Bullet Route!\n\nRoute the bullet from the turret to the target.");
            ShowTapPrompt("Tap to continue");
            _waitingForTap = true;
        }

        // ── Step 1: Show Turret ──
        private void ShowStep_Turret()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) { NextStep(); return; }

            Vector3 turretWorld = gridManager.GridToWorldPosition(new Vector2Int(0, 2));
            PointAt(turretWorld);
            TypeInstruction("This is the TURRET.\nIt fires a bullet in the arrow direction.");
            ShowTapPrompt("Tap to continue");
            _waitingForTap = true;
        }

        // ── Step 2: Show Target ──
        private void ShowStep_Target()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) { NextStep(); return; }

            Vector3 targetWorld = gridManager.GridToWorldPosition(new Vector2Int(4, 2));
            PointAt(targetWorld);
            TypeInstruction("This is the TARGET.\nYour goal: make the bullet reach here!");
            ShowTapPrompt("Tap to continue");
            _waitingForTap = true;
        }

        // ── Step 3: Explain Pipes ──
        private void ShowStep_Pipes()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) { NextStep(); return; }

            Vector3 pipeWorld = gridManager.GridToWorldPosition(new Vector2Int(2, 2));
            PointAt(pipeWorld);
            TypeInstruction("These are PIPES. Tap them to rotate.\nArrange them to create a path!\n\n(This level is already solved for you)");
            ShowTapPrompt("Tap to continue");
            _waitingForTap = true;
        }

        // ── Step 4: Fire Button ──
        private void ShowStep_FireButton()
        {
            ShowOverlay(false); // hide overlay so player can tap Fire
            HidePointer();
            TypeInstruction("Now press the FIRE button!");
            HideTapPrompt();
            _waitingForFire = true;

            // Pulse the instruction to draw attention
            if (_instructionPanel != null)
            {
                _instructionPanel.DOScale(1.05f, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        // ── Step 5: Bullet Flying ──
        private void ShowStep_BulletFlying()
        {
            DOTween.Kill(_instructionPanel);
            if (_instructionPanel != null)
                _instructionPanel.localScale = Vector3.one;

            TypeInstruction("Watch the bullet go!");
            HideTapPrompt();
        }

        // ── Step 6: Success ──
        private void ShowStep_Success()
        {
            ShowOverlay(true);
            HidePointer();
            TypeInstruction("Excellent!\nYou completed your first level!\n\nNow try harder puzzles!");
            ShowTapPrompt("Tap to finish tutorial");
            _waitingForTap = true;
            _currentStep = 99; // next tap will complete
        }

        private void CompleteTutorial()
        {
            _isActive = false;
            PlayerPrefs.SetInt(PREF_KEY, 1);
            PlayerPrefs.Save();
            Cleanup();
        }

        // ════════════════════════════════════════
        //  UI CREATION
        // ════════════════════════════════════════

        private void CreateUI()
        {
            // Canvas (Screen Space Overlay, on top of everything)
            var canvasGo = new GameObject("TutorialCanvas");
            canvasGo.transform.SetParent(transform);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100; // on top
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            canvasGo.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Overlay (semi-transparent dark background)
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(canvasGo.transform, false);
            _overlay = overlayGo.AddComponent<Image>();
            _overlay.color = _overlayColor;
            _overlay.raycastTarget = false;
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            Stretch(overlayRt);

            // Instruction panel (bottom area)
            var panelGo = new GameObject("InstructionPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            _instructionPanel = panelGo.GetComponent<RectTransform>();
            if (_instructionPanel == null) _instructionPanel = panelGo.AddComponent<RectTransform>();
            _instructionPanel.anchorMin = new Vector2(0.05f, 0.02f);
            _instructionPanel.anchorMax = new Vector2(0.95f, 0.22f);
            _instructionPanel.offsetMin = Vector2.zero;
            _instructionPanel.offsetMax = Vector2.zero;
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = _instructionBgColor;
            panelBg.raycastTarget = false;

            // Instruction text
            var textGo = new GameObject("InstructionText");
            textGo.transform.SetParent(panelGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.05f, 0.25f);
            textRt.anchorMax = new Vector2(0.95f, 0.95f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _instructionText = textGo.AddComponent<TextMeshProUGUI>();
            _instructionText.fontSize = 28;
            _instructionText.color = _textColor;
            _instructionText.alignment = TextAlignmentOptions.Center;
            _instructionText.enableWordWrapping = true;
            _instructionText.raycastTarget = false;

            // Tap prompt
            var promptGo = new GameObject("TapPrompt");
            promptGo.transform.SetParent(panelGo.transform, false);
            var promptRt = promptGo.AddComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0, 0);
            promptRt.anchorMax = new Vector2(1, 0.25f);
            promptRt.offsetMin = Vector2.zero;
            promptRt.offsetMax = Vector2.zero;
            _tapPrompt = promptGo.AddComponent<TextMeshProUGUI>();
            _tapPrompt.fontSize = 18;
            _tapPrompt.color = new Color(1, 1, 1, 0.5f);
            _tapPrompt.alignment = TextAlignmentOptions.Center;
            _tapPrompt.fontStyle = FontStyles.Italic;
            _tapPrompt.raycastTarget = false;

            // Pointer (arrow indicator)
            var pointerGo = new GameObject("Pointer");
            pointerGo.transform.SetParent(canvasGo.transform, false);
            _pointer = pointerGo.AddComponent<RectTransform>();
            _pointer.sizeDelta = new Vector2(80, 80);
            _pointerText = pointerGo.AddComponent<TextMeshProUGUI>();
            _pointerText.text = "\u25BC"; // ▼ down arrow
            _pointerText.fontSize = 50;
            _pointerText.color = _highlightColor;
            _pointerText.alignment = TextAlignmentOptions.Center;
            _pointerText.raycastTarget = false;
            _pointer.gameObject.SetActive(false);

            // Entrance animation
            _instructionPanel.localScale = Vector3.zero;
            _instructionPanel.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        // ════════════════════════════════════════
        //  UI HELPERS
        // ════════════════════════════════════════

        private void TypeInstruction(string text)
        {
            if (_instructionText == null) return;
            _typingSequence?.Kill();
            _instructionText.text = "";
            _instructionText.maxVisibleCharacters = 0;
            _instructionText.text = text;

            int totalChars = text.Length;
            _typingSequence = DOTween.Sequence();
            _typingSequence.Append(
                DOTween.To(() => _instructionText.maxVisibleCharacters,
                    x => _instructionText.maxVisibleCharacters = x,
                    totalChars, totalChars * _typingSpeed)
                .SetEase(Ease.Linear));
            _typingSequence.SetUpdate(true);
        }

        private void ShowTapPrompt(string text)
        {
            if (_tapPrompt == null) return;
            _tapPrompt.text = text;
            _tapPrompt.gameObject.SetActive(true);
            _tapPrompt.DOFade(0.3f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void HideTapPrompt()
        {
            if (_tapPrompt == null) return;
            DOTween.Kill(_tapPrompt);
            _tapPrompt.gameObject.SetActive(false);
        }

        private void PointAt(Vector3 worldPos)
        {
            if (_pointer == null || _mainCamera == null) return;
            _pointer.gameObject.SetActive(true);

            // Convert world position to screen position
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // Convert to canvas position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screenPos, null, out Vector2 canvasPos);

            // Position pointer above the target
            _pointer.anchoredPosition = canvasPos + new Vector2(0, 80);

            // Bounce animation
            _pointerAnim?.Kill();
            _pointerAnim = DOTween.Sequence()
                .Append(_pointer.DOAnchorPosY(canvasPos.y + 60, 0.5f).SetEase(Ease.InOutSine))
                .Append(_pointer.DOAnchorPosY(canvasPos.y + 80, 0.5f).SetEase(Ease.InOutSine))
                .SetLoops(-1)
                .SetUpdate(true);
        }

        private void HidePointer()
        {
            _pointerAnim?.Kill();
            if (_pointer != null)
                _pointer.gameObject.SetActive(false);
        }

        private void ShowOverlay(bool show)
        {
            if (_overlay == null) return;
            _overlay.DOFade(show ? _overlayColor.a : 0f, 0.3f).SetUpdate(true);
            _overlay.raycastTarget = show;
        }

        private void Cleanup()
        {
            _pointerAnim?.Kill();
            _typingSequence?.Kill();
            if (_canvas != null)
            {
                DOTween.Kill(_canvas.transform);
                Destroy(_canvas.gameObject);
            }
            _canvas = null;
            _overlay = null;
            _instructionPanel = null;
            _instructionText = null;
            _tapPrompt = null;
            _pointer = null;
        }

        private void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Call to force reset tutorial (for testing).
        /// </summary>
        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(PREF_KEY);
            PlayerPrefs.Save();
        }
    }
}
