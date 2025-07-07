using UnityEngine;

public class FileAssetAttribute : PropertyAttribute
{
    public string Extension { get; private set; }

    public FileAssetAttribute(string extension)
    {
        Extension = extension.StartsWith(".") ? extension.ToLower() : "." + extension.ToLower();
    }
}