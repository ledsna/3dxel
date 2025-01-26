using UnityEngine;

public class AlignToOrthographicCamera : MonoBehaviour
{
    
    [SerializeField] private Camera orthoCam;
    private Camera perspCam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        perspCam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        var distance = Vector3.Distance(orthoCam.transform.position +
                                             orthoCam.transform.forward * orthoCam.farClipPlane, 
            perspCam.transform.position);
        
        var horFOV = 2 * Mathf.Rad2Deg * Mathf.Atan(orthoCam.orthographicSize / distance);


        perspCam.nearClipPlane = distance;
        perspCam.fieldOfView = horFOV;
    }
}
