using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BinFileAsset))]
public class BinFileAssetEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		// Draw the default inspector
		DrawDefaultInspector();

		// Get the target object
		BinFileAsset binFileAsset = (BinFileAsset)target;

		// Add space between the default inspector and the custom buttons
		EditorGUILayout.Space();

		// Add the "Delete" button
		if (GUILayout.Button("Delete"))
		{
			// Handle the delete action
			if (EditorUtility.DisplayDialog("Delete File", "Are you sure you want to delete this file?", "Yes", "No"))
			{
				// Delete the file
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(binFileAsset.binFile));
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}

		// Add the "Parse" button
		if (GUILayout.Button("Parse"))
		{
			// Handle the parse action
			if (binFileAsset.binFile != null)
			{
				// Example parse logic (replace with your actual parsing logic)
				byte[] data = binFileAsset.binFile.bytes;
				Debug.Log("Parsing file: " + binFileAsset.binFile.name);
				Debug.Log("File size: " + data.Length + " bytes");
			}
			else
			{
				Debug.LogWarning("No bin file assigned.");
			}
		}
	}
}