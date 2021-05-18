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

    private bool transitioning = false;
    private float transitionProgress = 0.0f;
    private FunctionLibrary.FunctionName lastFunction;

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
        FunctionLibrary.Function lf = FunctionLibrary.GetFunction(lastFunction);
        var time = Time.time;
        var step = 2.0f / resolution;
        float v = 0.5f * step - 1.0f;
        for (int i = 0, x = 0, z = 0; i < nodes.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z++;
                v = (z + 0.5f) * step - 1.0f;
            }
            float u = (x + 0.5f) * step - 1.0f;
            if (transitioning)
                nodes[i].localPosition = FunctionLibrary.Morph(u, v, time, lf, f, transitionProgress);
            else
                nodes[i].localPosition = f(u, v, time);
        }
        if (transitioning)
        {
            transitionProgress += 0.05f;
            if (transitionProgress >= 1.0f) transitioning = false;
        }

        if (Input.GetKeyDown("escape")) Application.Quit();
        if (Input.GetKeyDown("1"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Wave;
            transitioning = true;
            transitionProgress = 0.0f;
        }
        if (Input.GetKeyDown("2"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.MultiWave;
            transitioning = true;
            transitionProgress = 0.0f;
        }
        if (Input.GetKeyDown("3"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Ripple;
            transitioning = true;
            transitionProgress = 0.0f;
        }
        if (Input.GetKeyDown("4"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Sphere;
            transitioning = true;
            transitionProgress = 0.0f;
        }
        if (Input.GetKeyDown("5"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Torus;
            transitioning = true;
            transitionProgress = 0.0f;
        }
    }

}
