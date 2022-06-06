using UnityEngine;

namespace UnityComputeShaders
{
    public class InstancedFlocking : MonoBehaviour
    {
        public struct Boid
        {
            public Vector3 position;
            public Vector3 direction;
            public float noise;
        }

        private const string KERNEL   = "CSMain";
        private const int BOID_STRIDE = 7 * sizeof(float);
        private const int ARGS_STRIDE = 5 * sizeof(uint);

        private static readonly int BoidsBufferID        = Shader.PropertyToID("boidsBuffer");
        private static readonly int RotationSpeedID      = Shader.PropertyToID("rotationSpeed");
        private static readonly int BoidSpeedID          = Shader.PropertyToID("boidSpeed");
        private static readonly int BoidSpeedVariationID = Shader.PropertyToID("boidSpeedVariation");
        private static readonly int FlockPositionID      = Shader.PropertyToID("flockPosition");
        private static readonly int NeighbourDistanceID  = Shader.PropertyToID("neighbourDistance");
        private static readonly int BoidsCountID         = Shader.PropertyToID("boidsCount");
        private static readonly int TimeID               = Shader.PropertyToID("time");
        private static readonly int DeltaTimeID          = Shader.PropertyToID("deltaTime");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField, Range(0.5f, 10f)]
        private float rotationSpeed = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float boidSpeed = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float neighbourDistance = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float boidSpeedVariation = 1f;
        [SerializeField]
        private Mesh boidMesh;
        [SerializeField]
        private Material boidMaterial;
        [SerializeField]
        private int boidsCount;
        [SerializeField, Range(0.5f, 10f)]
        private float spawnRadius;

        private int kernelHandle;
        private ComputeBuffer boidsBuffer;
        private ComputeBuffer argsBuffer;
        private readonly uint[] args = new uint[5];
        private Boid[] boids;
        private int groupSizeX;
        private Bounds bounds;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.boidsCount / (float)x);
            this.boidsCount = this.groupSizeX * (int)x;
            this.bounds = new(Vector3.zero, new(1000f, 1000f, 1000f));

            InitBoids();
            InitShader();
        }

        private void InitBoids()
        {
            this.boids = new Boid[this.boidsCount];
            for (int i = 0; i < this.boidsCount; i++)
            {
                Random.InitState(new System.Random().Next());
                Transform parent    = this.transform;
                Vector3 position    = parent.position + (Random.insideUnitSphere * this.spawnRadius);
                Quaternion rotation = Quaternion.Slerp(parent.rotation, Random.rotation, 0.3f);
                float offset        = Random.Range(0f, 1000f);
                this.boids[i]       = new()
                {
                    position  = position,
                    direction = rotation.eulerAngles,
                    noise     = offset
                };
            }
        }

        private void InitShader()
        {
            this.boidsBuffer = new(this.boidsCount, BOID_STRIDE);
            this.boidsBuffer.SetData(this.boids);

            this.shader.SetBuffer(this.kernelHandle, BoidsBufferID, this.boidsBuffer);
            this.boidMaterial.SetBuffer(BoidsBufferID, this.boidsBuffer);

            this.shader.SetFloat(RotationSpeedID, this.rotationSpeed);
            this.shader.SetFloat(BoidSpeedID, this.boidSpeed);
            this.shader.SetFloat(BoidSpeedVariationID, this.boidSpeedVariation);
            this.shader.SetVector(FlockPositionID, this.transform.position);
            this.shader.SetFloat(NeighbourDistanceID, this.neighbourDistance);
            this.shader.SetInt(BoidsCountID, this.boidsCount);

            this.argsBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.args[0] = this.boidMesh.GetIndexCount(0);
            this.args[1] = (uint)this.boidsCount;
            this.argsBuffer.SetData(this.args);
        }

        private void Update()
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.SetFloat(DeltaTimeID, Time.deltaTime);

            this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);

            Graphics.DrawMeshInstancedIndirect(this.boidMesh, 0, this.boidMaterial, this.bounds, this.argsBuffer);
        }

        private void OnDestroy()
        {
            this.boidsBuffer?.Dispose();
            this.argsBuffer?.Dispose();
        }
    }
}

