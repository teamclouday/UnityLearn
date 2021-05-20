using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TMPro;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class Fractal : MonoBehaviour
{
#if UNITY_STANDALONE
    const int maxDepth = 9, minDepth = 3;
#elif UNITY_ANDROID
    const int maxDepth = 6, minDepth = 3;
#endif

    [SerializeField, Range(minDepth, maxDepth)]
    int depth = 4;

    [SerializeField]
    Mesh mesh, leafMesh;

    [SerializeField]
    Material material;

    [SerializeField]
    Gradient gradientA, gradientB;

    [SerializeField]
    Color leafColorA, leafColorB;

    [SerializeField, Range(0f, 90f)]
    float maxSagAngleA = 15f, maxSagAngleB = 25f;

    [SerializeField]
    TextMeshProUGUI fpsDisplay, depthDisplay;

    int frames = 0;
    float duration = 0.0f;

    ComputeBuffer[] matricesBuffers;
    static readonly int matricesID = Shader.PropertyToID("_Matrices");
    static readonly int colorAID = Shader.PropertyToID("_ColorA");
    static readonly int colorBID = Shader.PropertyToID("_ColorB");
    static readonly int sequenceNumbersID = Shader.PropertyToID("_SequenceNumbers");
    static MaterialPropertyBlock propertyBlock;

    Vector4[] sequenceNumbers;

    struct FractalPart
    {
        public float3 worldPosition;
        public quaternion rotation, worldRotation;
        public float maxSagAngle, spinAngle;
    };

    NativeArray<FractalPart>[] parts;
    NativeArray<float3x4>[] matrices;

    static quaternion[] rotations =
    {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI),
        quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI),
        quaternion.RotateX(-0.5f * PI)
    };

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor
    {
        public float spinAngleDelta;
        public float scale;

        [ReadOnly]
        public NativeArray<FractalPart> parents;

        public NativeArray<FractalPart> parts;

        [WriteOnly]
        public NativeArray<float3x4> matrices;

        public void Execute(int i)
        {
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];
            part.spinAngle += spinAngleDelta;
            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            float3 sagAxis = cross(up(), upAxis);
            float sagLength = length(sagAxis);
            quaternion baseRotation;
            if(sagLength > 0f)
                baseRotation = mul(quaternion.AxisAngle(sagAxis / sagLength, part.maxSagAngle * sagLength), parent.worldRotation);
            else
                baseRotation = parent.worldRotation;
            part.worldRotation = mul(baseRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            part.worldPosition =
                parent.worldPosition +
                mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));
            parts[i] = part;
            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    };

    FractalPart CreatePart(int childIndex) => new FractalPart
    {
        maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
        rotation = rotations[childIndex]
    };

    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        sequenceNumbers = new Vector4[depth];
        int stride = 12 * sizeof(float);
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
            sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
        }

        parts[0][0] = CreatePart(0);
        for(int li = 1; li < parts.Length; li++)
        {
            NativeArray<FractalPart> levelParts = parts[li];
            for(int fpi = 0; fpi < levelParts.Length; fpi+=5)
            {
                for(int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }

        propertyBlock ??= new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }
        parts = null;
        matrices = null;
        matricesBuffers = null;
        sequenceNumbers = null;
    }

    private void OnValidate()
    {
        if(parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update()
    {
        float spinAngleDelta = 0.125f * PI * Time.deltaTime;
        float objectScale = transform.lossyScale.x;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = mul(transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        parts[0][0] = rootPart;
        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

        float scale = objectScale;
        JobHandle jobHandle = default;
        for(int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob
            {
                spinAngleDelta = spinAngleDelta,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 5, jobHandle);
        }
        jobHandle.Complete();

        var bounds = new Bounds(Vector3.zero, 3f * objectScale * Vector3.one);
        int leafIndex = matricesBuffers.Length - 1;
        for(int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            propertyBlock.SetBuffer(matricesID, buffer);
            propertyBlock.SetVector(sequenceNumbersID, sequenceNumbers[i]);
            Color colorA, colorB;
            Mesh instanceMesh;
            if(i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                float gradientInterpolate = i / (matricesBuffers.Length - 1f);
                colorA = gradientA.Evaluate(gradientInterpolate);
                colorB = gradientB.Evaluate(gradientInterpolate);
                instanceMesh = mesh;
            }
            propertyBlock.SetColor(colorAID, colorA);
            propertyBlock.SetColor(colorBID, colorB);
            Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, buffer.count, propertyBlock);
        }

        frames++;
        duration += Time.unscaledDeltaTime;

        UpdateTextMessage();

        if (frames > 50)
        {
            frames = 0;
            duration = 0.0f;
        }

        HandleKeys();
    }

    private void Awake()
    {
#if UNITY_ANDROID
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
#endif
    }

    void UpdateTextMessage()
    {
        fpsDisplay.SetText("{0:0}", frames / duration);
        depthDisplay.SetText("{0}", depth);
    }

    void HandleKeys()
    {
#if UNITY_STANDALONE
        if (Input.GetKeyDown("up"))
        {
            depth++;
            depth = min(depth, maxDepth);
            OnValidate();
        }
        if (Input.GetKeyDown("down"))
        {
            depth--;
            depth = max(depth, minDepth);
            OnValidate();
        }

        if (Input.GetKeyDown("escape")) Application.Quit();
#elif UNITY_ANDROID
        if(CustomCamera.IsDoubleTap())
        {
            depth++;
            if (depth > maxDepth) depth = minDepth;
            OnValidate();
        }
#endif
    }
}
