using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField]
    Transform nodePrefab = default;

    [SerializeField, Range(10, 100)]
    private int resolution = 10;

    [SerializeField]
    private FunctionLibrary.FunctionName function = default;

    private Transform[] nodes;

    private void Awake()
    {
        var step = 2.0f / resolution;
        var scale = Vector3.one * step;
        nodes = new Transform[resolution * resolution];
        for(int i = 0; i < nodes.Length; i++)
        {
            var node = Instantiate(nodePrefab);
            node.localScale = scale;
            node.SetParent(transform, false);
            nodes[i] = node;
        }
    }

    private void Update()
    {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
        var time = Time.time;
        var step = 2.0f / resolution;
        float v = 0.5f * step - 1.0f;
        for (int i = 0, x = 0, z = 0; i < nodes.Length; i++, x++)
        {
            if(x == resolution)
            {
                x = 0;
                z++;
                v = (z + 0.5f) * step - 1.0f;
            }
            float u = (x + 0.5f) * step - 1.0f;
            nodes[i].localPosition = f(u, v, time);
        }

        if (Input.GetKeyDown("escape")) Application.Quit();
        if (Input.GetKeyDown("1")) function = FunctionLibrary.FunctionName.Wave;
        if (Input.GetKeyDown("2")) function = FunctionLibrary.FunctionName.MultiWave;
        if (Input.GetKeyDown("3")) function = FunctionLibrary.FunctionName.Ripple;
        if (Input.GetKeyDown("4")) function = FunctionLibrary.FunctionName.Sphere;
        if (Input.GetKeyDown("5")) function = FunctionLibrary.FunctionName.Torus;
    }
}
