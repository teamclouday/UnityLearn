using UnityEngine;
using static UnityEngine.Mathf;

public class CustomCamera : MonoBehaviour
{
    [SerializeField]
    private Transform pos = default;

    private float distance = 3.0f;

    private Vector3 defaultPos = Vector3.zero;

    private float aroundY = 0.0f;
    private float aroundXZ = 45.0f;

    private bool mouseDown = false;
    private Vector3 mousePos = Vector3.zero;

#if UNITY_ANDROID
    private float zoomDist = 0.0f;
    private bool zooming = true;
#endif

    private void Awake()
    {
        ComputePosition();
        UpdateCamera();
    }

    private void Update()
    {
#if UNITY_STANDALONE
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
#elif UNITY_ANDROID
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                mouseDown = true;
                mousePos = touch.position;
            }
            if (mouseDown && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                mouseDown = false;

            if (mouseDown && touch.phase == TouchPhase.Moved)
            {
                Vector3 newPos = touch.position;
                Vector3 delta = newPos - mousePos;
                mousePos = newPos;
                aroundY += delta.x * 0.1f;
                aroundXZ -= delta.y * 0.1f;
                aroundXZ = Max(Min(aroundXZ, 89.0f), -89.0f);
                ComputePosition();
                UpdateCamera();
            }
        }

        if(Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            if(touch0.phase == TouchPhase.Began)
            {
                zooming = true;
                zoomDist = Vector3.Distance(touch0.position, touch1.position) / Screen.width;
            }
            if(zooming && touch0.phase == TouchPhase.Moved)
            {
                float newDist = Vector3.Distance(touch0.position, touch1.position) / Screen.width;
                distance -= (newDist - zoomDist) * 10.0f;
                distance = Max(Min(distance, 5.0f), 1.0f);
                zoomDist = newDist;
                UpdateCamera();
            }
            if (touch0.phase == TouchPhase.Ended || touch0.phase == TouchPhase.Canceled) zooming = false;
        }
#endif
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
        pos.localPosition = defaultPos * distance;
        pos.LookAt(Vector3.zero);
    }

    public static bool IsDoubleTap()
    {
        bool tap = false;
#if UNITY_ANDROID
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).tapCount == 2)
        {
            tap = true;
        }
#endif
        return tap;
    }
}
