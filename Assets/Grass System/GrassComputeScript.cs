using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class GrassComputeScript : MonoBehaviour
{
    // very slow, but will update always
    public bool autoUpdate;

    // main camera
    private Camera _mainCamera;

    // grass settings to send to the compute shader
    public SO_GrassSettings currentPresets;

    // interactors
    ShaderInteractor[] _interactors;

    // base data lists
    [SerializeField, HideInInspector]
    List<GrassData> grassData = new List<GrassData>();

    // list of all visible grass ids, rest are culled
    List<int> _grassVisibleIDs = new List<int>();

    // A state variable to help keep track of whether compute buffers have been set up
    private bool _initialized;
    // A compute buffer to hold vertex data of the source mesh
    private ComputeBuffer _sourceVertBuffer;
    // A compute buffer to hold vertex data of the generated mesh
    private ComputeBuffer _drawBuffer;
    // A compute buffer to hold indirect draw arguments
    private ComputeBuffer _argsBuffer;
    // Instantiate the shaders so data belong to their unique compute buffers
    private ComputeShader _instantiatedComputeShader;
    // buffer that contains the ids of all visible instances
    private ComputeBuffer _visibleIDBuffer;
    [FormerlySerializedAs("m_InstantiatedMaterial")] [SerializeField] Material instantiatedMaterial;
    // The id of the kernel in the grass compute shader
    private int _idGrassKernel;
    // The x dispatch size for the grass compute shader
    private int _dispatchSize;
    // compute shader thread group size
    uint threadGroupSize;

    // The size of one entry in the various compute buffers, size comes from the float3/float2 entrees in the shader
    private const int SOURCE_VERT_STRIDE = sizeof(float) * (3 + 3 + 2 + 3);
    private const int DRAW_STRIDE = sizeof(float) * (3 + 3 + ((3 + 2) * 3));

    // bounds of the total grass 
    Bounds bounds;


    private uint[] _argsBufferReset = new uint[5]
   {
        0,  // Number of vertices to render (Calculated in the compute shader with "InterlockedAdd(_IndirectArgsBuffer[0].numVertices);")
        1,  // Number of instances to render (should only be 1 instance since it should produce a single mesh)
        0,  // Index of the first vertex to render
        0,  // Index of the first instance to render
        0   // Not used
   };

    // culling tree data ----------------------------------------------------------------------
    CullingTreeNode cullingTree;
    List<Bounds> BoundsListVis = new List<Bounds>();
    List<CullingTreeNode> leaves = new List<CullingTreeNode>();
    Plane[] cameraFrustumPlanes = new Plane[6];
    float cameraOriginalFarPlane;

    // list of -1 to overwrite the grassvisible buffer with
    List<int> empty = new List<int>();

    // speeding up the editor a bit
    Vector3 m_cachedCamPos;
    Quaternion m_cachedCamRot;
    bool m_fastMode;
    int shaderID;

    // max buffer size can depend on platform and your draw stride, you may have to change it
    int maxBufferSize = 2500000;

    ///-------------------------------------------------------------------------------------

    public List<GrassData> SetGrassPaintedDataList
    {
        get { return grassData; }
        set { grassData = value; }
    }

#if UNITY_EDITOR
    SceneView view;

    void OnDestroy()
    {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.duringSceneGui -= this.OnScene;
    }

    void OnScene(SceneView scene)
    {
        view = scene;
        if (!Application.isPlaying)
        {
            if (view.camera != null)
            {
                _mainCamera = view.camera;
            }
        }
        else
        {
            _mainCamera = Camera.main;
        }
    }

    private void OnValidate()
    {
        // Set up components
        if (!Application.isPlaying)
        {
            if (view != null)
            {
                _mainCamera = view.camera;
            }
        }
        else
        {
            _mainCamera = Camera.main;
        }
    }
#endif



    private void OnEnable()
    {
        // If initialized, call on disable to clean things up
        if (_initialized)
        {
            OnDisable();
        }

        MainSetup(true);
    }

    void MainSetup(bool full)
    {
#if UNITY_EDITOR
        SceneView.duringSceneGui += OnScene;
        if (!Application.isPlaying)
            if (view != null)
                _mainCamera = view.camera;
#endif
        if (Application.isPlaying)
            _mainCamera = Camera.main;
        
        // Don't do anything if resources are not found,
        // or no vertex is put on the mesh.
        if (grassData.Count == 0)
            return;

        if (currentPresets.shaderToUse == null || currentPresets.materialToUse == null)
        {
            Debug.LogWarning("Missing Compute Shader/Material in grass Settings", this);
            return;
        }

        // empty array to replace the visible grass with
        PopulateEmptyList(grassData.Count);
        _initialized = true;

        // Instantiate the shaders so they can point to their own buffers
        _instantiatedComputeShader = Instantiate(currentPresets.shaderToUse);
        instantiatedMaterial = Instantiate(currentPresets.materialToUse);

        int numSourceVertices = grassData.Count;

        // amount of segmets
        int maxBladesPerVertex = Mathf.Max(1, currentPresets.allowedBladesPerVertex);
        int maxSegmentsPerBlade = Mathf.Max(1, currentPresets.allowedSegmentsPerBlade);
        // -1 is because the top part of the grass only has 1 triangle
        int maxBladeTriangles = maxBladesPerVertex * ((maxSegmentsPerBlade - 1) * 2 + 1);

        // Create compute buffers
        // The stride is the size, in bytes, each object in the buffer takes up
        _sourceVertBuffer = new ComputeBuffer(numSourceVertices, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        _sourceVertBuffer.SetData(grassData);


        _drawBuffer = new ComputeBuffer(maxBufferSize, DRAW_STRIDE, ComputeBufferType.Append);

        _argsBuffer = new ComputeBuffer(1, _argsBufferReset.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        _visibleIDBuffer = new ComputeBuffer(grassData.Count, sizeof(int), ComputeBufferType.Structured); //uint only, per visible grass

        // Cache the kernel IDs we will be dispatching
        _idGrassKernel = _instantiatedComputeShader.FindKernel("Main");

        // Set buffer data
        _instantiatedComputeShader.SetBuffer(_idGrassKernel, "_SourceVertices",
            _sourceVertBuffer);
        _instantiatedComputeShader.SetBuffer(_idGrassKernel, "_DrawTriangles", _drawBuffer);
        _instantiatedComputeShader.SetBuffer(_idGrassKernel, "_IndirectArgsBuffer", _argsBuffer);
        _instantiatedComputeShader.SetBuffer(_idGrassKernel, "_VisibleIDBuffer", _visibleIDBuffer);
        instantiatedMaterial.SetBuffer("_DrawTriangles", _drawBuffer);
        // Set vertex data
        _instantiatedComputeShader.SetInt("_NumSourceVertices", numSourceVertices);
        // cache shader property to int id for interactivity;
        shaderID = Shader.PropertyToID("_PositionsMoving");

        // Calculate the number of threads to use. Get the thread size from the kernel
        // Then, divide the number of triangles by that size
        _instantiatedComputeShader.GetKernelThreadGroupSizes(_idGrassKernel,
            out threadGroupSize, out _, out _);
        //set once only
        _dispatchSize = Mathf.CeilToInt(grassData.Count / threadGroupSize);
        SetGrassDataBase(full);

        if (full)
        {

            UpdateBounds();

        }
        SetupQuadTree(full);
    }

    void UpdateBounds()
    {
        // Get the bounds of all the grass points and then expand
        bounds = new Bounds(grassData[0].position, Vector3.one);

        for (int i = 0; i < grassData.Count; i++)
        {
            Vector3 target = grassData[i].position;

            bounds.Encapsulate(target);
        }
    }

    void SetupQuadTree(bool full)
    {
        if (full)
        {

            cullingTree = new CullingTreeNode(bounds, currentPresets.cullingTreeDepth);

            cullingTree.RetrieveAllLeaves(leaves);
            //add the id of each grass point into the right cullingtree
            for (int i = 0; i < grassData.Count; i++)
            {
                cullingTree.FindLeaf(grassData[i].position, i);
            }
            cullingTree.ClearEmpty();
        }
        else
        {
            // just make everything visible while editing grass
            GrassFastList(grassData.Count);
            _visibleIDBuffer.SetData(_grassVisibleIDs);
        }
    }

    void GrassFastList(int count)
    {
        _grassVisibleIDs = Enumerable.Range(0, count).ToArray().ToList();
    }

    void PopulateEmptyList(int count)
    {
        empty = new List<int>(count);
        empty.InsertRange(0, Enumerable.Repeat(-1, count));
    }

    void GetFrustumData()
    {
        if (_mainCamera == null)
        {
            return;
        }
        // if the camera didnt move, we dont need to change the culling;
        if (m_cachedCamRot == _mainCamera.transform.rotation && m_cachedCamPos == _mainCamera.transform.position && Application.isPlaying)
        {
            return;
        }
        // get frustum data from the main camera
        cameraOriginalFarPlane = _mainCamera.farClipPlane;
        _mainCamera.farClipPlane = currentPresets.maxDrawDistance;//allow drawDistance control    
        GeometryUtility.CalculateFrustumPlanes(_mainCamera, cameraFrustumPlanes);
        _mainCamera.farClipPlane = cameraOriginalFarPlane;//revert far plane edit

        if (!m_fastMode)
        {
            BoundsListVis.Clear();
            _visibleIDBuffer.SetData(empty);
            _grassVisibleIDs.Clear();
            cullingTree.RetrieveLeaves(cameraFrustumPlanes, BoundsListVis, _grassVisibleIDs);
            _visibleIDBuffer.SetData(_grassVisibleIDs);
        }

        // cache camera position to skip culling when not moved
        m_cachedCamPos = _mainCamera.transform.position;
        m_cachedCamRot = _mainCamera.transform.rotation;
    }

    private void OnDisable()
    {
        // Dispose of buffers and copied shaders here
        if (_initialized)
        {
            // If the application is not in play mode, we have to call DestroyImmediate
            if (Application.isPlaying)
            {
                Destroy(_instantiatedComputeShader);
                Destroy(instantiatedMaterial);
            }
            else
            {
                DestroyImmediate(_instantiatedComputeShader);
                DestroyImmediate(instantiatedMaterial);
            }
            // Release each buffer
            _sourceVertBuffer?.Release();
            _drawBuffer?.Release();
            _argsBuffer?.Release();
            _visibleIDBuffer?.Release();
        }
        _initialized = false;
    }

    // LateUpdate is called after all Update calls
    private void Update()
    {
        // If in edit mode, we need to update the shaders each Update to make sure settings changes are applied
        // Don't worry, in edit mode, Update isn't called each frame
        if (!Application.isPlaying && autoUpdate && !m_fastMode)
        {
            OnDisable();
            OnEnable();
        }

        // If not initialized, do nothing (creating zero-length buffer will crash)
        if (!_initialized)
        {
            // Initialization is not done, please check if there are null components
            // or just because there is not vertex being painted.
            return;
        }
        // get the data from the camera for culling
        GetFrustumData();
        // Update the shader with frame specific data
        SetGrassDataUpdate();
        // Clear the draw and indirect args buffers of last frame's data
        _drawBuffer.SetCounterValue(0);
        _argsBuffer.SetData(_argsBufferReset);
        _dispatchSize = Mathf.CeilToInt(_grassVisibleIDs.Count / threadGroupSize);
        if (_grassVisibleIDs.Count > 0)
        {
            // make sure the compute shader is dispatched even when theres very little grass
            _dispatchSize += 1;
        }
        if (_dispatchSize > 0)
        {
            // Dispatch the grass shader. It will run on the GPU
            _instantiatedComputeShader.Dispatch(_idGrassKernel, _dispatchSize, 1, 1);
            // DrawProceduralIndirect queues a draw call up for our generated mesh
            Graphics.DrawProceduralIndirect(instantiatedMaterial, bounds, MeshTopology.Triangles,
            _argsBuffer, 0, null, null, currentPresets.castShadow, true, gameObject.layer);
        }
    }

    private void SetGrassDataBase(bool full)
    {
        // Send things to compute shader that dont need to be set every frame
        _instantiatedComputeShader.SetFloat("_Time", Time.time);
        _instantiatedComputeShader.SetFloat("_GrassRandomHeightMin", currentPresets.grassRandomHeightMin);
        _instantiatedComputeShader.SetFloat("_GrassRandomHeightMax", currentPresets.grassRandomHeightMax);
        _instantiatedComputeShader.SetFloat("_WindSpeed", currentPresets.windSpeed);
        _instantiatedComputeShader.SetFloat("_WindStrength", currentPresets.windStrength);


        if (full)
        {
            _instantiatedComputeShader.SetFloat("_MinFadeDist", currentPresets.minFadeDistance);
            _instantiatedComputeShader.SetFloat("_MaxFadeDist", currentPresets.maxDrawDistance);
            _interactors = (ShaderInteractor[])FindObjectsOfType(typeof(ShaderInteractor));
        }
        else
        {
            // if theres a lot of grass, just cull earlier so we can still see what we're paiting, otherwise it will be invisible
            if (grassData.Count > 200000)
            {
                _instantiatedComputeShader.SetFloat("_MinFadeDist", 40f);
                _instantiatedComputeShader.SetFloat("_MaxFadeDist", 50f);
            }
            else
            {
                _instantiatedComputeShader.SetFloat("_MinFadeDist", currentPresets.minFadeDistance);
                _instantiatedComputeShader.SetFloat("_MaxFadeDist", currentPresets.maxDrawDistance);
            }

        }
        _instantiatedComputeShader.SetFloat("_InteractorStrength", currentPresets.affectStrength);
        _instantiatedComputeShader.SetFloat("_BladeRadius", currentPresets.bladeRadius);
        _instantiatedComputeShader.SetFloat("_BladeForward", currentPresets.bladeForwardAmount);
        _instantiatedComputeShader.SetFloat("_BladeCurve", Mathf.Max(0, currentPresets.bladeCurveAmount));
        _instantiatedComputeShader.SetFloat("_BottomWidth", currentPresets.bottomWidth);



        _instantiatedComputeShader.SetInt("_MaxBladesPerVertex", currentPresets.allowedBladesPerVertex);
        _instantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", currentPresets.allowedSegmentsPerBlade);

        _instantiatedComputeShader.SetFloat("_MinHeight", currentPresets.MinHeight);
        _instantiatedComputeShader.SetFloat("_MinWidth", currentPresets.MinWidth);

        _instantiatedComputeShader.SetFloat("_MaxHeight", currentPresets.MaxHeight);
        _instantiatedComputeShader.SetFloat("_MaxWidth", currentPresets.MaxWidth);
        instantiatedMaterial.SetColor("_TopTint", currentPresets.topTint);
        instantiatedMaterial.SetColor("_BottomTint", currentPresets.bottomTint);
    }

    public void Reset()
    {
        m_fastMode = false;
        OnDisable();
        MainSetup(true);
    }

    public void ResetFaster()
    {
        m_fastMode = true;
        OnDisable();
        MainSetup(false);
    }
    private void SetGrassDataUpdate()
    {
        // variables sent to the shader every frame
        _instantiatedComputeShader.SetFloat("_Time", Time.time);
        _instantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        if (_interactors.Length > 0)
        {
            Vector4[] positions = new Vector4[_interactors.Length];

            for (int i = 0; i < _interactors.Length; i++)
            {
                positions[i] = new Vector4(_interactors[i].transform.position.x, _interactors[i].transform.position.y, _interactors[i].transform.position.z,
                _interactors[i].radius);

            }
            _instantiatedComputeShader.SetVectorArray(shaderID, positions);
            _instantiatedComputeShader.SetFloat("_InteractorsLength", _interactors.Length);
        }
        if (_mainCamera != null)
        {
            _instantiatedComputeShader.SetVector("_CameraPositionWS", _mainCamera.transform.position);
        }
#if UNITY_EDITOR
        // if we dont have a main camera (it gets added during gameplay), use the scene camera
        else if (view != null)
        {
            _instantiatedComputeShader.SetVector("_CameraPositionWS", view.camera.transform.position);
        }
#endif
    }


    // draw the bounds gizmos
    void OnDrawGizmos()
    {
        if (currentPresets)
        {
            if (currentPresets.drawBounds)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                for (int i = 0; i < BoundsListVis.Count; i++)
                {
                    Gizmos.DrawWireCube(BoundsListVis[i].center, BoundsListVis[i].size);
                }
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

    }
}

[System.Serializable]
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind
       .Sequential)]
public struct GrassData
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 length;
    public Vector3 color;
}
