using System.Collections.Generic;
using CjLib;
using UnityEngine;

namespace UnityComputeShaders
{
    public class Challenge5 : MonoBehaviour
    {
        private struct RigidBody
        {
            public Vector3 position;
            public Quaternion quaternion;
            public Vector3 velocity;
            public Vector3 angularVelocity;
            public int particleIndex;
            public int particleCount;

            public RigidBody(Vector3 position, int particleIndex, int particleCount)
            {
                this.position = position;
                this.quaternion = Random.rotation; //Quaternion.identity;
                this.velocity = this.angularVelocity = Vector3.zero;
                this.particleIndex = particleIndex;
                this.particleCount = particleCount;
            }
        }

        private struct Particle
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector3 force;
            public Vector3 localPosition;
            public Vector3 offsetPosition;

            public Particle(Vector3 position)
            {
                this.position = this.velocity = this.force = this.offsetPosition = Vector3.zero;
                this.localPosition = position;
            }
        }

        private const int SIZE_RIGIDBODY           = (13 * sizeof(float)) + (2 * sizeof(int));
        private const int SIZE_PARTICLE            = 15 * sizeof(float);
        private const uint VERTEX_COUNT            = 0;
        private const string GENERATE_KERNEL       = "GenerateParticleValues";
        private const string CLEAR_KERNEL          = "ClearGrid";
        private const string POPULATE_KERNEL       = "PopulateGrid";
        private const string COLLISION_GRID_KERNEL = "CollisionDetectionWithGrid";
        private const string MOMENTA_KERNEL        = "ComputeMomenta";
        private const string COMPUTE_KERNEL        = "ComputePositionAndRotation";
        private const string COLLISION_KERNEL      = "CollisionDetection";

        private static readonly int GridDimensionsID             = Shader.PropertyToID("gridDimensions");
        private static readonly int GridMaxID                    = Shader.PropertyToID("gridMax");
        private static readonly int ParticlesPerRigidBodyID      = Shader.PropertyToID("particlesPerRigidBody");
        private static readonly int ParticleDiameterID           = Shader.PropertyToID("particleDiameter");
        private static readonly int SpringCoefficientID          = Shader.PropertyToID("springCoefficient");
        private static readonly int DampingCoefficientID         = Shader.PropertyToID("dampingCoefficient");
        private static readonly int FrictionCoefficientID        = Shader.PropertyToID("frictionCoefficient");
        private static readonly int AngularFrictionCoefficientID = Shader.PropertyToID("angularFrictionCoefficient");
        private static readonly int GravityCoefficientID         = Shader.PropertyToID("gravityCoefficient");
        private static readonly int TangentialCoefficientID      = Shader.PropertyToID("tangentialCoefficient");
        private static readonly int AngularForceScalarID         = Shader.PropertyToID("angularForceScalar");
        private static readonly int LinearForceScalarID          = Shader.PropertyToID("linearForceScalar");
        private static readonly int ParticleMassID               = Shader.PropertyToID("particleMass");
        private static readonly int ParticleCountID              = Shader.PropertyToID("particleCount");
        private static readonly int GridStartPositionID          = Shader.PropertyToID("gridStartPosition");
        private static readonly int RigidBodiesBufferID          = Shader.PropertyToID("rigidBodiesBuffer");
        private static readonly int ParticlesBufferID            = Shader.PropertyToID("particlesBuffer");
        private static readonly int VoxelGridBufferID            = Shader.PropertyToID("voxelGridBuffer");
        private static readonly int ScaleID                      = Shader.PropertyToID("scale");
        private static readonly int ActiveCountID                = Shader.PropertyToID("activeCount");
        private static readonly int DeltaTimeID                  = Shader.PropertyToID("deltaTime");

