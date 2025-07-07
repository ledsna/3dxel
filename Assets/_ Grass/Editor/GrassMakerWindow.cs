using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Editor
{
    public partial class GrassMakerWindow : EditorWindow
    {
        [FormerlySerializedAs("grassObject")] [SerializeField]
        private GameObject grassHolderObject;

        [SerializeField] private int grassCount = 1000;
        [SerializeField, Range(0, 1)] private float normalLimit = 1;

        [HideInInspector] public int toolbarInt = 0;

        private GrassHolder _grassHolder;
        private Vector2 scrollPos;
        private int currentMainTabId;
        private readonly string[] mainTabBarStrings = { "Paint", "Generate" };
        private bool paintModeActive;
        private readonly string[] toolbarStrings = { "Add", "Remove" };
        private float brushSize = 0.2f;
        private Ray ray;
        private Vector3 hitPos;
        private Vector3 hitNormal;
        private Vector3 cachedPos;
        private RaycastHit[] terrainHit;
        private Vector3 mousePos;
        private Vector3 lastPosition = Vector3.zero;
        private float density = 0.1f;

        public LayerMask cullGrassMask;
        public LayerMask paintHitMask;


        [MenuItem("Tools/Grass Maker")]
        static void Init()
        {
            GrassMakerWindow window = (GrassMakerWindow)GetWindow(typeof(GrassMakerWindow), false, "Grass Maker", true);
            window.titleContent = new GUIContent("Grass Maker");
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.BeginHorizontal();
            currentMainTabId = GUILayout.Toolbar(currentMainTabId, mainTabBarStrings, GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            grassHolderObject = (GameObject)EditorGUILayout.ObjectField("Grass Handle Object",
                grassHolderObject,
                typeof(GameObject),
                true);
            
            if (grassHolderObject == null) {
                grassHolderObject = FindFirstObjectByType<GrassHolder>()?.gameObject;
            }
            
            if (grassHolderObject == null){
                if (GUILayout.Button("Create Grass Holder")) {
                    CreateNewGrassHolder();
                }

                EditorGUILayout.LabelField("No Grass Holder found, create a new one", EditorStyles.label);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            _grassHolder = grassHolderObject?.GetComponent<GrassHolder>();

            if (_grassHolder is null)
            {
                EditorGUILayout.LabelField(
                    "One of necessary component are missing(GrassHolder). Creating grass is impossible",
                    EditorStyles.helpBox);
                EditorGUILayout.EndScrollView();
                return;
            }

            switch (currentMainTabId)
            {
                case 0:
                    ShowPaintPanel();
                    break;
                case 1:
                    ShowGeneratePanel();
                    break;
            }

            GUILayout.FlexibleSpace();


            EditorGUILayout.LabelField($"Count of grass:{_grassHolder.grassData.Count}",
                EditorStyles.label);
            EditorGUILayout.LabelField($"Count of visible grass:{_grassHolder.grassData.Count}",
                EditorStyles.label);

            if (GUILayout.Button("Clear Grass"))
            {
                if (EditorUtility.DisplayDialog("Clear All Grass?",
                        "Are you sure you want to clear the grass?", "Clear", "Don't Clear"))
                    if (GrassDataManager.TryClearGrassData(_grassHolder))
                        Debug.Log($"Clear Grass Success");
                    else
                        Debug.LogError($"Clear Grass Failed");
            }

            if (GUILayout.Button("Save Positions"))
            {
                if (GrassDataManager.TrySaveGrassData(_grassHolder))
                    Debug.Log("Grass Data Saved");
                else
                    Debug.LogError("Grass Data Not Saved");
            }

            if (GUILayout.Button("Load Positions"))
            {
                if (GrassDataManager.TryLoadGrassData(_grassHolder))
                    Debug.Log("Grass Data Loaded");
                else
                    Debug.LogError("Grass Data Not Loaded");
            }

            EditorGUILayout.EndScrollView();
        }

        private void ShowPaintPanel()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Paint Mode:", EditorStyles.boldLabel);
            paintModeActive = EditorGUILayout.Toggle(paintModeActive);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Hit Settings", EditorStyles.boldLabel);
            paintHitMask = EditorGUILayout.MaskField("Paint Hit Mask",
                InternalEditorUtility.LayerMaskToConcatenatedLayersMask(paintHitMask),
                InternalEditorUtility.layers);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Paint Status (Right-Mouse Button to paint)", EditorStyles.boldLabel);
            toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
            brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 50f);

            if (toolbarInt == 0)
            {
                normalLimit = EditorGUILayout.Slider("Normal Limit", normalLimit, 0, 1);
                density = EditorGUILayout.Slider("Density", density, 0.1f, 10f);
            }

            EditorGUILayout.Separator();
        }

        private void ShowGeneratePanel()
        {
            cullGrassMask = EditorGUILayout.MaskField("Cull Mask",
                cullGrassMask,
                UnityEditorInternal.InternalEditorUtility.layers);

            normalLimit = EditorGUILayout.Slider("Normal Limit", normalLimit, 0, 1);

            grassHolderObject ??= FindFirstObjectByType<GrassHolder>()?.gameObject;

            if (grassHolderObject != null)
            {
                grassCount = EditorGUILayout.IntField("Count Grass per Mesh", grassCount);
                if (GUILayout.Button("Generate Grass"))
                {
                    if (Selection.activeObject is null)
                        return;

                    if (!GrassCreator.TryGeneratePoints(_grassHolder,
                            Selection.activeGameObject,
                            grassCount,
                            cullGrassMask,
                            normalLimit))
                        Debug.LogError("GrassMaker: Selected object don't contain Mesh Filter!");
                    else
                    {
                        var obj = (GameObject)Selection.activeObject;
                        if (((1 << obj.layer) & cullGrassMask) != 0)
                        {
                            Debug.LogWarning(
                                $"Grass Maker: Grass generated on {Selection.activeObject.name} with cull layer: {obj.layer}");
                        }
                        else
                        {
                            Debug.Log($"GrassMaker: Grass created on {Selection.activeObject.name}");
                        }
                        if (GrassDataManager.TrySaveGrassData(_grassHolder))
                            Debug.Log("Grass Data Saved");
                        else
                            Debug.LogError("Grass Data Not Saved");
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Create Grass Holder"))
                {
                    CreateNewGrassHolder();
                }

                EditorGUILayout.LabelField("No Grass Holder found, create a new one", EditorStyles.label);
            }
        }

        void CreateNewGrassHolder()
        {
            grassHolderObject = new GameObject();
            grassHolderObject.name = "Grass Holder";
            grassHolderObject.layer = LayerMask.NameToLayer("Grass");
            _grassHolder = grassHolderObject.AddComponent<GrassHolder>();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (hasFocus && paintModeActive)
                DrawHandles();
        }
    }
}