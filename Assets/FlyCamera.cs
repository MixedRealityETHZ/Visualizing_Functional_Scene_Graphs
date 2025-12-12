using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float moveSpeed = 10.0f;
    public float lookSpeed = 2.0f;
    public float sprintMultiplier = 2.0f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    void Start()
    {
        // Initialize rotation to current camera rotation
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        // --- LOOK AROUND (Right Click) ---
        if (Input.GetMouseButton(1)) // 0=Left, 1=Right, 2=Middle
        {
            yaw += Input.GetAxis("Mouse X") * lookSpeed;
            pitch -= Input.GetAxis("Mouse Y") * lookSpeed;

            // Clamp pitch so you can't look too far up/down
            // pitch = Mathf.Clamp(pitch, -90f, 90f); 

            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

        // --- MOVEMENT (WASD + Q/E) ---
        float speed = moveSpeed;
        
        // Hold Left Shift to run
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed *= sprintMultiplier;
        }

        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        
        // Up and Down (E and Q)
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        transform.position += move * speed * Time.deltaTime;
    }
}