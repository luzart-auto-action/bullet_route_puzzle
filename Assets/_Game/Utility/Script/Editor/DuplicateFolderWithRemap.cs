using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
namespace Luzart
{
    public class DuplicateFolderWithRemap : EditorWindow
    {
        private DefaultAsset sourceFolder;
        private string newFolderName = "";

        [MenuItem("Luzart/LuzartTool/Duplicate Folder With Remap")]
        private static void Open()
        {
            GetWindow<DuplicateFolderWithRemap>("Duplicate Remap");
        }

        private void OnGUI()
        {
            GUILayout.Label("Duplicate Folder And Remap ScriptableObject References", EditorStyles.boldLabel);

            sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Source Folder", sourceFolder, typeof(DefaultAsset), false);
            newFolderName = EditorGUILayout.TextField("New Folder Name", newFolderName);

            if (GUILayout.Button("Duplicate With Remap", GUILayout.Height(30)))
            {
                if (sourceFolder == null)
                {
                    Debug.LogError("Chọn folder trước.");
                    return;
                }

                DuplicateAndRemap();
            }
        }

        private void DuplicateAndRemap()
        {
            string src = AssetDatabase.GetAssetPath(sourceFolder);
            if (!AssetDatabase.IsValidFolder(src))
            {
                Debug.LogError("Không phải folder hợp lệ.");
                return;
            }
            newFolderName = string.IsNullOrEmpty(newFolderName) ? sourceFolder.name + "_Copy" : newFolderName;

            string parent = Path.GetDirectoryName(src);
            string dst = parent + "/" + newFolderName;

            // Duplicate folder
            if (AssetDatabase.CopyAsset(src, dst))
            {
                Debug.Log($"Duplicated folder to: {dst}");
            }
            else
            {
                Debug.LogError("Copy asset folder failed.");
                return;
            }

            AssetDatabase.Refresh();

            // Build map oldGUID -> newGUID (include SubAssets)
            Dictionary<string, string> guidMap = new Dictionary<string, string>();

            string[] srcFiles = Directory.GetFiles(src, "*.asset", SearchOption.AllDirectories);
            foreach (string file in srcFiles)
            {
                string relativeSrc = file.Replace("\\", "/");
                string newFile = relativeSrc.Replace(src, dst);

                if (!File.Exists(newFile))
                    continue;

                // Main asset
                string oldGuid = AssetDatabase.AssetPathToGUID(relativeSrc);
                string newGuid = AssetDatabase.AssetPathToGUID(newFile);
                guidMap[oldGuid] = newGuid;

                // SubAssets
                Object[] srcAssets = AssetDatabase.LoadAllAssetsAtPath(relativeSrc);
                Object[] newAssets = AssetDatabase.LoadAllAssetsAtPath(newFile);
                if (srcAssets != null && newAssets != null && srcAssets.Length == newAssets.Length)
                {
                    for (int i = 0; i < srcAssets.Length; i++)
                    {
                        if (srcAssets[i] == null || newAssets[i] == null) continue;
                        string srcSubGuid = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(srcAssets[i], out string guid1, out long localId1) ? guid1 : null;
                        string newSubGuid = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newAssets[i], out string guid2, out long localId2) ? guid2 : null;
                        if (!string.IsNullOrEmpty(srcSubGuid) && !string.IsNullOrEmpty(newSubGuid))
                        {
                            guidMap[srcSubGuid] = newSubGuid;
                        }
                    }
                }
            }

            // Now remap in duplicated files
            string[] newFiles = Directory.GetFiles(dst, "*.asset", SearchOption.AllDirectories);

            foreach (string file in newFiles)
            {
                string text = File.ReadAllText(file);
                bool changed = false;
                foreach (var kv in guidMap)
                {
                    string oldGuid = kv.Key;
                    string newGuid = kv.Value;
                    if (text.Contains(oldGuid))
                    {
                        text = text.Replace(oldGuid, newGuid);
                        changed = true;
                    }
                }
                if (changed)
                {
                    File.WriteAllText(file, text);
                    Debug.Log("Remapped: " + file);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=green>Remap Completed!</color>");
        }
        [MenuItem("Assets/Duplicate With Remap", false, 19)]
        public static void DuplicateWithRemap()
        {
            // Cách chính xác nhất của Unity
            string path = GetSelectedFolderPath();

            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder!", "OK");
                return;
            }

            string parent = Path.GetDirectoryName(path);
            string newFolderPath = AssetDatabase.GenerateUniqueAssetPath(parent + "/" + Path.GetFileName(path) + "_Copy");

            AssetDatabase.CopyAsset(path, newFolderPath);
            AssetDatabase.Refresh();

            // Build remap table
            Dictionary<string, string> map = new Dictionary<string, string>();

            string[] oldFiles = Directory.GetFiles(path, "*.asset", SearchOption.AllDirectories);
            foreach (var old in oldFiles)
            {
                string newPath = old.Replace(path, newFolderPath);
                if (File.Exists(newPath))
                {
                    string oldGuid = AssetDatabase.AssetPathToGUID(old);
                    string newGuid = AssetDatabase.AssetPathToGUID(newPath);
                    map[oldGuid] = newGuid;
                }
            }

            // Replace guids
            string[] newFiles = Directory.GetFiles(newFolderPath, "*.asset", SearchOption.AllDirectories);
            foreach (var file in newFiles)
            {
                string content = File.ReadAllText(file);
                bool modified = false;

                foreach (var kv in map)
                {
                    if (content.Contains(kv.Key))
                    {
                        content = content.Replace(kv.Key, kv.Value);
                        modified = true;
                    }
                }

                if (modified)
                    File.WriteAllText(file, content);
            }

            AssetDatabase.Refresh();
            Debug.Log("Duplicate With Remap Completed!");
        }
        public static string GetSelectedFolderPath()
        {

            // 1. Nếu user chọn một asset/folder trong Project → lấy GUID
            if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);

                // Nếu là folder thật → trả thẳng
                if (AssetDatabase.IsValidFolder(path))
                    return path;

                // Nếu là file → trả folder chứa file
                string dir = Path.GetDirectoryName(path).Replace("\\", "/");
                if (AssetDatabase.IsValidFolder(dir))
                    return dir;
            }

            // 2. Unity fallback: nếu không chọn gì, trả "Assets"
            return null;
        }
        private string GetPath()
        {
            var selectedGUIDs = Selection.assetGUIDs;
            if (selectedGUIDs != null && selectedGUIDs.Length > 0)
            {
                string guid = selectedGUIDs[0];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    return assetPath;
                }
            }
            return null;
        }

    }
}

