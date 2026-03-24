#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Luzart
{
    // Class để đại diện cho một node trong cây
    [System.Serializable]
    public class FolderTreeNode
    {
        public string name;
        public string fullPath;
        public List<FolderTreeNode> children = new List<FolderTreeNode>();
        public List<Type> scriptableTypes = new List<Type>();
        public bool isExpanded = true;

        public FolderTreeNode(string name, string fullPath = "")
        {
            this.name = name;
            this.fullPath = fullPath;
        }

        public int GetTotalScriptableCount()
        {
            int count = scriptableTypes.Count;
            foreach (var child in children)
            {
                count += child.GetTotalScriptableCount();
            }
            return count;
        }

        public int GetFilteredScriptableCount(string filter)
        {
            int count = scriptableTypes.Count(t => PassesFilter(t.Name, filter));
            foreach (var child in children)
            {
                count += child.GetFilteredScriptableCount(filter);
            }
            return count;
        }

        public bool HasMatchingContent(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;

            // Check if any scriptable types match
            if (scriptableTypes.Any(t => PassesFilter(t.Name, filter)))
                return true;

            // Check if any children have matching content
            return children.Any(child => child.HasMatchingContent(filter));
        }

        public static bool PassesFilter(string text, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;

            string newText = text.ToLower();
            string newFilter = filter.ToLower().Trim();

            // Kiểm tra nếu filter chứa dấu cách (pattern "prefix suffix")
            if (newFilter.Contains(" "))
            {
                string[] filterParts = newFilter.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (filterParts.Length == 2)
                {
                    string prefix = filterParts[0];
                    string suffix = filterParts[1];

                    // Kiểm tra pattern "prefix + suffix" (ví dụ: "ui layer" -> tìm "UILayer", "UIManagerLayer", etc.)
                    bool hasPrefix = newText.StartsWith(prefix);
                    bool hasSuffix = newText.EndsWith(suffix);

                    if (hasPrefix && hasSuffix)
                        return true;

                    // Kiểm tra pattern với từ ở giữa (ví dụ: "ui layer" -> tìm "UI...Layer")
                    if (hasPrefix && newText.Contains(suffix))
                        return true;

                    // Kiểm tra cả hai từ có xuất hiện trong text không (fallback)
                    bool containsPrefix = newText.Contains(prefix);
                    bool containsSuffix = newText.Contains(suffix);

                    if (containsPrefix && containsSuffix)
                        return true;
                }
                else if (filterParts.Length > 2)
                {
                    // Nhiều từ khóa: tất cả phải xuất hiện trong text
                    return filterParts.All(part => newText.Contains(part));
                }
            }

            // Search thông thường (single keyword)
            return newText.IndexOf(newFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal class ScriptableCreationEditorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 debugScrollPosition;
        private FolderTreeNode rootNode;
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        private string searchFilter = "";
        private bool showEmptyFolders = true;
        private bool showDebugInfo = false;
        private List<string> debugMessages = new List<string>();

        private string subAssetRenameBuffer = string.Empty;
        private UnityEngine.Object subAssetRenameTarget = null;

        [MenuItem("Luzart/LuzartTool/Scriptable Object Creator")]
        public static void ShowWindow()
        {
            GetWindow<ScriptableCreationEditorWindow>("Scriptable Creator");
        }

        private void OnEnable()
        {
            RefreshScriptableObjectTypes();
        }

        private void RefreshScriptableObjectTypes()
        {
            debugMessages.Clear();
            var scriptableObjectTypes = new List<Type>();

            debugMessages.Add("=== Starting ScriptableObject scan ===");

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(ScriptableObject)) &&
                            !type.IsAbstract &&
                            !type.IsGenericType)
                        {
                            // Check for Luzart namespace
                            //if(!type.Namespace?.StartsWith("Luzart") ?? true)
                            if (type.Namespace?.Contains("Unity") ?? false)
                            {
                                debugMessages.Add($"SKIPPED (not Luzart): {type.Name} from {type.FullName}");
                                continue;
                            }
                            scriptableObjectTypes.Add(type);
                            debugMessages.Add($"ADDED: {type.Name} from {type.FullName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    debugMessages.Add($"Error loading assembly: {ex.Message}");
                }
            }

            debugMessages.Add($"=== Found {scriptableObjectTypes.Count} ScriptableObjects ===");

            // Tạo cây folder
            BuildFolderTree(scriptableObjectTypes);

            debugMessages.Add($"=== Tree built with {rootNode?.GetTotalScriptableCount() ?? 0} total items ===");
        }

        private void BuildFolderTree(List<Type> scriptableTypes)
        {
            rootNode = new FolderTreeNode("Root", "");

            foreach (Type type in scriptableTypes)
            {
                string folderPath = GetFolderFromType(type);
                debugMessages.Add($"Type: {type.Name} -> Folder: '{folderPath}'");
                AddTypeToTree(type, folderPath);
            }

            // Sort tất cả nodes
            SortTreeNodes(rootNode);
        }

        private void AddTypeToTree(Type type, string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || folderPath == "Unknown")
            {
                rootNode.scriptableTypes.Add(type);
                debugMessages.Add($"  -> Added {type.Name} to ROOT");
                return;
            }

            string[] pathParts = folderPath.Split('/');
            FolderTreeNode currentNode = rootNode;
            string currentPath = "";

            foreach (string part in pathParts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                currentPath = string.IsNullOrEmpty(currentPath) ? part : currentPath + "/" + part;

                // Tìm child node với tên này
                FolderTreeNode childNode = currentNode.children.FirstOrDefault(c => c.name == part);

                if (childNode == null)
                {
                    // Tạo node mới
                    childNode = new FolderTreeNode(part, currentPath);
                    currentNode.children.Add(childNode);

                    // Set trạng thái mở/đóng mặc định
                    if (!foldoutStates.ContainsKey(currentPath))
                    {
                        foldoutStates[currentPath] = true;
                    }

                    debugMessages.Add($"    -> Created folder: {currentPath}");
                }

                currentNode = childNode;
            }

            // Thêm type vào node cuối
            currentNode.scriptableTypes.Add(type);
            debugMessages.Add($"  -> Added {type.Name} to folder: {folderPath}");
        }

        private void SortTreeNodes(FolderTreeNode node)
        {
            // Sort children theo tên
            node.children = node.children.OrderBy(c => c.name).ToList();

            // Sort scriptable types theo tên
            node.scriptableTypes = node.scriptableTypes.OrderBy(t => t.Name).ToList();

            // Đệ quy sort children
            foreach (var child in node.children)
            {
                SortTreeNodes(child);
            }
        }

        private string GetFolderFromType(Type type)
        {
            // Tìm source file của type này để lấy đường dẫn thực tế
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script != null && script.GetClass() == type)
                {
                    string folder = GetFolderPathFromAssetPath(assetPath);
                    debugMessages.Add($"      Found: {assetPath} -> {folder}");
                    return folder;
                }
            }

            debugMessages.Add($"    -> No file found for {type.Name}, placing in ROOT");
            return "";
        }

        private string GetFolderPathFromAssetPath(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/');
            string directoryPath = Path.GetDirectoryName(assetPath).Replace('\\', '/');

            if (directoryPath.StartsWith("Assets/"))
            {
                directoryPath = directoryPath.Substring(7);
            }

            return directoryPath;
        }

        private void OnGUI()
        {
            try
            {
                // Header
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Scriptable Object Creator", EditorStyles.boldLabel);

                // Show current selection info
                ScriptableObject selectedScriptableObject = Selection.activeObject as ScriptableObject;
                if (selectedScriptableObject != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("📌 Selected:", GUILayout.Width(80));
                    EditorGUILayout.LabelField(selectedScriptableObject.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("(SubAsset mode)", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    // --- SubAsset Management Tools ---
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("Sub-Assets:", EditorStyles.boldLabel);
                    string assetPath = AssetDatabase.GetAssetPath(selectedScriptableObject);
                    UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    bool hasSubAssets = false;
                    foreach (var obj in assets)
                    {
                        if (obj == selectedScriptableObject || !(obj is ScriptableObject)) continue;
                        hasSubAssets = true;
                        EditorGUILayout.BeginHorizontal();
                        // Show sub-asset name
                        EditorGUILayout.LabelField($"- {obj.name}", GUILayout.Width(180));
                        // Rename field
                        if (subAssetRenameTarget == obj)
                        {
                            subAssetRenameBuffer = EditorGUILayout.TextField(subAssetRenameBuffer, GUILayout.Width(120));
                            if (GUILayout.Button("Save", GUILayout.Width(50)))
                            {
                                Undo.RecordObject(obj, "Rename SubAsset");
                                obj.name = subAssetRenameBuffer;
                                EditorUtility.SetDirty(obj);
                                AssetDatabase.SaveAssets();
                                subAssetRenameTarget = null;
                                subAssetRenameBuffer = string.Empty;
                            }
                            if (GUILayout.Button("Cancel", GUILayout.Width(50)))
                            {
                                subAssetRenameTarget = null;
                                subAssetRenameBuffer = string.Empty;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Rename", GUILayout.Width(60)))
                            {
                                subAssetRenameTarget = obj;
                                subAssetRenameBuffer = obj.name;
                            }
                        }
                        // Delete button
                        GUI.color = new Color(1f, 0.7f, 0.7f);
                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                        {
                            if (EditorUtility.DisplayDialog("Delete SubAsset", $"Are you sure you want to delete sub-asset '{obj.name}'?", "Delete", "Cancel"))
                            {
                                Undo.RecordObject(selectedScriptableObject, "Delete SubAsset");
                                UnityEngine.Object.DestroyImmediate(obj, true);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                                subAssetRenameTarget = null;
                                subAssetRenameBuffer = string.Empty;
                                EditorUtility.FocusProjectWindow();
                                Selection.activeObject = selectedScriptableObject;
                                break;
                            }
                        }
                        GUI.color = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                    if (!hasSubAssets)
                    {
                        EditorGUILayout.LabelField("(No sub-assets)", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.LabelField("💡 Tip: Select a ScriptableObject to create SubAssets", EditorStyles.helpBox);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔄 Refresh", GUILayout.Width(80)))
                {
                    RefreshScriptableObjectTypes();
                }

                showDebugInfo = EditorGUILayout.Toggle("Debug", showDebugInfo, GUILayout.Width(80));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("📂 Expand All", GUILayout.Width(100)))
                {
                    ExpandAll(true);
                }

                if (GUILayout.Button("📁 Collapse All", GUILayout.Width(100)))
                {
                    ExpandAll(false);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                // Search & Options
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
                searchFilter = EditorGUILayout.TextField(searchFilter);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    searchFilter = "";
                }
                EditorGUILayout.EndHorizontal();

                showEmptyFolders = EditorGUILayout.Toggle("Show empty folders", showEmptyFolders);
                EditorGUILayout.EndVertical();

                // Debug section
                if (showDebugInfo)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"🐛 Debug ({debugMessages.Count}):", EditorStyles.boldLabel);

                    debugScrollPosition = EditorGUILayout.BeginScrollView(debugScrollPosition, GUILayout.Height(150));
                    foreach (string msg in debugMessages)
                    {
                        EditorGUILayout.LabelField(msg, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }

                // Content
                if (rootNode == null)
                {
                    EditorGUILayout.HelpBox("rootNode is NULL - click Refresh", MessageType.Warning);
                    return;
                }

                int totalCount = rootNode.GetTotalScriptableCount();
                int filteredCount = string.IsNullOrEmpty(searchFilter) ? totalCount : rootNode.GetFilteredScriptableCount(searchFilter);

                string statsText = string.IsNullOrEmpty(searchFilter)
                    ? $"Found {totalCount} ScriptableObjects"
                    : $"Showing {filteredCount} of {totalCount} ScriptableObjects";

                EditorGUILayout.LabelField(statsText, EditorStyles.boldLabel);

                if (totalCount == 0)
                {
                    EditorGUILayout.HelpBox("No ScriptableObjects found. Enable Debug to see scan details.", MessageType.Info);
                    return;
                }

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                // Hiển thị scriptables ở root level trước
                if (rootNode.scriptableTypes.Count > 0)
                {
                    bool hasVisibleItems = rootNode.scriptableTypes.Any(t => PassesFilter(t.Name));
                    if (hasVisibleItems || string.IsNullOrEmpty(searchFilter))
                    {
                        EditorGUILayout.LabelField("📄 Root Scripts", EditorStyles.boldLabel);

                        foreach (Type type in rootNode.scriptableTypes)
                        {
                            if (PassesFilter(type.Name))
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(20); // Indent for root items
                                DrawScriptableObjectRow(type);
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.Space();
                    }
                }

                // Hiển thị cây folder
                foreach (var child in rootNode.children)
                {
                    if (ShouldShowNode(child))
                    {
                        DrawTreeNode(child, 0);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.HelpBox($"Error: {ex.Message}", MessageType.Error);
            }
        }

        private bool PassesFilter(string text)
        {
            return FolderTreeNode.PassesFilter(text, searchFilter);
        }

        private bool ShouldShowNode(FolderTreeNode node)
        {
            if (string.IsNullOrEmpty(searchFilter))
            {
                return showEmptyFolders || node.GetTotalScriptableCount() > 0;
            }

            return node.HasMatchingContent(searchFilter);
        }

        private void ExpandAll(bool expand)
        {
            var keys = foldoutStates.Keys.ToList();
            foreach (string key in keys)
            {
                foldoutStates[key] = expand;
            }
        }

        private void DrawTreeNode(FolderTreeNode node, int depth)
        {
            string key = node.fullPath;
            if (!foldoutStates.ContainsKey(key))
                foldoutStates[key] = true;

            // Tính số lượng filtered items
            int totalCount = string.IsNullOrEmpty(searchFilter)
                ? node.GetTotalScriptableCount()
                : node.GetFilteredScriptableCount(searchFilter);

            if (totalCount == 0 && !showEmptyFolders && !string.IsNullOrEmpty(searchFilter))
                return;

            EditorGUILayout.BeginHorizontal();

            // Indent dựa trên depth
            GUILayout.Space(depth * 20);

            // Foldout với icon folder
            bool wasExpanded = foldoutStates[key];
            string folderIcon = wasExpanded ? "📂" : "📁";
            string label = $"{folderIcon} {node.name}";

            // Thêm số lượng
            if (totalCount > 0)
            {
                label += $" ({totalCount})";
            }

            // Highlight folder nếu có matching content
            Color originalColor = GUI.color;
            if (!string.IsNullOrEmpty(searchFilter) && totalCount > 0)
            {
                GUI.color = new Color(0.8f, 1f, 0.8f); // Light green tint
            }

            foldoutStates[key] = EditorGUILayout.Foldout(foldoutStates[key], label, true, EditorStyles.foldout);

            GUI.color = originalColor; // Reset color

            EditorGUILayout.EndHorizontal();

            if (foldoutStates[key])
            {
                // Hiển thị scriptable objects trong folder này
                if (node.scriptableTypes.Count > 0)
                {
                    foreach (Type type in node.scriptableTypes)
                    {
                        if (PassesFilter(type.Name))
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space((depth + 1) * 20);
                            DrawScriptableObjectRow(type);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                // Đệ quy hiển thị children
                foreach (var child in node.children)
                {
                    if (ShouldShowNode(child))
                    {
                        DrawTreeNode(child, depth + 1);
                    }
                }
            }
        }

        private void DrawScriptableObjectRow(Type type)
        {
            // Highlight matching text
            Color originalColor = GUI.color;
            if (!string.IsNullOrEmpty(searchFilter) && PassesFilter(type.Name))
            {
                GUI.color = new Color(1f, 1f, 0.8f); // Light yellow highlight
            }

            // Icon và tên class
            EditorGUILayout.LabelField($"📄 {type.Name}", GUILayout.Width(250));

            // Check if a ScriptableObject is selected to determine button text and color
            ScriptableObject selectedScriptableObject = Selection.activeObject as ScriptableObject;
            string buttonText = "Create";
            Color buttonColor = GUI.color;
            
            if (selectedScriptableObject != null)
            {
                buttonText = "Create Sub";
                buttonColor = new Color(0.8f, 1f, 0.8f); // Light green for SubAsset creation
            }

            Color originalButtonColor = GUI.color;
            GUI.color = buttonColor;
            
            if (GUILayout.Button(buttonText, GUILayout.Width(80)))
            {
                CreateScriptableObject(type);
            }
            
            GUI.color = originalButtonColor; // Reset button color
            GUI.color = originalColor; // Reset text color

            GUILayout.FlexibleSpace();
            // Hiển thị base class
            string baseTypeName = type.BaseType?.Name ?? "";
            if (baseTypeName != "ScriptableObject")
            {
                EditorGUILayout.LabelField($"({baseTypeName})", EditorStyles.miniLabel, GUILayout.Width(120));
            }

            // Show info about selected parent if applicable
            if (selectedScriptableObject != null)
            {
                EditorGUILayout.LabelField($"→ {selectedScriptableObject.name}", EditorStyles.miniLabel, GUILayout.Width(150));
            }
        }

        private void CreateScriptableObject(Type type)
        {
            // Check if a ScriptableObject is currently selected
            ScriptableObject selectedScriptableObject = Selection.activeObject as ScriptableObject;
            
            if (selectedScriptableObject != null)
            {
                // Create as SubAsset
                CreateSubAsset(type, selectedScriptableObject);
            }
            else
            {
                // Create as standalone asset
                CreateStandaloneAsset(type);
            }
        }

        private void CreateSubAsset(Type type, ScriptableObject parentAsset)
        {
            ScriptableObject subAsset = ScriptableObject.CreateInstance(type);
            subAsset.name = $"|{type.Name}";

            // Add as sub-asset to the selected ScriptableObject
            AssetDatabase.AddObjectToAsset(subAsset, parentAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Focus and select the newly created sub-asset
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = subAsset;
            EditorGUIUtility.PingObject(subAsset);

            string parentPath = AssetDatabase.GetAssetPath(parentAsset);
            Debug.Log($"Created {type.Name} as SubAsset of {parentAsset.name} at: {parentPath}");
        }

        private void CreateStandaloneAsset(Type type)
        {
            string selectedPath = GetSelectedFolderPath();

            string fileName = type.Name + ".asset";
            string fullPath = Path.Combine(selectedPath, fileName);

            // Đảm bảo tên file là duy nhất
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Focus và select asset vừa tạo
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"Created {type.Name} at: {fullPath}");
        }

        private string GetSelectedFolderPath()
        {
            string selectedPath = "Assets"; // Default fallback

            // Phương pháp 1: Lấy object được chọn trong Project window (ưu tiên cao nhất)
            if (Selection.activeObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    // Nếu là file, lấy thư mục chứa file
                    if (File.Exists(assetPath))
                    {
                        selectedPath = Path.GetDirectoryName(assetPath);
                        debugMessages.Add($"Method 1 - Selected file path resolved to: {selectedPath}");
                        return NormalizePath(selectedPath);
                    }
                    // Nếu là folder, sử dụng trực tiếp
                    else if (Directory.Exists(assetPath))
                    {
                        selectedPath = assetPath;
                        debugMessages.Add($"Method 1 - Selected folder path resolved to: {selectedPath}");
                        return NormalizePath(selectedPath);
                    }
                }
                else
                {
                    debugMessages.Add($"Selected object has no asset path, trying method 2");
                }
            }

            // Phương pháp 2: Thử lấy thư mục đang được chọn từ Project Browser GUID
            var selectedGUIDs = Selection.assetGUIDs;
            if (selectedGUIDs != null && selectedGUIDs.Length > 0)
            {
                string guid = selectedGUIDs[0];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    if (Directory.Exists(assetPath))
                    {
                        selectedPath = assetPath;
                        debugMessages.Add($"Method 2 - Using selected folder from GUID: {selectedPath}");
                        return NormalizePath(selectedPath);
                    }
                    else if (File.Exists(assetPath))
                    {
                        selectedPath = Path.GetDirectoryName(assetPath);
                        debugMessages.Add($"Method 2 - Using parent folder of selected file: {selectedPath}");
                        return NormalizePath(selectedPath);
                    }
                }
            }

            // Phương pháp 3: Lấy thư mục đang được mở/focus trong Project window (fallback)
            try
            {
                var method = typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (method != null)
                {
                    string activePath = (string)method.Invoke(null, null);
                    if (!string.IsNullOrEmpty(activePath))
                    {
                        selectedPath = activePath;
                        debugMessages.Add($"Method 3 - Using active Project window folder: {selectedPath}");
                        return NormalizePath(selectedPath);
                    }
                }
            }
            catch (System.Exception ex)
            {
                debugMessages.Add($"Method 3 failed - Could not get active folder: {ex.Message}");
            }

            // Phương pháp 4: Fallback cuối cùng
            debugMessages.Add("All methods failed, using default Assets folder");
            return NormalizePath(selectedPath);
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                debugMessages.Add("Path is null/empty, using Assets");
                return "Assets";
            }

            // Normalize path separators
            path = path.Replace('\\', '/');

            // Ensure it starts with Assets if it's a valid Unity path
            if (!path.StartsWith("Assets") && !Path.IsPathRooted(path))
            {
                path = "Assets";
                debugMessages.Add("Path correction: forcing to Assets folder");
            }

            debugMessages.Add($"Final creation path: {path}");
            return path;
        }
    }
}
#endif