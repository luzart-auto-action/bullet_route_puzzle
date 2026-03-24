using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    public class RectTransformSizeConstrainer : MonoBehaviour
    {
        [SerializeField] RectTransform target;
        [SerializeField] Vector2 padding;
        [SerializeField] Vector2 minSizeTarget;

        private void OnEnable()
        {
            DoConstraint();
        }

        void OnRectTransformDimensionsChange()
        {
            DoConstraint();
        }
        private void OnValidate()
        {
            DoConstraint();
        }

        [ContextMenu("Constraint")]
        private void DoConstraint()
        {
            Vector2 mySize = (transform as RectTransform).rect.size + padding;
            if (target == null)
            {
                Debug.LogError($"❌ Target RectTransform of {this.name} is null.");
                return;
            }
            var currTargetSize = target.rect.size;
            Vector2 deltaTargetSize = mySize - currTargetSize;
            Vector2 targetSize = target.sizeDelta + deltaTargetSize;
            targetSize.x = Mathf.Max(targetSize.x, minSizeTarget.x);
            targetSize.y = Mathf.Max(targetSize.y, minSizeTarget.y);
            target.sizeDelta = targetSize;
            LayoutRebuilder.MarkLayoutForRebuild(target);
            if (target.parent is RectTransform rt)
            {
                if (!gameObject.activeInHierarchy)
                {
                    return;
                }
                if(_ieWaitAFrame != null)
                {
                    StopCoroutine(_ieWaitAFrame);
                }
                _ieWaitAFrame = StartCoroutine(IECountAFrame(rt));
            }
        }
        private Coroutine _ieWaitAFrame = null;
        private IEnumerator IECountAFrame(RectTransform rt)
        {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild(rt);
        }
        private void OnDisable()
        {
            if(_ieWaitAFrame != null)
            {
                StopCoroutine(_ieWaitAFrame);
            }
        }
    }
}
