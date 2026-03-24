using DG.Tweening;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Luzart
{
    public class TweenAnimation : TweenAnimationBase
    {
        [SerializeField] private EAnimation typeAnimation;
        [SerializeField] private TweenAnimationSettings tweenAnimationSettings = new TweenAnimationSettings();
        private ITweenAnimation _currentTweenAnimation;
        public bool IsAnimationVector3 => typeAnimation == EAnimation.Move ||
                                                typeAnimation == EAnimation.MoveLocal ||
                                                typeAnimation == EAnimation.MoveAnchors ||
                                                typeAnimation == EAnimation.Scale ||
                                                typeAnimation == EAnimation.Euler ||
                                                typeAnimation == EAnimation.SizeDelta ||
                                                typeAnimation == EAnimation.AnchorMin ||
                                                typeAnimation == EAnimation.AnchorMax;
        public bool IsAnimationFloat => typeAnimation == EAnimation.FadeByCanvasGroup ||
            typeAnimation == EAnimation.Float;

        protected override Tween DoShow()
        {
            var tweenAnimation = GetTweenAnimation();
            if (tweenAnimation == null)
            {
                Debug.LogError("Tween Animation Type not found: " + typeAnimation.ToString());
                return null;
            }
            if(tweenAnimationSettings.General.Target == null)
            {
                tweenAnimationSettings.General.Target = this.gameObject;
            }

            tweenAnimation.InitSetting(tweenAnimationSettings);
            _currentTweenAnimation = tweenAnimation;
            return tweenAnimation.Show();
        }

        protected override void DoDispose()
        {
            _currentTweenAnimation?.Dispose();
            _currentTweenAnimation = null;
        }

        private ITweenAnimation GetTweenAnimation()
        {
            return typeAnimation switch
            {
                EAnimation.Move => new TweenAnimationMove(),
                EAnimation.MoveLocal => new TweenAnimationMoveLocal(),
                EAnimation.MoveAnchors => new TweenAnimationMoveAnchors(),
                EAnimation.Euler => new TweenAnimationEuler(),
                EAnimation.Scale => new TweenAnimationScale(),
                EAnimation.SizeDelta => new TweenAnimationSizeDelta(),
                EAnimation.AnchorMin => new TweenAnimationAnchorMin(),
                EAnimation.AnchorMax => new TweenAnimationAnchorMax(),
                EAnimation.FadeByCanvasGroup => new TweenAnimationFade(),
                EAnimation.TextMeshProDOText => new TweenAnimationTextMeshPro(),
                EAnimation.Float => new TweenAnimationFade(), // Float animation uses same logic as Fade
                EAnimation.UnityEvent => new TweenAnimationFade(),
                _ => null
            };
        }

        [ContextMenu("Set Default Settings")]
        private void SetDefaultSettings()
        {
            tweenAnimationSettings.Values.Vector3To = Vector3Int.one * -1;
            tweenAnimationSettings.Values.Vector3From = Vector3Int.one * -1;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (tweenAnimationSettings == null || tweenAnimationSettings.General == null || tweenAnimationSettings.General.Target == null)
            {
                return;
            }
            AddTweenAnimation();
#endif
        }
        private void Reset()
        {
            AddTweenAnimation();
            tweenAnimationSettings.General.Target = gameObject;
        }
        private void AddTweenAnimation()
        {
            if (typeAnimation == EAnimation.FadeByCanvasGroup || typeAnimation == EAnimation.Float)
            {
                if (tweenAnimationSettings.General.Target is not CanvasGroup)
                {
                    GameObject go = tweenAnimationSettings.General.Target as GameObject;
                    if (go == null)
                    {
                        var comp = tweenAnimationSettings.General.Target as Component;
                        if (comp != null)
                        {
                            go = comp.gameObject;
                        }
                    }
                    if(go == null)
                    {
                        throw new Exception("Tween Animation Fade: Target is not GameObject or Component");
                    }
                    var canvas = go.GetComponent<CanvasGroup>();
                    if (canvas == null)
                    {
                        canvas = go.AddComponent<CanvasGroup>();
                    }
                    tweenAnimationSettings.General.Target = canvas;
                }
            }
        }

        public override ITweenSettings GetTweenAnimationSettings()
        {
            return tweenAnimationSettings;
        }
    }

    #region Data Structures

    // Interface for all settings types
    public interface ITweenSettings
    {
        public float Duration { get; }
        public bool IgnoreTimeScale { get; }
        TweenTimingSettings Timing { get; }
        TweenLoopSettings Loop { get; }
    }

    // Full settings for individual animations
    [System.Serializable]
    public class TweenAnimationSettings : ITweenSettings
    {
        public TweenGeneralSettings General;
        
        public TweenTimingSettings Timing;
        
        public TweenLoopSettings Loop;
        
        public TweenValueSettings Values;

        TweenTimingSettings ITweenSettings.Timing => Timing;
        TweenLoopSettings ITweenSettings.Loop => Loop;

        float ITweenSettings.Duration
        {
            get
            {
                if(Loop.IsLoop)
                {
                    if (Loop.LoopCount < 0)
                    {
                        return float.MaxValue;
                    }
                    return Timing.DelayStart + (General.Duration + Timing.TimeDelayPreLoop + Timing.TimeDelayAfterLoop) * Loop.LoopCount;
                }
                else
                {
                    return Timing.DelayStart + General.Duration;
                }
            }
        }

        public bool IgnoreTimeScale => General.IsIgnoreTimeScale;

        public TweenAnimationSettings()
        {
            General = new TweenGeneralSettings();
            Timing = new TweenTimingSettings();
            Loop = new TweenLoopSettings();
            Values = new TweenValueSettings();
        }
    }

    [System.Serializable]
    public class TweenGeneralSettings
    {
        public UnityEngine.Object Target;
        public float Duration = 1f;
        public Ease Easing = Ease.Linear;
        public bool IsIgnoreTimeScale = false;
    }

    [System.Serializable]
    public struct TweenTimingSettings
    {
        public float DelayStart;
        [ShowIf("../Loop.IsLoop", true)]
        public float TimeDelayPreLoop;
        [ShowIf("../Loop.IsLoop", true)]
        public float TimeDelayAfterLoop;
    }

    [System.Serializable]
    public struct TweenLoopSettings
    {
        public bool IsLoop;
        public LoopType LoopType;
        public int LoopCount;
    }

    [System.Serializable]
    public class TweenValueSettings
    {
        public bool IsSetFromInInit = false;

        public bool IsSetRuntimeFrom = false;
        public bool IsSetRuntimeTo = false;

        [ShowIf("../../IsAnimationVector3", true)]
        public Vector3 Vector3From = -Vector3Int.one;

        [ShowIf("../../IsAnimationVector3", true)]
        public Vector3 Vector3To = -Vector3Int.one;

        [ShowIf("../../IsAnimationFloat", true)]
        public float FloatFrom = -1;
        
        [ShowIf("../../IsAnimationFloat", true)]
        public float FloatTo = -1;

        [ShowIf("../../typeAnimation", EAnimation.TextMeshProDOText)]
        public string StringFrom = "";
        
        [ShowIf("../../typeAnimation", EAnimation.TextMeshProDOText)]
        [DisableIf("../General.Target", null)]
        public string StringTo = "";

        [ShowIf("../../typeAnimation", EAnimation.Float)]
        public UnityEvent<float> OnFloatUnityEventInvoke;
        [ShowIf("../../typeAnimation", EAnimation.UnityEvent)]
        public UnityEvent OnUnityEventInvoke;

        // Helper methods to get type-specific values
        public Vector3 GetVector3From() => Vector3From;
        public Vector3 GetVector3To() => Vector3To;
        public float GetFloatFrom() => FloatFrom;
        public float GetFloatTo() => FloatTo;
        public string GetStringFrom() => StringFrom;
        public string GetStringTo() => StringTo;
    }
    

    public enum ETypeShow
    {
        None = 0,
        Awake = 1,
        Start = 2,
        OnEnable = 3,
    }

    public enum EAnimation
    {
        Move = 0,
        MoveLocal = 1,
        MoveAnchors = 2,
        Float = 3,
        Euler = 4,
        Scale = 5,
        SizeDelta = 6,
        AnchorMin = 7,
        AnchorMax = 8,
        FadeByCanvasGroup = 9,
        TextMeshProDOText = 10,
        UnityEvent = 11,
    }

    #endregion

    #region Base Classes and Interface

    public interface ITweenAnimation : IDisposable
    {
        ITweenSettings Settings { get; }
        void InitSetting(TweenAnimationSettings settings);
        Tween Show();
    }

    #endregion
}