        #region
        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private Material rigidBodyMaterial;
        [SerializeField]
        private Material sphereMaterial;
        [SerializeField]
        private Material lineMaterial;
        [SerializeField]
        private Bounds bounds;
        [SerializeField]
        private float cubeMass;
        [SerializeField]
        private float scale;
        [SerializeField]
        private float springCoefficient;
        [SerializeField]
        private float dampingCoefficient;
        [SerializeField]
        private float tangentialCoefficient;
        [SerializeField]
        private float gravityCoefficient;
        [SerializeField]
        private float frictionCoefficient;
        [SerializeField]
        private float angularFrictionCoefficient;
        [SerializeField]
        private float angularForceScalar;
        [SerializeField]
        private float linearForceScalar;
        [SerializeField]
        private Vector3Int gridSize = new(5, 5, 5);
        [SerializeField]
        private Vector3 gridPosition; //centre of grid
        [SerializeField]
        private bool useGrid = true;
        [SerializeField]
        private int rigidBodyCount = 1000;
        [SerializeField, Range(1, 20)]
        private int stepsPerUpdate = 10;
        [SerializeField, Header("Debug")]
        private bool debugWireframe;

        // calculated
        private Vector3 cubeScale;

        private float particleDiameter;

        private RigidBody[] rigidBodiesArray;
        private Particle[] particlesArray;
        private int[] voxelGridArray;

        private ComputeBuffer rigidBodiesBuffer;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer argsSphereBuffer;
        private ComputeBuffer argsLineBuffer;
        private ComputeBuffer voxelGridBuffer; // int4*2

        private int kernelGenerateParticleValues;
        private int kernelClearGrid;
        private int kernelPopulateGrid;
        private int kernelCollisionDetectionWithGrid;
        private int kernelComputeMomenta;
        private int kernelComputePositionAndRotation;
        private int kernelCollisionDetection;

        private int groupsPerRigidBody;
        private int groupsPerParticle;
        private int groupsPerGridCell;

        private Mesh mesh;

        private int activeCount;
        private int particlesPerBody;

        private readonly List<Vector3> particleInitialPositions = new();

        private int frameCounter;

        private static Mesh SphereMesh => PrimitiveMeshFactory.SphereWireframe(6, 6);

        private static Mesh LineMesh   => PrimitiveMeshFactory.Line(Vector3.zero, Vector3.one);

        private void Start()
        {
            //1. Get the VoxelizeMesh component
            //2. Use meshToVoxelize as the property mesh
            //3. Use it to voxelize its mesh
            //4. Set the particleInitialPositions to the PositionList
            //5. Set the particlesPerBody
            //6. Set vertex count
            //7. Set the particle diameter

            InitArrays();

            InitRigidBodies();

            InitParticles();

            InitBuffers();

            InitShader();

            InitInstancing();

        }

        private void InitArrays()
        {
            this.rigidBodiesArray = new RigidBody[this.rigidBodyCount];
            this.particlesArray   = new Particle[this.rigidBodyCount * this.particlesPerBody];
            this.voxelGridArray   = new int[this.gridSize.x * this.gridSize.y * this.gridSize.z * 8];
        }

        private void InitRigidBodies()
        {
            int pIndex = 0;

            for(int i = 0; i < this.rigidBodyCount; i++)
            {
                Vector3 pos = Random.insideUnitSphere * 5f;
                pos.y += 15;
                this.rigidBodiesArray[i] = new(pos, pIndex, this.particlesPerBody);
                pIndex += this.particlesPerBody;
            }
        }

        private void InitParticles()
        {
            int count = this.rigidBodyCount * this.particlesPerBody;

            this.particlesArray = new Particle[count];

            for (int i = 0; i < this.rigidBodyCount; i++)
            {
                RigidBody body = this.rigidBodiesArray[i];

                for (int j = 0; j < this.particleInitialPositions.Count; j++)
                {
                    Vector3 pos = this.particleInitialPositions[j];
                    this.particlesArray[body.particleIndex + j] = new(pos);
                }
            }

            Debug.Log($"particleCount: {this.rigidBodyCount * this.particlesPerBody}");
        }

        private void InitBuffers()
        {
            this.rigidBodiesBuffer = new(this.rigidBodyCount, SIZE_RIGIDBODY);
            this.rigidBodiesBuffer.SetData(this.rigidBodiesArray);

            int numOfParticles = this.rigidBodyCount * this.particlesPerBody;
            this.particlesBuffer = new(numOfParticles, SIZE_PARTICLE);
            this.particlesBuffer.SetData(this.particlesArray);

            int numGridCells = this.gridSize.x * this.gridSize.y * this.gridSize.z;
            this.voxelGridBuffer = new(numGridCells, 8 * sizeof(int));
        }
        #endregion

