using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// Import any files with the .grassdata extension
/// </summary>
[ScriptedImporter(1, "grassdata")]
public sealed class GrassDataAssetImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var path = new TextAsset(ctx.assetPath);
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/grass_icon.png");
        ctx.AddObjectToAsset("Grass Data",path, icon);
    }
}