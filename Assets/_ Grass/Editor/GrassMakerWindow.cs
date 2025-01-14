using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Editor {
	public class GrassMakerWindow : EditorWindow {
		[SerializeField] private GameObject grassObject;
		[SerializeField] private int grassCount = 1000;
		[SerializeField, Range(0, 1)] private float normalLimit = 1;

		private GrassCreator _grassCreator;
		private GrassHolder _grassHolder;
		public LayerMask cullGrassMask;


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

			cullGrassMask =
				EditorGUILayout.MaskField("Cull Mask", cullGrassMask, UnityEditorInternal.InternalEditorUtility.layers);

			normalLimit = EditorGUILayout.Slider("Normal Limit", normalLimit, 0, 1);

			if (grassObject == null) {
				grassObject = FindFirstObjectByType<GrassHolder>()?.gameObject;
			}

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
						if (Selection.activeObject is null)
							return;

						if (!_grassCreator.TryGeneratePoints(Selection.activeGameObject, grassCount, cullGrassMask,
						                                     normalLimit))
							Debug.LogError("GrassMaker: Selected object don't contain Mesh Filter!");
						else {

							var obj = (GameObject)Selection.activeObject;
							if (((1 << obj.layer) & cullGrassMask) != 0) {
								Debug.LogWarning(
									$"Grass Maker: Grass generated on {Selection.activeObject.name} with cull layer: {obj.layer}");
							}
							else {
								Debug.Log($"GrassMaker: Grass created on {Selection.activeObject.name}");
							}
						}
					}

					if (GUILayout.Button("Release Positions")) {
						_grassHolder.Release();
						// Debug.Log($"GrassMaker: Grass data was cleared");
					}

					if (GUILayout.Button("Save Positions")) {
						DataManager.TrySaveGrassData("Assets/_ Grass/Grass Data/data.bin", _grassHolder.grassData);
						// Debug.Log($"GrassMaker: Grass data was saved");
					}

					if (GUILayout.Button("Load Positions")) {
						DataManager.TryLoadGrassData("Assets/_ Grass/Grass Data/data.bin", out var grassData);
						_grassHolder.grassData = grassData;
						_grassHolder.OnEnable();
						// Debug.Log($"GrassMaker: Grass data was Loaded");
					}

					if (_grassHolder.grassData != null) {
						EditorGUILayout.LabelField($"Grass On Scene:{_grassHolder.grassData.Count}",
						                           EditorStyles.label);
						EditorGUILayout.LabelField($"Visible Grass On Scene:{_grassHolder.mapIdToDataList.Count}",
						                           EditorStyles.label);
					}
					else {
						EditorGUILayout.LabelField($"Grass On Scene: 0", EditorStyles.label);
					}
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
}