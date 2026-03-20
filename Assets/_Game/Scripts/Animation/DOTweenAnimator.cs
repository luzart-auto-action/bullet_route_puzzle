using UnityEngine;
using DG.Tweening;

namespace BulletRoute.Animation
{
    public enum AnimationType
    {
        Scale,
        Move,
        Rotate,
        Fade,
        PunchScale,
        PunchPosition,
        PunchRotation,
        ShakePosition,
        ShakeRotation
    }

    [System.Serializable]
    public class DOTweenAnimationData
    {
        public string AnimationName;
        public AnimationType Type;
        public float Duration = 0.5f;
        public float Delay;
        public Ease EaseType = Ease.OutQuad;
        public Vector3 TargetValue = Vector3.one;
        public int Loops;
        public LoopType LoopMode = LoopType.Restart;
        public bool PlayOnEnable;
        public bool UseRelative;
    }

    public class DOTweenAnimator : MonoBehaviour
    {
        [Header("Animations")]
        [SerializeField] private DOTweenAnimationData[] _animations;

        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Tween[] _activeTweens;

        private void Awake()
        {
            if (_target == null) _target = transform;
            _activeTweens = new Tween[_animations != null ? _animations.Length : 0];
        }

        private void OnEnable()
        {
            if (_animations == null) return;
            for (int i = 0; i < _animations.Length; i++)
            {
                if (_animations[i].PlayOnEnable)
                    Play(i);
            }
        }

        public void Play(string animationName)
        {
            if (_animations == null) return;
            for (int i = 0; i < _animations.Length; i++)
            {
                if (_animations[i].AnimationName == animationName)
                {
                    Play(i);
                    return;
                }
            }
        }

        public void Play(int index)
        {
            if (_animations == null || index < 0 || index >= _animations.Length) return;

            var data = _animations[index];
            _activeTweens[index]?.Kill();

            Tween tween = null;

            switch (data.Type)
            {
                case AnimationType.Scale:
                    tween = _target.DOScale(data.TargetValue, data.Duration);
                    break;
                case AnimationType.Move:
                    tween = data.UseRelative
                        ? _target.DOMove(data.TargetValue, data.Duration).SetRelative()
                        : _target.DOLocalMove(data.TargetValue, data.Duration);
                    break;
                case AnimationType.Rotate:
                    tween = _target.DOLocalRotate(data.TargetValue, data.Duration);
                    break;
                case AnimationType.Fade:
                    if (_canvasGroup != null)
                        tween = _canvasGroup.DOFade(data.TargetValue.x, data.Duration);
                    break;
                case AnimationType.PunchScale:
                    tween = _target.DOPunchScale(data.TargetValue, data.Duration);
                    break;
                case AnimationType.PunchPosition:
                    tween = _target.DOPunchPosition(data.TargetValue, data.Duration);
                    break;
                case AnimationType.PunchRotation:
                    tween = _target.DOPunchRotation(data.TargetValue, data.Duration);
                    break;
                case AnimationType.ShakePosition:
                    tween = _target.DOShakePosition(data.Duration, data.TargetValue);
                    break;
                case AnimationType.ShakeRotation:
                    tween = _target.DOShakeRotation(data.Duration, data.TargetValue);
                    break;
            }

            if (tween != null)
            {
                tween.SetDelay(data.Delay)
                    .SetEase(data.EaseType)
                    .SetLoops(data.Loops, data.LoopMode);
                _activeTweens[index] = tween;
            }
        }

        public void StopAll()
        {
            if (_activeTweens == null) return;
            for (int i = 0; i < _activeTweens.Length; i++)
            {
                _activeTweens[i]?.Kill();
                _activeTweens[i] = null;
            }
        }

        public void Stop(string animationName)
        {
            if (_animations == null) return;
            for (int i = 0; i < _animations.Length; i++)
            {
                if (_animations[i].AnimationName == animationName)
                {
                    _activeTweens[i]?.Kill();
                    _activeTweens[i] = null;
                    return;
                }
            }
        }

        private void OnDisable()
        {
            StopAll();
        }

        private void OnDestroy()
        {
            StopAll();
        }
    }
}
