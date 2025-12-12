using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class JoystickMenuController : MonoBehaviour
{
    [Header("UI Assignments")]
    public GameObject menuCanvas;
    public GameObject firstButton;
    
    // NEW: We need to know where the player's head is
    public Transform playerHead; 

    [Header("Settings")]
    public float distanceFromFace = 1.5f; // How far away the menu appears
    public OVRInput.Button openButton = OVRInput.Button.Start;
    public OVRInput.Button selectButton = OVRInput.Button.One;

    void Start()
    {
        if (menuCanvas != null) menuCanvas.SetActive(false);
    }

    void Update()
    {
        if (OVRInput.GetDown(openButton))
        {
            ToggleMenu();
        }

        if (menuCanvas.activeSelf && OVRInput.GetDown(selectButton))
        {
            GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
            if (selectedObj != null)
            {
                ExecuteEvents.Execute(selectedObj, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }
    }

    void ToggleMenu()
    {
        bool isActive = !menuCanvas.activeSelf;
        
        if (isActive)
        {
            // === NEW CODE: TELEPORT CANVAS TO FRONT OF PLAYER ===
            if (playerHead != null)
            {
                // 1. Position: Start at head, move X meters in the direction looking
                Vector3 targetPosition = playerHead.position + (playerHead.forward * distanceFromFace);
                
                // Optional: Keep the menu straight (horizon level) so it doesn't tilt up/down
                // If you want it to tilt with your head, remove this line and use targetPosition.y
                targetPosition.y = playerHead.position.y; 

                menuCanvas.transform.position = targetPosition;

                // 2. Rotation: Make the canvas face the same direction as the player
                // This ensures the text is readable and flat in front of you
                Vector3 lookDirection = playerHead.forward;
                lookDirection.y = 0; // Keep rotation level with horizon
                menuCanvas.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            // ====================================================

            menuCanvas.SetActive(true);

            // Reset selection to first button
            EventSystem.current.SetSelectedGameObject(null); 
            EventSystem.current.SetSelectedGameObject(firstButton);
        }
        else
        {
            menuCanvas.SetActive(false);
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}