#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomPropertyDrawer(typeof(FileAssetAttribute))]
public class FileAssetDrawer : PropertyDrawer
{
    private bool userMakeMistake;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the file extension from the attribute
        FileAssetAttribute fileAttribute = (FileAssetAttribute)attribute;
        string allowedExtension = fileAttribute.Extension;

        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            // Draw Object Field (Allows dragging & dropping files)
            Rect objectFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, label, property.objectReferenceValue, typeof(TextAsset), false);

            if (property.objectReferenceValue != null)
            {
                string path = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                string extension = Path.GetExtension(path).ToLower();

                // Validate file extension
                if (extension != allowedExtension)
                {
                    property.objectReferenceValue = null; // Clear invalid file
                    userMakeMistake = true;
                }
                else
                {
                    // Display file path
                    GUIStyle pathStyle = new GUIStyle(EditorStyles.label) { fontSize = 10, fontStyle = FontStyle.Italic };
                    Rect pathRect = new Rect(position.x, position.y + 20, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(pathRect, $"Path: {path}", pathStyle);
                    userMakeMistake = false;
                }
            }
            if (userMakeMistake)
                EditorGUILayout.HelpBox($"Only {allowedExtension} files are allowed!", MessageType.Warning);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [FileAsset(\".extension\")] with a DefaultAsset field.");
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return property.objectReferenceValue != null ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight;
    }
}
#endif
