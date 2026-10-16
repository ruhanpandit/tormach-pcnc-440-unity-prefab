using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// PCNC 440 Door Controller
/// 
/// HOW TO USE:
/// 1. Place this script in your Assets/Scripts folder
/// 2. Drag it onto your PCNC 440 root object
/// 3. Assign LeftDoorPivot and RightDoorPivot in the Inspector
/// 4. Hit Play — point at the machine and press GRIP to toggle doors
/// </summary>
public class PCNC440DoorController : MonoBehaviour
{
    [Header("=== Pivot References ===")]
    [Tooltip("Drag your LeftDoorPivot here")]
    public Transform leftDoorPivot;

    [Tooltip("Drag your RightDoorPivot here")]
    public Transform rightDoorPivot;

    [Header("=== Door Open Rotations ===")]
    [Tooltip("Euler rotation for left door when fully open")]
    public Vector3 leftDoorOpenRotation = new Vector3(-20f, 110f, 15f);

    [Tooltip("Euler rotation for right door when fully open")]
    public Vector3 rightDoorOpenRotation = new Vector3(-20f, -110f, -15f);

    [Header("=== Door Settings ===")]
    public float doorSpeed = 3f;

    // --- Internal state ---
    private bool doorsOpen = false;
    private Vector3 leftDoorClosedRotation;
    private Vector3 rightDoorClosedRotation;
    private Quaternion leftDoorCurrent;
    private Quaternion leftDoorTargetQuat;
    private Quaternion rightDoorCurrent;
    private Quaternion rightDoorTargetQuat;

    private void Start()
    {
        // Store closed rotations (should be 0,0,0)
        if (leftDoorPivot != null)
        {
            leftDoorClosedRotation = leftDoorPivot.localEulerAngles;
            leftDoorCurrent = leftDoorPivot.localRotation;
            leftDoorTargetQuat = leftDoorCurrent;
        }
        if (rightDoorPivot != null)
        {
            rightDoorClosedRotation = rightDoorPivot.localEulerAngles;
            rightDoorCurrent = rightDoorPivot.localRotation;
            rightDoorTargetQuat = rightDoorCurrent;
        }

        SetupInteractable();
    }

    private void Update()
    {
        // Smoothly animate left door
        if (leftDoorPivot != null)
        {
            leftDoorCurrent = Quaternion.Slerp(leftDoorCurrent, leftDoorTargetQuat, doorSpeed * Time.deltaTime);
            leftDoorPivot.localRotation = leftDoorCurrent;
        }

        // Smoothly animate right door
        if (rightDoorPivot != null)
        {
            rightDoorCurrent = Quaternion.Slerp(rightDoorCurrent, rightDoorTargetQuat, doorSpeed * Time.deltaTime);
            rightDoorPivot.localRotation = rightDoorCurrent;
        }
    }

    private void SetupInteractable()
    {
        // Add collider if none exists
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            var bounds = new Bounds(transform.position, Vector3.zero);
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            box.center = transform.InverseTransformPoint(bounds.center);
            box.size = transform.InverseTransformVector(bounds.size);
            box.size *= 1.1f;
        }

        // Add Rigidbody
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // Add XR Simple Interactable
        var interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnMachineSelected);

        Debug.Log("[PCNC 440] Ready! Point at the machine and GRIP to toggle doors.");
    }

    private void OnMachineSelected(SelectEnterEventArgs args)
    {
        doorsOpen = !doorsOpen;

        if (doorsOpen)
        {
            leftDoorTargetQuat = Quaternion.Euler(leftDoorOpenRotation);
            rightDoorTargetQuat = Quaternion.Euler(rightDoorOpenRotation);
            Debug.Log("[PCNC 440] Doors opening!");
        }
        else
        {
            leftDoorTargetQuat = Quaternion.Euler(leftDoorClosedRotation);
            rightDoorTargetQuat = Quaternion.Euler(rightDoorClosedRotation);
            Debug.Log("[PCNC 440] Doors closing!");
        }
    }
}
