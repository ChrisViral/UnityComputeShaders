using UnityEngine;

namespace UnityComputeShaders
{
    public class SimpleFlocking : MonoBehaviour
    {
        public struct Boid
        {
            public Vector3 position;
            public Vector3 direction;
        }

        private const string KERNEL   = "CSMain";
        private const int BOID_STRIDE = 6 * sizeof(float);

        private static readonly int BoidsBufferID       = Shader.PropertyToID("boidsBuffer");
        private static readonly int BoidSpeedID         = Shader.PropertyToID("boidSpeed");
        private static readonly int FlockPositionID     = Shader.PropertyToID("flockPosition");
        private static readonly int NeighbourDistanceID = Shader.PropertyToID("neighbourDistance");
        private static readonly int BoidsCountID        = Shader.PropertyToID("boidsCount");
        private static readonly int DeltaTimeID         = Shader.PropertyToID("deltaTime");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField, Range(0.5f, 10f)]
        private float boidSpeed = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float neighbourDistance = 1f;
        [SerializeField]
        private GameObject boidPrefab;
        [SerializeField]
        private int boidsCount;
        [SerializeField, Range(0.5f, 10f)]
        private float spawnRadius;

        private int kernelHandle;
        private ComputeBuffer boidsBuffer;
        private Boid[] boids;
        private Transform[] boidTransforms;
        private int groupSizeX;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.boidsCount / (float)x);
            this.boidsCount = this.groupSizeX * (int)x;

            InitBoids();
            InitShader();
        }

        private void OnDestroy()
        {
            this.boidsBuffer?.Dispose();
        }

        private void InitBoids()
        {
            this.boids          = new Boid[this.boidsCount];
            this.boidTransforms = new Transform[this.boidsCount];

            for (int i = 0; i < this.boidsCount; i++)
            {
                Random.InitState(new System.Random().Next());
                Vector3 position            = Random.insideUnitSphere * this.spawnRadius;
                Boid boid                   = new() { position = position };
                Transform boidTransform     = Instantiate(this.boidPrefab, Vector3.zero, Quaternion.identity, this.transform).transform;
                boidTransform.localPosition = position;
                boid.direction              = boidTransform.forward;
                this.boids[i]               = boid;
                this.boidTransforms[i]      = boidTransform;
            }
        }

        private void InitShader()
        {
            this.boidsBuffer = new(this.boidsCount, BOID_STRIDE);
            this.boidsBuffer.SetData(this.boids);

            this.shader.SetBuffer(this.kernelHandle, BoidsBufferID, this.boidsBuffer);
            this.shader.SetFloat(BoidSpeedID, this.boidSpeed);
            this.shader.SetVector(FlockPositionID, this.transform.position);
            this.shader.SetFloat(NeighbourDistanceID, this.neighbourDistance);
            this.shader.SetInt(BoidsCountID, this.boidsCount);
        }

        private void Update()
        {
            this.shader.SetFloat(DeltaTimeID, Time.deltaTime);

            this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);

            this.boidsBuffer.GetData(this.boids);

            for (int i = 0; i < this.boidsCount; i++)
            {
                Boid boid                   = this.boids[i];
                Transform boidTransform     = this.boidTransforms[i];
                boidTransform.localPosition = boid.position;

                if (boid.direction != Vector3.zero)
                {
                    boidTransform.rotation = Quaternion.LookRotation(boid.direction);
                }
            }
        }
    }
}

