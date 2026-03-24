#if UNITY_EDITOR
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(Image)), CanEditMultipleObjects]
public class ImageArtNativeSizeEditor : ImageEditor
{
    private static readonly Vector2 artReferenceResolution = new Vector2(1920, 1080);

    public override void OnInspectorGUI()
    {
        // 👇 Vẽ lại toàn bộ Inspector gốc của Unity
        base.OnInspectorGUI();

        // 🔽 Thêm phần tùy chỉnh bên dưới
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎨 Art Layout Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Set Native Size Match Art Resolution"))
        {
            foreach (var t in targets)
            {
                SetNativeSizeToMatchArt(t as Image);
            }
        }
    }

    private void SetNativeSizeToMatchArt(Image img)
    {
        if (img == null || img.sprite == null)
        {
            Debug.LogWarning($"❌ Skipped: {img?.name} (No sprite)");
            return;
        }

        CanvasScaler scaler = img.GetComponentInParent<CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            Debug.LogWarning($"❌ Skipped: {img.name} (Missing CanvasScaler or wrong mode)");
            return;
        }

        Vector2 currentRef = scaler.referenceResolution;
        float widthRatio = currentRef.x / artReferenceResolution.x;
        float heightRatio = currentRef.y / artReferenceResolution.y;

        float compensateScale = 1f;
        switch (scaler.screenMatchMode)
        {
            case CanvasScaler.ScreenMatchMode.MatchWidthOrHeight:
                compensateScale = Mathf.Lerp(widthRatio, heightRatio, scaler.matchWidthOrHeight);
                break;
            case CanvasScaler.ScreenMatchMode.Expand:
                compensateScale = Mathf.Max(widthRatio, heightRatio);
                break;
            case CanvasScaler.ScreenMatchMode.Shrink:
                compensateScale = Mathf.Min(widthRatio, heightRatio);
                break;
        }

        Vector2 spriteSize = img.sprite.rect.size;
        Vector2 finalSize = spriteSize * compensateScale;

        Undo.RecordObject(img.rectTransform, "Set Native Size Match Art");
        img.rectTransform.sizeDelta = finalSize;

        Debug.Log($"✅ {img.name} sizeDelta = {finalSize} (scale = {compensateScale})");
    }

}
public static class NativeSizeMatchArtResolution
{
    // Đây là kích thước gốc art team dùng để thiết kế layout UI
    private static readonly Vector2 artReferenceResolution = new Vector2(1920, 1080);

    [MenuItem("Luzart/LuzartTool/Set Native Size Match Art Resolution")]
    public static void SetNativeSizeMatchArt()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Image img = obj.GetComponent<Image>();
            if (img == null || img.sprite == null)
            {
                Debug.LogWarning($"❌ Skipped: {obj.name} (No Image or no Sprite)");
                continue;
            }

            CanvasScaler scaler = obj.GetComponentInParent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                Debug.LogWarning($"❌ Skipped: {obj.name} (CanvasScaler missing or not ScaleWithScreenSize)");
                continue;
            }

            Vector2 currentReferenceResolution = scaler.referenceResolution;
            float match = scaler.matchWidthOrHeight;

            // ✅ Tính tỉ lệ lệch giữa resolution hiện tại và art reference
            float widthRatio = currentReferenceResolution.x / artReferenceResolution.x;
            float heightRatio = currentReferenceResolution.y / artReferenceResolution.y;
            float compensateScale = 1;
            // ✅ Xử lý chính xác theo screenMatchMode
            switch (scaler.screenMatchMode)
            {
                case CanvasScaler.ScreenMatchMode.MatchWidthOrHeight:
                    compensateScale = Mathf.Lerp(widthRatio, heightRatio, scaler.matchWidthOrHeight);
                    break;
                case CanvasScaler.ScreenMatchMode.Expand:
                    compensateScale = Mathf.Max(widthRatio, heightRatio); // Expand = dùng tỉ lệ nhỏ hơn
                    break;
                case CanvasScaler.ScreenMatchMode.Shrink:
                    compensateScale = Mathf.Min(widthRatio, heightRatio); // Shrink = dùng tỉ lệ lớn hơn
                    break;
            }

            // ✅ Lấy kích thước thật của sprite (theo pixel)
            Vector2 spritePixelSize = img.sprite.rect.size;

            // ✅ Điều chỉnh lại sizeDelta sao cho trông giống y như bản thiết kế ở canvas 1920x1080
            Vector2 finalSizeDelta = spritePixelSize * compensateScale/*/ (scaler.defaultSpriteDPI/100)*/;

            Undo.RecordObject(img.rectTransform, "Set Native Size Match Art");
            img.rectTransform.sizeDelta = finalSizeDelta;

            Debug.Log($"✅ {obj.name} → sizeDelta = {finalSizeDelta} to match ArtLayout (1920x1080) scaleFactor {scaler.scaleFactor}");
        }
    }
}
#endif
