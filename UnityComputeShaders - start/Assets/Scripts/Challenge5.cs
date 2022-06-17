using CjLib;
using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(VoxelizeMesh))]
    public class Challenge5 : MonoBehaviour
    {// ReSharper disable NotAccessedField.Local
        #pragma warning disable CS0649
        private struct Rigidbody
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public Vector3 angularVelocity;
            public int particleOffset;
        }

        private struct Particle
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector3 force;
            public Vector3 localPosition;
            public Vector3 offsetPosition;
        }
        #pragma warning restore CS0649
        // ReSharper restore NotAccessedField.Local

        private const int RIGIDBODY_STRIDE    = (13 * sizeof(float)) + sizeof(int);
        private const int PARTICLE_STRIDE     = 15 * sizeof(float);
        private const int ARGS_STRIDE         = 5 * sizeof(uint);
        private const int VOXEL_STRIDE        = 8 * sizeof(int);
        private const string GENERATE_KERNEL  = "GenerateParticleValues";
        private const string CLEAR_KERNEL     = "ClearGrid";
        private const string POPULATE_KERNEL  = "PopulateGrid";
        private const string GRID_KERNEL      = "CollisionDetectionWithGrid";
        private const string COLLISION_KERNEL = "CollisionDetection";
        private const string MOMENTA_KERNEL   = "ComputeMomenta";
        private const string COMPUTE_KERNEL   = "ComputePositionAndRotation";
        private const float STANDARD_GRAVITY  = 9.80665f;

        private static readonly int RigidBodiesBufferID          = Shader.PropertyToID("rigidbodies");
        private static readonly int ParticlesBufferID            = Shader.PropertyToID("particles");
        private static readonly int VoxelGridBufferID            = Shader.PropertyToID("voxels");

        private static readonly int GridDimensionsID             = Shader.PropertyToID("gridDimensions");
        private static readonly int GridMaxID                    = Shader.PropertyToID("gridMax");
        private static readonly int GridStartPositionID          = Shader.PropertyToID("gridStartPosition");
        private static readonly int ParticleCountID              = Shader.PropertyToID("particleCount");
        private static readonly int ParticlesPerBodyID           = Shader.PropertyToID("particlesPerBody");
        private static readonly int ParticleMassID               = Shader.PropertyToID("particleMass");
        private static readonly int SpringCoefficientID          = Shader.PropertyToID("springCoefficient");
        private static readonly int DampingCoefficientID         = Shader.PropertyToID("dampingCoefficient");
        private static readonly int TangentialCoefficientID      = Shader.PropertyToID("tangentialCoefficient");
        private static readonly int GravityCoefficientID         = Shader.PropertyToID("gravityCoefficient");
        private static readonly int ParticleDiameterID           = Shader.PropertyToID("particleDiameter");
        private static readonly int FrictionCoefficientID        = Shader.PropertyToID("frictionCoefficient");
        private static readonly int LinearForceScalarID          = Shader.PropertyToID("linearForceScalar");
        private static readonly int AngularFrictionCoefficientID = Shader.PropertyToID("angularFrictionCoefficient");
        private static readonly int AngularForceScalarID         = Shader.PropertyToID("angularForceScalar");
        private static readonly int ActiveCountID                = Shader.PropertyToID("activeCount");
        private static readonly int DeltaTimeID                  = Shader.PropertyToID("deltaTime");
        private static readonly int ScaleID                      = Shader.PropertyToID("scale");

        [SerializeField, Header("Rendering")]
        private ComputeShader shader;
        [SerializeField]
        private Mesh mesh;
        [SerializeField]
        private Material material;
        [SerializeField]
        private Material sphereMaterial;
        [SerializeField]
        private Material lineMaterial;
        [SerializeField]
        private bool debugWireframe;
        [SerializeField, Header("Cube"), Range(0.1f, 10f)]
        private float cubeMass;
        [SerializeField, Header("Coefficients"), Range(0.1f, 10f)]
        private float springCoefficient;
        [SerializeField, Range(0.001f, 2f)]
        private float dampingCoefficient;
        [SerializeField, Range(0.001f, 2f)]
        private float tangentialCoefficient;
        [SerializeField, Range(0.5f, 10f)]
        private float gravityCoefficient;
        [SerializeField, Range(0.1f, 10f)]
        private float frictionCoefficient;
        [SerializeField, Range(0.1f, 10f)]
        private float angularFrictionCoefficient;
        [SerializeField, Range(10f, 200f)]
        private float angularForceScalar;
        [SerializeField, Range(10f, 200f)]
        private float linearForceScalar;
        [SerializeField, Header("Voxelization")]
        private bool useGrid = true;
        [SerializeField]
        private Vector3Int gridSize = new(5, 5, 5);
        [SerializeField]
        private Vector3 gridPosition;
        [SerializeField, Header("Simulation")]
        private int rigidbodyCount = 1000;
        [SerializeField, Range(1, 20)]
        private int stepsPerUpdate = 10;
        [SerializeField]
        private Bounds bounds;

        private VoxelizeMesh voxelizer;
        private Mesh sphereMesh;
        private Mesh lineMesh;
        private int particlesPerBody;
        private float particleDiameter;
        private int activeCount;
        private int frameCounter;

        private Vector3[] particlePositions;
        private Rigidbody[] rigidbodies;
        private Particle[] particles;
        private ComputeBuffer rigidbodiesBuffer;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer voxelGridBuffer;

        private readonly uint[] cubeArgs   = new uint[5];
        private readonly uint[] sphereArgs = new uint[5];
        private readonly uint[] lineArgs   = new uint[5];
        private ComputeBuffer argsCubeBuffer;
        private ComputeBuffer argsSphereBuffer;
        private ComputeBuffer argsLineBuffer;

        private int generateParticleValuesHandle;
        private int collisionDetectionHandle;
        private int computeMomentaHandle;
        private int computePositionAndRotationHandle;
        private int clearGridHandle;
        private int collisionDetectionWithGridHandle;
        private int populateGridHandle;
        private int groupsPerRigidbody;
        private int groupsPerParticle;
        private int groupsPerGridCell;
        private int gridTotalSize;

        #region Functions
        private void Awake()
        {
            Random.InitState(new System.Random().Next());
            this.sphereMesh = PrimitiveMeshFactory.SphereWireframe(6, 6);
            this.lineMesh   = PrimitiveMeshFactory.Line(Vector3.zero, Vector3.one);
            this.voxelizer  = GetComponent<VoxelizeMesh>();
        }

        private void Start()
        {
            InitVoxelization();
            InitArrays();
            InitRigidBodies();
            InitParticles();
            InitBuffers();
            InitKernels();
            InitShader();
            InitInstancing();

        }

        private void OnDestroy()
        {
            this.rigidbodiesBuffer.Release();
            this.particlesBuffer.Release();
            this.argsCubeBuffer?.Release();
            this.argsSphereBuffer?.Release();
            this.argsLineBuffer?.Release();
            this.voxelGridBuffer?.Release();
        }

        private void Update()
        {
            if (this.debugWireframe)
            {
                Graphics.DrawMeshInstancedIndirect(this.sphereMesh, 0, this.sphereMaterial, this.bounds, this.argsSphereBuffer);
                Graphics.DrawMeshInstancedIndirect(this.lineMesh, 0, this.lineMaterial, this.bounds, this.argsLineBuffer);
            }
            else
            {
                Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, this.bounds, this.argsCubeBuffer);
            }
        }

        private void FixedUpdate()
        {
            if (this.activeCount < this.rigidbodyCount && this.frameCounter++ > 5)
            {
                this.activeCount++;
                this.frameCounter = 0;
                this.shader.SetInt(ActiveCountID, this.activeCount);

                this.cubeArgs[1] = (uint)this.activeCount;
                this.argsCubeBuffer.SetData(this.cubeArgs);
            }

            this.shader.SetFloat(DeltaTimeID, Time.fixedDeltaTime / this.stepsPerUpdate);

            for (int i = 0; i < this.stepsPerUpdate; i++)
            {
                this.shader.Dispatch(this.generateParticleValuesHandle, this.groupsPerRigidbody, 1, 1);
                if (this.useGrid)
                {
                    this.shader.Dispatch(this.clearGridHandle, this.groupsPerGridCell, 1, 1);
                    this.shader.Dispatch(this.populateGridHandle, this.groupsPerParticle, 1, 1);
                    this.shader.Dispatch(this.collisionDetectionWithGridHandle, this.groupsPerParticle, 1, 1);
                }
                else
                {
                    this.shader.Dispatch(this.collisionDetectionHandle, this.groupsPerParticle, 1, 1);
                }

                this.shader.Dispatch(this.computeMomentaHandle, this.groupsPerRigidbody, 1, 1);
                this.shader.Dispatch(this.computePositionAndRotationHandle, this.groupsPerRigidbody, 1, 1);
            }
        }
        #endregion

        #region Initialization
        private void InitVoxelization()
        {
            //1. Get the VoxelizeMesh component
            //2. Use meshToVoxelize as the property mesh
            //3. Use it to voxelize its mesh
            this.voxelizer.Voxelize();

            //4. Set the particleInitialPositions to the PositionList
            this.particlePositions = new Vector3[this.voxelizer.Positions.Count];
            this.voxelizer.Positions.CopyTo(this.particlePositions);

            //5. Set the particlesPerBody
            this.particlesPerBody = this.particlePositions.Length;

            //6. Set vertex count
            //7. Set the particle diameter
            this.particleDiameter = this.voxelizer.ParticleSize;
        }

        private void InitArrays()
        {
            this.rigidbodies      = new Rigidbody[this.rigidbodyCount];
            this.particles        = new Particle[this.rigidbodyCount * this.particlesPerBody];
        }

        private void InitRigidBodies()
        {
            for (int i = 0, offset = 0; i < this.rigidbodyCount; i++, offset += this.particlesPerBody)
            {
                Vector3 position = Random.insideUnitSphere * 5f;
                position.y      += 10f;
                this.rigidbodies[i] = new()
                {
                    position       = position,
                    rotation       = Random.rotation,
                    particleOffset = offset
                };
            }
        }

        private void InitParticles()
        {
            this.particles = new Particle[this.rigidbodyCount * this.particlesPerBody];

            foreach (Rigidbody body in this.rigidbodies)
            {
                for (int j = 0; j < this.particlePositions.Length; j++)
                {
                    Vector3 position = this.particlePositions[j];
                    this.particles[body.particleOffset + j] = new()
                    {
                        localPosition = position
                    };
                }
            }
        }

        private void InitBuffers()
        {
            this.rigidbodiesBuffer = new(this.rigidbodyCount, RIGIDBODY_STRIDE);
            this.rigidbodiesBuffer.SetData(this.rigidbodies);

            this.particlesBuffer = new(this.particles.Length, PARTICLE_STRIDE);
            this.particlesBuffer.SetData(this.particles);


            this.gridTotalSize = this.gridSize.x * this.gridSize.y * this.gridSize.z;
            this.voxelGridBuffer = new(this.gridTotalSize, VOXEL_STRIDE);
        }

        private void InitKernels()
        {
            this.generateParticleValuesHandle     = this.shader.FindKernel(GENERATE_KERNEL);
            this.clearGridHandle                  = this.shader.FindKernel(CLEAR_KERNEL);
            this.populateGridHandle               = this.shader.FindKernel(POPULATE_KERNEL);
            this.collisionDetectionWithGridHandle = this.shader.FindKernel(GRID_KERNEL);
            this.collisionDetectionHandle         = this.shader.FindKernel(COLLISION_KERNEL);
            this.computeMomentaHandle             = this.shader.FindKernel(MOMENTA_KERNEL);
            this.computePositionAndRotationHandle = this.shader.FindKernel(COMPUTE_KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.generateParticleValuesHandle, out uint x, out _, out _);
            this.groupsPerRigidbody = Mathf.CeilToInt(this.rigidbodyCount / (float)x);

            this.shader.GetKernelThreadGroupSizes(this.collisionDetectionHandle, out x, out _, out _);
            this.groupsPerParticle = Mathf.CeilToInt(this.particles.Length / (float)x);

            this.shader.GetKernelThreadGroupSizes(this.clearGridHandle, out x, out _, out _);
            this.groupsPerGridCell = Mathf.CeilToInt(this.gridTotalSize / (float)x);
        }

        private void InitShader()
        {
            Vector3 gridStartPosition = (this.gridPosition * this.particleDiameter) - ((Vector3)this.gridSize * (this.particleDiameter / 2f));

            this.shader.SetInts(GridDimensionsID, this.gridSize.x, this.gridSize.y, this.gridSize.z);
            this.shader.SetInt(GridMaxID, this.gridTotalSize);
            this.shader.SetFloats(GridStartPositionID, gridStartPosition.x, gridStartPosition.y, gridStartPosition.z);
            this.shader.SetInt(ParticleCountID, this.particles.Length);
            this.shader.SetInt(ParticlesPerBodyID, this.particlesPerBody);
            this.shader.SetFloat(ParticleMassID, this.cubeMass / this.particlesPerBody);
            this.shader.SetFloat(SpringCoefficientID, this.springCoefficient);
            this.shader.SetFloat(DampingCoefficientID, this.dampingCoefficient);
            this.shader.SetFloat(TangentialCoefficientID, this.tangentialCoefficient);
            this.shader.SetFloat(GravityCoefficientID, this.gravityCoefficient * STANDARD_GRAVITY);
            this.shader.SetFloat(ParticleDiameterID, this.particleDiameter);
            this.shader.SetFloat(FrictionCoefficientID, this.frictionCoefficient);
            this.shader.SetFloat(LinearForceScalarID, this.linearForceScalar);
            this.shader.SetFloat(AngularFrictionCoefficientID, this.angularFrictionCoefficient);
            this.shader.SetFloat(AngularForceScalarID, this.angularForceScalar);

            this.shader.SetBuffer(this.generateParticleValuesHandle, RigidBodiesBufferID, this.rigidbodiesBuffer);
            this.shader.SetBuffer(this.generateParticleValuesHandle, ParticlesBufferID, this.particlesBuffer);

            this.shader.SetBuffer(this.clearGridHandle, VoxelGridBufferID, this.voxelGridBuffer);

            this.shader.SetBuffer(this.populateGridHandle, ParticlesBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.populateGridHandle, VoxelGridBufferID, this.voxelGridBuffer);

            this.shader.SetBuffer(this.collisionDetectionWithGridHandle, ParticlesBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.collisionDetectionWithGridHandle, VoxelGridBufferID, this.voxelGridBuffer);

            this.shader.SetBuffer(this.computeMomentaHandle, RigidBodiesBufferID, this.rigidbodiesBuffer);
            this.shader.SetBuffer(this.computeMomentaHandle, ParticlesBufferID, this.particlesBuffer);

            this.shader.SetBuffer(this.computePositionAndRotationHandle, RigidBodiesBufferID, this.rigidbodiesBuffer);

            this.shader.SetBuffer(this.collisionDetectionHandle, ParticlesBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.collisionDetectionHandle, VoxelGridBufferID, this.voxelGridBuffer);
        }

        private void InitInstancing()
        {
            this.material.SetBuffer(RigidBodiesBufferID, this.rigidbodiesBuffer);

            this.sphereMaterial.SetBuffer(ParticlesBufferID, this.particlesBuffer);
            this.sphereMaterial.SetFloat(ScaleID, this.particleDiameter / 2f);

            this.lineMaterial.SetBuffer(ParticlesBufferID, this.particlesBuffer);

            this.cubeArgs[0]    = this.mesh.GetIndexCount(0);
            this.cubeArgs[1]    = 1u;
            this.argsCubeBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.argsCubeBuffer.SetData(this.cubeArgs);

            this.sphereArgs[0]    = this.sphereMesh.GetIndexCount(0);
            this.sphereArgs[1]    = (uint)this.particles.Length;
            this.argsSphereBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.argsSphereBuffer.SetData(this.sphereArgs);

            this.lineArgs[0]    = this.lineMesh.GetIndexCount(0);
            this.lineArgs[1]    = (uint)this.particles.Length;
            this.argsLineBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.argsLineBuffer.SetData(this.lineArgs);
        }
        #endregion
    }
}