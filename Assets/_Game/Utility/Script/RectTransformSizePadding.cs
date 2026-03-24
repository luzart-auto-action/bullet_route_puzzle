using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    [ExecuteAlways]
    public class RectTransformSizePadding : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] RectTransform target;

        [Header("Padding")]
        [SerializeField] float paddingLeft;
        [SerializeField] float paddingRight;
        [SerializeField] float paddingTop;
        [SerializeField] float paddingBottom;

        [Header("Min Size")]
        [SerializeField] Vector2 minSizeTarget = Vector2.zero;

        RectTransform _self;
        Coroutine _rebuildCoroutine;

        void Awake()
        {
            _self = transform as RectTransform;
        }

        void OnEnable()
        {
            DoConstraint();
        }

        void OnRectTransformDimensionsChange()
        {
            DoConstraint();
        }

        void OnValidate()
        {
            if (!gameObject.activeInHierarchy)
                return;

            _self = transform as RectTransform;
            DoConstraint();
        }

        [ContextMenu("Constraint")]
        void DoConstraint()
        {
            if (_self == null || target == null)
                return;

            // Size thật của content
            Vector2 contentSize = _self.rect.size;

            // Áp padding từng cạnh
            float width = contentSize.x + paddingLeft + paddingRight;
            float height = contentSize.y + paddingTop + paddingBottom;

            // Ép min size
            width = Mathf.Max(width, minSizeTarget.x);
            height = Mathf.Max(height, minSizeTarget.y);

            // Tránh rebuild vô hạn
            if (Mathf.Abs(target.rect.width - width) < 0.1f &&
                Mathf.Abs(target.rect.height - height) < 0.1f)
                return;

            // Set size an toàn với anchor
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            // Rebuild layout
            LayoutRebuilder.MarkLayoutForRebuild(target);

            if (target.parent is RectTransform parent)
            {
                RestartRebuild(parent);
            }
        }

        void RestartRebuild(RectTransform parent)
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (_rebuildCoroutine != null)
                StopCoroutine(_rebuildCoroutine);

            _rebuildCoroutine = StartCoroutine(RebuildNextFrame(parent));
        }

        IEnumerator RebuildNextFrame(RectTransform rt)
        {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild(rt);
        }

        void OnDisable()
        {
            if (_rebuildCoroutine != null)
            {
                StopCoroutine(_rebuildCoroutine);
                _rebuildCoroutine = null;
            }
        }
    }

}
