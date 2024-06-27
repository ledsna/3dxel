using UnityEngine;


public class CubeRotation : MonoBehaviour
{
    public float speed = 50.0f; // Speed of rotation

    void Update()
    {
        // Rotate the cube around an axis at speed degrees per second.
        transform.Rotate(new Vector3(1, 1, 1) * speed * Time.deltaTime);
    }
}