        private void InitShader()
        {
            int[] gridDimensions = { this.gridSize.x, this.gridSize.y, this.gridSize.z };
            this.shader.SetInts(GridDimensionsID, gridDimensions);
            this.shader.SetInt(GridMaxID, this.gridSize.x * this.gridSize.y * this.gridSize.z);
            this.shader.SetInt(ParticlesPerRigidBodyID, this.particlesPerBody);
            this.shader.SetFloat(ParticleDiameterID, this.particleDiameter);
            this.shader.SetFloat(SpringCoefficientID, this.springCoefficient);
            this.shader.SetFloat(DampingCoefficientID, this.dampingCoefficient);
            this.shader.SetFloat(FrictionCoefficientID, this.frictionCoefficient);
            this.shader.SetFloat(AngularFrictionCoefficientID, this.angularFrictionCoefficient);
            this.shader.SetFloat(GravityCoefficientID, this.gravityCoefficient);
            this.shader.SetFloat(TangentialCoefficientID, this.tangentialCoefficient);
            this.shader.SetFloat(AngularForceScalarID, this.angularForceScalar);
            this.shader.SetFloat(LinearForceScalarID, this.linearForceScalar);
            this.shader.SetFloat(ParticleMassID, this.cubeMass / this.particlesPerBody);
            this.shader.SetInt(ParticleCountID, this.rigidBodyCount * this.particlesPerBody);
            Vector3 halfSize = new Vector3(this.gridSize.x, this.gridSize.y, this.gridSize.z) * this.particleDiameter * 0.5f;
            Vector3 pos = this.gridPosition - halfSize;
            this.shader.SetFloats(GridStartPositionID, pos.x, pos.y, pos.z);

            int particleCount = this.rigidBodyCount * this.particlesPerBody;
            // Get Kernels
            this.kernelGenerateParticleValues = this.shader.FindKernel(GENERATE_KERNEL);
            this.kernelClearGrid = this.shader.FindKernel(CLEAR_KERNEL);
            this.kernelPopulateGrid = this.shader.FindKernel(POPULATE_KERNEL);
            this.kernelCollisionDetectionWithGrid = this.shader.FindKernel(COLLISION_GRID_KERNEL);
            this.kernelComputeMomenta = this.shader.FindKernel(MOMENTA_KERNEL);
            this.kernelComputePositionAndRotation = this.shader.FindKernel(COMPUTE_KERNEL);
            this.kernelCollisionDetection = this.shader.FindKernel(COLLISION_KERNEL);

            // Count Thread Groups
            this.groupsPerRigidBody = Mathf.CeilToInt(this.rigidBodyCount / 8.0f);
            this.groupsPerParticle = Mathf.CeilToInt(particleCount / 8f);
            this.groupsPerGridCell = Mathf.CeilToInt((this.gridSize.x * this.gridSize.y * this.gridSize.z) / 8f);

            // Bind buffers

            // kernel 0 GenerateParticleValues
            this.shader.SetBuffer(this.kernelGenerateParticleValues, RigidBodiesBufferID, this.rigidBodiesBuffer);
            this.shader.SetBuffer(this.kernelGenerateParticleValues, ParticlesBufferID, this.particlesBuffer);

            // kernel 1 ClearGrid
            this.shader.SetBuffer(this.kernelClearGrid, VoxelGridBufferID, this.voxelGridBuffer);

            // kernel 2 Populate Grid
            this.shader.SetBuffer(this.kernelPopulateGrid, VoxelGridBufferID, this.voxelGridBuffer);
            this.shader.SetBuffer(this.kernelPopulateGrid, ParticlesBufferID, this.particlesBuffer);

            // kernel 3 Collision Detection using Grid
            this.shader.SetBuffer(this.kernelCollisionDetectionWithGrid, ParticlesBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.kernelCollisionDetectionWithGrid, VoxelGridBufferID, this.voxelGridBuffer);

            // kernel 4 Computation of Momenta
            this.shader.SetBuffer(this.kernelComputeMomenta, RigidBodiesBufferID, this.rigidBodiesBuffer);
            this.shader.SetBuffer(this.kernelComputeMomenta, ParticlesBufferID, this.particlesBuffer);

            // kernel 5 Compute Position and Rotation
            this.shader.SetBuffer(this.kernelComputePositionAndRotation, RigidBodiesBufferID, this.rigidBodiesBuffer);

            // kernel 6 Collision Detection
            this.shader.SetBuffer(this.kernelCollisionDetection, ParticlesBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.kernelCollisionDetection, VoxelGridBufferID, this.voxelGridBuffer);
        }

