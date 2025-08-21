using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

public class BoidsVatController : MonoBehaviour
{
    [Header("Setup")]
    public VAT_AnimationData vatData;
    public ComputeShader boidsComputeShader;
    public Material sourceMaterial; // Material đã được gán shader Boids_VAT_URP

    [Header("Boids Settings")]
    [Range(1, 1000000)]
    public int boidCount = 10000;
    public float maxSpeed = 5.0f;
    public float maxForce = 1.0f;
    public float separationRadius = 2.0f;
    public float alignmentRadius = 5.0f;
    public float cohesionRadius = 7.0f;
    [Space]
    public float separationFactor = 1.5f;
    public float alignmentFactor = 1.0f;
    public float cohesionFactor = 1.0f;

    [Header("Simulation Area")]
    public Vector3 simulationBoundsCenter = Vector3.zero;
    public Vector3 simulationBoundsSize = new Vector3(100, 50, 100);

    private int _kernelHandle;
    private ComputeBuffer _boidsBufferRead;
    private ComputeBuffer _boidsBufferWrite;
    private ComputeBuffer _argsBuffer;
    private Material _instancedMaterial;
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

    private readonly int THREAD_GROUP_SIZE = 64;

    // Struct này phải có layout và kích thước khớp chính xác với struct trong các shader
    private struct Boid
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    void OnEnable()
    {
        InitializeBoids();
        InitializeRendering();
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    private void InitializeBoids()
    {
        _kernelHandle = boidsComputeShader.FindKernel("CSMain");

        int boidStructSize = Marshal.SizeOf(typeof(Boid));
        _boidsBufferRead = new ComputeBuffer(boidCount, boidStructSize, ComputeBufferType.Default);
        _boidsBufferWrite = new ComputeBuffer(boidCount, boidStructSize, ComputeBufferType.Default);

        var initialBoids = new Boid[boidCount];
        for (int i = 0; i < boidCount; i++)
        {
            initialBoids[i] = new Boid
            {
                position = simulationBoundsCenter + Random.insideUnitSphere * Mathf.Min(simulationBoundsSize.x, simulationBoundsSize.y, simulationBoundsSize.z) * 0.5f,
                velocity = Random.insideUnitSphere * maxSpeed
            };
        }
        _boidsBufferRead.SetData(initialBoids);
        _boidsBufferWrite.SetData(initialBoids);
    }

    private void InitializeRendering()
    {
        _instancedMaterial = new Material(sourceMaterial);
        _instancedMaterial.SetTexture("_PositionTexture", vatData.positionTexture);
        _instancedMaterial.SetVector("_PositionMin", vatData.positionMinBounds);
        _instancedMaterial.SetVector("_PositionMax", vatData.positionMaxBounds);

        // Buffer cho DrawMeshInstancedIndirect
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (vatData.bakedMesh != null)
        {
            _args[0] = vatData.bakedMesh.GetIndexCount(0);
            _args[1] = (uint)boidCount;
            _args[2] = vatData.bakedMesh.GetIndexStart(0);
            _args[3] = vatData.bakedMesh.GetBaseVertex(0);
        }
        _argsBuffer.SetData(_args);
    }

    void Update()
    {
        RunSimulation();
        DrawInstances();
    }

    private void RunSimulation()
    {
        boidsComputeShader.SetInt("numBoids", boidCount);
        boidsComputeShader.SetFloat("deltaTime", Time.deltaTime);
        boidsComputeShader.SetFloat("maxSpeed", maxSpeed);
        boidsComputeShader.SetFloat("maxForce", maxForce);
        boidsComputeShader.SetFloat("separationRadius", separationRadius);
        boidsComputeShader.SetFloat("alignmentRadius", alignmentRadius);
        boidsComputeShader.SetFloat("cohesionRadius", cohesionRadius);
        boidsComputeShader.SetFloat("separationFactor", separationFactor);
        boidsComputeShader.SetFloat("alignmentFactor", alignmentFactor);
        boidsComputeShader.SetFloat("cohesionFactor", cohesionFactor);
        boidsComputeShader.SetVector("simulationBoundsCenter", simulationBoundsCenter);
        boidsComputeShader.SetVector("simulationBoundsSize", simulationBoundsSize);

        boidsComputeShader.SetBuffer(_kernelHandle, "boidsBufferRead", _boidsBufferRead);
        boidsComputeShader.SetBuffer(_kernelHandle, "boidsBufferWrite", _boidsBufferWrite);

        int threadGroups = Mathf.CeilToInt((float)boidCount / THREAD_GROUP_SIZE);
        boidsComputeShader.Dispatch(_kernelHandle, threadGroups, 1, 1);

        // Swap buffers cho frame tiếp theo
        (_boidsBufferRead, _boidsBufferWrite) = (_boidsBufferWrite, _boidsBufferRead);
    }

    private void DrawInstances()
    {
        // Ta có thể dùng một VAT_Animator "master" để lấy thông tin animation
        // và set vào _instancedMaterial ở đây. Ví dụ:
        // _instancedMaterial.SetFloat("_CurrentAnimNormalizedTime", masterAnimator.GetCurrentNormalizedTime());
        // ...
        // Tạm thời, để đơn giản, ta sẽ cho animation tự chạy theo thời gian
        float normalizedTime = (Time.time * 0.5f) % 1.0f; // Giả sử animation lặp lại
        _instancedMaterial.SetFloat("_CurrentAnimNormalizedTime", normalizedTime);
        _instancedMaterial.SetFloat("_AnimationBlendWeight", 0); // Không blending

        _instancedMaterial.SetBuffer("_BoidDataBuffer", _boidsBufferRead);

        var renderBounds = new Bounds(simulationBoundsCenter, simulationBoundsSize);
        Graphics.DrawMeshInstancedIndirect(
            vatData.bakedMesh,
            0,
            _instancedMaterial,
            renderBounds,
            _argsBuffer,
            0,
            null,
            ShadowCastingMode.On,
            true,
            gameObject.layer
        );
    }

    private void ReleaseBuffers()
    {
        _boidsBufferRead?.Release();
        _boidsBufferWrite?.Release();
        _argsBuffer?.Release();
        _boidsBufferRead = null;
        _boidsBufferWrite = null;
        _argsBuffer = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireCube(simulationBoundsCenter, simulationBoundsSize);
    }
}