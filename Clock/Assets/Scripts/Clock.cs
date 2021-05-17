using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Transform pivotHours = default, pivotMinutes = default, pivotSeconds = default;

    private const float hoursToDegree = -30.0f, minutesToDegree = -6.0f, secondsToDegree = -6.0f;

    private void Awake()
    {
        //Debug.Log(DateTime.Now);
        var time = DateTime.Now.TimeOfDay;
        pivotHours.localRotation = Quaternion.Euler(0.0f, 0.0f, hoursToDegree * (float)time.TotalHours);
        pivotMinutes.localRotation = Quaternion.Euler(0.0f, 0.0f, minutesToDegree * (float)time.TotalMinutes);
        pivotSeconds.localRotation = Quaternion.Euler(0.0f, 0.0f, secondsToDegree * (float)time.TotalSeconds);
    }

    private void Update()
    {
        var time = DateTime.Now.TimeOfDay;
        pivotHours.localRotation = Quaternion.Euler(0.0f, 0.0f, hoursToDegree * (float)time.TotalHours);
        pivotMinutes.localRotation = Quaternion.Euler(0.0f, 0.0f, minutesToDegree * (float)time.TotalMinutes);
        pivotSeconds.localRotation = Quaternion.Euler(0.0f, 0.0f, secondsToDegree * (float)time.TotalSeconds);

        if (Input.GetKeyDown("escape")) Application.Quit();
    }
}
