using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GrassDataManager {
    public static bool TryLoadGrassData(GrassHolder grassHolder)
    {
        var grassData = new List<GrassData>();

        try
        {
            string path = AssetDatabase.GetAssetPath(grassHolder.GrassDataSource);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Error: Grass data source path is invalid or missing.");
                return false;
            }
        
            var data = new GrassData();
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader binaryReader = new BinaryReader(fileStream)) {
                while (fileStream.Position < fileStream.Length) {
                    data.position = ReadVector3(binaryReader);
                    data.normal = ReadVector3(binaryReader);
                    grassData.Add(data);
                }
            }
            
            grassHolder.grassData.Clear();
            grassHolder.grassData = grassData;
            grassHolder.FastRebuild();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load grass data: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public static bool TrySaveGrassData(GrassHolder grassHolder)
    {
        try
        {
            string path = AssetDatabase.GetAssetPath(grassHolder.GrassDataSource);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Error: Grass data source path is invalid or missing.");
                return false;
            }

            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                foreach (var data in grassHolder.grassData)
                {
                    SaveVector3(data.position, binaryWriter);
                    SaveVector3(data.normal, binaryWriter);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save grass data: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public static bool TryClearGrassData(GrassHolder grassHolder)
    {
        try
        {
            string path = AssetDatabase.GetAssetPath(grassHolder.GrassDataSource);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Error: Grass data source path is invalid or missing.");
                return false;
            }

            // Open the file in "Create" mode, which overwrites it with an empty file
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                // Just opening the file in Create mode clears it
            }

            // Clear the in-memory grass data as well
            grassHolder.grassData.Clear();
            grassHolder.OnEnable();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to clear grass data: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    
    public static void CreateGrassDataAsset(string folderPath, GrassHolder grassHolder = null)
    {
        string baseName = "New Grass Data";

        // Ensure the folder exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        int index = 0;
        string filePath;
        string fileName;
        do
        {
            fileName = index == 0 ? $"{baseName}.grassdata" : $"{baseName}({index}).grassdata";
            filePath = Path.Combine(folderPath, fileName);
            index++;
        } while (File.Exists(filePath));

        // Создаем файл
        using (File.Create(filePath))
        {
        }


        // Refresh Unity's asset database
        AssetDatabase.Refresh();

        // Load the new TextAsset
        if (grassHolder != null)
        {
            grassHolder.GrassDataSource = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
            EditorUtility.SetDirty(grassHolder);
        }
    }

    private static void SaveVector3(Vector3 vector, BinaryWriter writer) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}

	private static Vector3 ReadVector3(BinaryReader binaryReader) {
		Vector3 res;
		res.x = binaryReader.ReadSingle();
		res.y = binaryReader.ReadSingle();
		res.z = binaryReader.ReadSingle();
		return res;
	}
}