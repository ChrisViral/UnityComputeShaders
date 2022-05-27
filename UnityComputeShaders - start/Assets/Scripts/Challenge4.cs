using UnityEngine;

namespace UnityComputeShaders
{
    public class Challenge4 : MonoBehaviour
    {
        public struct Boid
        {
            public Vector3 position;
            public Vector3 direction;
            // ReSharper disable once InconsistentNaming
            public float noise_offset;
            public float theta;

            public Boid(Vector3 pos, Vector3 dir, float offset)
            {
                this.position.x   = pos.x;
                this.position.y   = pos.y;
                this.position.z   = pos.z;
                this.direction.x  = dir.x;
                this.direction.y  = dir.y;
                this.direction.z  = dir.z;
                this.noise_offset = offset;
                this.theta        = Random.value * Mathf.PI * 2f;
            }
        }

        private const string KERNEL = "CSMain";

        private static readonly int UniqueID             = Shader.PropertyToID("_UniqueID");
        private static readonly int RotationSpeedID      = Shader.PropertyToID("rotationSpeed");
        private static readonly int BoidSpeedID          = Shader.PropertyToID("boidSpeed");
        private static readonly int BoidSpeedVariationID = Shader.PropertyToID("boidSpeedVariation");
        private static readonly int FlockPositionID      = Shader.PropertyToID("flockPosition");
        private static readonly int NeighbourDistanceID  = Shader.PropertyToID("neighbourDistance");
        private static readonly int BoidsCountID         = Shader.PropertyToID("boidsCount");
        private static readonly int BoidsBufferID        = Shader.PropertyToID("boidsBuffer");
        private static readonly int TimeID               = Shader.PropertyToID("time");
        private static readonly int DeltaTimeID          = Shader.PropertyToID("deltaTime");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private float rotationSpeed = 1f;
        [SerializeField]
        private float boidSpeed = 1f;
        [SerializeField]
        private float neighbourDistance = 1f;
        [SerializeField]
        private float boidSpeedVariation = 1f;
        [SerializeField]
        private Mesh boidMesh;
        [SerializeField]
        private Material boidMaterial;
        [SerializeField]
        private int boidsCount;
        [SerializeField]
        private float spawnRadius;
        [SerializeField]
        private Transform target;

        private int kernelHandle;
        private ComputeBuffer boidsBuffer;
        private ComputeBuffer argsBuffer;
        private readonly uint[] args = new uint[5];
        private Boid[] boidsArray;
        private GameObject[] boids;
        private int groupSizeX;
        private int numOfBoids;
        private Bounds bounds;
        private MaterialPropertyBlock props;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.boidsCount / (float)x);
            this.numOfBoids = this.groupSizeX * (int)x;

            this.bounds = new(Vector3.zero, new(1000f, 1000f, 1000f));
            this.props  = new();
            this.props.SetFloat(UniqueID, Random.value);

            InitBoids();
            InitShader();

            //Debug.Log(boidMesh.bounds);
        }

        private void InitBoids()
        {
            this.boids = new GameObject[this.numOfBoids];
            this.boidsArray = new Boid[this.numOfBoids];

            for (int i = 0; i < this.numOfBoids; i++)
            {
                // ReSharper disable once LocalVariableHidesMember
                Transform transform = this.transform;
                Vector3 position    = transform.position + Random.insideUnitSphere * this.spawnRadius;
                Quaternion rotation = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
                float offset        = Random.value * 1000f;
                this.boidsArray[i]  = new(position, rotation.eulerAngles, offset);
            }
        }

        private void InitShader()
        {
            this.boidsBuffer = new(this.numOfBoids, 8 * sizeof(float));
            this.boidsBuffer.SetData(this.boidsArray);

            this.argsBuffer = new(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            if (this.boidMesh != null)
            {
                this.args[0] = this.boidMesh.GetIndexCount(0);
                this.args[1] = (uint)this.numOfBoids;
            }
            this.argsBuffer.SetData(this.args);

            this.shader.SetBuffer(this.kernelHandle, BoidsBufferID, this.boidsBuffer);
            this.shader.SetFloat(RotationSpeedID, this.rotationSpeed);
            this.shader.SetFloat(BoidSpeedID, this.boidSpeed);
            this.shader.SetFloat(BoidSpeedVariationID, this.boidSpeedVariation);
            this.shader.SetVector(FlockPositionID, this.target.transform.position);
            this.shader.SetFloat(NeighbourDistanceID, this.neighbourDistance);
            this.shader.SetInt(BoidsCountID, this.numOfBoids);

            this.boidMaterial.SetBuffer(BoidsBufferID, this.boidsBuffer);
        }

        private void Update()
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.SetFloat(DeltaTimeID, Time.deltaTime);

            this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);

            Graphics.DrawMeshInstancedIndirect(this.boidMesh, 0, this.boidMaterial, this.bounds, this.argsBuffer, 0, this.props);
        }

        private void OnDestroy()
        {
            this.boidsBuffer?.Dispose();
            this.argsBuffer?.Dispose();
        }
    }
}
