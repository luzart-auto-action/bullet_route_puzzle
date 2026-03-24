#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ReferenceFinder : EditorWindow
{
    private Object targetObject;
    private Vector2 scrollPosition;
    private List<ReferenceInfo> foundReferences = new List<ReferenceInfo>();
    private bool searchInPrefabs = true;
    private bool searchInScenes = true;
    private bool searchInAssets = true;
    private bool searchInScriptableObjects = true;
    private bool searchInScripts = false; // Mặc định tắt vì chậm
    
    [MenuItem("Luzart/LuzartTool/Find References (Optimized)")]
    public static void ShowWindow()
    {
        GetWindow<ReferenceFinder>("Reference Finder");
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawTargetSelection();
        DrawSearchOptions();
        DrawActionButtons();
        DrawResults();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space();
        GUILayout.Label("🔍 Reference Finder (Optimized)", EditorStyles.boldLabel);
        EditorGUILayout.Space();
    }

    private void DrawTargetSelection()
    {
        GUILayout.Label("Target Object:", EditorStyles.boldLabel);
        targetObject = EditorGUILayout.ObjectField("Target", targetObject, typeof(Object), true);
        
        if (GUILayout.Button("📋 Use Selected Object"))
        {
            if (Selection.activeObject != null)
            {
                targetObject = Selection.activeObject;
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select an object in the hierarchy or project.", "OK");
            }
        }
        EditorGUILayout.Space();
    }

    private void DrawSearchOptions()
    {
        GUILayout.Label("Search Options:", EditorStyles.boldLabel);
        
        using (new EditorGUILayout.HorizontalScope())
        {
            searchInScenes = EditorGUILayout.Toggle("Scenes", searchInScenes);
            searchInPrefabs = EditorGUILayout.Toggle("Prefabs", searchInPrefabs);
        }
        
        using (new EditorGUILayout.HorizontalScope())
        {
            searchInScriptableObjects = EditorGUILayout.Toggle("ScriptableObjects", searchInScriptableObjects);
            searchInAssets = EditorGUILayout.Toggle("Other Assets", searchInAssets);
        }
        
        searchInScripts = EditorGUILayout.Toggle("Script Files (Slow)", searchInScripts);
        
        EditorGUILayout.Space();
    }

    private void DrawActionButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = targetObject != null;
            if (GUILayout.Button("🔍 Find References"))
            {
                FindReferences();
            }
            GUI.enabled = true;

            if (GUILayout.Button("🔍 Find Selected"))
            {
                FindSelectedReferences();
            }

            // Nút riêng cho tìm kiếm script
            GUI.enabled = targetObject != null;
            if (GUILayout.Button("🔍 Find Script References"))
            {
                FindScriptReferencesOnly();
            }
            GUI.enabled = true;

            GUI.enabled = foundReferences.Count > 0;
            if (GUILayout.Button("🧹 Remove Duplicates"))
            {
                RemoveDuplicates();
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("❌ Clear"))
            {
                foundReferences.Clear();
            }
        }
        EditorGUILayout.Space();
    }

    private void DrawResults()
    {
        if (foundReferences.Count > 0)
        {
            // Hiển thị thống kê
            var duplicateGroups = foundReferences
                .GroupBy(r => new { 
                    TargetObjectId = r.TargetObject?.GetInstanceID() ?? 0, 
                    r.PropertyName, 
                    r.ObjectPath 
                })
                .Where(g => g.Count() > 1)
                .ToList();
                
            if (duplicateGroups.Any())
            {
                EditorGUILayout.HelpBox($"⚠️ Found {duplicateGroups.Count} duplicate groups with {duplicateGroups.Sum(g => g.Count() - 1)} extra duplicates.", MessageType.Warning);
            }
            
            GUILayout.Label($"📋 Found {foundReferences.Count} References:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var reference in foundReferences)
            {
                DrawReferenceItem(reference);
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("No references found.", MessageType.Info);
        }
    }

    private void DrawReferenceItem(ReferenceInfo reference)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string icon = GetReferenceIcon(reference);
                GUILayout.Label($"{icon} {reference.ObjectName}", EditorStyles.boldLabel);
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    PingReference(reference);
                }
            }
            
            GUILayout.Label($"Type: {reference.ComponentType}", EditorStyles.miniLabel);
            
            if (!string.IsNullOrEmpty(reference.PropertyName))
            {
                GUILayout.Label($"Property: {reference.PropertyName}", EditorStyles.miniLabel);
            }
            
            if (!string.IsNullOrEmpty(reference.ScriptContent))
            {
                GUILayout.Label($"Context: {reference.ScriptContent}", EditorStyles.miniLabel);
            }
            
            GUILayout.Label($"Path: {reference.ObjectPath}", EditorStyles.miniLabel);
        }
        GUILayout.Space(2);
    }

    private string GetReferenceIcon(ReferenceInfo reference)
    {
        if (reference.IsInScene) return "🎬";
        if (reference.IsScriptableObject) return "📜";
        if (reference.IsScript) return "📝";
        return "📦";
    }

    private void PingReference(ReferenceInfo reference)
    {
        if (reference.TargetObject != null)
        {
            Selection.activeObject = reference.TargetObject;
            EditorGUIUtility.PingObject(reference.TargetObject);
        }
    }

    private void FindSelectedReferences()
    {
        var selectedObjects = Selection.objects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select objects to find references for.", "OK");
            return;
        }

        foundReferences.Clear();
        
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Finding References", $"Processing {selectedObjects[i].name}...", 
                i / (float)selectedObjects.Length);
            
            FindReferencesForTarget(selectedObjects[i]);
        }
        
        EditorUtility.ClearProgressBar();
        Debug.Log($"✅ Found {foundReferences.Count} references for {selectedObjects.Length} selected objects.");
    }

    private void FindReferences()
    {
        if (targetObject == null)
        {
            EditorUtility.DisplayDialog("No Target", "Please select a target object.", "OK");
            return;
        }

        foundReferences.Clear();
        EditorUtility.DisplayProgressBar("Finding References", "Starting search...", 0f);
        
        try
        {
            FindReferencesForTarget(targetObject);
            Debug.Log($"✅ Found {foundReferences.Count} references for '{targetObject.name}'.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void FindReferencesForTarget(Object target)
    {
        var searchTasks = new List<System.Action>
        {
            () => { if (searchInScenes) SearchInCurrentScene(target); },
            () => { if (searchInPrefabs) SearchInPrefabs(target); },
            () => { if (searchInScriptableObjects) SearchInScriptableObjects(target); },
            () => { if (searchInAssets) SearchInOtherAssets(target); }
            // Đã loại bỏ SearchInScripts khỏi đây
        };

        for (int i = 0; i < searchTasks.Count; i++)
        {
            EditorUtility.DisplayProgressBar("Finding References", $"Search step {i + 1}/{searchTasks.Count}", 
                i / (float)searchTasks.Count);
            searchTasks[i]();
        }
    }

    // Hàm mới: chỉ tìm kiếm script trong thư mục Assets
    private void FindScriptReferencesOnly()
    {
        if (targetObject == null)
        {
            EditorUtility.DisplayDialog("No Target", "Please select a target object.", "OK");
            return;
        }

        foundReferences.Clear();
        EditorUtility.DisplayProgressBar("Finding Script References", "Starting search...", 0f);
        try
        {
            // Lấy Type của target
            System.Type targetType = null;
            if (targetObject is MonoScript monoScript)
            {
                targetType = monoScript.GetClass();
            }
            else if (targetObject is Component component)
            {
                targetType = component.GetType();
            }
            else if (targetObject is ScriptableObject scriptableObject)
            {
                targetType = scriptableObject.GetType();
            }

            if (targetType == null) return;

            // Tìm tất cả các class con kế thừa từ targetType
            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
            var classNames = new HashSet<string> { targetType.Name };
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var scriptClass = scriptAsset?.GetClass();
                if (scriptClass != null && scriptClass != targetType && targetType.IsAssignableFrom(scriptClass))
                {
                    classNames.Add(scriptClass.Name);
                }
            }

            // Tìm reference cho tất cả các class cha + con
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Searching Scripts", $"{i}/{scriptGuids.Length}", i / (float)scriptGuids.Length);
                }
                var path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                CheckScriptFileForReferences_MultiClass(path, targetObject, classNames);
            }
            Debug.Log($"✅ Found {foundReferences.Count} script references for '{targetObject.name}' and subclasses.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // Hàm mới: kiểm tra nhiều class
    private void CheckScriptFileForReferences_MultiClass(string scriptPath, Object target, HashSet<string> classNames)
    {
        try
        {
            var scriptContent = File.ReadAllText(scriptPath);
            var foundLines = ExtractRelevantLines_MultiClass(scriptContent, classNames);
            if (foundLines.Count > 0)
            {
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (scriptAsset != null)
                {
                    AddScriptReference(scriptAsset, string.Join("\n", foundLines), target, scriptPath);
                }
            }
        }
        catch (System.Exception)
        {
            // Skip files that can't be read
        }
    }

    private List<string> ExtractRelevantLines_MultiClass(string scriptContent, HashSet<string> classNames)
    {
        var lines = scriptContent.Split('\n');
        var foundLines = new List<string>();
        for (int i = 0; i < lines.Length && foundLines.Count < 3; i++)
        {
            var cleanLine = RemoveComments(lines[i]);
            var words = SplitIntoWords(cleanLine);
            if (words.Any(classNames.Contains))
            {
                foundLines.Add($"Line {i + 1}: {lines[i].Trim()}");
            }
        }
        return foundLines;
    }

    private void SearchInCurrentScene(Object target)
    {
        var allComponents = Object.FindObjectsOfType<Component>();
        SearchInObjects(allComponents.Cast<Object>().ToArray(), target, true);
    }

    private void SearchInPrefabs(Object target)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            if (i % 50 == 0) // Update progress every 50 items
            {
                EditorUtility.DisplayProgressBar("Searching Prefabs", 
                    $"{i}/{prefabGuids.Length}", i / (float)prefabGuids.Length);
            }
            
            var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                var components = prefab.GetComponentsInChildren<Component>(true);
                SearchInObjects(components.Cast<Object>().ToArray(), target, false);
            }
        }
    }

    private void SearchInScriptableObjects(Object target)
    {
        var soGuids = AssetDatabase.FindAssets("t:ScriptableObject");
        var scriptableObjects = new List<Object>();
        
        for (int i = 0; i < soGuids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(soGuids[i]);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so != null) scriptableObjects.Add(so);
        }
        
        SearchInObjects(scriptableObjects.ToArray(), target, false, true);
    }

    private void SearchInOtherAssets(Object target)
    {
        var allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(path => !path.EndsWith(".unity") && !path.EndsWith(".prefab") && 
                          !path.EndsWith(".cs") && !path.EndsWith(".meta") &&
                          !path.EndsWith(".dll") && !path.EndsWith(".asmdef"))
            .ToArray();
        
        var assets = new List<Object>();
        
        for (int i = 0; i < allAssets.Length; i++)
        {
            if (i % 100 == 0)
            {
                EditorUtility.DisplayProgressBar("Loading Assets", 
                    $"{i}/{allAssets.Length}", i / (float)allAssets.Length);
            }
            
            var asset = AssetDatabase.LoadAssetAtPath<Object>(allAssets[i]);
            if (asset != null && !(asset is MonoScript)) assets.Add(asset);
        }
        
        SearchInObjects(assets.ToArray(), target, false);
    }

    private void SearchInScriptFiles(Object target)
    {
        var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
        string targetName = GetTargetNameForScriptSearch(target);
        
        if (string.IsNullOrEmpty(targetName)) return;
        
        for (int i = 0; i < scriptGuids.Length; i++)
        {
            if (i % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("Searching Scripts", 
                    $"{i}/{scriptGuids.Length}", i / (float)scriptGuids.Length);
            }
            
            var path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
            CheckScriptFileForReferences(path, target, targetName);
        }
    }

    private void SearchInObjects(Object[] objects, Object target, bool isInScene, bool isScriptableObject = false)
    {
        foreach (var obj in objects)
        {
            if (obj == null || obj == target) continue;
            
            try
            {
                var serializedObject = new SerializedObject(obj);
                CheckObjectForReferences(serializedObject, target, obj, isInScene, isScriptableObject);
            }
            catch (System.Exception)
            {
                // Skip objects that can't be serialized
            }
        }
    }

    private void CheckObjectForReferences(SerializedObject serializedObject, Object target, Object obj, 
        bool isInScene, bool isScriptableObject)
    {
        var iterator = serializedObject.GetIterator();
        var checkedProperties = new HashSet<string>(); // Thêm HashSet để track properties đã check
        
        while (iterator.NextVisible(true))
        {
            // Tránh check duplicate properties
            if (checkedProperties.Contains(iterator.propertyPath))
                continue;
                
            checkedProperties.Add(iterator.propertyPath);
            
            if (iterator.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (iterator.objectReferenceValue == target)
                {
                    AddReference(obj, iterator.propertyPath, target, isInScene, isScriptableObject);
                }
            }
        }
    }

    private string GetTargetNameForScriptSearch(Object target)
    {
        if (target is MonoScript monoScript)
        {
            var scriptClass = monoScript.GetClass();
            return scriptClass?.Name;
        }
        else if (target is Component component)
        {
            return component.GetType().Name;
        }
        else if (target is ScriptableObject scriptableObject)
        {
            return scriptableObject.GetType().Name;
        }
        
        return null;
    }

    private void CheckScriptFileForReferences(string scriptPath, Object target, string targetName)
    {
        try
        {
            var scriptContent = File.ReadAllText(scriptPath);
            
            if (IsValidScriptReference(scriptContent, targetName))
            {
                var foundLines = ExtractRelevantLines(scriptContent, targetName);
                
                if (foundLines.Count > 0)
                {
                    var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    if (scriptAsset != null)
                    {
                        AddScriptReference(scriptAsset, string.Join("\n", foundLines), target, scriptPath);
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // Skip files that can't be read
        }
    }

    private bool IsValidScriptReference(string scriptContent, string targetName)
    {
        var lines = scriptContent.Split('\n');
        
        return lines.Any(line =>
        {
            var cleanLine = RemoveComments(line);
            var words = SplitIntoWords(cleanLine);
            return words.Contains(targetName);
        });
    }

    private List<string> ExtractRelevantLines(string scriptContent, string targetName)
    {
        var lines = scriptContent.Split('\n');
        var foundLines = new List<string>();
        
        for (int i = 0; i < lines.Length && foundLines.Count < 3; i++)
        {
            var cleanLine = RemoveComments(lines[i]);
            var words = SplitIntoWords(cleanLine);
            
            if (words.Contains(targetName))
            {
                foundLines.Add($"Line {i + 1}: {lines[i].Trim()}");
            }
        }
        
        return foundLines;
    }

    private string RemoveComments(string line)
    {
        int commentIndex = line.IndexOf("//");
        return commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
    }

    private string[] SplitIntoWords(string line)
    {
        return line.Split(new char[] { ' ', '\t', '(', ')', '[', ']', '<', '>', '{', '}', ';', ',', ':', '.' }, 
                         System.StringSplitOptions.RemoveEmptyEntries);
    }

    private void AddReference(Object referencingObject, string propertyPath, Object target, bool isInScene, bool isScriptableObject)
    {
        // Tránh duplicate references
        bool isDuplicate = foundReferences.Any(r => 
            r.TargetObject == referencingObject && 
            r.PropertyName == propertyPath);
            
        if (isDuplicate) return;
        
        var referenceInfo = new ReferenceInfo
        {
            TargetObject = referencingObject,
            ObjectName = referencingObject.name,
            ComponentType = referencingObject.GetType().Name,
            PropertyName = propertyPath,
            ObjectPath = GetObjectPath(referencingObject),
            IsInScene = isInScene,
            IsScriptableObject = isScriptableObject,
            IsScript = false
        };
        
        foundReferences.Add(referenceInfo);
    }

    private void AddScriptReference(MonoScript script, string foundContent, Object target, string scriptPath)
    {
        // Tránh duplicate script references
        bool isDuplicate = foundReferences.Any(r => 
            r.TargetObject == script && 
            r.ObjectPath == scriptPath);
            
        if (isDuplicate) return;
        
        var referenceInfo = new ReferenceInfo
        {
            TargetObject = script,
            ObjectName = script.name,
            ComponentType = "MonoScript",
            PropertyName = "",
            ScriptContent = foundContent,
            ObjectPath = scriptPath,
            IsInScene = false,
            IsScriptableObject = false,
            IsScript = true
        };
        
        foundReferences.Add(referenceInfo);
    }

    private string GetObjectPath(Object obj)
    {
        if (obj is Component component)
        {
            return GetGameObjectPath(component.gameObject);
        }
        else if (obj is GameObject gameObject)
        {
            return GetGameObjectPath(gameObject);
        }
        else
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(assetPath) ? assetPath : obj.name;
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "Unknown";
        
        var path = obj.name;
        var parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return "/" + path;
    }

    private void RemoveDuplicates()
    {
        int originalCount = foundReferences.Count;
        
        // Group by unique combination và chỉ giữ lại 1
        foundReferences = foundReferences
            .GroupBy(r => new { 
                TargetObjectId = r.TargetObject?.GetInstanceID() ?? 0, 
                r.PropertyName, 
                r.ObjectPath 
            })
            .Select(g => g.First())
            .ToList();
            
        int removedCount = originalCount - foundReferences.Count;
        
        if (removedCount > 0)
        {
            Debug.Log($"🧹 Removed {removedCount} duplicate references. {foundReferences.Count} unique references remain.");
        }
        else
        {
            Debug.Log("✅ No duplicates found.");
        }
    }
}

// Simplified reference info class
[System.Serializable]
public class ReferenceInfo
{
    public Object TargetObject;
    public string ObjectName;
    public string ComponentType;
    public string PropertyName;
    public string ScriptContent;
    public string ObjectPath;
    public bool IsInScene;
    public bool IsScriptableObject;
    public bool IsScript;
}
#endif