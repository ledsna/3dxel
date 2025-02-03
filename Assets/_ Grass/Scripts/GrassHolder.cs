using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct GrassData {
	public Vector3 position;
	public Vector3 normal;
	public Vector2 lightmapUV;
}

[ExecuteAlways]
public class GrassHolder : MonoBehaviour
{
	[NonSerialized] public List<GrassData> grassData = new();
	[HideInInspector] public Material _rootMeshMaterial;

	// Lightmapping
	[HideInInspector] public int lightmapIndex;

	// Properties
	[SerializeField] private Material instanceMaterial;
	[SerializeField] private Mesh mesh;
	
	[SerializeField] private bool drawBounds;
	[SerializeField, Min(0f)] private float maxDrawDistance = 50;
	[SerializeField, Range(1,6)] private int depthCullingTree = 3;
	// [Separator(1, 10)]
	[BinFile] public string sourceFile;
	
	// Material of the surface on which the grass is being instanced

	// Buffers And GPU Instance Components
	private ComputeBuffer _sourcePositionGrass;
	private GraphicsBuffer _graphicsBuffer;
	private GraphicsBuffer.IndirectDrawIndexedArgs[] _bufferData;
	private RenderParams _renderParams;
	private MaterialPropertyBlock _materialPropertyBlock;

	// Precomputed Rotation * Scale Matrix 
	private Matrix4x4 _rotationScaleMatrix;
	
	// Main Camera
	private Camera _mainCamera;

	// Stride For Grass Data Buffer
	private const int GrassDataStride = sizeof(float) * (3 + 3 + 2);

	// Initialized State
	private bool _initialized;

	// Grass Culling Tree
	// ------------------
	private GrassCullingTree cullingTree;
	[NonSerialized] public List<int> mapIdToDataList = new List<int>();
	private ComputeBuffer mapIdToDataBuffer;
	Plane[] cameraFrustumPlanes = new Plane[6];
	float cameraOriginalFarPlane;
	Vector3 cachedCamPos;
	Quaternion cachedCamRot;

	// list of -1 to overwrite the grassvisible buffer with
	readonly List<int> empty = new();

	private int maxBufferSize = 2500000;
	// ------------------

	#region Setup and Rendering

	private void Setup() {
		#if UNITY_EDITOR
		SceneView.duringSceneGui += OnScene;
		if (!Application.isPlaying) {
			if (_view is not null) {
				_mainCamera = _view.camera;
			}
		}
		#endif
		
		if (Application.isPlaying) {
			_mainCamera = Camera.main;
		}

		if (grassData.Count == 0) {
			return;
		}

		// Init Buffers
		// Source Buffer
		_sourcePositionGrass = new ComputeBuffer(maxBufferSize, GrassDataStride,
			ComputeBufferType.Structured,
			ComputeBufferMode.Immutable); // Experimental. Possible should change to Dynamic
		_sourcePositionGrass.SetData(grassData);

		// Init other variables
		_materialPropertyBlock = new MaterialPropertyBlock();
		_materialPropertyBlock.SetBuffer("_SourcePositionGrass", _sourcePositionGrass);

		instanceMaterial.CopyMatchingPropertiesFromMaterial(_rootMeshMaterial);
		instanceMaterial.EnableKeyword("_ALPHATEST_ON");

		// grassBounds = GetGrassBound();

		if (lightmapIndex >= 0 && LightmapSettings.lightmaps.Length > 0)
		{
			instanceMaterial.EnableKeyword("LIGHTMAP_ON");
			if (LightmapSettings.lightmapsMode == LightmapsMode.CombinedDirectional)
				instanceMaterial.EnableKeyword("DIRLIGHTMAP_COMBINED");
			// if (QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask)
			instanceMaterial.EnableKeyword("MAIN_LIGHT_CALCULATE_SHADOWS");
			// instanceMaterial.EnableKeyword("SHADOWS_SHADOWMASK");
		}


		_renderParams = new RenderParams(instanceMaterial)
		{
			layer = gameObject.layer,
			worldBounds = new Bounds(Vector3.zero, Vector3.one*100),
			matProps = _materialPropertyBlock
		};
		_rotationScaleMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

		Shader.SetGlobalFloat("_Scale", instanceMaterial.GetFloat("_Scale"));

		_initialized = true;
	}

