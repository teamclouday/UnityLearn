using UnityEngine;
using static UnityEngine.Mathf;

public class CustomCamera : MonoBehaviour
{
    private float distance = 5.0f;

    private Vector3 defaultPos = Vector3.zero;

    private float aroundY = 45.0f;
    private float aroundXZ = 20.0f;

    private bool mouseDown = false;
    private Vector3 mousePos = Vector3.zero;

    private void Awake()
    {
        ComputePosition();
        UpdateCamera();
    }

    private void Update()
    {
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if(scroll > 0.0f)
        {
            distance -= 0.1f;
            distance = Max(distance, 0.1f);
            UpdateCamera();
        }
        else if(scroll < 0.0f)
        {
            distance += 0.1f;
            distance = Min(distance, 5.0f);
            UpdateCamera();
        }
        if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            mousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0)) mouseDown = false;
        if(mouseDown)
        {
            Vector3 newPos = Input.mousePosition;
            Vector3 delta = newPos - mousePos;
            mousePos = newPos;
            aroundY += delta.x * 0.1f;
            aroundXZ -= delta.y * 0.1f;
            aroundXZ = Max(Min(aroundXZ, 89.0f), -89.0f);
            ComputePosition();
            UpdateCamera();
        }
    }

    private void ComputePosition()
    {
        defaultPos.x = Sin(PI / 180.0f * aroundY);
        defaultPos.z = Cos(PI / 180.0f * aroundY);
        defaultPos.y = Tan(PI / 180.0f * aroundXZ);
        defaultPos = Vector3.Normalize(defaultPos);
    }

    private void UpdateCamera()
    {
        transform.localPosition = defaultPos * distance;
        transform.LookAt(Vector3.zero);
    }
}
