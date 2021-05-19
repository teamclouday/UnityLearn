using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Transform pivotHours = default, pivotMinutes = default, pivotSeconds = default;

    private const float hoursToDegree = -30.0f, minutesToDegree = -6.0f, secondsToDegree = -6.0f;

    private Quaternion defaultRotation;

    private void Start()
    {
        defaultRotation = transform.rotation;
    }

    private void Awake()
    {
        //Debug.Log(DateTime.Now);
        var time = DateTime.Now.TimeOfDay;
        pivotHours.localRotation = Quaternion.Euler(0.0f, 0.0f, hoursToDegree * (float)time.TotalHours);
        pivotMinutes.localRotation = Quaternion.Euler(0.0f, 0.0f, minutesToDegree * (float)time.TotalMinutes);
        pivotSeconds.localRotation = Quaternion.Euler(0.0f, 0.0f, secondsToDegree * (float)time.TotalSeconds);

#if UNITY_ANDROID
        if (!Input.gyro.enabled)
        {
            Input.gyro.enabled = true;
        }
#endif
    }

    private void Update()
    {
        var time = DateTime.Now.TimeOfDay;
        pivotHours.localRotation = Quaternion.Euler(0.0f, 0.0f, hoursToDegree * (float)time.TotalHours);
        pivotMinutes.localRotation = Quaternion.Euler(0.0f, 0.0f, minutesToDegree * (float)time.TotalMinutes);
        pivotSeconds.localRotation = Quaternion.Euler(0.0f, 0.0f, secondsToDegree * (float)time.TotalSeconds);

        if (Input.GetKeyDown("escape")) Application.Quit();

#if UNITY_ANDROID
        Vector3 gyroAngle = Input.gyro.rotationRateUnbiased;
        gyroAngle.z = 0.0f;
        transform.eulerAngles += gyroAngle;
        if (IsDoubleTap()) transform.rotation = defaultRotation;
#endif
    }

    // reference: https://answers.unity.com/questions/369230/how-to-detect-double-tap-in-android.html
    private bool IsDoubleTap()
    {
        bool result = false;

#if UNITY_ANDROID
        float MaxTimeWait = 2;
        float VariancePosition = 2;

        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            float DeltaTime = Input.GetTouch(0).deltaTime;
            float DeltaPositionLength = Input.GetTouch(0).deltaPosition.magnitude;

            if (DeltaTime > 0 && DeltaTime < MaxTimeWait && DeltaPositionLength < VariancePosition)
                result = true;
        }
#endif
        return result;
    }
}