	[ExecuteAlways]
	private void Update() {
		if (!_initialized)
			return;

		UpdateRotationScaleMatrix(instanceMaterial.GetFloat("_Scale"));
		instanceMaterial.SetMatrix("m_RS", _rotationScaleMatrix);
		
		PrepareCommandBuffer();
		if (_graphicsBuffer == null)
			return;
		Graphics.RenderMeshIndirect(_renderParams, mesh, _graphicsBuffer, _graphicsBuffer.count);
	}

	private void PrepareCommandBuffer()
    {
        if (_mainCamera == null)
            return;
        
        // if the camera didnt move, we dont need to change the culling;
        if (cachedCamRot == _mainCamera.transform.rotation && cachedCamPos == _mainCamera.transform.position &&
            Application.isPlaying) {
            return;
        }

        
        _graphicsBuffer?.Release();
        _graphicsBuffer = null;
        // Octree culling work only in build, but this behaviour can be changed
        if (Application.isPlaying)
        {
            // if (UseOctreeCulling)
            // {
            //     
            //     var buffers = new List<GraphicsBuffer.IndirectDrawIndexedArgs>();
            //     GeometryUtility.CalculateFrustumPlanes(_mainCamera, cameraFrustumPlanes);
            //     foreach (var chunkIndex in cullingTree.GetVisibleChunkIndices(cameraFrustumPlanes))
            //     {
            //         var buffer = new GraphicsBuffer.IndirectDrawIndexedArgs();
            //         buffer.instanceCount = cullingTree.Chunks[chunkIndex].InstanceCount;
            //         buffer.startInstance = cullingTree.Chunks[chunkIndex].StartInstance;
            //         buffer.indexCountPerInstance = 6;
            //         buffers.Add(buffer);
            //     }
            //     if (buffers.Count == 0)
            //         return;
            //     
            //     _commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, buffers.Count,
            //         GraphicsBuffer.IndirectDrawIndexedArgs.size);
            //     _commandBuffer.SetData(buffers.ToArray());
            //     return;
            // }
        }
        _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1,
            GraphicsBuffer.IndirectDrawIndexedArgs.size);
        _bufferData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        _bufferData[0].indexCountPerInstance = 6;
        _bufferData[0].instanceCount = (uint)grassData.Count;
        _graphicsBuffer.SetData(_bufferData);
        
