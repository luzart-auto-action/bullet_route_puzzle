using UnityEngine;
using UnityEditor;
using BulletRoute.Core;
using BulletRoute.Tile;
using BulletRoute.Turret;
using BulletRoute.Target;

namespace BulletRoute.Editor
{
    public class TilePrefabCreator : EditorWindow
    {
        private TileType _selectedType = TileType.Straight;
        private string _prefabFolder = "Assets/_Game/Prefabs/Tiles";
        private bool _createPlaceholder3D = true;
        private Color _tileColor = Color.white;
        private Vector2 _scrollPos;

        // Tile-specific settings
        private bool _mirrorIsForwardSlash = true;

        [MenuItem("BulletRoute/Tile Prefab Creator", false, 10)]
        public static void ShowWindow()
        {
            GetWindow<TilePrefabCreator>("Tile Prefab Creator").minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(10);
            GUILayout.Label("TILE PREFAB CREATOR", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tao prefab tile voi dung hierarchy, components va FX spawn points.\n" +
                "Chi can chon loai tile va bam Create!",
                MessageType.Info);

            EditorGUILayout.Space(10);

            _selectedType = (TileType)EditorGUILayout.EnumPopup("Tile Type", _selectedType);
            _tileColor = EditorGUILayout.ColorField("Placeholder Color", _tileColor);
            _createPlaceholder3D = EditorGUILayout.Toggle("Tao 3D placeholder", _createPlaceholder3D);

            // Tile-specific options
            if (_selectedType == TileType.Mirror)
            {
                _mirrorIsForwardSlash = EditorGUILayout.Toggle("Forward Slash (/)", _mirrorIsForwardSlash);
            }

            EditorGUILayout.Space(5);
            _prefabFolder = EditorGUILayout.TextField("Save Folder", _prefabFolder);

            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button($"TAO PREFAB: {_selectedType}", GUILayout.Height(35)))
            {
                CreateTilePrefab(_selectedType);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(20);
            GUILayout.Label("TAO NHANH TAT CA TILES", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
            if (GUILayout.Button("TAO TAT CA 11 TILE PREFABS", GUILayout.Height(35)))
            {
                CreateAllTilePrefabs();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            GUILayout.Label("TAO RIENG TUNG LOAI", EditorStyles.boldLabel);

            DrawQuickCreateButton(TileType.Straight, "Straight (dan di thang)", new Color(0.4f, 0.8f, 0.4f));
            DrawQuickCreateButton(TileType.Corner, "Corner (re 90 do)", new Color(0.8f, 0.6f, 0.2f));
            DrawQuickCreateButton(TileType.Cross, "Cross (dan xuyen qua)", new Color(0.6f, 0.6f, 0.9f));
            DrawQuickCreateButton(TileType.Block, "Block (chan dan)", new Color(0.5f, 0.5f, 0.5f));
            DrawQuickCreateButton(TileType.Mirror, "Mirror (phan xa)", new Color(0.7f, 0.9f, 1f));
            DrawQuickCreateButton(TileType.Splitter, "Splitter (tach dan)", new Color(1f, 0.6f, 0.8f));
            DrawQuickCreateButton(TileType.Portal, "Portal (teleport)", new Color(0f, 1f, 1f));
            DrawQuickCreateButton(TileType.Bomb, "Bomb (pha wall)", new Color(1f, 0.3f, 0.3f));
            DrawQuickCreateButton(TileType.Absorb, "Absorb (nuot dan)", new Color(0.3f, 0f, 0.5f));
            DrawQuickCreateButton(TileType.Turret, "Turret (ban dan)", new Color(0.9f, 0.9f, 0.2f));
            DrawQuickCreateButton(TileType.Target, "Target (muc tieu)", new Color(1f, 0.5f, 0f));

            EditorGUILayout.Space(15);
            GUILayout.Label("GAN VAO TILE FACTORY", EditorStyles.boldLabel);

            if (GUILayout.Button("Tu dong gan tat ca prefabs vao TileFactory"))
            {
                AutoAssignToTileFactory();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickCreateButton(TileType type, string label, Color color)
        {
            string prefabPath = GetPrefabPath(type);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = exists ? new Color(0.3f, 0.3f, 0.3f) : color;
            string btnLabel = exists ? $"[DA CO] {label}" : label;

            if (GUILayout.Button(btnLabel, GUILayout.Height(25)))
            {
                if (exists)
                {
                    if (EditorUtility.DisplayDialog("Prefab da ton tai",
                        $"Tile_{type} da co. Ghi de?", "Ghi de", "Huy"))
                    {
                        CreateTilePrefab(type);
                    }
                }
                else
                {
                    CreateTilePrefab(type);
                }
            }
            GUI.backgroundColor = Color.white;

            if (exists && GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CreateAllTilePrefabs()
        {
            TileType[] types = {
                TileType.Straight, TileType.Corner, TileType.Cross, TileType.Block,
                TileType.Mirror, TileType.Splitter, TileType.Portal,
                TileType.Bomb, TileType.Absorb, TileType.Turret, TileType.Target
            };

            int created = 0;
            foreach (var type in types)
            {
                string path = GetPrefabPath(type);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    CreateTilePrefab(type);
                    created++;
                }
            }

            AutoAssignToTileFactory();

            EditorUtility.DisplayDialog("Done",
                $"Da tao {created} prefabs moi.\nTat ca da duoc gan vao TileFactory.",
                "OK");
        }

        private void CreateTilePrefab(TileType type)
        {
            string folder = GetFolderForType(type);
            EnsureFolder(folder);

            var root = new GameObject($"Tile_{type}");

            // Add the correct script
            AddTileComponent(root, type);

            // Create VisualRoot
            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            // Create placeholder 3D model
            if (_createPlaceholder3D)
            {
                CreatePlaceholderModel(type, visualRoot.transform);
            }

            // Create FX spawn points
            CreateFXPoints(root.transform);

            // Type-specific children
            switch (type)
            {
                case TileType.Turret:
                    CreateTurretChildren(root, visualRoot.transform);
                    break;
                case TileType.Target:
                    CreateTargetChildren(root, visualRoot.transform);
                    break;
            }

            // Set references via SerializedObject
            SetTileReferences(root, type, visualRoot.transform);

            // Save prefab
            string path = GetPrefabPath(type);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[BulletRoute] Tile prefab created: {path}");
        }

        private void AddTileComponent(GameObject go, TileType type)
        {
            switch (type)
            {
                case TileType.Straight: go.AddComponent<StraightTile>(); break;
                case TileType.Corner: go.AddComponent<CornerTile>(); break;
                case TileType.Cross: go.AddComponent<CrossTile>(); break;
                case TileType.Block: go.AddComponent<BlockTile>(); break;
                case TileType.Mirror: go.AddComponent<MirrorTile>(); break;
                case TileType.Splitter: go.AddComponent<SplitterTile>(); break;
                case TileType.Portal: go.AddComponent<PortalTile>(); break;
                case TileType.Bomb: go.AddComponent<BombTile>(); break;
                case TileType.Absorb: go.AddComponent<AbsorbTile>(); break;
                case TileType.Turret: go.AddComponent<TurretController>(); break;
                case TileType.Target: go.AddComponent<TargetController>(); break;
            }
        }

        private void CreatePlaceholderModel(TileType type, Transform parent)
        {
            GameObject model;
            switch (type)
            {
                case TileType.Straight:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Cube, new Vector3(0.3f, 0.15f, 1f));
                    SetMaterial(model, "Tile_Straight");
                    var arrow = CreatePrimitive("Arrow", parent, PrimitiveType.Cube, new Vector3(0.15f, 0.2f, 0.3f));
                    arrow.transform.localPosition = new Vector3(0, 0.1f, 0.3f);
                    SetMaterial(arrow, "Tile_Straight_Arrow");
                    break;

                case TileType.Corner:
                    model = CreatePrimitive("Model_H", parent, PrimitiveType.Cube, new Vector3(0.5f, 0.15f, 0.3f));
                    model.transform.localPosition = new Vector3(0.15f, 0, 0);
                    SetMaterial(model, "Tile_Corner");
                    var modelV = CreatePrimitive("Model_V", parent, PrimitiveType.Cube, new Vector3(0.3f, 0.15f, 0.5f));
                    modelV.transform.localPosition = new Vector3(0, 0, 0.15f);
                    SetMaterial(modelV, "Tile_Corner");
                    break;

                case TileType.Cross:
                    model = CreatePrimitive("Model_H", parent, PrimitiveType.Cube, new Vector3(1f, 0.15f, 0.3f));
                    SetMaterial(model, "Tile_Cross_H");
                    var crossV = CreatePrimitive("Model_V", parent, PrimitiveType.Cube, new Vector3(0.3f, 0.15f, 1f));
                    SetMaterial(crossV, "Tile_Cross_V");
                    break;

                case TileType.Block:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Cube, new Vector3(0.8f, 0.4f, 0.8f));
                    SetMaterial(model, "Tile_Block");
                    break;

                case TileType.Mirror:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Cube, new Vector3(0.8f, 0.3f, 0.08f));
                    model.transform.localRotation = Quaternion.Euler(0, 45, 0);
                    SetMaterial(model, "Tile_Mirror");
                    break;

                case TileType.Splitter:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Cube, new Vector3(0.8f, 0.15f, 0.3f));
                    SetMaterial(model, "Tile_Splitter");
                    var splitV = CreatePrimitive("Model_V", parent, PrimitiveType.Cube, new Vector3(0.3f, 0.15f, 0.4f));
                    splitV.transform.localPosition = new Vector3(0, 0, -0.15f);
                    SetMaterial(splitV, "Tile_Splitter_V");
                    break;

                case TileType.Portal:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Cylinder, new Vector3(0.6f, 0.1f, 0.6f));
                    SetMaterial(model, "Tile_Portal");
                    var ring = CreatePrimitive("Ring", parent, PrimitiveType.Cylinder, new Vector3(0.8f, 0.05f, 0.8f));
                    ring.transform.localPosition = new Vector3(0, 0.1f, 0);
                    SetMaterial(ring, "Tile_Portal_Ring");
                    break;

                case TileType.Bomb:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Sphere, new Vector3(0.6f, 0.6f, 0.6f));
                    SetMaterial(model, "Tile_Bomb");
                    var fuse = CreatePrimitive("Fuse", parent, PrimitiveType.Cube, new Vector3(0.05f, 0.2f, 0.05f));
                    fuse.transform.localPosition = new Vector3(0, 0.4f, 0);
                    SetMaterial(fuse, "Tile_Bomb_Fuse");
                    break;

                case TileType.Absorb:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Sphere, new Vector3(0.7f, 0.3f, 0.7f));
                    SetMaterial(model, "Tile_Absorb");
                    break;

