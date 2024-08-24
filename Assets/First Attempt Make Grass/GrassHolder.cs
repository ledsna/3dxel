using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct GrassData {
	public Vector3 position;
	public Vector3 normal;
	public Vector3 color;
}

[ExecuteAlways]
public class GrassHolder : MonoBehaviour {
	[HideInInspector] public List<GrassData> grassData = new();

	// Properties
	[SerializeField] private Material instanceMaterial;
	[SerializeField] private Mesh mesh;
	[SerializeField] private bool drawBounds;

	// Buffers And GPU Instance Componetns
	private ComputeBuffer _sourcePositionGrass;
	private GraphicsBuffer _commandBuffer;
	private GraphicsBuffer.IndirectDrawIndexedArgs[] _commandData;
	private RenderParams _renderParams;
	private MaterialPropertyBlock _materialPropertyBlock;

	// Precomputed Rotation * Scale Matrix 
	private Matrix4x4 _rotationScaleMatrix;

	// Main Camera
	private Camera _mainCamera;

	// Stride For Grass Data Buffer
	private const int GrassDataStride = sizeof(float) * (3 + 3 + 3);

	// Bounds for culling
	private Bounds _bounds;

	// Initialized State
	private bool _initialized;


	#region Main Logic

	private void Setup() {
		#if UNITY_EDITOR
		SceneView.duringSceneGui += this.OnScene;
		if (!Application.isPlaying) {
			if (_view != null) {
				_mainCamera = _view.camera;
			}
		}
		#endif

		if (grassData.Count == 0) {
			return;
		}

		// Init Buffers
		// ------------
		// Source Buffer
		_sourcePositionGrass = new ComputeBuffer(grassData.Count, GrassDataStride,
		                                         ComputeBufferType.Structured,
		                                         ComputeBufferMode.Dynamic);
		_sourcePositionGrass.SetData(grassData);

		// Command Buffer
		_commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1,
		                                    GraphicsBuffer.IndirectDrawIndexedArgs.size);
		// Length of this array mean count of render call. We render all grass by one call, so length is 1
		_commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
		_commandData[0].indexCountPerInstance = 6;
		_commandData[0].instanceCount = (uint)grassData.Count;
		_commandBuffer.SetData(_commandData);
		// ------------

		// Init other variables
		// --------------------
		_materialPropertyBlock = new MaterialPropertyBlock();
		_materialPropertyBlock.SetBuffer("_SourcePositionGrass", _sourcePositionGrass);

		UpdateBounds();

		_renderParams = new RenderParams(instanceMaterial) {
			layer = gameObject.layer,
			worldBounds = _bounds,
			matProps = _materialPropertyBlock
		};
		_rotationScaleMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));


		_initialized = true;
		// --------------------
	}

	[ExecuteAlways]
	private void Update() {
		if (!_initialized)
			return;

		UpdateRotationScaleMatrix(instanceMaterial.GetFloat("_Size"));
		instanceMaterial.SetMatrix("m_RS", _rotationScaleMatrix);

		Graphics.RenderMeshIndirect(_renderParams, mesh, _commandBuffer);
	}

	#endregion

	#region Minor Logic

	public void UpdateBuffers() {
		if (_initialized)
			OnDisable();
		Setup();
	}

	public void Release() {
		OnDisable();
		grassData.Clear();
	}

	// This can be optimized! Call this function only when camera rotates in game. 
	// But in edit mode update each frame
	private void UpdateRotationScaleMatrix(float scale) {
		if (_mainCamera is null) {
			return;
		}

		_rotationScaleMatrix.SetColumn(0, _mainCamera.transform.right * scale);
		_rotationScaleMatrix.SetColumn(1, _mainCamera.transform.up * scale);
		_rotationScaleMatrix.SetColumn(2, _mainCamera.transform.forward * scale);
	}

	void UpdateBounds() {
		// Get the bounds of all the grass points and then expand
		_bounds = new Bounds(grassData[0].position, Vector3.one);

		for (int i = 0; i < grassData.Count; i++) {
			_bounds.Encapsulate(grassData[i].position);
		}
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
	private void OnEnable() {
		if (_initialized) {
			OnDisable();
		}

		Setup();
	}

	private void OnDisable() {
		if (_initialized) {
			_sourcePositionGrass?.Release();
			_commandBuffer?.Release();
			_commandData = null;
			_bounds = default;
		}

		_initialized = false;
	}

	// draw the bounds gizmos
	void OnDrawGizmos() {
		if (drawBounds) {
			Gizmos.color = new Color(1, 0, 0, 0.3f);
			Gizmos.DrawWireCube(_bounds.center, _bounds.size);
		}
	}

	#endregion
}