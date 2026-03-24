using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    public class RectTransformSizeChangeLayoutRebuilder : MonoBehaviour
    {
        private void OnRectTransformDimensionsChange()
        {
            if (transform.parent is RectTransform rt)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rt);
            }
        }
    }
}
