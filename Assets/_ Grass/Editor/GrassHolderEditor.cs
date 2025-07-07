using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrassHolder))] 
public class GrassHolderEditor : UnityEditor.Editor
{
    // Serialized Properties
    private SerializedProperty instanceMaterial;
    private SerializedProperty mesh;
    private SerializedProperty depthCullingTree;
    private SerializedProperty UseOctreeCulling;
    private SerializedProperty drawBounds;
    private SerializedProperty GrassDataSource;
    private SerializedProperty OrtographicCamera;

    private void OnEnable()
    {
        // Initialize serialized properties
        instanceMaterial = serializedObject.FindProperty("instanceMaterial");
        mesh = serializedObject.FindProperty("mesh");
        depthCullingTree = serializedObject.FindProperty("depthCullingTree");
        UseOctreeCulling = serializedObject.FindProperty("UseOctreeCulling");
        drawBounds = serializedObject.FindProperty("drawBounds");
        GrassDataSource = serializedObject.FindProperty("GrassDataSource");
        OrtographicCamera = serializedObject.FindProperty("OrtographicCamera");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // Start updating serialized properties
        
        // Get reference to the target script
        var script = (GrassHolder)target;
        
        // Check if the TextAsset is null
        if (script.GrassDataSource == null)
        {
            // Grass Data Source
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grass Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(GrassDataSource, new GUIContent("Grass Data Source"));
            
            EditorGUILayout.HelpBox("Grass Data Source missing! Create new or select existing .grassdata file", MessageType.Error);
            
            serializedObject.ApplyModifiedProperties();
            return;
        }
        
        // Grass Data Source
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grass Data", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(GrassDataSource, new GUIContent("Grass Data Source"));
        
        // Material and Mesh
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(instanceMaterial, new GUIContent("Instance Material"));
        EditorGUILayout.PropertyField(mesh, new GUIContent("Mesh"));
        
        // Culling Options
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Culling Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(UseOctreeCulling, new GUIContent("Use Octree Culling"));
        
        if (UseOctreeCulling.boolValue)
        {
            EditorGUILayout.PropertyField(drawBounds, new GUIContent("Draw Bounds"));
            
            // Draw the int slider for Depth Culling Tree
            EditorGUILayout.IntSlider(depthCullingTree, 1, 6, new GUIContent("Depth Culling Tree"));

        }

        // Apply changes
        serializedObject.ApplyModifiedProperties();
        
    }
}