using System;
using UnityEngine;

public class LevelEditorGizmo : MonoBehaviour
{
    public enum GizmoMode
    {
        Translate,
        Rotate
    }

    public enum GizmoAxis
    {
        X,
        Y,
        Z
    }

    [SerializeField] private float moveSensitivity = 0.01f;
    [SerializeField] private float rotateSensitivity = 0.2f;

    public Camera sceneCamera;
    public GizmoMode mode = GizmoMode.Translate;
    public Action onTargetModified;
    public Vector3 offset = new Vector3(0f, 0f, -3f);

    public Transform Target { get; private set; }
    public bool IsDragging => isDragging;

    private bool isDragging;
    private Vector3 lastMousePosition;
    private GizmoAxis? activeAxis;
    private readonly Color[] axisColors = { Color.red, Color.green, Color.blue };

    private void Awake()
    {
        CreateHandle(GizmoAxis.X, Vector3.right, Quaternion.Euler(0f, 0f, 90f));
        CreateHandle(GizmoAxis.Y, Vector3.up, Quaternion.identity);
        CreateHandle(GizmoAxis.Z, Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
    }

    private void Update()
    {
        if (Target == null || sceneCamera == null) return;

        transform.position = Target.position + offset;
        transform.rotation = Quaternion.identity;

        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag();
        }

        if (Input.GetMouseButton(0) && isDragging && activeAxis.HasValue)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            ApplyDrag(mouseDelta, activeAxis.Value);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            activeAxis = null;
        }
    }

    public void SetTarget(Transform target)
    {
        Target = target;
        gameObject.SetActive(Target != null);
    }

    private void TryStartDrag()
    {
        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            LevelEditorGizmoHandle handle = hit.collider.GetComponent<LevelEditorGizmoHandle>();
            if (handle != null)
            {
                activeAxis = handle.axis;
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
        }
    }

    private void ApplyDrag(Vector3 mouseDelta, GizmoAxis axis)
    {
        Vector3 axisVector = AxisToVector(axis);
        float amount = (mouseDelta.x + mouseDelta.y) * (mode == GizmoMode.Translate ? moveSensitivity : rotateSensitivity);

        if (mode == GizmoMode.Translate)
        {
            Target.position += axisVector * amount;
        }
        else
        {
            Target.Rotate(axisVector, amount, Space.World);
        }

        onTargetModified?.Invoke();
    }

    private void CreateHandle(GizmoAxis axis, Vector3 direction, Quaternion rotation)
    {
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = $"Gizmo_{axis}";
        handle.transform.SetParent(transform, false);
        handle.transform.localRotation = rotation;
        handle.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
        handle.transform.localPosition = direction * 0.6f;
        handle.GetComponent<Renderer>().material.color = axisColors[(int)axis];
        handle.AddComponent<LevelEditorGizmoHandle>().axis = axis;
    }

    private Vector3 AxisToVector(GizmoAxis axis)
    {
        switch (axis)
        {
            case GizmoAxis.X:
                return Vector3.right;
            case GizmoAxis.Y:
                return Vector3.up;
            case GizmoAxis.Z:
                return Vector3.forward;
            default:
                return Vector3.zero;
        }
    }
}

public class LevelEditorGizmoHandle : MonoBehaviour
{
    public LevelEditorGizmo.GizmoAxis axis;
}
