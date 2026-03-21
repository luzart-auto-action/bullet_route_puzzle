using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace BulletRoute.Editor
{
    public class MaterialFactory : EditorWindow
    {
        private const string MAT_PATH = "Assets/_Game/Art/Materials";
        private Vector2 _scrollPos;

        // ===================== MATERIAL DEFINITIONS =====================
        private static readonly Dictionary<string, MaterialDef> AllMaterials = new Dictionary<string, MaterialDef>
        {
            // Tile Materials
            { "Tile_Straight",       new MaterialDef(new Color(0.30f, 0.75f, 0.35f), emission: new Color(0.1f, 0.3f, 0.1f)) },
            { "Tile_Straight_Arrow", new MaterialDef(new Color(0.15f, 0.55f, 0.18f), emission: new Color(0.05f, 0.2f, 0.05f)) },
            { "Tile_Corner",         new MaterialDef(new Color(0.85f, 0.55f, 0.15f), emission: new Color(0.3f, 0.15f, 0.0f)) },
            { "Tile_Cross_H",        new MaterialDef(new Color(0.45f, 0.50f, 0.85f), emission: new Color(0.1f, 0.1f, 0.3f)) },
            { "Tile_Cross_V",        new MaterialDef(new Color(0.50f, 0.55f, 0.90f), emission: new Color(0.1f, 0.12f, 0.35f)) },
            { "Tile_Block",          new MaterialDef(new Color(0.45f, 0.45f, 0.50f), metallic: 0.6f, smoothness: 0.3f) },
            { "Tile_Mirror",         new MaterialDef(new Color(0.75f, 0.92f, 1.0f),  metallic: 0.9f, smoothness: 0.85f, emission: new Color(0.2f, 0.4f, 0.5f)) },
            { "Tile_Splitter",       new MaterialDef(new Color(1.0f, 0.45f, 0.70f),  emission: new Color(0.4f, 0.1f, 0.25f)) },
            { "Tile_Splitter_V",     new MaterialDef(new Color(0.95f, 0.40f, 0.65f), emission: new Color(0.35f, 0.08f, 0.2f)) },
            { "Tile_Portal",         new MaterialDef(new Color(0.0f, 0.90f, 0.95f),  emission: new Color(0.0f, 0.5f, 0.6f) * 1.5f) },
            { "Tile_Portal_Ring",    new MaterialDef(new Color(0.0f, 0.70f, 0.75f, 0.5f), emission: new Color(0.0f, 0.3f, 0.4f), transparent: true) },
            { "Tile_Bomb",           new MaterialDef(new Color(0.90f, 0.20f, 0.15f), emission: new Color(0.4f, 0.05f, 0.02f)) },
            { "Tile_Bomb_Fuse",      new MaterialDef(new Color(0.25f, 0.25f, 0.25f)) },
            { "Tile_Absorb",         new MaterialDef(new Color(0.25f, 0.02f, 0.40f), emission: new Color(0.1f, 0.0f, 0.2f)) },

            // Turret Materials
            { "Turret_Body",         new MaterialDef(new Color(0.85f, 0.85f, 0.15f), metallic: 0.4f, smoothness: 0.5f, emission: new Color(0.3f, 0.3f, 0.0f)) },
            { "Turret_Barrel",       new MaterialDef(new Color(0.65f, 0.65f, 0.08f), metallic: 0.7f, smoothness: 0.6f) },

            // Target Materials
            { "Target_Center",       new MaterialDef(new Color(1.0f, 0.30f, 0.0f),  emission: new Color(0.5f, 0.1f, 0.0f) * 2f) },
            { "Target_Ring",         new MaterialDef(new Color(1.0f, 0.50f, 0.0f, 0.7f), emission: new Color(0.4f, 0.15f, 0.0f), transparent: true) },

            // Bullet Materials
            { "Bullet_Core",         new MaterialDef(new Color(1.0f, 0.80f, 0.15f), emission: new Color(1.0f, 0.6f, 0.0f) * 2.5f) },
            { "Bullet_Glow",         new MaterialDef(new Color(1.0f, 0.90f, 0.30f, 0.3f), emission: new Color(1.0f, 0.7f, 0.1f), transparent: true) },
            { "Bullet_Trail",        new MaterialDef(new Color(1.0f, 0.70f, 0.10f), useSpritesDefault: true) },

            // Grid / Environment
            { "Grid_Floor",          new MaterialDef(new Color(0.12f, 0.12f, 0.18f)) },
            { "Grid_Line",           new MaterialDef(new Color(0.20f, 0.25f, 0.35f, 0.5f), emission: new Color(0.05f, 0.08f, 0.15f), transparent: true) },
        };

        // ===================== WINDOW =====================
        [MenuItem("BulletRoute/Material Factory", false, 12)]
        public static void ShowWindow()
        {
            GetWindow<MaterialFactory>("Material Factory").minSize = new Vector2(420, 550);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(10);
            GUILayout.Label("MATERIAL FACTORY", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tao tat ca Material assets va tu dong gan vao Prefabs.\n" +
                "Materials luu tai: Assets/_Game/Art/Materials/",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Main buttons
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("TAO TAT CA MATERIALS + GAN VAO PREFABS", GUILayout.Height(40)))
            {
                CreateAllMaterialsAndAssign();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            GUILayout.Label("THAO TAC RIENG", EditorStyles.boldLabel);

            if (GUILayout.Button("Chi tao Material Assets (khong gan)"))
                CreateAllMaterialAssets();

            if (GUILayout.Button("Chi gan Materials vao Prefabs (da co .mat)"))
                AssignAllMaterialsToPrefabs();

            EditorGUILayout.Space(15);
            GUILayout.Label("DANH SACH MATERIALS", EditorStyles.boldLabel);

            foreach (var kvp in AllMaterials)
            {
                EditorGUILayout.BeginHorizontal();
                string path = $"{MAT_PATH}/{kvp.Key}.mat";
                bool exists = AssetDatabase.LoadAssetAtPath<Material>(path) != null;

                GUI.contentColor = exists ? Color.green : Color.red;
                GUILayout.Label(exists ? "[OK]" : "[--]", GUILayout.Width(30));
                GUI.contentColor = Color.white;

                EditorGUILayout.ColorField(GUIContent.none, kvp.Value.color, false, kvp.Value.transparent, false, GUILayout.Width(40));
                GUILayout.Label(kvp.Key);

                if (exists && GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Material>(path);
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // ===================== CREATE ALL + ASSIGN =====================
        private void CreateAllMaterialsAndAssign()
        {
            CreateAllMaterialAssets();
            AssignAllMaterialsToPrefabs();

            EditorUtility.DisplayDialog("Material Factory",
                $"Da tao {AllMaterials.Count} materials va gan vao tat ca prefabs!\n\n" +
                $"Materials tai: {MAT_PATH}/",
                "OK");
        }

        // ===================== CREATE MATERIAL ASSETS =====================
        private void CreateAllMaterialAssets()
        {
            EnsureFolder(MAT_PATH);
            EnsureFolder(MAT_PATH + "/Tiles");
            EnsureFolder(MAT_PATH + "/Bullet");
            EnsureFolder(MAT_PATH + "/Environment");

            int created = 0;
            foreach (var kvp in AllMaterials)
            {
                string subfolder = GetSubfolder(kvp.Key);
                string path = $"{MAT_PATH}/{subfolder}{kvp.Key}.mat";

                // Always recreate to ensure up-to-date
                var mat = CreateMaterial(kvp.Key, kvp.Value);
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(mat, existing);
                    Object.DestroyImmediate(mat);
                }
                else
                {
                    AssetDatabase.CreateAsset(mat, path);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MaterialFactory] {created} new materials created, {AllMaterials.Count - created} updated. Total: {AllMaterials.Count}");
        }

        // ===================== ASSIGN TO PREFABS =====================
        private void AssignAllMaterialsToPrefabs()
        {
            AssignTilePrefab("Tile_Straight", new[] {
                ("Model",   "Tile_Straight"),
                ("Arrow",   "Tile_Straight_Arrow"),
            });

            AssignTilePrefab("Tile_Corner", new[] {
                ("Model_H", "Tile_Corner"),
                ("Model_V", "Tile_Corner"),
            });

            AssignTilePrefab("Tile_Cross", new[] {
                ("Model_H", "Tile_Cross_H"),
                ("Model_V", "Tile_Cross_V"),
            });

            AssignTilePrefab("Tile_Block", new[] {
                ("Model", "Tile_Block"),
            });

            AssignTilePrefab("Tile_Mirror", new[] {
                ("Model", "Tile_Mirror"),
            });

            AssignTilePrefab("Tile_Splitter", new[] {
                ("Model",   "Tile_Splitter"),
                ("Model_V", "Tile_Splitter_V"),
            });

            AssignTilePrefab("Tile_Portal", new[] {
                ("Model", "Tile_Portal"),
                ("Ring",  "Tile_Portal_Ring"),
            });

            AssignTilePrefab("Tile_Bomb", new[] {
                ("Model", "Tile_Bomb"),
                ("Fuse",  "Tile_Bomb_Fuse"),
            });

            AssignTilePrefab("Tile_Absorb", new[] {
                ("Model", "Tile_Absorb"),
            });

            // Turret
            AssignPrefabMaterials("Assets/_Game/Prefabs/Turret/Tile_Turret.prefab", new[] {
                ("Body",         "Turret_Body"),
                ("BarrelModel",  "Turret_Barrel"),
            });

            // Target
            AssignPrefabMaterials("Assets/_Game/Prefabs/Target/Tile_Target.prefab", new[] {
                ("TargetCenter", "Target_Center"),
                ("TargetRing",   "Target_Ring"),
            });

            // Bullet
            AssignPrefabMaterials("Assets/_Game/Prefabs/Bullet/Bullet.prefab", new[] {
                ("BulletModel", "Bullet_Core"),
                ("Glow",        "Bullet_Glow"),
            });

            // Bullet TrailRenderer
            AssignTrailMaterial("Assets/_Game/Prefabs/Bullet/Bullet.prefab", "Bullet_Trail");

            AssetDatabase.SaveAssets();
            Debug.Log("[MaterialFactory] All materials assigned to prefabs");
        }

        // ===================== HELPERS =====================
        private Material CreateMaterial(string name, MaterialDef def)
        {
            Shader shader;
            if (def.useSpritesDefault)
                shader = Shader.Find("Sprites/Default");
            else
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = name;
            mat.color = def.color;

            if (def.useSpritesDefault)
                return mat;

            mat.SetFloat("_Metallic", def.metallic);
            mat.SetFloat("_Glossiness", def.smoothness);

            // Transparent mode
            if (def.transparent)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            // Emission
            if (def.emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", def.emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            return mat;
        }

        private void AssignTilePrefab(string prefabName, (string childName, string matName)[] assignments)
        {
            string path = $"Assets/_Game/Prefabs/Tiles/{prefabName}.prefab";
            AssignPrefabMaterials(path, assignments);
        }

        private void AssignPrefabMaterials(string prefabPath, (string childName, string matName)[] assignments)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[MaterialFactory] Prefab not found: {prefabPath}");
                return;
            }

            // Open prefab for editing
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            bool changed = false;
            foreach (var (childName, matName) in assignments)
            {
                var child = FindDeepChild(instance.transform, childName);
                if (child == null)
                {
                    Debug.LogWarning($"[MaterialFactory] Child '{childName}' not found in {prefabPath}");
                    continue;
                }

                var renderer = child.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"[MaterialFactory] No Renderer on '{childName}' in {prefabPath}");
                    continue;
                }

                string subfolder = GetSubfolder(matName);
                string matPath = $"{MAT_PATH}/{subfolder}{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    Debug.LogWarning($"[MaterialFactory] Material not found: {matPath}");
                    continue;
                }

                renderer.sharedMaterial = mat;
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            Object.DestroyImmediate(instance);
        }

        private void AssignTrailMaterial(string prefabPath, string matName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            var trail = instance.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = instance.GetComponentInChildren<TrailRenderer>();
            }

            if (trail != null)
            {
                string subfolder = GetSubfolder(matName);
                string matPath = $"{MAT_PATH}/{subfolder}{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat != null)
                {
                    trail.material = mat;
                    // Also set gradient colors
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
                }
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private string GetSubfolder(string matName)
        {
            if (matName.StartsWith("Tile_") || matName.StartsWith("Turret_") || matName.StartsWith("Target_"))
                return "Tiles/";
            if (matName.StartsWith("Bullet_"))
                return "Bullet/";
            if (matName.StartsWith("Grid_"))
                return "Environment/";
            return "";
        }

        private void EnsureFolder(string path)
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

        // ===================== DATA =====================
        private struct MaterialDef
        {
            public Color color;
            public float metallic;
            public float smoothness;
            public Color? emission;
            public bool transparent;
            public bool useSpritesDefault;

            public MaterialDef(Color color, float metallic = 0f, float smoothness = 0.5f,
                Color? emission = null, bool transparent = false, bool useSpritesDefault = false)
            {
                this.color = color;
                this.metallic = metallic;
                this.smoothness = smoothness;
                this.emission = emission;
                this.transparent = transparent || color.a < 1f;
                this.useSpritesDefault = useSpritesDefault;
            }
        }

        // ===================== STATIC API (for other tools) =====================
        public static Material GetOrCreateMaterial(string matName)
        {
            // Check all subfolders
            string[] subfolders = { "Tiles/", "Bullet/", "Environment/", "" };
            foreach (var sub in subfolders)
            {
                string path = $"{MAT_PATH}/{sub}{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null) return mat;
            }

            // Create if not found
            if (AllMaterials.ContainsKey(matName))
            {
                var factory = CreateInstance<MaterialFactory>();
                var mat = factory.CreateMaterial(matName, AllMaterials[matName]);
                string subfolder = factory.GetSubfolder(matName);
                factory.EnsureFolder($"{MAT_PATH}/{subfolder}".TrimEnd('/'));
                string assetPath = $"{MAT_PATH}/{subfolder}{matName}.mat";
                AssetDatabase.CreateAsset(mat, assetPath);
                AssetDatabase.SaveAssets();
                DestroyImmediate(factory);
                return mat;
            }

            return null;
        }
    }
}
