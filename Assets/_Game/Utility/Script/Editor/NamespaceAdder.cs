#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NamespaceAdder : EditorWindow
{
    private string namespaceName = "Luzart";
    private string oldNamespaceName = "";
    private string newNamespaceName = "Luzart";
    private string folderPath = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Add Namespace", "Remove Namespace", "Replace Namespace", "Preview Changes", "Namespace Tree" };
    
    // Preview data
    private List<FileChangeInfo> previewChanges = new List<FileChangeInfo>();
    private Vector2 previewScrollPosition = Vector2.zero;
    private bool showPreview = false;
    
    // Namespace tree data
    private NamespaceTreeNode namespaceTreeRoot;
    private Vector2 treeScrollPosition = Vector2.zero;
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    [MenuItem("Luzart/LuzartTool/Namespace Manager")]
    public static void ShowWindow()
    {
        NamespaceAdder window = GetWindow<NamespaceAdder>("Namespace Manager");
        window.minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        GUILayout.Label("Namespace Manager", EditorStyles.boldLabel);
        
        // Tab selection
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        
        GUILayout.Space(10);
        
        // Folder path selection (common for most tabs)
        if (selectedTab != 4) // Don't show folder selection for Namespace Tree tab
        {
            GUILayout.BeginHorizontal();
            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selectedFolder = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    folderPath = selectedFolder;
                }
            }
            if (GUILayout.Button("Current Project", GUILayout.Width(100)))
            {
                folderPath = Application.dataPath;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        // Tab content
        switch (selectedTab)
        {
            case 0: // Add Namespace
                DrawAddNamespaceTab();
                break;
            case 1: // Remove Namespace
                DrawRemoveNamespaceTab();
                break;
            case 2: // Replace Namespace
                DrawReplaceNamespaceTab();
                break;
            case 3: // Preview Changes
                DrawPreviewTab();
                break;
            case 4: // Namespace Tree
                DrawNamespaceTreeTab();
                break;
        }
    }

    private void DrawAddNamespaceTab()
    {
        GUILayout.Label("Add Namespace to Scripts", EditorStyles.boldLabel);
        namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview Add"))
        {
            PreviewAddNamespace();
        }
        if (GUILayout.Button("Add Namespace"))
        {
            AddNamespaceToScripts();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("This will add the specified namespace to all C# scripts that don't already have a namespace.", MessageType.Info);
        
        if (showPreview && previewChanges.Count > 0)
        {
            DrawPreviewSection();
        }
    }

    private void DrawRemoveNamespaceTab()
    {
        GUILayout.Label("Remove Namespace from Scripts", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview Remove"))
        {
            PreviewRemoveNamespace();
        }
        if (GUILayout.Button("Remove All Namespaces"))
        {
            RemoveNamespaceFromScripts();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("This will remove ALL namespaces from C# scripts, moving the content back to global scope.", MessageType.Warning);
        
        if (showPreview && previewChanges.Count > 0)
        {
            DrawPreviewSection();
        }
    }

    private void DrawReplaceNamespaceTab()
    {
        GUILayout.Label("Replace Namespace in Scripts", EditorStyles.boldLabel);
        oldNamespaceName = EditorGUILayout.TextField("Old Namespace", oldNamespaceName);
        newNamespaceName = EditorGUILayout.TextField("New Namespace", newNamespaceName);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview Replace"))
        {
            PreviewReplaceNamespace();
        }
        if (GUILayout.Button("Replace Namespace"))
        {
            ReplaceNamespaceInScripts();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("This will replace the old namespace with the new one in all matching C# scripts.", MessageType.Info);
        
        if (showPreview && previewChanges.Count > 0)
        {
            DrawPreviewSection();
        }
    }

    private void DrawPreviewTab()
    {
        GUILayout.Label("Preview Changes", EditorStyles.boldLabel);
        
        if (previewChanges.Count == 0)
        {
            EditorGUILayout.HelpBox("No preview data available. Use 'Preview' buttons from other tabs to see changes.", MessageType.Info);
            return;
        }
        
        DrawPreviewSection();
    }

    private void DrawNamespaceTreeTab()
    {
        GUILayout.Label("Namespace Tree View", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Refresh Tree"))
        {
            RefreshNamespaceTree();
        }
        if (GUILayout.Button("📁 Analyze Current Project"))
        {
            folderPath = Application.dataPath;
            RefreshNamespaceTree();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        if (namespaceTreeRoot != null)
        {
            treeScrollPosition = EditorGUILayout.BeginScrollView(treeScrollPosition);
            DrawNamespaceTree(namespaceTreeRoot, 0);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Click 'Refresh Tree' to analyze namespaces in your project.", MessageType.Info);
        }
    }

    #region Preview Methods
    
    private void PreviewAddNamespace()
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(namespaceName.Trim()))
        {
            EditorUtility.DisplayDialog("Error", "Please provide both folder path and namespace name.", "OK");
            return;
        }
        
        previewChanges.Clear();
        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var scriptPath in scriptFiles)
        {
            string[] lines = File.ReadAllLines(scriptPath);
            
            if (!HasNamespace(lines))
            {
                var change = new FileChangeInfo
                {
                    FilePath = scriptPath,
                    ChangeType = ChangeType.Add,
                    OldNamespace = "No namespace",
                    NewNamespace = namespaceName.Trim(),
                    PreviewContent = GeneratePreviewForAdd(lines, namespaceName.Trim())
                };
                previewChanges.Add(change);
            }
        }
        
        showPreview = true;
        selectedTab = 3; // Switch to preview tab
        Debug.Log($"Preview generated for {previewChanges.Count} files");
    }
    
    private void PreviewRemoveNamespace()
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Please provide folder path.", "OK");
            return;
        }
        
        previewChanges.Clear();
        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var scriptPath in scriptFiles)
        {
            string[] lines = File.ReadAllLines(scriptPath);
            
            if (HasNamespace(lines))
            {
                string existingNamespace = ExtractNamespaceName(lines);
                var change = new FileChangeInfo
                {
                    FilePath = scriptPath,
                    ChangeType = ChangeType.Remove,
                    OldNamespace = existingNamespace,
                    NewNamespace = "No namespace",
                    PreviewContent = GeneratePreviewForRemove(lines)
                };
                previewChanges.Add(change);
            }
        }
        
        showPreview = true;
        selectedTab = 3; // Switch to preview tab
        Debug.Log($"Preview generated for {previewChanges.Count} files");
    }
    
    private void PreviewReplaceNamespace()
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(oldNamespaceName.Trim()) || string.IsNullOrEmpty(newNamespaceName.Trim()))
        {
            EditorUtility.DisplayDialog("Error", "Please provide folder path and both namespace names.", "OK");
            return;
        }
        
        previewChanges.Clear();
        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var scriptPath in scriptFiles)
        {
            string[] lines = File.ReadAllLines(scriptPath);
            string existingNamespace = ExtractNamespaceName(lines);
            
            if (existingNamespace == oldNamespaceName.Trim())
            {
                var change = new FileChangeInfo
                {
                    FilePath = scriptPath,
                    ChangeType = ChangeType.Replace,
                    OldNamespace = oldNamespaceName.Trim(),
                    NewNamespace = newNamespaceName.Trim(),
                    PreviewContent = GeneratePreviewForReplace(lines, oldNamespaceName.Trim(), newNamespaceName.Trim())
                };
                previewChanges.Add(change);
            }
        }
        
        showPreview = true;
        selectedTab = 3; // Switch to preview tab
        Debug.Log($"Preview generated for {previewChanges.Count} files");
    }

    private void DrawPreviewSection()
    {
        GUILayout.Space(10);
        GUILayout.Label($"📋 Preview Changes ({previewChanges.Count} files)", EditorStyles.boldLabel);
        
        if (GUILayout.Button("❌ Clear Preview"))
        {
            previewChanges.Clear();
            showPreview = false;
        }
        
        previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.Height(300));
        
        foreach (var change in previewChanges)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            // File header
            string changeIcon = change.ChangeType == ChangeType.Add ? "➕" : 
                               change.ChangeType == ChangeType.Remove ? "➖" : "🔄";
            GUILayout.Label($"{changeIcon} {Path.GetFileName(change.FilePath)}", EditorStyles.boldLabel);
            GUILayout.Label($"Path: {change.FilePath}", EditorStyles.miniLabel);
            GUILayout.Label($"Old: {change.OldNamespace} → New: {change.NewNamespace}", EditorStyles.miniLabel);
            
            // Preview content (first few lines)
            if (!string.IsNullOrEmpty(change.PreviewContent))
            {
                GUILayout.Label("Preview:", EditorStyles.miniBoldLabel);
                string[] previewLines = change.PreviewContent.Split('\n');
                for (int i = 0; i < Mathf.Min(5, previewLines.Length); i++)
                {
                    GUILayout.Label($"  {previewLines[i]}", EditorStyles.miniLabel);
                }
                if (previewLines.Length > 5)
                {
                    GUILayout.Label("  ...", EditorStyles.miniLabel);
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Namespace Tree Methods
    
    private void RefreshNamespaceTree()
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            folderPath = Application.dataPath;
        }
        
        namespaceTreeRoot = new NamespaceTreeNode("Root", NodeType.Folder);
        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var scriptPath in scriptFiles)
        {
            string[] lines = File.ReadAllLines(scriptPath);
            string namespaceName = ExtractNamespaceName(lines);
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                AddToTree(namespaceTreeRoot, scriptPath, namespaceName);
            }
            else
            {
                AddToTree(namespaceTreeRoot, scriptPath, "No Namespace");
            }
        }
        
        foldoutStates.Clear();
        Debug.Log($"Namespace tree refreshed with {scriptFiles.Length} files analyzed");
    }
    
    private void AddToTree(NamespaceTreeNode root, string filePath, string namespaceName)
    {
        string relativePath = filePath.Replace(folderPath, "").Replace("\\", "/").TrimStart('/');
        string[] pathParts = relativePath.Split('/');
        
        NamespaceTreeNode currentNode = root;
        
        // Navigate through folder structure
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            string folderName = pathParts[i];
            NamespaceTreeNode folderNode = currentNode.Children.Find(n => n.Name == folderName && n.Type == NodeType.Folder);
            
            if (folderNode == null)
            {
                folderNode = new NamespaceTreeNode(folderName, NodeType.Folder);
                currentNode.Children.Add(folderNode);
            }
            
            currentNode = folderNode;
        }
        
        // Add file with namespace info
        string fileName = pathParts[pathParts.Length - 1];
        NamespaceTreeNode fileNode = new NamespaceTreeNode(fileName, NodeType.File)
        {
            FullPath = filePath,
            Namespace = namespaceName
        };
        currentNode.Children.Add(fileNode);
    }
    
    private void DrawNamespaceTree(NamespaceTreeNode node, int indentLevel)
    {
        if (node.Name == "Root")
        {
            foreach (var child in node.Children.OrderBy(n => n.Type).ThenBy(n => n.Name))
            {
                DrawNamespaceTree(child, 0);
            }
            return;
        }
        
        GUILayout.BeginHorizontal();
        GUILayout.Space(indentLevel * 20);
        
        if (node.Type == NodeType.Folder)
        {
            string foldoutKey = node.GetPath();
            bool foldout = foldoutStates.ContainsKey(foldoutKey) ? foldoutStates[foldoutKey] : false;
            
            string folderIcon = foldout ? "📂" : "📁";
            foldout = EditorGUILayout.Foldout(foldout, $"{folderIcon} {node.Name}");
            foldoutStates[foldoutKey] = foldout;
            
            GUILayout.EndHorizontal();
            
            if (foldout)
            {
                foreach (var child in node.Children.OrderBy(n => n.Type).ThenBy(n => n.Name))
                {
                    DrawNamespaceTree(child, indentLevel + 1);
                }
            }
        }
        else // File
        {
            string fileIcon = "📄";
            string namespaceInfo = string.IsNullOrEmpty(node.Namespace) || node.Namespace == "No Namespace" 
                ? "❌ No namespace" 
                : $"✅ {node.Namespace}";
            
            GUILayout.Label($"{fileIcon} {node.Name}");
            GUILayout.FlexibleSpace();
            GUILayout.Label(namespaceInfo, EditorStyles.miniLabel);
            
            if (GUILayout.Button("📍", GUILayout.Width(25)))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(node.FullPath.Replace(Application.dataPath, "Assets")));
            }
            
            GUILayout.EndHorizontal();
        }
    }

    #endregion

    #region Helper Classes and Methods
    
    private string GeneratePreviewForAdd(string[] lines, string namespaceName)
    {
        var preview = new List<string>
        {
            $"namespace {namespaceName}",
            "{"
        };
        
        for (int i = 0; i < Mathf.Min(3, lines.Length); i++)
        {
            preview.Add($"    {lines[i]}");
        }
        
        if (lines.Length > 3)
        {
            preview.Add("    ...");
        }
        
        preview.Add("}");
        
        return string.Join("\n", preview);
    }
    
    private string GeneratePreviewForRemove(string[] lines)
    {
        var preview = new List<string>();
        bool insideNamespace = false;
        int namespaceIndentLevel = 0;
        int previewCount = 0;
        
        foreach (var line in lines)
        {
            if (previewCount >= 5) break;
            
            string trimmedLine = line.TrimStart();
            
            if (trimmedLine.StartsWith("namespace"))
            {
                insideNamespace = true;
                namespaceIndentLevel = line.Length - line.TrimStart().Length;
                continue;
            }
            
            if (insideNamespace)
            {
                if (line.Trim() == "}" && (line.Length - line.TrimStart().Length) == namespaceIndentLevel)
                {
                    insideNamespace = false;
                    continue;
                }
                
                if (line.StartsWith("    "))
                {
                    preview.Add(line.Substring(4));
                    previewCount++;
                }
                else if (line.StartsWith("\t"))
                {
                    preview.Add(line.Substring(1));
                    previewCount++;
                }
                else
                {
                    preview.Add(line);
                    previewCount++;
                }
            }
            else
            {
                preview.Add(line);
                previewCount++;
            }
        }
        
        return string.Join("\n", preview);
    }
    
    private string GeneratePreviewForReplace(string[] lines, string oldNamespace, string newNamespace)
    {
        var preview = new List<string>();
        int previewCount = 0;
        
        foreach (var line in lines)
        {
            if (previewCount >= 5) break;
            
            string trimmedLine = line.TrimStart();
            
            if (trimmedLine.StartsWith("namespace"))
            {
                string currentNamespace = trimmedLine.Substring(9).Trim();
                if (currentNamespace == oldNamespace)
                {
                    string indentation = line.Substring(0, line.Length - line.TrimStart().Length);
                    preview.Add($"{indentation}namespace {newNamespace}");
                }
                else
                {
                    preview.Add(line);
                }
            }
            else
            {
                preview.Add(line);
            }
            
            previewCount++;
        }
        
        return string.Join("\n", preview);
    }

    // Existing methods (AddNamespaceToScripts, RemoveNamespaceFromScripts, ReplaceNamespaceInScripts, HasNamespace, ExtractNamespaceName)
    // Keep all the original implementation methods here...
    
    private void AddNamespaceToScripts()
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Folder path is empty. Please select a folder.");
            return;
        }

        if (string.IsNullOrEmpty(namespaceName.Trim()))
        {
            Debug.LogError("Namespace name is empty. Please enter a valid namespace name.");
            return;
        }

        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        int processedCount = 0;

        foreach (var scriptPath in scriptFiles)
        {
            string[] lines = File.ReadAllLines(scriptPath);

            if (HasNamespace(lines))
            {
                Debug.Log($"Skipped: {scriptPath} (Namespace already exists)");
                continue;
            }

            using (StreamWriter writer = new StreamWriter(scriptPath))
            {
                writer.WriteLine($"namespace {namespaceName.Trim()}");
                writer.WriteLine("{");

                foreach (var line in lines)
                {
                    writer.WriteLine($"    {line}");
                }

                writer.WriteLine("}");
            }

            processedCount++;
            Debug.Log($"Namespace added to: {scriptPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Namespace addition complete. Processed {processedCount} files.");
    }

    private void RemoveNamespaceFromScripts()
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Folder path is empty. Please select a folder.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm Remove Namespaces", 
            "Are you sure you want to remove all namespaces from the scripts in the selected folder?\n\nThis action cannot be undone.", 
            "Yes, Remove", "Cancel"))
        {
            return;
        }

        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        int processedCount = 0;

        foreach (var scriptPath in scriptFiles)
        {
            string[] lines = File.ReadAllLines(scriptPath);

            if (!HasNamespace(lines))
            {
                Debug.Log($"Skipped: {scriptPath} (No namespace found)");
                continue;
            }

            List<string> newLines = new List<string>();
            bool insideNamespace = false;
            int namespaceIndentLevel = 0;

            foreach (var line in lines)
            {
                string trimmedLine = line.TrimStart();
                
                if (trimmedLine.StartsWith("namespace"))
                {
                    insideNamespace = true;
                    namespaceIndentLevel = line.Length - line.TrimStart().Length;
                    continue;
                }
                
                if (insideNamespace)
                {
                    if (line.Trim() == "}" && (line.Length - line.TrimStart().Length) == namespaceIndentLevel)
                    {
                        insideNamespace = false;
                        continue;
                    }
                    
                    if (line.StartsWith("    "))
                    {
                        newLines.Add(line.Substring(4));
                    }
                    else if (line.StartsWith("\t"))
                    {
                        newLines.Add(line.Substring(1));
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(scriptPath, newLines);
            processedCount++;
            Debug.Log($"Namespace removed from: {scriptPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Namespace removal complete. Processed {processedCount} files.");
    }

    private void ReplaceNamespaceInScripts()
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Folder path is empty. Please select a folder.");
            return;
        }

        if (string.IsNullOrEmpty(oldNamespaceName.Trim()) || string.IsNullOrEmpty(newNamespaceName.Trim()))
        {
            Debug.LogError("Both old and new namespace names must be provided.");
            return;
        }

        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        int processedCount = 0;

        foreach (var scriptPath in scriptFiles)
        {
            string[] lines = File.ReadAllLines(scriptPath);
            bool hasChanges = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmedLine = lines[i].TrimStart();
                
                if (trimmedLine.StartsWith("namespace"))
                {
                    string currentNamespaceLine = trimmedLine;
                    string currentNamespace = currentNamespaceLine.Substring(9).Trim();
                    
                    if (currentNamespace == oldNamespaceName.Trim())
                    {
                        string indentation = lines[i].Substring(0, lines[i].Length - lines[i].TrimStart().Length);
                        lines[i] = indentation + $"namespace {newNamespaceName.Trim()}";
                        hasChanges = true;
                        break;
                    }
                }
            }

            if (hasChanges)
            {
                File.WriteAllLines(scriptPath, lines);
                processedCount++;
                Debug.Log($"Namespace replaced in: {scriptPath}");
            }
            else
            {
                Debug.Log($"Skipped: {scriptPath} (Old namespace not found)");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Namespace replacement complete. Processed {processedCount} files.");
    }

    private bool HasNamespace(string[] lines)
    {
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("namespace"))
            {
                return true;
            }
        }
        return false;
    }

    private string ExtractNamespaceName(string[] lines)
    {
        foreach (var line in lines)
        {
            string trimmedLine = line.TrimStart();
            if (trimmedLine.StartsWith("namespace"))
            {
                return trimmedLine.Substring(9).Trim();
            }
        }
        return "";
    }
}

#endregion
#endif