using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BinFileAttribute))]
public class BinFileDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		Rect contentPosition = EditorGUI.PrefixLabel(position, label);
		
		// Calculate the position for the text field and the button
		float buttonWidth = 20f;
		Rect textFieldPosition = new Rect(contentPosition.x, contentPosition.y, contentPosition.width - buttonWidth, contentPosition.height);
		Rect buttonPosition = new Rect(contentPosition.x + contentPosition.width - buttonWidth, contentPosition.y, buttonWidth, contentPosition.height);

		// Draw the text field
		string filePath = property.stringValue;
		string displayText = string.IsNullOrEmpty(filePath) ? "No file selected" : System.IO.Path.GetFileName(filePath);
		EditorGUI.TextField(textFieldPosition, displayText);

		// Draw the button
		if (GUI.Button(buttonPosition, "..."))
		{
			string path = EditorUtility.OpenFilePanel("Select .bin File", "", "bin");
			if (!string.IsNullOrEmpty(path))
			{
				property.stringValue = path;
			}
		}
		
		// Handle drag-and-drop
		Event evt = Event.current;
		if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
		{
			if (contentPosition.Contains(evt.mousePosition))
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				if (evt.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();

					foreach (string draggedPath in DragAndDrop.paths)
					{
						if (System.IO.Path.HasExtension(draggedPath) && System.IO.Path.GetExtension(draggedPath).ToLower() == ".bin")
						{
							property.stringValue = draggedPath;
							break;
						}
					}
				}

				Event.current.Use();
			}
		}
		

		EditorGUI.EndProperty();
	}
}