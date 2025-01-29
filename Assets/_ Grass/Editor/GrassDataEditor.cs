// using UnityEditor;
//
// [CustomEditor(typeof(DefaultAsset))]
// public class GrassDataEditor : UnityEditor.Editor
// {
//     public override void OnInspectorGUI()
//     {
//         // Get the file path
//         string path = AssetDatabase.GetAssetPath(target);
//
//         // Check if this is a .grassdata file
//         if (path.EndsWith(".grassdata"))
//         {
//             EditorGUILayout.LabelField("Grass Data File", EditorStyles.boldLabel);
//             EditorGUILayout.LabelField("Path:", path);
//             EditorGUILayout.HelpBox("This is a custom grass data file.", MessageType.Info);
//         }
//         else
//         {
//             // If it's not a .grassdata file, use the default inspector
//             base.OnInspectorGUI();
//         }
//     }
// }