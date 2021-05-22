using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;
using TMPro;

public class HashVisual : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [WriteOnly]
        public NativeArray<uint> hashes;

        public int resolution;
        public float invResolution;
        public SmallXXHash hash;

        public void Execute(int i)
        {
            int v = (int)floor(invResolution * i + 0.000000f);
            int u = i - resolution * v - resolution / 2;
            v -= resolution / 2;
            hashes[i] = hash.Eat(u).Eat(v);
        }
    }

    static int
        idHashes = Shader.PropertyToID("_Hashes"),
        idConfig = Shader.PropertyToID("_Config");

    [SerializeField]
    Mesh instanceMesh = default;

    [SerializeField]
    Material material = default;

    [SerializeField]
    int seed = 0;

    [SerializeField, Range(-2f, 2f)]
    float verticalOffset = 1f;

    [SerializeField, Range(1, 512)]
    int resolution = 16;

    [SerializeField]
    TextMeshProUGUI seedDisplay;

    NativeArray<uint> hashes;

    ComputeBuffer hashesBuffer;

    MaterialPropertyBlock propertyBlock;

    public readonly struct SmallXXHash
    {
        const uint primeA = 0b10011110001101110111100110110001;
        const uint primeB = 0b10000101111010111100101001110111;
        const uint primeC = 0b11000010101100101010111000111101;
        const uint primeD = 0b00100111110101001110101100101111;
        const uint primeE = 0b00010110010101100110011110110001;

        readonly uint accumulator;

        public SmallXXHash(uint accumulator)
        {
            this.accumulator = accumulator;
        }

        //public void Eat(int data)
        //{
        //    accumulator = RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;
        //}

        //public void Eat(byte data)
        //{
        //    accumulator = RotateLeft(accumulator + data * primeE, 11) * primeA;
        //}

        public static implicit operator SmallXXHash(uint accumulator) => new SmallXXHash(accumulator);

        public SmallXXHash Eat(int data) => RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;

        public SmallXXHash Eat(byte data) => RotateLeft(accumulator + data * primeC, 11) * primeA;

        public static SmallXXHash Seed(int seed) => (uint)seed + primeE;

        static uint RotateLeft(uint data, int steps) => 
            (data << steps) | (data >> 32 - steps);

        public static implicit operator uint(SmallXXHash hash)
        {
            uint avalanche = hash.accumulator;
            avalanche ^= avalanche >> 15;
            avalanche *= primeB;
            avalanche ^= avalanche >> 13;
            avalanche *= primeC;
            avalanche ^= avalanche >> 16;
            return avalanche;
        }
    }

    private void OnEnable()
    {
        int length = resolution * resolution;
        hashes = new NativeArray<uint>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(length, sizeof(uint));

        new HashJob
        {
            hashes = hashes,
            resolution = resolution,
            invResolution = 1f / resolution,
            hash = SmallXXHash.Seed(seed)
        }.ScheduleParallel(hashes.Length, resolution, default).Complete();

        hashesBuffer.SetData(hashes);

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.SetBuffer(idHashes, hashesBuffer);
        propertyBlock.SetVector(idConfig, new Vector4(resolution, 1f / resolution, verticalOffset / resolution));

        seedDisplay.SetText("{}", seed);
    }

    private void OnDisable()
    {
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;

        seedDisplay.SetText("Seed");
    }

    private void OnValidate()
    {
        if(hashesBuffer != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one),
            hashes.Length, propertyBlock
        );

        if(Input.GetKeyDown(KeyCode.Space))
        {
            seed = Random.Range(0, 999);
            verticalOffset = Random.Range(-2f, 2f);
            OnValidate();
            seedDisplay.SetText("{}", seed);
        }
        if(Input.GetKeyDown(KeyCode.UpArrow))
        {
            resolution += 5;
            resolution = min(resolution, 500);
            OnValidate();
        }
        if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            resolution -= 5;
            resolution = max(resolution, 5);
            OnValidate();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }
}