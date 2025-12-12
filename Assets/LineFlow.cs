using UnityEngine;

public class LineFlow : MonoBehaviour
{
    public float scrollSpeed = 2.0f;
    private LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (lr != null)
        {
            // Animate the texture offset to make it look like it's moving
            Material mat = lr.material;
            Vector2 offset = mat.mainTextureOffset;
            offset.x -= Time.deltaTime * scrollSpeed; // Negative moves tail to head
            mat.mainTextureOffset = offset;
        }
    }
}