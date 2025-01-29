using UnityEditor;

[CustomEditor(typeof(GrassHolder))] 
public class GrassHolderEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        // Get reference to the target script
        var script = (GrassHolder)target;

        // Check if the TextAsset is null
        if (script.GrassDataSource == null)
        {
            EditorGUILayout.HelpBox("Grass Data Source missing! Create new or select existing .grassdata file", MessageType.Error);
        }

        // Draw default inspector properties
        DrawDefaultInspector();
    }
}