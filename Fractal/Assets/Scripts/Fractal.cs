using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TMPro;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour
{
    const int maxDepth = 8, minDepth = 1;

    [SerializeField, Range(minDepth, maxDepth)]
    int depth = 4;

    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    [SerializeField]
    TextMeshProUGUI fpsDisplay;

    int frames = 0;
    float duration = 0.0f;

    [SerializeField]
    TextMeshProUGUI depthDisplay;

    ComputeBuffer[] matricesBuffers;
    static readonly int matricesID = Shader.PropertyToID("_Matrices");
    static MaterialPropertyBlock propertyBlock;

    struct FractalPart
    {
        public float3 direction, worldPosition;
        public quaternion rotation, worldRotation;
        public float spinAngle;
    };

    NativeArray<FractalPart>[] parts;
    NativeArray<float3x4>[] matrices;

    static float3[] directions =
    {
        up(), right(), left(), forward(), back()
    };

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
            part.worldRotation = mul(parent.worldRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            part.worldPosition =
                parent.worldPosition +
                mul(parent.worldRotation,
                (1.5f * scale * part.direction));
            parts[i] = part;
            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    };

    FractalPart CreatePart(int childIndex) => new FractalPart
    {
        direction = directions[childIndex],
        rotation = rotations[childIndex]
    };

    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        int stride = 12 * sizeof(float);
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
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
        for(int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            //material.SetBuffer(matricesID, buffer);
            propertyBlock.SetBuffer(matricesID, buffer);
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, buffer.count, propertyBlock);
        }

        frames++;
        duration += Time.unscaledDeltaTime;

        fpsDisplay.SetText("FPS {0:0}", frames / duration);

        if(frames > 50)
        {
            frames = 0;
            duration = 0.0f;
        }

        if(Input.GetKeyDown("up"))
        {
            depth++;
            depth = min(depth, maxDepth);
            OnValidate();
        }
        if(Input.GetKeyDown("down"))
        {
            depth--;
            depth = max(depth, minDepth);
            OnValidate();
        }

        depthDisplay.SetText("Depth {0}", depth);

        if (Input.GetKeyDown("escape")) Application.Quit();
    }
}
