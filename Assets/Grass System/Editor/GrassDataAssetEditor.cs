using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrassDataAsset))]
public class GrassDataAssetEditor : UnityEditor.Editor {
	private SerializedProperty _filePath;

	private void OnEnable() {
		_filePath = serializedObject.FindProperty("filePath");
	}

	public override void OnInspectorGUI() {
		serializedObject.UpdateIfRequiredOrScript();

		var rect = new Rect(0,0,100,20);
		
		// // Begin property
		// EditorGUI.BeginProperty(new Rect(0, 0, 100, 100), new GUIContent("File Path"), _filePath);
		//
		//
		// // Customize the input field (e.g., change the background color)
		// EditorGUI.BeginChangeCheck();
		// string newFilePath = EditorGUILayout.TextField("File Path", _filePath.stringValue);
		// if (EditorGUI.EndChangeCheck()) {
		// 	_filePath.stringValue = newFilePath;
		// }
		//
		// // End property
		// EditorGUI.EndProperty();

		EditorGUI.PropertyField(rect, _filePath);

		serializedObject.ApplyModifiedProperties();
	}
}