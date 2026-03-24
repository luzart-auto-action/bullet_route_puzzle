using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;

public class BulkRenameWindow : EditorWindow
{
    private enum RenameMode
    {
        SelectedAssets,
        CurrentFolder
    }

    private enum RenameType
    {
        AddPrefix,
        AddSuffix,
        ReplaceText,
        SequentialRename,
        GroupSequentialByPrefix,
        ManualSymbolRename
    }

    private RenameMode renameMode = RenameMode.SelectedAssets;
    private RenameType renameType = RenameType.AddPrefix;

    // Normal rename params
    private string prefix = "";
    private string suffix = "";
    private string oldText = "";
    private string newText = "";
    private string baseName = "Item_";
    private int startIndex = 1;

    // Manual symbol rename params
    private string manualOldSymbol = "";
    private string manualNewSymbol = "";
    private bool updateReferences = true;
    private bool skipStringsAndComments = true;
    
    // Preview
    private bool previewMode = false;
    private Vector2 previewScrollPos;
    private List<RenamePreview> previewList = new List<RenamePreview>();

    [MenuItem("Luzart/LuzartTool/Bulk Rename")]
    public static void ShowWindow()
    {
        GetWindow(typeof(BulkRenameWindow), false, "Bulk Rename");
    }

    private void OnGUI()
    {
        GUILayout.Label("Bulk Rename Tool", EditorStyles.boldLabel);

        renameMode = (RenameMode)EditorGUILayout.EnumPopup("Target", renameMode);
        EditorGUILayout.Space(5);

        renameType = (RenameType)EditorGUILayout.EnumPopup("Rename Type", renameType);
        EditorGUILayout.Space(10);

        DrawRenameOptions();
        EditorGUILayout.Space(20);

        GUILayout.BeginHorizontal();
        if (renameType == RenameType.ManualSymbolRename)
        {
            if (GUILayout.Button("Preview", GUILayout.Height(35)))
            {
                PreviewManualSymbolRename();
            }
            if (GUILayout.Button("Apply Rename", GUILayout.Height(35)))
            {
                RenameAssets();
            }
        }
        else
        {
            if (GUILayout.Button("Rename", GUILayout.Height(35)))
            {
                RenameAssets();
            }
        }
        GUILayout.EndHorizontal();

        if (previewMode && previewList.Count > 0)
        {
            DrawPreview();
        }
    }

    private void DrawRenameOptions()
    {
        switch (renameType)
        {
            case RenameType.AddPrefix:
                prefix = EditorGUILayout.TextField("Prefix:", prefix);
                break;

            case RenameType.AddSuffix:
                suffix = EditorGUILayout.TextField("Suffix:", suffix);
                break;

            case RenameType.ReplaceText:
                oldText = EditorGUILayout.TextField("Replace:", oldText);
                newText = EditorGUILayout.TextField("With:", newText);
                break;

            case RenameType.SequentialRename:
                baseName = EditorGUILayout.TextField("Base Name:", baseName);
                startIndex = EditorGUILayout.IntField("Start Index:", startIndex);
                break;

            case RenameType.GroupSequentialByPrefix:
                EditorGUILayout.HelpBox(
                    "Tự động nhóm theo prefix (trước dấu '_') và đánh số 001, 002, 003... trong từng nhóm.",
                    MessageType.Info
                );
                break;

            case RenameType.ManualSymbolRename:
                EditorGUILayout.HelpBox(
                    "Manual Symbol Rename:\n" +
                    "• Nhập tên cũ và tên mới\n" +
                    "• Tự động đổi file + class/interface/enum\n" +
                    "• Update references trong project",
                    MessageType.Info
                );

                manualOldSymbol = EditorGUILayout.TextField("Old Symbol Name:", manualOldSymbol);
                manualNewSymbol = EditorGUILayout.TextField("New Symbol Name:", manualNewSymbol);

                updateReferences = EditorGUILayout.Toggle("Update All References", updateReferences);
                skipStringsAndComments = EditorGUILayout.Toggle("Skip Strings & Comments", skipStringsAndComments);
                break;
        }
    }

