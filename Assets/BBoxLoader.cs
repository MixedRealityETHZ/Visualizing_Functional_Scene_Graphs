using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using TMPro;

public class BBoxLoader : MonoBehaviour
{
    [System.Serializable]
    public class SceneRoot
    {
        public List<NodeData> gt_nodes;
        public List<NodeData> detected_nodes;
    }

    [System.Serializable]
    public class NodeData
    {
        public int id;
        public string name;
        public float[] position;
        public float[] bbox_extent;
        public float[] rotation;
        public List<InteractionData> interactions;
    }

    [System.Serializable]
    public class InteractionData
    {
        public int target_object_idx;
        public string description;
    }

    [Header("Templates")]
    public Material transparentMaterialTemplate;
    public Material wireframeMaterialTemplate;
    public GameObject textPrefab;

    public enum NodeSource
    {
        GT,
        Detected
    }

    [Header("Settings")]
    public string roomID = "401";
    public NodeSource nodeSource = NodeSource.GT;
    public float lineWidth = 0.02f;
    public float movingArrowScale = 0.5f;

    // --- Internal State ---
    private Mesh sharedWireframeMesh;
    private Mesh sharedArrowMesh;
    
    private Dictionary<int, GameObject> createdNodes = new Dictionary<int, GameObject>();
    private Dictionary<int, Color> nodeColors = new Dictionary<int, Color>();

    void Start()
    {
        sharedWireframeMesh = CreateCubeWireframeMesh();
        sharedArrowMesh = CreateArrowMesh(); 
        
        // CHANGED: We now start a Coroutine because loading files on Quest is asynchronous
        StartCoroutine(LoadScene());
    }

    // CHANGED: Converted from void to IEnumerator
    IEnumerator LoadScene()
    {
        createdNodes.Clear();
        nodeColors.Clear();

        string fileName = $"scene_graph_{roomID}.json";
        // We build the path to the file inside StreamingAssets/UnityData
        string filePath = Path.Combine(Application.streamingAssetsPath, "UnityData", fileName);
        
        string jsonContent = "";

        // On Android (Quest), the file is inside a JAR (Zip), so we must use UnityWebRequest.
        // On PC (Editor), we can use standard File I/O, but UnityWebRequest works for local files too if we prefix properly.
        // We check if the path is a web/jar path to decide.
        if (filePath.Contains("://") || filePath.Contains("jar:")) 
        {
            using (UnityWebRequest www = UnityWebRequest.Get(filePath))
            {
                // Wait for the file to be extracted from the APK
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error loading JSON on device: " + www.error);
                    yield break; // Stop execution if failed
                }
                else
                {
                    jsonContent = www.downloadHandler.text;
                }
            }
        }
        else
        {
            // Standard PC / Editor fallback
            if (File.Exists(filePath))
            {
                jsonContent = File.ReadAllText(filePath);
            }
            else
            {
                Debug.LogError("File not found in Editor: " + filePath);
                yield break;
            }
        }

