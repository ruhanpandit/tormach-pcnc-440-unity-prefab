using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// PCNC 440 Simple Demo Controller
/// 
/// One click: doors open → spindle down → table shifts → spindle up → doors open → reset
/// Uses direct position setting per frame instead of lerp for reliability.
/// </summary>
public class PCNC440Controller : MonoBehaviour
{
    [Header("=== Pivot References ===")]
    public Transform leftDoorPivot;
    public Transform rightDoorPivot;
    public Transform spindlePivot;
    public Transform tablePivot;

    [Header("=== Door Settings ===")]
    public Vector3 leftDoorOpenRotation = new Vector3(-20f, 110f, 15f);
    public Vector3 rightDoorOpenRotation = new Vector3(-20f, -110f, -15f);
    public float doorSpeed = 3f;

    [Header("=== Spindle Settings ===")]
    public float spindlePlungeDistance = 0.1f;
    public float spindleSpeed = 0.2f;

    [Header("=== Table Settings ===")]
    public float tableShiftDistance = 0.05f;
    public float tableSpeed = 0.2f;

    // --- State ---
    private bool demoRunning = false;
    private int step = 0;
    private float timer = 0f;

    private Quaternion leftDoorClosedRot;
    private Quaternion rightDoorClosedRot;
    private Vector3 spindleStartPos;
    private Vector3 tableStartPos;

    private void Start()
    {
        if (leftDoorPivot != null) leftDoorClosedRot = leftDoorPivot.localRotation;
        if (rightDoorPivot != null) rightDoorClosedRot = rightDoorPivot.localRotation;
        if (spindlePivot != null) spindleStartPos = spindlePivot.localPosition;
        if (tablePivot != null) tableStartPos = tablePivot.localPosition;

        SetupInteractable();
    }

    private void Update()
    {
        if (!demoRunning) return;

        timer += Time.deltaTime;

        switch (step)
        {
            case 0: // Open doors
                AnimateDoors(leftDoorOpenRotation, rightDoorOpenRotation);
                if (timer > 3f) Next();
                break;

            case 1: // Close doors
                AnimateDoors(Vector3.zero, Vector3.zero);
                if (timer > 3f) Next();
                break;

            case 2: // Spindle moves down
                MoveToward(spindlePivot, spindleStartPos + new Vector3(0, -spindlePlungeDistance, 0), spindleSpeed);
                if (timer > 3f) Next();
                break;

            case 3: // Table shifts right
                MoveToward(tablePivot, tableStartPos + new Vector3(tableShiftDistance, 0, 0), tableSpeed);
                if (timer > 2f) Next();
                break;

            case 4: // Table shifts back
                MoveToward(tablePivot, tableStartPos, tableSpeed);
                if (timer > 2f) Next();
                break;

            case 5: // Spindle moves up
                MoveToward(spindlePivot, spindleStartPos, spindleSpeed);
                if (timer > 3f) Next();
                break;

            case 6: // Open doors
                AnimateDoors(leftDoorOpenRotation, rightDoorOpenRotation);
                if (timer > 3f) Next();
                break;

            case 7: // Close doors and finish
                AnimateDoors(Vector3.zero, Vector3.zero);
                if (timer > 3f)
                {
                    demoRunning = false;
                    Debug.Log("[PCNC 440] Demo complete!");
                }
                break;
        }
    }

    private void Next()
    {
        step++;
        timer = 0f;
    }

    private void AnimateDoors(Vector3 leftTarget, Vector3 rightTarget)
    {
        if (leftDoorPivot != null)
            leftDoorPivot.localRotation = Quaternion.Slerp(
                leftDoorPivot.localRotation,
                Quaternion.Euler(leftTarget),
                doorSpeed * Time.deltaTime);

        if (rightDoorPivot != null)
            rightDoorPivot.localRotation = Quaternion.Slerp(
                rightDoorPivot.localRotation,
                Quaternion.Euler(rightTarget),
                doorSpeed * Time.deltaTime);
    }

    private void MoveToward(Transform pivot, Vector3 target, float speed)
    {
        if (pivot == null) return;
        pivot.localPosition = Vector3.MoveTowards(pivot.localPosition, target, speed * Time.deltaTime);
    }

    // ========================================
    // XR SETUP
    // ========================================

    private void SetupInteractable()
    {
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

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        var interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnMachineSelected);
        Debug.Log("[PCNC 440] Ready! Point and GRIP to start.");
    }

    private void OnMachineSelected(SelectEnterEventArgs args)
    {
        if (demoRunning) return;

        // Reset everything to start
        if (spindlePivot != null) spindlePivot.localPosition = spindleStartPos;
        if (tablePivot != null) tablePivot.localPosition = tableStartPos;
        if (leftDoorPivot != null) leftDoorPivot.localRotation = leftDoorClosedRot;
        if (rightDoorPivot != null) rightDoorPivot.localRotation = rightDoorClosedRot;

        demoRunning = true;
        step = 0;
        timer = 0f;
        Debug.Log("[PCNC 440] Demo started!");
    }
}
