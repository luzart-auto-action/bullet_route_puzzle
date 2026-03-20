using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public abstract class TileBase : MonoBehaviour, IBulletRouter, IRotatable, IDraggable
    {
        [Header("Tile Settings")]
        [SerializeField] protected TileType _tileType;
        [SerializeField] protected bool _isLocked;
        [SerializeField] protected bool _isFixed; // Cannot be moved or rotated

        [Header("Visual")]
        [SerializeField] protected Transform _visualRoot;
        [SerializeField] protected Transform _arrowIndicator;

        [Header("FX Spawn Points")]
        [SerializeField] protected Transform _fxSpawnCenter;
        [SerializeField] protected Transform _fxSpawnTop;
        [SerializeField] protected Transform _fxSpawnBottom;
        [SerializeField] protected Transform _fxSpawnLeft;
        [SerializeField] protected Transform _fxSpawnRight;
        [SerializeField] protected Transform[] _customFXSpawnPoints;

        [Header("DOTween Animation Settings")]
        [SerializeField] protected float _rotateDuration = 0.25f;
        [SerializeField] protected Ease _rotateEase = Ease.OutBack;
        [SerializeField] protected float _swapMoveDuration = 0.3f;
        [SerializeField] protected Ease _swapEase = Ease.OutQuad;
        [SerializeField] protected float _selectScaleMultiplier = 1.15f;
        [SerializeField] protected float _selectDuration = 0.15f;
        [SerializeField] protected float _hoverBounceIntensity = 0.05f;
        [SerializeField] protected float _idlePulseDuration = 2f;
        [SerializeField] protected float _idlePulseScale = 0.02f;

        protected int _rotationState; // 0=Up, 1=Right, 2=Down, 3=Left
        protected Tween _currentRotateTween;
        protected Tween _idleTween;
        protected Sequence _bulletPassSequence;

        public TileType TileType => _tileType;
        public Vector2Int GridPosition { get; set; }
        public bool IsLocked => _isLocked || _isFixed;
        public int RotationState => _rotationState;
        public bool CanRotate => !_isFixed && !_isLocked;
        public bool CanDrag => !_isFixed && !_isLocked;
        public Transform FXCenter => _fxSpawnCenter;
        public Transform VisualRoot => _visualRoot;

        protected virtual void Start()
        {
            if (_visualRoot == null) _visualRoot = transform;
            CreateFXSpawnPoints();
            StartIdleAnimation();
        }

        private void CreateFXSpawnPoints()
        {
            if (_fxSpawnCenter == null)
            {
                _fxSpawnCenter = new GameObject("FX_Center").transform;
                _fxSpawnCenter.SetParent(transform);
                _fxSpawnCenter.localPosition = Vector3.zero;
            }
            if (_fxSpawnTop == null)
            {
                _fxSpawnTop = new GameObject("FX_Top").transform;
                _fxSpawnTop.SetParent(transform);
                _fxSpawnTop.localPosition = Vector3.forward * 0.5f;
            }
            if (_fxSpawnBottom == null)
            {
                _fxSpawnBottom = new GameObject("FX_Bottom").transform;
                _fxSpawnBottom.SetParent(transform);
                _fxSpawnBottom.localPosition = Vector3.back * 0.5f;
            }
            if (_fxSpawnLeft == null)
            {
                _fxSpawnLeft = new GameObject("FX_Left").transform;
                _fxSpawnLeft.SetParent(transform);
                _fxSpawnLeft.localPosition = Vector3.left * 0.5f;
            }
            if (_fxSpawnRight == null)
            {
                _fxSpawnRight = new GameObject("FX_Right").transform;
                _fxSpawnRight.SetParent(transform);
                _fxSpawnRight.localPosition = Vector3.right * 0.5f;
            }
        }

        public Transform GetFXSpawnPoint(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return _fxSpawnTop;
                case Direction.Down: return _fxSpawnBottom;
                case Direction.Left: return _fxSpawnLeft;
                case Direction.Right: return _fxSpawnRight;
                default: return _fxSpawnCenter;
            }
        }

        // === Rotation ===
        public void Rotate(int steps = 1)
        {
            if (!CanRotate) return;

            _rotationState = (_rotationState + steps) % 4;
            if (_rotationState < 0) _rotationState += 4;

            AnimateRotation(steps);

            EventBus.Publish(new TileRotatedEvent
            {
                GridPos = GridPosition,
                NewRotation = _rotationState
            });
        }

        public void SetRotation(int state, bool animate = false)
        {
            _rotationState = state % 4;
            if (animate)
            {
                AnimateRotation(0);
            }
            else
            {
                var target = _visualRoot != null ? _visualRoot : transform;
                target.localRotation = Quaternion.Euler(0f, _rotationState * -90f, 0f);
            }
        }

        protected virtual void AnimateRotation(int steps)
        {
            _currentRotateTween?.Kill();

            var target = _visualRoot != null ? _visualRoot : transform;
            float targetAngle = _rotationState * -90f;

            // Punch scale for juice
            _currentRotateTween = DOTween.Sequence()
                .Append(target.DOLocalRotate(new Vector3(0f, targetAngle, 0f), _rotateDuration).SetEase(_rotateEase))
                .Join(target.DOPunchScale(Vector3.one * 0.1f, _rotateDuration, 1, 0.5f));
        }

        // === Selection Animation ===
        public virtual void AnimateSelect()
        {
            var target = _visualRoot != null ? _visualRoot : transform;
            _idleTween?.Kill();
            target.DOScale(Vector3.one * _selectScaleMultiplier, _selectDuration).SetEase(Ease.OutBack);
        }

        public virtual void AnimateDeselect()
        {
            var target = _visualRoot != null ? _visualRoot : transform;
            target.DOScale(Vector3.one, _selectDuration).SetEase(Ease.InBack)
                .OnComplete(() => StartIdleAnimation());
        }

        // === Swap Animation ===
        public virtual Tween AnimateMoveTo(Vector3 targetPos)
        {
            return transform.DOMove(targetPos, _swapMoveDuration).SetEase(_swapEase);
        }

        // === Bullet Pass Animation ===
        public virtual void AnimateBulletPass(Direction entryDir, Direction exitDir)
        {
            _bulletPassSequence?.Kill();
            var target = _visualRoot != null ? _visualRoot : transform;

            _bulletPassSequence = DOTween.Sequence()
                .Append(target.DOPunchScale(Vector3.one * _hoverBounceIntensity, 0.3f, 2, 0.5f))
                .Join(target.DOPunchRotation(new Vector3(0, 0, 5f), 0.3f, 5, 0.5f));

            // FX: request bullet pass effect
            EventBus.Publish(new FXRequestEvent
            {
                FXName = "BulletPass",
                Position = GetFXSpawnPoint(exitDir).position,
                Rotation = Quaternion.identity
            });
        }

        // === Idle Animation ===
        protected virtual void StartIdleAnimation()
        {
            _idleTween?.Kill();
            var target = _visualRoot != null ? _visualRoot : transform;
            _idleTween = target.DOScale(Vector3.one * (1f + _idlePulseScale), _idlePulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // === Highlight Animation (for hints) ===
        public virtual void AnimateHighlight(bool on)
        {
            var target = _visualRoot != null ? _visualRoot : transform;
            if (on)
            {
                target.DOScale(Vector3.one * 1.1f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                DOTween.Kill(target);
                target.DOScale(Vector3.one, 0.2f);
                StartIdleAnimation();
            }
        }

        // === Abstract: each tile type implements its own routing ===
        public abstract List<Direction> GetExitDirections(Direction entryDirection);

        // === Apply rotation to a direction ===
        protected Direction ApplyRotation(Direction baseDir)
        {
            return (Direction)(((int)baseDir + _rotationState) % 4);
        }

        protected Direction ReverseRotation(Direction dir)
        {
            return (Direction)(((int)dir - _rotationState + 4) % 4);
        }

        protected virtual void OnDestroy()
        {
            _currentRotateTween?.Kill();
            _idleTween?.Kill();
            _bulletPassSequence?.Kill();
        }
    }
}
