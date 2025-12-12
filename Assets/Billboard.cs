using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        // Rotate the text to look at the camera
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }
}