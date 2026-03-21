using UnityEngine;
using UnityEditor;
using BulletRoute.Bullet;

namespace BulletRoute.Editor
{
    public static class BulletPrefabCreator
    {
        [MenuItem("BulletRoute/Create Bullet Prefab", false, 11)]
        public static void CreateBulletPrefab()
        {
            string folder = "Assets/_Game/Prefabs/Bullet";
            EnsureFolder(folder);

            string path = $"{folder}/Bullet.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                if (!EditorUtility.DisplayDialog("Bullet Prefab", "Bullet.prefab da ton tai. Ghi de?", "Ghi de", "Huy"))
                    return;
            }

            var root = new GameObject("Bullet");
            root.AddComponent<BulletController>();

            // VisualRoot
            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            // Bullet model (small sphere)
            var model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.name = "BulletModel";
            model.transform.SetParent(visualRoot.transform, false);
            model.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            var col = model.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            // Glow ring
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "Glow";
            glow.transform.SetParent(visualRoot.transform, false);
            glow.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var glowCol = glow.GetComponent<Collider>();
            if (glowCol != null) Object.DestroyImmediate(glowCol);

            // Materials - use saved assets from MaterialFactory
            var bulletMat = MaterialFactory.GetOrCreateMaterial("Bullet_Core");
            if (bulletMat != null)
                model.GetComponent<Renderer>().sharedMaterial = bulletMat;

            var glowMat = MaterialFactory.GetOrCreateMaterial("Bullet_Glow");
            if (glowMat != null)
                glow.GetComponent<Renderer>().sharedMaterial = glowMat;

            // Trail
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;

            var trailMat = MaterialFactory.GetOrCreateMaterial("Bullet_Trail");
            if (trailMat != null)
                trail.material = trailMat;

            // Trail gradient colors
            var colorGrad = new Gradient();
            colorGrad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = colorGrad;

            // FX Points
            var fxFront = new GameObject("FX_Front");
            fxFront.transform.SetParent(root.transform, false);
            fxFront.transform.localPosition = new Vector3(0, 0, 0.15f);

            var fxCenter = new GameObject("FX_Center");
            fxCenter.transform.SetParent(root.transform, false);

            var fxTrail = new GameObject("FX_Trail");
            fxTrail.transform.SetParent(root.transform, false);
            fxTrail.transform.localPosition = new Vector3(0, 0, -0.15f);

            // Set references
            var bc = root.GetComponent<BulletController>();
            var so = new SerializedObject(bc);
            so.FindProperty("_visualRoot").objectReferenceValue = visualRoot.transform;
            so.FindProperty("_trail").objectReferenceValue = trail;
            so.FindProperty("_fxFront").objectReferenceValue = fxFront.transform;
            so.FindProperty("_fxCenter").objectReferenceValue = fxCenter.transform;
            so.FindProperty("_fxTrail").objectReferenceValue = fxTrail.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            // Auto-assign to BulletManager
            var bm = Object.FindObjectOfType<BulletManager>();
            if (bm != null)
            {
                var bmSo = new SerializedObject(bm);
                bmSo.FindProperty("_bulletPrefab").objectReferenceValue = prefab.GetComponent<BulletController>();
                bmSo.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[BulletRoute] Bullet prefab auto-assigned to BulletManager");
            }

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[BulletRoute] Bullet prefab created at {path}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