                case TileType.Turret:
                    // Body created separately in CreateTurretChildren
                    break;

                case TileType.Target:
                    // Created separately in CreateTargetChildren
                    break;

                default:
                    model = CreatePrimitive("Model", parent, PrimitiveType.Cube, new Vector3(0.8f, 0.15f, 0.8f));
                    SetColor(model, Color.white);
                    break;
            }
        }

        private void CreateTurretChildren(GameObject root, Transform visualRoot)
        {
            var body = CreatePrimitive("Body", visualRoot, PrimitiveType.Cube, new Vector3(0.7f, 0.3f, 0.7f));
            SetMaterial(body, "Turret_Body");

            var barrel = new GameObject("Barrel");
            barrel.transform.SetParent(visualRoot, false);
            var barrelModel = CreatePrimitive("BarrelModel", barrel.transform, PrimitiveType.Cube, new Vector3(0.15f, 0.15f, 0.5f));
            barrelModel.transform.localPosition = new Vector3(0, 0.1f, 0.25f);
            SetMaterial(barrelModel, "Turret_Barrel");

            var muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(barrel.transform, false);
            muzzle.transform.localPosition = new Vector3(0, 0.1f, 0.5f);

            var fxMuzzle = new GameObject("FX_MuzzleFlash");
            fxMuzzle.transform.SetParent(root.transform, false);
            fxMuzzle.transform.localPosition = new Vector3(0, 0.1f, 0.5f);
        }

        private void CreateTargetChildren(GameObject root, Transform visualRoot)
        {
            var center = CreatePrimitive("TargetCenter", visualRoot, PrimitiveType.Cylinder, new Vector3(0.3f, 0.15f, 0.3f));
            SetMaterial(center, "Target_Center");

            var ring = CreatePrimitive("TargetRing", visualRoot, PrimitiveType.Cylinder, new Vector3(0.7f, 0.05f, 0.7f));
            ring.transform.localPosition = new Vector3(0, 0.1f, 0);
            SetMaterial(ring, "Target_Ring");
        }

        private void CreateFXPoints(Transform root)
        {
            CreatePoint("FX_Center", root, Vector3.zero);
            CreatePoint("FX_Top", root, new Vector3(0, 0, 0.5f));
            CreatePoint("FX_Bottom", root, new Vector3(0, 0, -0.5f));
            CreatePoint("FX_Left", root, new Vector3(-0.5f, 0, 0));
            CreatePoint("FX_Right", root, new Vector3(0.5f, 0, 0));
        }

        private void CreatePoint(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }

        private void SetTileReferences(GameObject root, TileType type, Transform visualRoot)
        {
            var tile = root.GetComponent<TileBase>();
            if (tile == null) return;

            var so = new SerializedObject(tile);
            so.FindProperty("_tileType").enumValueIndex = (int)type;

            var vrProp = so.FindProperty("_visualRoot");
            if (vrProp != null) vrProp.objectReferenceValue = visualRoot;

            // FX points
            SetTransformRef(so, "_fxSpawnCenter", root.transform.Find("FX_Center"));
            SetTransformRef(so, "_fxSpawnTop", root.transform.Find("FX_Top"));
            SetTransformRef(so, "_fxSpawnBottom", root.transform.Find("FX_Bottom"));
            SetTransformRef(so, "_fxSpawnLeft", root.transform.Find("FX_Left"));
            SetTransformRef(so, "_fxSpawnRight", root.transform.Find("FX_Right"));

            // Type-specific
            if (type == TileType.Turret)
            {
                var barrel = visualRoot.Find("Barrel");
                var muzzle = barrel?.Find("MuzzlePoint");
                SetTransformRef(so, "_barrel", barrel);
                SetTransformRef(so, "_muzzlePoint", muzzle);
                SetTransformRef(so, "_fxMuzzleFlash", root.transform.Find("FX_MuzzleFlash"));

                // Mark as fixed
                so.FindProperty("_isFixed").boolValue = true;
            }
            else if (type == TileType.Target)
            {
                SetTransformRef(so, "_targetRing", visualRoot.Find("TargetRing"));
                SetTransformRef(so, "_targetCenter", visualRoot.Find("TargetCenter"));
                so.FindProperty("_isFixed").boolValue = true;
            }
            else if (type == TileType.Mirror)
            {
                var fwdProp = so.FindProperty("_isForwardSlash");
                if (fwdProp != null) fwdProp.boolValue = _mirrorIsForwardSlash;
            }
            else if (type == TileType.Block)
            {
                so.FindProperty("_isFixed").boolValue = true;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetTransformRef(SerializedObject so, string propName, Transform value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }

        private void AutoAssignToTileFactory()
        {
            var factory = Object.FindObjectOfType<TileFactory>();
            if (factory == null)
            {
                Debug.LogWarning("[BulletRoute] TileFactory khong tim thay trong scene. Chay Scene Setup truoc!");
                return;
            }

            var so = new SerializedObject(factory);
            var prefabsProp = so.FindProperty("_tilePrefabs");
            prefabsProp.ClearArray();

            TileType[] types = {
                TileType.Straight, TileType.Corner, TileType.Cross, TileType.Block,
                TileType.Mirror, TileType.Splitter, TileType.Portal,
                TileType.Bomb, TileType.Absorb, TileType.Turret, TileType.Target
            };

            int assigned = 0;
            foreach (var type in types)
            {
                string path = GetPrefabPath(type);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var tileComp = prefab.GetComponent<TileBase>();
                if (tileComp == null) continue;

                prefabsProp.InsertArrayElementAtIndex(prefabsProp.arraySize);
                var element = prefabsProp.GetArrayElementAtIndex(prefabsProp.arraySize - 1);
                element.FindPropertyRelative("Type").enumValueIndex = (int)type;
                element.FindPropertyRelative("Prefab").objectReferenceValue = tileComp;
                assigned++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[BulletRoute] TileFactory: {assigned} prefabs assigned");
        }

        // ===================== HELPERS =====================
        private GameObject CreatePrimitive(string name, Transform parent, PrimitiveType prim, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(prim);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            // Remove collider from visual mesh
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            return go;
        }

        private void SetColor(GameObject go, Color color)
        {
            SetMaterial(go, null, color);
        }

        private void SetMaterial(GameObject go, string matName, Color fallbackColor = default)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            // Try to load saved material asset first
            if (!string.IsNullOrEmpty(matName))
            {
                var savedMat = MaterialFactory.GetOrCreateMaterial(matName);
                if (savedMat != null)
                {
                    renderer.sharedMaterial = savedMat;
                    return;
                }
            }

            // Fallback: create inline material
            var mat = new Material(Shader.Find("Standard"));
            mat.color = fallbackColor;
            if (fallbackColor.a < 1f)
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
            renderer.sharedMaterial = mat;
        }

        private string GetFolderForType(TileType type)
        {
            switch (type)
            {
                case TileType.Turret: return "Assets/_Game/Prefabs/Turret";
                case TileType.Target: return "Assets/_Game/Prefabs/Target";
                default: return "Assets/_Game/Prefabs/Tiles";
            }
        }

        private string GetPrefabPath(TileType type)
        {
            return $"{GetFolderForType(type)}/Tile_{type}.prefab";
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
    }
}
