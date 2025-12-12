using UnityEngine;

public class ArrowMover : MonoBehaviour
{
    public Vector3 startPos;
    public Vector3 endPos;
    public float duration = 1.5f; // Seconds to travel one way

    private float timer = 0f;

    void Update()
    {
        // Increment timer
        timer += Time.deltaTime;

        // Calculate 't' (0.0 to 1.0) based on time
        float t = (timer % duration) / duration;

        // Move the object
        transform.position = Vector3.Lerp(startPos, endPos, t);
    }
}