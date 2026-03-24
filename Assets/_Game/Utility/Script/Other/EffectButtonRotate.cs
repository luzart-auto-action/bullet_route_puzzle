namespace Luzart
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class EffectButtonRotate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Target")]
        [SerializeField] RectTransform targetRotate;

        [Header("Rotate Config")]
        public float rotateSpeed = 180f; // độ / giây
        public bool clockwise = true;

        public bool isAutoButton = false;
        private Button btn;

        protected Coroutine corIERotate = null;
        protected bool isHolding = false;

        protected void Awake()
        {
            if (!isAutoButton)
            {
                btn = GetComponent<Button>();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (btn != null && !btn.interactable) return;
            if (targetRotate == null) return;

            isHolding = true;

            if (corIERotate != null)
            {
                StopCoroutine(corIERotate);
            }

            corIERotate = StartCoroutine(IERotate());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHolding = false;

            if (corIERotate != null)
            {
                StopCoroutine(corIERotate);
                corIERotate = null;
            }
            targetRotate.eulerAngles = new Vector3(0, 0, 0);
        }

        protected IEnumerator IERotate()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0);
            float dir = clockwise ? -1f : 1f;

            while (isHolding)
            {
                targetRotate.Rotate(0, 0, rotateSpeed * dir * Time.deltaTime);
                yield return wait;
            }
        }

        protected void OnDisable()
        {
            isHolding = false;

            if (corIERotate != null)
            {
                StopCoroutine(corIERotate);
                corIERotate = null;
            }
        }
    }
}
