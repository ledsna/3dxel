using UnityEditor;
using UnityEngine;

public class GrassMakerWindow : EditorWindow {
	[SerializeField] private GameObject grassObject;
	[SerializeField] private int grassCount = 100;
	
	private GrassCreator _grassCreator;
	private GrassHolder _grassHolder;


	[MenuItem("Tools/Grass Maker")]
	static void Init() {
		// Get existing open window or if none, make a new one:
		GrassMakerWindow window = (GrassMakerWindow)GetWindow(typeof(GrassMakerWindow), false, "Grass Maker", true);
		window.titleContent = new GUIContent("Grass Maker");
		window.Show();
	}

	private void OnGUI() {
		grassObject =
			(GameObject)EditorGUILayout.ObjectField("Grass Handle Object", grassObject, typeof(GameObject), true);
		
		if (grassObject != null) {
			_grassCreator = grassObject.GetComponent<GrassCreator>();
			_grassHolder = grassObject.GetComponent<GrassHolder>();

			if (_grassHolder is null || _grassCreator is null) {
				EditorGUILayout.LabelField(
					"One of necessary components are missing(GrassCreator or GrassHolder). Creating grass is impossible");
			}
			else {
				grassCount = EditorGUILayout.IntField("Count Grass per Mesh", grassCount);
				if (GUILayout.Button("Generate Grass")) {
					if(Selection.activeObject is null)
						return;
					
					if (!_grassCreator.TryGeneratePoints(Selection.activeGameObject, grassCount))
						Debug.LogError("GrassMaker: Selected object don't contain Mesh Filter!");
					else
						Debug.Log($"GrassMaker: Grass created on {Selection.activeObject.name}");
				}

				if (GUILayout.Button("Release Positions")) {
					_grassHolder.Release();
					Debug.Log($"GrassMaker: Grass data was cleared");
				}
				
				EditorGUILayout.LabelField($"Grass On Scene:{_grassHolder.grassData.Count}", EditorStyles.label);
			}
		}
		else {
			if (GUILayout.Button("Create Grass Holder")) {
				CreateNewGrassHolder();
			}

			EditorGUILayout.LabelField("No Grass Holder found, create a new one", EditorStyles.label);
		}
	}

	void CreateNewGrassHolder() {
		grassObject = new GameObject();
		grassObject.name = "Grass Holder";
		_grassCreator = grassObject.AddComponent<GrassCreator>();
		_grassHolder = grassObject.AddComponent<GrassHolder>();
	}
}