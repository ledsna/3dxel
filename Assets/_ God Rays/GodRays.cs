using System;
using UnityEngine;

public class GodRays : MonoBehaviour
{
    [SerializeField, Range(0, 64)] public int PlaneCount;
    [SerializeField] public Camera MainCamera;
    [SerializeField] public Mesh QuadMesh;
    [SerializeField] public Material QuadMaterial;
    [SerializeField] public Light DirectionalLight;
    
    private float[] distances;
    private int godRaysLayer;
    
    public void Start()
    {
        CalcilateDistances();
    }

    public void OnValidate()
    {
        CalcilateDistances();
    }

    public void Update()
    {
        if (distances == null)
            return;
        
        for (int i = 0; i < PlaneCount; i++)
        {
            var wordPos = MainCamera.transform.TransformPoint(new Vector3(0, 0, distances[i]));
            var rot = MainCamera.transform.rotation;

            var height = MainCamera.orthographicSize * 2;
            var width = height * MainCamera.aspect;
            
            var matrix = Matrix4x4.TRS(wordPos, rot, new Vector3(width, height, 1f));
            Graphics.DrawMesh(QuadMesh, matrix, QuadMaterial, godRaysLayer);
        }
    }

    private void CalcilateDistances()
    {
        // TODO: Remove hard-code
        godRaysLayer = LayerMask.NameToLayer("GodRays");
        
        var dt = (MainCamera.farClipPlane) / PlaneCount;

        if (dt <= 0)
        {
            Debug.LogError("Offset too big");
            return;
        }
        
        // distances = new float[PlaneCount];
        // for (int i = 0; i < PlaneCount; i++)
        //     distances[i] = ForwardOffset + dt * i;
    }
}
