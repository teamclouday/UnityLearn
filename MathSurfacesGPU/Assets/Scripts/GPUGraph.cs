using UnityEngine;
using TMPro;

public class GPUGraph : MonoBehaviour
{
    const int minResolution = 10;
    const int maxResolution = 1000;

    [SerializeField, Range(minResolution, maxResolution)]
    int resolution = 100;

    [SerializeField]
    FunctionLibrary.FunctionName function = default;

    FunctionLibrary.FunctionName lastFunction = default;
    bool isTransition = false;
    float progress = 0.0f;

    bool resoUp = false, resoDown = false;

    ComputeBuffer positionsBuffer;

    [SerializeField]
    ComputeShader computeShader;

    [SerializeField]
    Material material;

    [SerializeField]
    Mesh mesh;

    [SerializeField]
    TextMeshProUGUI objectCountDisplay;

    [SerializeField]
    TextMeshProUGUI fpsDisplay;

    int frames = 0;
    float duration = 0.0f;

    static readonly int
        positionsID = Shader.PropertyToID("_Positions"),
        resolutionID = Shader.PropertyToID("_Resolution"),
        stepID = Shader.PropertyToID("_Step"),
        timeID = Shader.PropertyToID("_Time"),
        functionNewID = Shader.PropertyToID("_FunctionNew"),
        functionLastID = Shader.PropertyToID("_FunctionLast"),
        isTransitionID = Shader.PropertyToID("_IsTransition"),
        progressID = Shader.PropertyToID("_Progress");

    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * sizeof(float));
        objectCountDisplay.SetText("{0} x {0}", resolution);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
        objectCountDisplay.SetText("");
    }

    private void Update()
    {
        if(isTransition)
        {
            progress += 0.1f;
            if (progress >= 1.0f) isTransition = false;
        }
        
        UpdateFunctionOnGPU();

        if (Input.GetKeyDown("escape")) Application.Quit();
        if (Input.GetKeyDown("1"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Wave;
            if (function != lastFunction)
            {
                isTransition = true;
                progress = 0.0f;
            }
        }
        if (Input.GetKeyDown("2"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.MultiWave;
            if (function != lastFunction)
            {
                isTransition = true;
                progress = 0.0f;
            }
        }
        if (Input.GetKeyDown("3"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Ripple;
            if (function != lastFunction)
            {
                isTransition = true;
                progress = 0.0f;
            }
        }
        if (Input.GetKeyDown("4"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Sphere;
            if (function != lastFunction)
            {
                isTransition = true;
                progress = 0.0f;
            }
        }
        if (Input.GetKeyDown("5"))
        {
            lastFunction = function;
            function = FunctionLibrary.FunctionName.Torus;
            if (function != lastFunction)
            {
                isTransition = true;
                progress = 0.0f;
            }
        }

        if (Input.GetKeyDown("up")) resoUp = true;
        if (Input.GetKeyUp("up")) resoUp = false;
        if (Input.GetKeyDown("down")) resoDown = true;
        if (Input.GetKeyUp("down")) resoDown = false;
        if (resoUp)
        {
            resolution += 1;
            resolution = Mathf.Min(resolution, maxResolution);
            objectCountDisplay.SetText("{0} x {0}", resolution);
        }
        if(resoDown)
        {
            resolution -= 1;
            resolution = Mathf.Max(resolution, minResolution);
            objectCountDisplay.SetText("{0} x {0}", resolution);
        }

        duration += Time.unscaledDeltaTime;
        frames++;

        fpsDisplay.SetText("FPS {0:0}", frames / duration);

        if(frames > 50)
        {
            frames = 0;
            duration = 0.0f;
        }
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2.0f / resolution;
        computeShader.SetInt(resolutionID, resolution);
        computeShader.SetInt(functionNewID, (int)function);
        computeShader.SetInt(functionLastID, (int)lastFunction);
        if (isTransition) computeShader.SetFloat(isTransitionID, 1.0f);
        else computeShader.SetFloat(isTransitionID, 0.0f);
        computeShader.SetFloat(progressID, progress);
        computeShader.SetFloat(stepID, step);
        computeShader.SetFloat(timeID, Time.time);
        computeShader.SetBuffer(0, positionsID, positionsBuffer);
        int groups = Mathf.CeilToInt(resolution / 8.0f);
        computeShader.Dispatch(0, groups, groups, 1);

        material.SetBuffer(positionsID, positionsBuffer);
        material.SetFloat(stepID, step);

        var bounds = new Bounds(Vector3.zero, Vector3.one * (2.0f + 2.0f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }
}
