using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class TogglePlanes : MonoBehaviour
{
    [Header("AR Settings")]
    public ARPlaneManager planeManager;

    [Header("Object Toggle")]
    [SerializeField] private GameObject objectToToggle;

    private bool planesVisible = true;  // Track the current state

    private void Start()
    {
        // Initialize states
        planesVisible = true;
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(false);  // Start with object hidden
        }
    }

    public void TogglePlaneDetection()
    {
        // Flip the state
        planesVisible = !planesVisible;

        // Update plane manager and planes
        planeManager.enabled = planesVisible;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(planesVisible);
        }

        // Toggle the object (opposite of planes)
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(!planesVisible);
        }
    }

    // Method to set which object should be toggled
    public void SetToggleObject(GameObject newObject)
    {
        objectToToggle = newObject;
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(!planesVisible);
        }
    }
}