using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PortalManager : MonoBehaviour
{
    public Material[] materials;
    public GameObject[] objectsToHideOnEnter;
    public GameObject[] objectsToUnhideOnEnter;

    private Camera mainCamera;
    private Vector3 camPositionInPortalSpace;
    private bool wasInFront;
    private bool inOtherWorld;
    private bool hasCollided;

    void Start()
    {
        mainCamera = Camera.main; // Cache the main camera reference
        SetMaterials(false);
        SetObjectVisibility(false); // Initially keep objects in normal state
    }

    void SetMaterials(bool fullRender)
    {
        var stencilTest = fullRender ? CompareFunction.NotEqual : CompareFunction.Equal;
        foreach (var mat in materials)
        {
            if (mat != null)
                mat.SetInt("_StencilComp", (int)stencilTest);
        }
    }

    void SetObjectVisibility(bool inPortalWorld)
    {
        // Hide objects when entering the other world
        foreach (GameObject obj in objectsToHideOnEnter)
            if (obj) obj.SetActive(!inPortalWorld);

        // Unhide objects when entering the other world
        foreach (GameObject obj in objectsToUnhideOnEnter)
            if (obj) obj.SetActive(inPortalWorld);
    }

    bool GetIsInFront()
    {
        Vector3 worldPos = mainCamera.transform.position + mainCamera.transform.forward * mainCamera.nearClipPlane;
        camPositionInPortalSpace = transform.InverseTransformPoint(worldPos);
        return camPositionInPortalSpace.y >= 0;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject != mainCamera.gameObject)
            return;

        wasInFront = GetIsInFront();
        hasCollided = true;
    }

    private void OnTriggerStay(Collider collider)
    {
        if (collider.gameObject != mainCamera.gameObject || !hasCollided)
            return;

        bool isInFront = GetIsInFront();
        if (isInFront != wasInFront)
        {
            inOtherWorld = !inOtherWorld;
            SetMaterials(inOtherWorld);
            SetObjectVisibility(inOtherWorld);
        }
        wasInFront = isInFront;
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject != mainCamera.gameObject)
            return;

        hasCollided = false;
    }

    //private void OnDestroy()
    //{
    //    SetMaterials(true);
    //    SetObjectVisibility(false); // Reset objects when destroyed
    //}
}
