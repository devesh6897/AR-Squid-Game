using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceOnPlane : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    private GameObject m_PlacedPrefab;

    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }

    public GameObject spawnedObject { get; private set; }

    private ARRaycastManager m_RaycastManager;
    private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) // Only register the first click
        {
            touchPosition = Input.mousePosition;
            return true;
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) // Only first touch
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
#endif
        touchPosition = default;
        return false;
    }

    void Update()
    {
        if (spawnedObject != null) return; // Prevents further spawns

        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = s_Hits[0].pose;
            spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
        }
    }
}
