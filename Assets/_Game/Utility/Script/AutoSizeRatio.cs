using Luzart;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AutoSizeRatio : MonoBehaviour, ICallBehavior
{
    [SerializeField]
    private BehaviorMode mode = BehaviorMode.Awake;
    [SerializeField]
    private Image image;
    [SerializeField]
    private RectTransform parentImage;
    private RectTransform RectTransform
    {
        get
        {
            return Image.rectTransform;
        }
    }

    private Image Image
    {
        get
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }
            return image;
        }
    }
    private RectTransform ParentImage
    {
        get
        {
            if(parentImage == null)
            {
                parentImage = RectTransform.parent as RectTransform;
            }
            return parentImage;
        }
    }

    BehaviorMode ICallBehavior.Mode => mode;

    void Awake()
    {
        if (mode == BehaviorMode.Awake)
            DoApply();
    }
    void Start()
    {
        if (mode == BehaviorMode.Start)
            DoApply();
    }

    void OnEnable()
    {
        if (mode == BehaviorMode.OnEnable)
            DoApply();
    }
    void Update()
    {
        if (mode == BehaviorMode.Update)
            DoApply();
    }
    void OnDisable()
    { 
        if (mode == BehaviorMode.OnDisable)
            DoApply();
    }
    void OnDestroy()
    {
        if (mode == BehaviorMode.OnDestroy)
            DoApply();
    }
    [ContextMenu("Do Apply")]
    [Button]
    private void DoApply()
    {
        if (Image == null || Image.sprite == null)
            return;

        RectTransform parent = ParentImage;
        if (parent == null)
            return;

        // Set anchor stretch full
        RectTransform.anchorMin = Vector2.one/2;
        RectTransform.anchorMax = Vector2.one/2;
        RectTransform.offsetMin = Vector2.zero;
        RectTransform.offsetMax = Vector2.zero;

        Rect parentRect = parent.rect;

        float parentWidth = parentRect.width;
        float parentHeight = parentRect.height;

        Image.SetNativeSize();
        
        float spriteWidth = RectTransform.rect.width;
        float spriteHeight = RectTransform.rect.height;

        float parentAspect = parentWidth / parentHeight;
        float spriteAspect = spriteWidth / spriteHeight;

        Vector2 size = Vector2.zero;

        if (parentAspect > spriteAspect)
        {
            // Màn hình rộng hơn → fit theo chiều ngang
            size.x = parentWidth;
            size.y = parentWidth / spriteAspect;
        }
        else
        {
            // Màn hình cao hơn → fit theo chiều dọc
            size.y = parentHeight;
            size.x = parentHeight * spriteAspect;
        }

        RectTransform.sizeDelta = size;
    }

    void ICallBehavior.Apply()
    {
        DoApply();
    }
}
[System.Serializable]
public enum BehaviorMode
{
    None = 0,
    Awake = 1,
    OnEnable = 2,
    Start = 3,
    Update = 4,
    OnDisable = 5,
    OnDestroy = 6
}

public interface ICallBehavior
{
    BehaviorMode Mode { get; }
    void Apply();
}