    private void DrawPreview()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label($"📋 Preview Changes ({previewList.Count} files):", EditorStyles.boldLabel);

        previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos, GUILayout.Height(200));

        foreach (var preview in previewList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.textArea);
            
            GUILayout.Label($"🆕 {preview.OriginalName}", EditorStyles.miniLabel);
            GUILayout.Label($"➡️ {preview.NewName}", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(preview.ClassChanges))
            {
                GUILayout.Label($"🔧 {preview.ClassChanges}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void PreviewManualSymbolRename()
    {
        previewList.Clear();

        if (string.IsNullOrEmpty(manualOldSymbol) || string.IsNullOrEmpty(manualNewSymbol))
        {
            EditorUtility.DisplayDialog("Manual Symbol Rename", "Vui lòng nhập tên symbol cũ và mới!", "OK");
            return;
        }

        if (manualOldSymbol == manualNewSymbol)
        {
            EditorUtility.DisplayDialog("Manual Symbol Rename", "Tên cũ và mới không được giống nhau!", "OK");
            return;
        }

        List<string> assetPaths = CollectAssetPaths();
        var relevantFiles = new List<string>();

        foreach (var path in assetPaths)
        {
            if (Path.GetExtension(path) != ".cs") continue;

            try
            {
                string content = File.ReadAllText(path);
                if (ContainsSymbol(content, manualOldSymbol))
                {
                    relevantFiles.Add(path);
                }
            }
            catch
            {
                continue;
            }
        }

        foreach (var path in relevantFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);

            var preview = new RenamePreview
            {
                Path = path,
                OriginalName = fileName,
                NewName = fileName == manualOldSymbol ? manualNewSymbol : fileName
            };

            var (classChanges, referenceCount) = AnalyzeManualSymbolChanges(path, manualOldSymbol, manualNewSymbol);
            preview.ClassChanges = $"{classChanges} (Found {referenceCount} references)";

            previewList.Add(preview);
        }

        previewMode = true;

        if (previewList.Count == 0)
        {
            EditorUtility.DisplayDialog("Manual Symbol Rename", $"Không tìm thấy symbol '{manualOldSymbol}'!", "OK");
        }
    }

    private bool ContainsSymbol(string content, string symbolName)
    {
        string pattern = $@"\b{Regex.Escape(symbolName)}\b";
        return Regex.IsMatch(content, pattern);
    }

    private (string changes, int referenceCount) AnalyzeManualSymbolChanges(string scriptPath, string oldName, string newName)
    {
        try
        {
            string content = File.ReadAllText(scriptPath);
            var changes = new List<string>();
            int totalReferences = 0;

            // Check for declarations
            var declarationPatterns = new[]
            {
                ("class", $@"\bclass\s+{Regex.Escape(oldName)}\b"),
                ("enum", $@"\benum\s+{Regex.Escape(oldName)}\b"),
                ("interface", $@"\binterface\s+{Regex.Escape(oldName)}\b"),
                ("struct", $@"\bstruct\s+{Regex.Escape(oldName)}\b")
            };

            bool hasDeclaration = false;
            foreach (var (type, pattern) in declarationPatterns)
            {
                if (Regex.IsMatch(content, pattern))
                {
                    changes.Add($"{type} {oldName} → {newName}");
                    hasDeclaration = true;
                }
            }

            // Count references
            if (skipStringsAndComments)
            {
                totalReferences = CountReferencesExcludingStringsComments(content, oldName);
            }
            else
            {
                string pattern = $@"\b{Regex.Escape(oldName)}\b";
                totalReferences = Regex.Matches(content, pattern).Count;
            }

            if (!hasDeclaration && totalReferences > 0)
            {
                changes.Add("References only");
            }

            return (changes.Count > 0 ? string.Join(", ", changes) : "", totalReferences);
        }
        catch
        {
            return ("", 0);
        }
    }

    private int CountReferencesExcludingStringsComments(string content, string symbolName)
    {
        string cleanContent = RemoveStringsAndComments(content);
        string pattern = $@"\b{Regex.Escape(symbolName)}\b";
        return Regex.Matches(cleanContent, pattern).Count;
    }

    private string RemoveStringsAndComments(string content)
    {
        // Remove single-line comments
        content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
        // Remove multi-line comments  
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);
        // Remove string literals
        content = Regex.Replace(content, @"""(?:[^""\\]|\\.)*""", "");
        content = Regex.Replace(content, @"'(?:[^'\\]|\\.)*'", "");
        // Remove verbatim strings
        content = Regex.Replace(content, @"@""(?:[^""]|"""")*""", "");

        return content;
    }

    private void RenameAssets()
    {
        List<string> assetPaths = CollectAssetPaths();

        if (assetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Bulk Rename", "Không tìm thấy asset nào!", "OK");
            return;
        }

        Undo.RecordObjects(Selection.objects, "Bulk Rename");

        switch (renameType)
        {
            case RenameType.GroupSequentialByPrefix:
                GroupSequentialRename(assetPaths);
                break;
            case RenameType.ManualSymbolRename:
                ManualSymbolRename(assetPaths);
                break;
            default:
                NormalRename(assetPaths);
                break;
        }
    }

    private void ManualSymbolRename(List<string> assetPaths)
    {
        if (string.IsNullOrEmpty(manualOldSymbol) || string.IsNullOrEmpty(manualNewSymbol))
        {
            EditorUtility.DisplayDialog("Manual Symbol Rename", "Vui lòng nhập tên symbol cũ và mới!", "OK");
            return;
        }

        if (manualOldSymbol == manualNewSymbol)
        {
            EditorUtility.DisplayDialog("Manual Symbol Rename", "Tên cũ và mới không được giống nhau!", "OK");
            return;
        }

        var csFiles = assetPaths.Where(p => Path.GetExtension(p) == ".cs").ToList();

        if (csFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Manual Symbol Rename", "No C# files selected!", "OK");
            return;
        }

        int renamedCount = 0;
        int updatedCount = 0;
        var symbolMappings = new Dictionary<string, string> { [manualOldSymbol] = manualNewSymbol };

        try
        {
            // Update references in all .cs files if enabled
            if (updateReferences)
            {
                var allCsFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
                
                for (int i = 0; i < allCsFiles.Length; i++)
                {
                    string file = allCsFiles[i];
                    EditorUtility.DisplayProgressBar("Manual Symbol Rename", $"Updating {Path.GetFileName(file)}...", (float)i / allCsFiles.Length);

                    try
                    {
                        UpdateReferencesInFile(file, symbolMappings);
                        updatedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"⚠️ Failed to update references in {file}: {e.Message}");
                    }
                }
            }

            // Rename files that match the symbol name
            var filesToRename = csFiles.Where(path => Path.GetFileNameWithoutExtension(path) == manualOldSymbol).ToList();

            foreach (var path in filesToRename)
            {
                // Update symbol declarations in the file
                RenameScriptContent(path, manualOldSymbol, manualNewSymbol);

                // Rename the file itself
                string result = AssetDatabase.RenameAsset(path, manualNewSymbol);
                if (string.IsNullOrEmpty(result))
                {
                    renamedCount++;
                    Debug.Log($"✅ Manual Symbol Renamed: {manualOldSymbol} → {manualNewSymbol}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to rename file: {result}");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"Manual Symbol Rename completed!\n\n" +
                        $"🔄 Symbol: {manualOldSymbol} → {manualNewSymbol}\n" +
                        $"✅ Renamed files: {renamedCount}\n" +
                        $"🔧 Updated files: {updatedCount}";

        EditorUtility.DisplayDialog("Manual Symbol Rename", message, "OK");

        previewMode = false;
        previewList.Clear();
    }

    private void UpdateReferencesInFile(string filePath, Dictionary<string, string> symbolMappings)
    {
        string content = File.ReadAllText(filePath);
        string newContent = content;
        bool hasChanges = false;

        foreach (var kvp in symbolMappings)
        {
            string oldSymbol = kvp.Key;
            string newSymbol = kvp.Value;

            if (skipStringsAndComments)
            {
                newContent = ReplaceSymbolContextAware(newContent, oldSymbol, newSymbol);
            }
            else
            {
                string pattern = $@"\b{Regex.Escape(oldSymbol)}\b";
                if (Regex.IsMatch(newContent, pattern))
                {
                    newContent = Regex.Replace(newContent, pattern, newSymbol);
                }
            }
            
            hasChanges = newContent != content;
        }

        if (hasChanges)
        {
            File.WriteAllText(filePath, newContent);
            Debug.Log($"🔧 Updated references in: {Path.GetFileName(filePath)}");
        }
    }

    private string ReplaceSymbolContextAware(string content, string oldSymbol, string newSymbol)
    {
        var result = new System.Text.StringBuilder();
        var tokenPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|@""(?:[^""]|"""")*""|//.*$|/\*.*?\*/|\b\w+\b|.";
        var matches = Regex.Matches(content, tokenPattern, RegexOptions.Multiline | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            string token = match.Value;

            // Skip strings and comments
            if (token.StartsWith("\"") || token.StartsWith("'") || token.StartsWith("@\"") ||
                token.StartsWith("//") || token.StartsWith("/*"))
            {
                result.Append(token);
            }
            else if (token == oldSymbol)
            {
                // Check word boundaries
                bool isWordBoundary = true;
                int start = match.Index;
                int end = match.Index + match.Length;

                if (start > 0 && char.IsLetterOrDigit(content[start - 1]))
                    isWordBoundary = false;
                if (end < content.Length && char.IsLetterOrDigit(content[end]))
                    isWordBoundary = false;

                result.Append(isWordBoundary ? newSymbol : token);
            }
            else
            {
                result.Append(token);
            }
        }

        return result.ToString();
    }

    private void RenameScriptContent(string scriptPath, string oldClassName, string newClassName)
    {
        try
        {
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                Debug.LogError($"❌ Script file does not exist: {scriptPath}");
                return;
            }

            if (oldClassName.Equals(newClassName))
                return;

            string content = File.ReadAllText(scriptPath);
            string newContent = content;

            // Replace declarations
            var patterns = new[]
            {
                ($@"\bclass\s+{Regex.Escape(oldClassName)}\b", $"class {newClassName}"),
                ($@"\benum\s+{Regex.Escape(oldClassName)}\b", $"enum {newClassName}"),
                ($@"\binterface\s+{Regex.Escape(oldClassName)}\b", $"interface {newClassName}"),
                ($@"\bstruct\s+{Regex.Escape(oldClassName)}\b", $"struct {newClassName}")
            };

            bool hasChanges = false;
            foreach (var (pattern, replacement) in patterns)
            {
                if (Regex.IsMatch(newContent, pattern))
                {
                    newContent = Regex.Replace(newContent, pattern, replacement);
                    hasChanges = true;
                }
            }

            // Replace constructors
            string constructorPattern = $@"\b{Regex.Escape(oldClassName)}\s*\(";
            if (Regex.IsMatch(newContent, constructorPattern))
            {
                newContent = Regex.Replace(newContent, constructorPattern, $"{newClassName}(");
                hasChanges = true;
            }

            if (hasChanges)
            {
                File.WriteAllText(scriptPath, newContent);
                Debug.Log($"🔧 Updated script content: {oldClassName} → {newClassName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to update script content for {scriptPath}: {e.Message}");
        }
    }

    private List<string> CollectAssetPaths()
    {
        List<string> assetPaths = new List<string>();

        if (renameMode == RenameMode.SelectedAssets)
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                    assetPaths.Add(path);
            }
        }
        else
        {
            string folderPath = GetCurrentProjectWindowFolder();
            if (folderPath != null)
            {
                string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!Directory.Exists(path))
                        assetPaths.Add(path);
                }
            }
        }

        return assetPaths;
    }

    private string GetCurrentProjectWindowFolder()
    {
        string path = "Assets";

        foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path))
                return path;
        }

        return path;
    }

    private void NormalRename(List<string> assetPaths)
    {
        int index = startIndex;

        // Handle folder renaming for ReplaceText
        if (renameType == RenameType.ReplaceText)
        {
            var folderPaths = assetPaths.Where(p => Directory.Exists(p)).ToList();
            foreach (var folderPath in folderPaths)
            {
                string folderName = Path.GetFileName(folderPath);
                if (!string.IsNullOrEmpty(folderName) && folderName.Contains(oldText))
                {
                    string newFolderName = folderName.Replace(oldText, newText);
                    string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
                    string newFolderPath = string.IsNullOrEmpty(parent) ? newFolderName : parent + "/" + newFolderName;
                    
                    string result = AssetDatabase.MoveAsset(folderPath, newFolderPath);
                    if (!string.IsNullOrEmpty(result))
                    {
                        Debug.LogError($"Failed to rename folder {folderPath} to {newFolderPath}: {result}");
                    }
                    else
                    {
                        Debug.Log($"Renamed folder: {folderPath} → {newFolderPath}");
                    }
                }
            }
        }

        foreach (var path in assetPaths)
        {
            if (Directory.Exists(path))
                continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            string newName = fileName;

            switch (renameType)
            {
                case RenameType.AddPrefix:
                    newName = prefix + fileName;
                    break;
                case RenameType.AddSuffix:
                    newName = fileName + suffix;
                    break;
                case RenameType.ReplaceText:
                    newName = fileName.Replace(oldText, newText);
                    break;
                case RenameType.SequentialRename:
                    newName = baseName + index.ToString();
                    index++;
                    break;
            }

            AssetDatabase.RenameAsset(path, newName);

            // Rename SubAssets for ReplaceText
            if (renameType == RenameType.ReplaceText)
            {
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var subAsset in subAssets)
                {
                    if (subAsset != null && !string.IsNullOrEmpty(subAsset.name) && subAsset.name.Contains(oldText))
                    {
                        subAsset.name = subAsset.name.Replace(oldText, newText);
                        EditorUtility.SetDirty(subAsset);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Bulk Rename", "Rename Completed!", "OK");
    }

    private void GroupSequentialRename(List<string> assetPaths)
    {
        Dictionary<string, List<(string path, string filename)>> groups = new Dictionary<string, List<(string, string)>>();

        // Grouping by prefix
        foreach (var path in assetPaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            int underscore = fileName.IndexOf('_');
            if (underscore < 0) continue;

            string prefix = fileName.Substring(0, underscore);

            if (!groups.ContainsKey(prefix))
                groups[prefix] = new List<(string, string)>();

            groups[prefix].Add((path, fileName));
        }

        // Rename each group
        foreach (var kvp in groups)
        {
            string prefix = kvp.Key;
            var files = kvp.Value;

            files.Sort((a, b) => a.filename.CompareTo(b.filename));

            int index = 0;
            foreach (var item in files)
            {
                string newName = $"{prefix}_{index.ToString("000")}";
                AssetDatabase.RenameAsset(item.path, newName);
                index++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Bulk Rename", "Group Sequential Rename Completed!", "OK");
    }

    [System.Serializable]
    public class RenamePreview
    {
        public string Path;
        public string OriginalName;
        public string NewName;
        public string ClassChanges;
    }
}
