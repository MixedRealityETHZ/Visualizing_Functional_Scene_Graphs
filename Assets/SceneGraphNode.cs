using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SceneGraphNode))]
public class SceneGraphNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SceneGraphNode sceneNode = (SceneGraphNode)target;
        if (GUILayout.Button("Ray Enter"))
        {
            sceneNode.OnRayEnter();
        }

        if (GUILayout.Button("Ray Exit"))
        {
            sceneNode.OnRayExit();
        }
    }
}
#endif

// This component is attached to the main GameObject of each bounding box (Node)
public class SceneGraphNode : MonoBehaviour
{
    // The ID of the node from the JSON data
    public int nodeId; 
    
    // A list of all outgoing connection GameObjects (the line + the arrow)
    public List<GameObject> outgoingEdges = new List<GameObject>(); 

    // NEW: A list of all incoming connection GameObjects
    public List<GameObject> incomingEdges = new List<GameObject>();

    // NEW: Reference to the node's text label
    public GameObject nodeLabel;
    
    // A reference to the main loader script for utility functions
    [HideInInspector]
    public BBoxLoader loader; 

    // A flag to track the original color of the node (for visual feedback)
    private Color originalColor;
    private Renderer boxRenderer;
    private Renderer wireframeRenderer;

    void Start()
    {
        boxRenderer = GetComponent<Renderer>();
        // Find the wireframe renderer child
        Transform wireframeChild = transform.Find("Wireframe");
        if (wireframeChild != null)
        {
            wireframeRenderer = wireframeChild.GetComponent<MeshRenderer>();
        }

        // Store the original color of the box for later resetting
        if (boxRenderer != null)
        {
            originalColor = boxRenderer.material.color;
        }
    }

    // Called when the Quest pointer/ray hits this object
    public void OnRayEnter()
    {
        // Highlight the node
        SetHighlight(true);
        // Show all connected edges
        SetEdgesVisibility(true);
    }

    // Called when the Quest pointer/ray leaves this object
    public void OnRayExit()
    {
        // Remove highlight
        SetHighlight(false);
        // Hide all connected edges
        SetEdgesVisibility(false);
    }

    private void SetHighlight(bool isHighlighted)
    {
        Color highlightColor = isHighlighted ? Color.yellow : originalColor;
        
        // Update the material color of the box (transparent material)
        if (boxRenderer != null)
        {
            Color boxColor = originalColor;
            boxColor.a = isHighlighted ? 0.4f : 0.2f; // Make it slightly more opaque when selected
            boxRenderer.material.color = highlightColor;
        }

        // Update the material color of the wireframe
        if (wireframeRenderer != null)
        {
            wireframeRenderer.material.color = highlightColor;
        }
    }

    public void SetEdgesVisibility(bool isVisible)
    {
        // NEW: Control visibility for the node's label
        if (nodeLabel != null)
        {
            nodeLabel.SetActive(isVisible);
        }

        // Iterate over outgoing edges
        foreach (GameObject edge in outgoingEdges)
        {
            edge.SetActive(isVisible);
        }

        // NEW: Iterate over incoming edges
        foreach (GameObject edge in incomingEdges)
        {
            edge.SetActive(isVisible);
        }
    }
}