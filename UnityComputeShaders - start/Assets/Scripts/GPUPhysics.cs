using System;
using CjLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityComputeShaders
{
    public class GPUPhysics : MonoBehaviour
    {
        private struct Vector4Int
        {
            public int x;
            public int y;
            public int z;
            public int w;

            public Vector4Int(int x, int y, int z, int w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }
        }

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

        private struct Voxel
        {
            public Vector4Int vox1;
            public Vector4Int vox2;
        }

        private const int RIGIDBODY_STRIDE    = (13 * sizeof(float)) + sizeof(int);
        private const int PARTICLE_STRIDE     = 15 * sizeof(float);
        private const int ARGS_STRIDE         = 5 * sizeof(uint);
        private const int VOXEL_STRIDE        = 8 * sizeof(int);
        private const string GENERATE_KERNEL  = "GenerateParticleValues";
        private const string COLLISION_KERNEL = "CollisionDetection";
        private const string MOMENTA_KERNEL   = "ComputeMomenta";
        private const string COMPUTE_KERNEL   = "ComputePositionAndRotation";
        private const float STANDARD_GRAVITY  = 9.80665f;

        private static readonly int RigidBodiesBufferID          = Shader.PropertyToID("rigidbodies");
        private static readonly int ParticlesBufferID            = Shader.PropertyToID("particles");
        private static readonly int VoxelGridBufferID            = Shader.PropertyToID("voxels");

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

        #region
        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private Material cubeMaterial;
        [SerializeField]
        private Bounds bounds;
        [SerializeField, Range(0.1f, 10f)]
        private float cubeMass;
        [SerializeField, Range(0.1f, 10f)]
        private float scale;
        [SerializeField, Range(2, 10)]
        private int particlesPerEdge;
        [SerializeField, Range(0.1f, 10f)]
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
        [SerializeField]
        private int rigidbodyCount = 1000;
        [SerializeField, Range(1, 20)]
        private int stepsPerUpdate = 10;

        private Mesh mesh;
        private Vector3 cubeScale;
        private int particlesPerBody;
        private float particleDiameter;
        private int activeCount;
        private int frameCounter;

        private Rigidbody[] rigidbodies;
        private Particle[] particles;
        private readonly uint[] args = new uint[5];
        private Voxel[] voxels;
        private ComputeBuffer rigidbodiesBuffer;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer voxelGridBuffer;

        private int generateParticleValuesHandle;
        private int collisionDetectionHandle;
        private int computeMomentaHandle;
        private int computePositionAndRotationHandle;
        private int groupsPerRigidbody;
        private int groupsPerParticle;
        #endregion

        #region Functions
        private void Awake()
        {
            Random.InitState(new System.Random().Next());
            this.mesh = PrimitiveMeshFactory.BoxFlatShaded();
        }

        private void Start()
        {
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
            this.argsBuffer?.Release();
            this.voxelGridBuffer?.Release();
        }

        private void Update()
        {

            Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.cubeMaterial, this.bounds, this.argsBuffer);
        }

        private void FixedUpdate()
        {
            if (this.activeCount < this.rigidbodyCount && this.frameCounter++ > 5)
            {
                this.activeCount++;
                this.frameCounter = 0;
                this.shader.SetInt(ActiveCountID, this.activeCount);

                this.args[1] = (uint)this.activeCount;
                this.argsBuffer.SetData(this.args);
            }

            this.shader.SetFloat(DeltaTimeID, Time.fixedDeltaTime / this.stepsPerUpdate);

            for (int i = 0; i < this.stepsPerUpdate; i++)
            {
                this.shader.Dispatch(this.generateParticleValuesHandle, this.groupsPerRigidbody, 1, 1);
                this.shader.Dispatch(this.collisionDetectionHandle, this.groupsPerParticle, 1, 1);
                this.shader.Dispatch(this.computeMomentaHandle, this.groupsPerRigidbody, 1, 1);
                this.shader.Dispatch(this.computePositionAndRotationHandle, this.groupsPerRigidbody, 1, 1);
            }
        }
        #endregion

        #region Init
        private void InitArrays()
        {
            this.particlesPerBody = this.particlesPerEdge * this.particlesPerEdge * this.particlesPerEdge;
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
            this.particleDiameter = this.scale / this.particlesPerEdge;
            float center = (this.particleDiameter - this.scale) / 2f;
            Vector3 offset = new(center, center, center);

            for (int x = 0, i = 0; x < this.particlesPerEdge; x++)
            {
                for (int y = 0; y < this.particlesPerEdge; y++)
                {
                    for (int z = 0; z < this.particlesPerEdge; z++, i++)
                    {
                        Particle particle = new()
                        {
                            localPosition = offset + (new Vector3(x, y, z) * this.particleDiameter)
                        };

                        foreach (Rigidbody body in this.rigidbodies)
                        {
                            this.particles[body.particleOffset + i] = particle;
                        }
                    }
                }
            }
        }

        private void InitBuffers()
        {
            this.rigidbodiesBuffer = new(this.rigidbodyCount, RIGIDBODY_STRIDE);
            this.rigidbodiesBuffer.SetData(this.rigidbodies);

            this.particlesBuffer = new(this.particles.Length, PARTICLE_STRIDE);
            this.particlesBuffer.SetData(this.particles);
        }

        private void InitKernels()
        {
            this.generateParticleValuesHandle     = this.shader.FindKernel(GENERATE_KERNEL);
            this.collisionDetectionHandle         = this.shader.FindKernel(COLLISION_KERNEL);
            this.computeMomentaHandle             = this.shader.FindKernel(MOMENTA_KERNEL);
            this.computePositionAndRotationHandle = this.shader.FindKernel(COMPUTE_KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.generateParticleValuesHandle, out uint x, out _, out _);
            this.groupsPerRigidbody = Mathf.CeilToInt(this.rigidbodyCount / (float)x);

            this.shader.GetKernelThreadGroupSizes(this.collisionDetectionHandle, out x, out _, out _);
            this.groupsPerParticle = Mathf.CeilToInt(this.particles.Length / (float)x);
        }

        private void InitShader()
        {
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
            this.shader.SetBuffer(this.collisionDetectionHandle, ParticlesBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.computeMomentaHandle, RigidBodiesBufferID, this.rigidbodiesBuffer);
            this.shader.SetBuffer(this.computeMomentaHandle, ParticlesBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.computePositionAndRotationHandle, RigidBodiesBufferID, this.rigidbodiesBuffer);
        }

        private void InitInstancing()
        {
            this.cubeMaterial.SetBuffer(RigidBodiesBufferID, this.rigidbodiesBuffer);

            this.args[0] = this.mesh.GetIndexCount(0);
            this.argsBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.args);
        }
        #endregion
    }
}