        // Proceed to parse the JSON content we just loaded
        if (!string.IsNullOrEmpty(jsonContent))
        {
            try 
            {
                SceneRoot sceneData = JsonUtility.FromJson<SceneRoot>(jsonContent);

                // Determine which list to use based on the Inspector setting
                List<NodeData> nodesToProcess = (nodeSource == NodeSource.GT) 
                    ? sceneData.gt_nodes 
                    : sceneData.detected_nodes;

                if (nodesToProcess != null)
                {
                    // PASS 1: Create Nodes
                    foreach (var node in nodesToProcess)
                    {
                        CreateVisualBox(node);
                    }

                    // PASS 2: Create Edges
                    foreach (var node in nodesToProcess)
                    {
                        if (node.interactions != null)
                        {
                            foreach (var interaction in node.interactions)
                            {
                                CreateGradientEdge(node, interaction);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"The selected list ({nodeSource}) was null or empty in the JSON.");
                }

            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON Parsing Error: " + e.Message);
            }
        }
    }

    void CreateVisualBox(NodeData node)
    {
        // 1. Generate and Store Color
        float hue = (node.id * 0.13f) % 1.0f;
        Color baseColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
        
        if (!nodeColors.ContainsKey(node.id)) nodeColors.Add(node.id, baseColor);

        // 2. Create Object
        GameObject solidBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        solidBox.name = "Node_" + node.id;

        // 3. Position: Open3D (x, y, z) -> Unity (x, z, y)
        Vector3 pos = new Vector3(node.position[0], node.position[2], node.position[1]);

        // 4. Rotation: Convert Quaternion (RH to LH)
        // Mapping: (x, y, z, w) -> (-x, -z, -y, w)
        Quaternion rot = Quaternion.identity;
        if (node.rotation != null && node.rotation.Length == 4)
        {
            rot = new Quaternion(
                -node.rotation[0], // -x
                -node.rotation[2], // -z (swapped into y slot)
                -node.rotation[1], // -y (swapped into z slot)
                node.rotation[3]   // w
            );
        }

        // 5. Scale: SWAP Y AND Z
        // Because the Rotation maps Open3D-Z (Height) to Unity-Y (Height),
        // we must apply the Open3D-Z extent to the Unity-Y scale axis.
        // Open3D Extent (x, y, z) -> Unity Scale (x, z, y)
        Vector3 scale = new Vector3(node.bbox_extent[0], node.bbox_extent[2], node.bbox_extent[1]);

        // --- Apply Transforms ---
        solidBox.transform.position = pos;
        solidBox.transform.rotation = rot;
        solidBox.transform.localScale = scale;

        // --- NEW: Add the SceneGraphNode component ---
        SceneGraphNode nodeComp = solidBox.AddComponent<SceneGraphNode>();
        nodeComp.nodeId = node.id;
        nodeComp.loader = this; // Give it a reference to the main script

        // -------------------------------------------------------------------
        // CORRECTED ADDITION: Set the object's layer for raycast detection
        solidBox.layer = LayerMask.NameToLayer("SceneGraphNode"); 
        // -------------------------------------------------------------------

        // =========================================================
        // VITAL FIX: Add and Configure Rigidbody for OVRGrabbable
        // =========================================================
        Rigidbody rb = solidBox.AddComponent<Rigidbody>();
        rb.isKinematic = true;      // We don't want the objects to move due to gravity/forces
        rb.useGravity = false;      // Disable gravity for bounding boxes
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Apply Material
        if (transparentMaterialTemplate != null)
        {
            Material faceMat = new Material(transparentMaterialTemplate);
            faceMat.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);
            solidBox.GetComponent<Renderer>().material = faceMat;
        }

        // Create Wireframe
        if (wireframeMaterialTemplate != null)
        {
            GameObject wireframe = new GameObject("Wireframe");
            wireframe.transform.SetParent(solidBox.transform, false);
            wireframe.AddComponent<MeshFilter>().sharedMesh = sharedWireframeMesh;
            Material edgeMat = new Material(wireframeMaterialTemplate);
            edgeMat.color = baseColor;
            wireframe.AddComponent<MeshRenderer>().material = edgeMat;
        }

        // Create Text
        // if (textPrefab != null)
        // {
        //     GameObject textObj = Instantiate(textPrefab);
            
        //     // Text position: global Up offset
        //     textObj.transform.position = pos + (Vector3.up * 0.5f); 
            
        //     TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        //     if(tmp != null) {
        //         tmp.text = node.name;
        //         tmp.color = Color.white;
        //     }
        //     textObj.transform.SetParent(solidBox.transform.parent); 
        // }

        if (!createdNodes.ContainsKey(node.id)) createdNodes.Add(node.id, solidBox);

        // if (solidBox.GetComponent<OVRGrabbable>() == null)
        // {
        //     // Try to add the OVR Grabbable component instead
        //     try
        //     {
        //         solidBox.AddComponent<OVRGrabbable>();
        //     }
        //     catch (System.Exception e)
        //     {
        //         Debug.LogWarning("OVRGrabbable component not found or failed to add. Proceeding without specific XR Interactable component. Manual raycast setup will be required: " + e.Message);
        //     }
        // }
    }

