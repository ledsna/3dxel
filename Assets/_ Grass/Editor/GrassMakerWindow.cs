using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Editor
{
    public class GrassMakerWindow : EditorWindow
    {
        [FormerlySerializedAs("grassObject")] [SerializeField]
        private GameObject grassHolderObject;

        [SerializeField] private int grassCount = 1000;
        [SerializeField, Range(0, 1)] private float normalLimit = 1;
        
        [HideInInspector]
        public int toolbarInt = 0;
        [HideInInspector]
        public int toolbarIntEdit = 0;
        
        private GrassHolder _grassHolder;
        private Vector2 scrollPos;
        private int currentMainTabId;
        private readonly string[] mainTabBarStrings = { "Paint", "Generate" };
        private bool paintModeActive;
        private readonly string[] toolbarStrings = { "Add", "Remove", "Edit", "Reproject" };
        private float brushSize = 0.2f;
        
        
        public LayerMask cullGrassMask;
        public LayerMask paintHitMask;


        [MenuItem("Tools/Grass Maker")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
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

            switch (currentMainTabId)
            {
                case 0:
                    ShowPaintPanel();
                    break;

                case 1:
                    ShowGeneratePanel();
                    break;
            }

            if (_grassHolder.grassData != null)
            {
                EditorGUILayout.LabelField($"Grass On Scene:{_grassHolder.grassData.Count}",
                    EditorStyles.label);
                EditorGUILayout.LabelField($"Visible Grass On Scene:{_grassHolder.mapIdToDataList.Count}",
                    EditorStyles.label);
            }
            else
                EditorGUILayout.LabelField($"Grass On Scene: 0", EditorStyles.label);

            if (GUILayout.Button("Clear Grass"))
            {
                if (EditorUtility.DisplayDialog("Clear All Grass?",
                        "Are you sure you want to clear the grass?", "Clear", "Don't Clear"))
                    _grassHolder.Release();
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
            
            //toolSettings.hitMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            // LayerMask tempMask2 = EditorGUILayout.MaskField("Painting Mask",
            //     InternalEditorUtility.LayerMaskToConcatenatedLayersMask(toolSettings.paintMask),
            //     InternalEditorUtility.layers);
            // toolSettings.paintMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask2);
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Paint Status (Right-Mouse Button to paint)", EditorStyles.boldLabel);
            toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
            brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 50f);

            if (toolbarInt == 0)
            {
                normalLimit = EditorGUILayout.Slider("Normal Limit", normalLimit, 0, 1);
                // toolSettings.density = EditorGUILayout.Slider("Density", toolSettings.density, 0.1f, 10f);
            }

            // if (toolbarInt == 2)
            // {
            //     toolbarIntEdit = GUILayout.Toolbar(toolbarIntEdit, toolbarStringsEdit);
            //     EditorGUILayout.Separator();
            //
            //     EditorGUILayout.LabelField("Soft Falloff Settings", EditorStyles.boldLabel);
            //     toolSettings.brushFalloffSize =
            //         EditorGUILayout.Slider("Brush Falloff Size", toolSettings.brushFalloffSize, 0.01f, 1f);
            //     toolSettings.Flow = EditorGUILayout.Slider("Brush Flow", toolSettings.Flow, 0.1f, 10f);
            //     EditorGUILayout.Separator();
            //     EditorGUILayout.LabelField("Adjust Width and Length Gradually", EditorStyles.boldLabel);
            //     toolSettings.adjustWidth =
            //         EditorGUILayout.Slider("Grass Width Adjustment", toolSettings.adjustWidth, -1f, 1f);
            //     toolSettings.adjustLength =
            //         EditorGUILayout.Slider("Grass Length Adjustment", toolSettings.adjustLength, -1f, 1f);
            //
            //     toolSettings.adjustWidthMax = EditorGUILayout.Slider("Grass Width Adjustment Max Clamp",
            //         toolSettings.adjustWidthMax, 0.01f, 3f);
            //     toolSettings.adjustHeightMax = EditorGUILayout.Slider("Grass Length Adjustment Max Clamp",
            //         toolSettings.adjustHeightMax, 0.01f, 3f);
            //     EditorGUILayout.Separator();
            // }

            if (toolbarInt == 0 || toolbarInt == 2)
            {
                EditorGUILayout.Separator();

                // if (toolbarInt == 0)
                // {
                //     EditorGUILayout.LabelField("Width and Length ", EditorStyles.boldLabel);
                //     toolSettings.sizeWidth = EditorGUILayout.Slider("Grass Width", toolSettings.sizeWidth, 0.01f, 2f);
                //     toolSettings.sizeLength =
                //         EditorGUILayout.Slider("Grass Length", toolSettings.sizeLength, 0.01f, 2f);
                // }


                // EditorGUILayout.Separator();
                // EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
                // toolSettings.AdjustedColor = EditorGUILayout.ColorField("Brush Color", toolSettings.AdjustedColor);
                // EditorGUILayout.LabelField("Random Color Variation", EditorStyles.boldLabel);
                // toolSettings.rangeR = EditorGUILayout.Slider("Red", toolSettings.rangeR, 0f, 1f);
                // toolSettings.rangeG = EditorGUILayout.Slider("Green", toolSettings.rangeG, 0f, 1f);
                // toolSettings.rangeB = EditorGUILayout.Slider("Blue", toolSettings.rangeB, 0f, 1f);
            }

            // if (toolbarInt == 3)
            // {
            //     EditorGUILayout.Separator();
            //     EditorGUILayout.BeginHorizontal();
            //     EditorGUILayout.LabelField("Reprojection Y Offset", EditorStyles.boldLabel);
            //
            //     toolSettings.reprojectOffset = EditorGUILayout.FloatField(toolSettings.reprojectOffset);
            //     EditorGUILayout.EndHorizontal();
            // }

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
                _grassHolder = grassHolderObject.GetComponent<GrassHolder>();

                if (_grassHolder is null)
                    EditorGUILayout.LabelField(
                        "One of necessary component are missing(GrassHolder). Creating grass is impossible");
                else
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
                        }
                    }


                    if (GUILayout.Button("Save Positions"))
                        DataManager.TrySaveGrassData("Assets/_ Grass/Grass Data/data.bin", _grassHolder.grassData);

                    if (GUILayout.Button("Load Positions"))
                    {
                        DataManager.TryLoadGrassData("Assets/_ Grass/Grass Data/data.bin", out var grassData);
                        _grassHolder.grassData = grassData;
                        _grassHolder.OnEnable();
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
            _grassHolder = grassHolderObject.AddComponent<GrassHolder>();
        }
    }
}