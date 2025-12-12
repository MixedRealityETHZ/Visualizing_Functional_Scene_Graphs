using UnityEngine;
using System.Collections.Generic;

// Attach this script to the 'TestRaycaster' GameObject
public class CustomRaycaster : MonoBehaviour
{
    [Tooltip("The LayerMask to hit. Should be set to 'SceneGraphNode'.")]
    public LayerMask hitLayers;
    [Tooltip("How far the ray extends.")]
    public float maxDistance = 10f;
    
    private LineRenderer lineRenderer;
    private SceneGraphNode currentHitNode = null;

    void Start()
    {
        // Get the Line Renderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("CustomRaycaster requires a Line Renderer component.");
            enabled = false;
        }
    }

    void Update()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;
        
        // 1. Perform Raycast and Hit Detection
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance, hitLayers))
        {
            // Ray Hit Something
            SceneGraphNode hitNode = hit.collider.GetComponent<SceneGraphNode>();

            // 2. Update the Visual Line (Line ends at the hit point)
            lineRenderer.SetPosition(0, rayOrigin);
            lineRenderer.SetPosition(1, hit.point);
            
            // 3. Trigger Enter/Exit methods
            if (hitNode != null)
            {
                if (hitNode != currentHitNode)
                {
                    if (currentHitNode != null) currentHitNode.OnRayExit();
                    
                    currentHitNode = hitNode;
                    currentHitNode.OnRayEnter(); // Trigger the show connections logic!
                }
            }
            else
            {
                // Hit a non-SceneGraphNode object (e.g., the scene OBJ mesh)
                if (currentHitNode != null)
                {
                    currentHitNode.OnRayExit();
                    currentHitNode = null;
                }
            }
        }
        else
        {
            // Ray Hit Nothing
            
            // 2. Update the Visual Line (Line extends to max distance)
            lineRenderer.SetPosition(0, rayOrigin);
            lineRenderer.SetPosition(1, rayOrigin + rayDirection * maxDistance);

            // 3. Trigger Exit method
            if (currentHitNode != null)
            {
                currentHitNode.OnRayExit(); // Trigger the hide connections logic!
                currentHitNode = null;
            }
        }
    }
}