    void CreateGradientEdge(NodeData sourceNode, InteractionData interaction)
    {
        if (!createdNodes.ContainsKey(sourceNode.id) || !createdNodes.ContainsKey(interaction.target_object_idx)) return;

        GameObject startObj = createdNodes[sourceNode.id];
        
        SceneGraphNode sourceNodeComp = startObj.GetComponent<SceneGraphNode>();
        if (sourceNodeComp == null) return; // Safety check

        GameObject endObj = createdNodes[interaction.target_object_idx];
        SceneGraphNode targetNodeComp = endObj.GetComponent<SceneGraphNode>(); // NEW: Get target component
        if (targetNodeComp == null) return; // Safety check

        Vector3 startPos = startObj.transform.position;
        Vector3 endPos = endObj.transform.position;
        
        // Calculate Direction explicitly for alignment
        Vector3 direction = (endPos - startPos).normalized;
        if (direction == Vector3.zero) return; // Prevent errors if positions are identical

        Color colorA = nodeColors[sourceNode.id];
        Color colorB = nodeColors[interaction.target_object_idx];

        // --- PART 1: The Gradient Line ---
        GameObject lineObj = new GameObject($"Line_{sourceNode.id}_to_{interaction.target_object_idx}");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        // FIX 1: Use 'Sprites/Default' shader. This natively supports vertex colors (gradients).
        lr.material = new Material(Shader.Find("Sprites/Default")); 
        
        lr.positionCount = 2;
        lr.SetPosition(0, startPos);
        lr.SetPosition(1, endPos);
        lr.widthMultiplier = lineWidth;

        // Set Gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(colorA, 0.0f), new GradientColorKey(colorB, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.8f, 1.0f) }
        );
        lr.colorGradient = gradient;

        // --- PART 2: The Moving Arrow ---
        GameObject arrowObj = new GameObject("MovingArrow");
        arrowObj.transform.SetParent(lineObj.transform);
        
        MeshFilter mf = arrowObj.AddComponent<MeshFilter>();
        mf.sharedMesh = sharedArrowMesh;
        MeshRenderer mr = arrowObj.AddComponent<MeshRenderer>();
        
        // We use Sprites/Default for the arrow too, so it looks bright and clean
        Material arrowMat = new Material(Shader.Find("Sprites/Default"));
        arrowMat.color = Color.white; 
        mr.material = arrowMat;

        // Scale
        arrowObj.transform.localScale = Vector3.one * movingArrowScale;

        // FIX 2: Explicit Rotation
        // We force the arrow to look in the direction vector we calculated earlier
        arrowObj.transform.rotation = Quaternion.LookRotation(direction);

        // Add Movement Script
        ArrowMover mover = arrowObj.AddComponent<ArrowMover>();
        mover.startPos = startPos;
        mover.endPos = endPos;
        mover.duration = 2.0f;

        // --- NEW: Store the edge and hide it by default ---
        sourceNodeComp.outgoingEdges.Add(lineObj); 
        // NEW: Add to target node's incoming list
        targetNodeComp.incomingEdges.Add(lineObj);
        
        lineObj.SetActive(false); // <--- HIDE THE EDGE BY DEFAULT

        // // --- PART 3: The Text Label ---
        // if (textPrefab != null)
        // {
        //     Vector3 midPoint = (startPos + endPos) / 2;
        //     GameObject labelObj = Instantiate(textPrefab, midPoint + Vector3.up * 0.1f, Quaternion.identity);
        //     labelObj.name = "EdgeLabel";
        //     labelObj.transform.SetParent(lineObj.transform);
            
        //     TextMeshPro tmp = labelObj.GetComponent<TextMeshPro>();
        //     if(tmp != null) {
        //         tmp.text = interaction.description;
        //         tmp.fontSize = 2.0f;
        //         tmp.color = Color.yellow; 
        //     }
        // }
    }

    Mesh CreateArrowMesh()
    {
        Mesh mesh = new Mesh();
        // A simple arrowhead shape
        float w = 0.1f; // width
        float l = 0.2f; // length
        
        // This mesh points along positive Z axis
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, l/2),    // Tip (Forward)
            new Vector3(-w, 0, -l/2),  // Back Left
            new Vector3(w, 0, -l/2),   // Back Right
            new Vector3(0, w, -l/2),   // Back Top
            new Vector3(0, -w, -l/2)   // Back Bottom
        };

        int[] triangles = new int[]
        {
            0,1,3, 0,3,2, 0,2,4, 0,4,1, // Sides
            1,4,2, 2,3,1 // Back cap
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    Mesh CreateCubeWireframeMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f) };
        int[] indices = new int[] { 0,1, 1,2, 2,3, 3,0, 4,5, 5,6, 6,7, 7,4, 0,4, 1,5, 2,6, 3,7 };
        mesh.vertices = vertices; mesh.SetIndices(indices, MeshTopology.Lines, 0); mesh.RecalculateBounds(); return mesh;
    }
}