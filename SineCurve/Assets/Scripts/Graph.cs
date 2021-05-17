using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField]
    private Transform pointPrefab = default;

    [SerializeField, Range(10, 100)]
    private int resolution = 10;

    private Transform[] points;

    private void Awake()
    {
        var position = Vector3.zero;
        var scale = Vector3.one * (2.0f / resolution);
        points = new Transform[resolution];
        for(int i = 0; i < resolution; i++)
        {
            var point = Instantiate(pointPrefab);
            position.x = (i + 0.5f) * (2.0f / resolution) - 1.0f;
            //position.y = position.x * position.x * position.x;
            point.localPosition = position;
            point.localScale = scale;
            point.SetParent(transform, true);
            points[i] = point;
        }
    }

    private void Update()
    {
        float time = Time.time;
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            var position = point.localPosition;
            position.y = Mathf.Sin(Mathf.PI * (position.x + time));
            point.localPosition = position;
        }

        if (Input.GetKeyDown("escape")) Application.Quit();
    }
}
