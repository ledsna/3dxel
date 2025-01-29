// using UnityEditor;
// using UnityEngine;
//
// public class GrassDataPostProcessor : AssetPostprocessor
// {
//     static void OnPostprocessAllAssets(
//         string[] importedAssets, 
//         string[] deletedAssets, 
//         string[] movedAssets, 
//         string[] movedFromAssetPaths)
//     {
//         foreach (string assetPath in importedAssets)
//         {
//             if (assetPath.EndsWith(".grassdata"))
//             {
//                 // Load a custom icon from Resources or specify a built-in Unity icon
//                 Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/grass_icon.png") as Texture2D;
//                 if (icon != null)
//                 {
//                     Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
//                     EditorGUIUtility.SetIconForObject(asset, icon);
//                 }
//                 else
//                 {
//                     Debug.LogWarning("Custom grassdata icon not found! Place it in 'Assets/Editor/Icons/grass_icon.png'");
//                 }
//             }
//         }
//     }
// }