        private void InitInstancing()
        {
            // Setup Indirect Renderer
            this.rigidBodyMaterial.SetBuffer(RigidBodiesBufferID, this.rigidBodiesBuffer);

            uint[] args = { VERTEX_COUNT, 1, 0, 0, 0 };
            this.argsBuffer = new(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(args);

            if (!this.debugWireframe) return;

            int numOfParticles = this.rigidBodyCount * this.particlesPerBody;

            uint[] sphereArgs = { SphereMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
            this.argsSphereBuffer = new(1, sphereArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsSphereBuffer.SetData(sphereArgs);

            uint[] lineArgs = { LineMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
            this.argsLineBuffer = new(1, lineArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsLineBuffer.SetData(lineArgs);

            this.sphereMaterial.SetBuffer(ParticlesBufferID, this.particlesBuffer);
            this.sphereMaterial.SetFloat(ScaleID, this.particleDiameter * 0.5f);

            this.lineMaterial.SetBuffer(ParticlesBufferID, this.particlesBuffer);
        }

        private void Update()
        {
            if (this.activeCount < this.rigidBodyCount && this.frameCounter++ > 5)
            {
                this.activeCount++;
                this.frameCounter = 0;
                this.shader.SetInt(ActiveCountID, this.activeCount);
                uint[] args = { VERTEX_COUNT, (uint)this.activeCount, 0, 0, 0 };
                this.argsBuffer.SetData(args);
            }

            float dt = Time.deltaTime / this.stepsPerUpdate;
            this.shader.SetFloat(DeltaTimeID, dt);

            for (int i = 0; i < this.stepsPerUpdate; i++)
            {
                this.shader.Dispatch(this.kernelGenerateParticleValues, this.groupsPerRigidBody, 1, 1);
                if (this.useGrid)
                {
                    this.shader.Dispatch(this.kernelClearGrid, this.groupsPerGridCell, 1, 1);
                    this.shader.Dispatch(this.kernelPopulateGrid, this.groupsPerParticle, 1, 1);
                    this.shader.Dispatch(this.kernelCollisionDetectionWithGrid, this.groupsPerParticle, 1, 1);
                }
                else
                {
                    this.shader.Dispatch(this.kernelCollisionDetection, this.groupsPerParticle, 1, 1);
                }
                this.shader.Dispatch(this.kernelComputeMomenta, this.groupsPerRigidBody, 1, 1);
                this.shader.Dispatch(this.kernelComputePositionAndRotation, this.groupsPerRigidBody, 1, 1);
            }

            if (this.debugWireframe)
            {
                Graphics.DrawMeshInstancedIndirect(SphereMesh, 0, this.sphereMaterial, this.bounds, this.argsSphereBuffer);
                Graphics.DrawMeshInstancedIndirect(LineMesh, 0, this.lineMaterial, this.bounds, this.argsLineBuffer);
            }
            else
            {
                Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.rigidBodyMaterial, this.bounds, this.argsBuffer);
            }
        }

        private void OnDestroy()
        {
            this.rigidBodiesBuffer.Release();
            this.particlesBuffer.Release();

            this.voxelGridBuffer.Release();

            this.argsSphereBuffer?.Release();
            this.argsLineBuffer?.Release();
            this.argsBuffer?.Release();
        }
    }
}