        // cache camera position to skip culling when not moved
        cachedCamPos = _mainCamera.transform.position;
        cachedCamRot = _mainCamera.transform.rotation;
    }
	
	#endregion
	
	private void CreateGrassCullingTree(int depth = 3, float extrude = 0.5f) {
		if (cullingTree != null) {
			cullingTree.Release();
		}

		// Calculate bounds of all grass
		var mostLeftBottom = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		var mostRightTop = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		foreach (var data in grassData) {
			var position = data.position;
			mostLeftBottom.x = Mathf.Min(mostLeftBottom.x, position.x);
			mostLeftBottom.y = Mathf.Min(mostLeftBottom.y, position.y);
			mostLeftBottom.z = Mathf.Min(mostLeftBottom.z, position.z);

			mostRightTop.x = Mathf.Max(mostRightTop.x, position.x);
			mostRightTop.y = Mathf.Max(mostRightTop.y, position.y);
			mostRightTop.z = Mathf.Max(mostRightTop.z, position.z);
		}

		// Init culling tree
		cullingTree =
			new GrassCullingTree(
				new Bounds((mostLeftBottom + mostRightTop) / 2, mostRightTop - mostLeftBottom + Vector3.one * extrude),
				depth
			);

		// Assign every grass ID to properly leaf
		for (int i = 0; i < grassData.Count; i++) {
			cullingTree.FindLeaf(grassData[i].position, i);
		}

		// Optional: for better performance 
		cullingTree.RecalculateBoundsHeight(grassData);
	}

	void GetFrustumData() {
		if (_mainCamera is null) 
		{
			return;
		}

		// if the camera didn't move, we don't need to change the culling;
		if (cachedCamRot == _mainCamera.transform.rotation && cachedCamPos == _mainCamera.transform.position &&
		    Application.isPlaying) 
		{
			return;
		}

		// get frustum data from the main camera
		cameraOriginalFarPlane = _mainCamera.farClipPlane;
		_mainCamera.farClipPlane = maxDrawDistance;
		GeometryUtility.CalculateFrustumPlanes(_mainCamera, cameraFrustumPlanes);
		_mainCamera.farClipPlane = cameraOriginalFarPlane;
		
		mapIdToDataList.Clear();
		mapIdToDataBuffer.SetData(empty);
		cullingTree.RetrieveLeaves(cameraFrustumPlanes, mapIdToDataList);
		mapIdToDataBuffer.SetData(mapIdToDataList);
		
		// cache camera position to skip culling when not moved
		cachedCamPos = _mainCamera.transform.position;
		cachedCamRot = _mainCamera.transform.rotation;
	}

	#region F1Soda magic pls document

	public void Release() {
		OnDisable();
		grassData.Clear();
		mapIdToDataList.Clear();
	}

	private void UpdateRotationScaleMatrix(float scale) {
		if (_mainCamera is null || _mainCamera.transform.rotation == cachedCamRot) {
			return;
		}

		_rotationScaleMatrix.SetColumn(0, _mainCamera.transform.right * scale);
		_rotationScaleMatrix.SetColumn(1, _mainCamera.transform.up * scale);
		_rotationScaleMatrix.SetColumn(2, _mainCamera.transform.forward * scale);
	}

	#endregion

	#region Event Functions

	#if UNITY_EDITOR
	SceneView _view;

	void OnDestroy() {
		// When the window is destroyed, remove the delegate
		// so that it will no longer do any drawing.
		SceneView.duringSceneGui -= this.OnScene;
	}

	void OnScene(SceneView scene) {
		_view = scene;
		if (!Application.isPlaying) {
			if (_view.camera != null) {
				_mainCamera = _view.camera;
			}
		}
		else {
			_mainCamera = Camera.main;
		}
	}

	private void OnValidate() {
		if (!Application.isPlaying) {
			if (_view != null) {
				_mainCamera = _view.camera;
			}
		}
		else {
			_mainCamera = Camera.main;
		}
	}
	#endif
	public void OnEnable() {
		if (_initialized) {
			OnDisable();
		}

		Setup();
	}

	public void OnDisable() {
		if (_initialized) {
			_sourcePositionGrass?.Release();
			_graphicsBuffer?.Release();
			mapIdToDataBuffer?.Release();
			_materialPropertyBlock.Clear();
			mapIdToDataList.Clear();
			_bufferData = null;
			// cullingTree.Release();
			cullingTree = null;
		}

		_initialized = false;
	}

	// draw the bounds gizmos
	void OnDrawGizmos() {
		void RecursivelyDrawTreeBounds(GrassCullingTree tree, Color color) {
			foreach (var child in tree.children) {
				if (child.isDrawn) {
					RecursivelyDrawTreeBounds(child, color * 2);
					Gizmos.color = color;
					Gizmos.DrawWireCube(child.bounds.center, child.bounds.size);
				}
			}
		}

		if (drawBounds && cullingTree != null) {
			Gizmos.color = new Color(0.4f, 0.8f, 0.9f, 1f) / 4;
			Gizmos.DrawWireCube(cullingTree.bounds.center, cullingTree.bounds.size);
			RecursivelyDrawTreeBounds(cullingTree, Gizmos.color);
		}
	}

	#endregion
}