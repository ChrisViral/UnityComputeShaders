using CjLib;
using UnityEngine;

namespace UnityComputeShaders
{
    public class GPUPhysicsCompute : MonoBehaviour
    {
        private struct RigidBody
        {
            public Vector3 position;
            public Quaternion quaternion;
            public Vector3 velocity;
            public Vector3 angularVelocity;
            public int particleIndex;
            public int particleCount;

            public RigidBody(Vector3 pos, int pIndex, int pCount)
            {
                this.position = pos;
                this.quaternion = Random.rotation; //Quaternion.identity;
                this.velocity = this.angularVelocity = Vector3.zero;
                this.particleIndex = pIndex;
                this.particleCount = pCount;
            }
        }

        private int SIZE_RIGIDBODY = 13 * sizeof(float) + 2 * sizeof(int);

        private struct Particle
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector3 force;
            public Vector3 localPosition;
            public Vector3 offsetPosition;

            public Particle(Vector3 pos)
            {
                this.position = this.velocity = this.force = this.offsetPosition = Vector3.zero;
                this.localPosition = pos;
            }
        }

        private int SIZE_PARTICLE = 15 * sizeof(float);

        // set from editor
        public Mesh cubeMesh
        {
            get
            {
                return PrimitiveMeshFactory.BoxFlatShaded();
            }
        }

        public ComputeShader shader;
        public Material cubeMaterial;
        public Bounds bounds;
        public float cubeMass;
        public float scale;
        public int particlesPerEdge;
        public float springCoefficient;
        public float dampingCoefficient;
        public float tangentialCoefficient;
        public float gravityCoefficient;
        public float frictionCoefficient;
        public float angularFrictionCoefficient;
        public float angularForceScalar;
        public float linearForceScalar;
        public int rigidBodyCount = 1000;
        [Range(1, 20)]
        public int stepsPerUpdate = 10;

        // calculated
        private int particlesPerBody;
        private float particleDiameter;

        private RigidBody[] rigidBodiesArray;
        private Particle[] particlesArray;
        private uint[] argsArray = { 0, 0, 0, 0, 0 };

        private ComputeBuffer rigidBodiesBuffer;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer argsBuffer;

        private int kernelGenerateParticleValues;
        private int kernelCollisionDetection;
        private int kernelComputeMomenta;
        private int kernelComputePositionAndRotation;

        private int groupsPerRigidBody;
        private int groupsPerParticle;
        private int deltaTimeID;

        private int activeCount;

        private int frameCounter;

        private void Start()
        {
            InitArrays();

            InitRigidBodies();

            InitParticles();

            InitBuffers();

            InitShader();

            InitInstancing();

        }

        private void InitArrays()
        {
            this.particlesPerBody = this.particlesPerEdge * this.particlesPerEdge * this.particlesPerEdge;

            this.rigidBodiesArray = new RigidBody[this.rigidBodyCount];
            this.particlesArray = new Particle[this.rigidBodyCount * this.particlesPerBody];
        }

        private void InitRigidBodies()
        {
            int pIndex = 0;

            for (int i = 0; i < this.rigidBodyCount; i++)
            {
                Vector3 pos = Random.insideUnitSphere * 5.0f;
                pos.y += 15;
                this.rigidBodiesArray[i] = new(pos, pIndex, this.particlesPerBody);
                pIndex += this.particlesPerBody;
            }
        }

        private void InitParticles()
        {
            this.particleDiameter = this.scale / this.particlesPerEdge;

            // initial local particle positions within a rigidbody
            int index = 0;
            float centerer = (this.particleDiameter - this.scale) * 0.5f;
            Vector3 centeringOffset = new(centerer, centerer, centerer);

            for (int x = 0; x < this.particlesPerEdge; x++)
            {
                for (int y = 0; y < this.particlesPerEdge; y++)
                {
                    for (int z = 0; z < this.particlesPerEdge; z++)
                    {
                        Vector3 pos = centeringOffset + new Vector3(x, y, z) * this.particleDiameter;
                        for (int i = 0; i < this.rigidBodyCount; i++)
                        {
                            RigidBody body = this.rigidBodiesArray[i];
                            this.particlesArray[body.particleIndex + index] = new(pos);
                        }
                        index++;
                    }
                }
            }
            Debug.Log("particleCount: " + this.rigidBodyCount * this.particlesPerBody);
        }

        private void InitBuffers()
        {
            this.rigidBodiesBuffer = new(this.rigidBodyCount, this.SIZE_RIGIDBODY);
            this.rigidBodiesBuffer.SetData(this.rigidBodiesArray);

            int numOfParticles = this.rigidBodyCount * this.particlesPerBody;
            this.particlesBuffer = new(numOfParticles, this.SIZE_PARTICLE);
            this.particlesBuffer.SetData(this.particlesArray);
        }

        private void InitShader()
        {
            this.deltaTimeID = Shader.PropertyToID("deltaTime");

            this.shader.SetInt("particlesPerRigidBody", this.particlesPerBody);
            this.shader.SetFloat("particleDiameter", this.particleDiameter);
            this.shader.SetFloat("springCoefficient", this.springCoefficient);
            this.shader.SetFloat("dampingCoefficient", this.dampingCoefficient);
            this.shader.SetFloat("frictionCoefficient", this.frictionCoefficient);
            this.shader.SetFloat("angularFrictionCoefficient", this.angularFrictionCoefficient);
            this.shader.SetFloat("gravityCoefficient", this.gravityCoefficient);
            this.shader.SetFloat("tangentialCoefficient", this.tangentialCoefficient);
            this.shader.SetFloat("angularForceScalar", this.angularForceScalar);
            this.shader.SetFloat("linearForceScalar", this.linearForceScalar);
            this.shader.SetFloat("particleMass", this.cubeMass / this.particlesPerBody);
            int particleCount = this.rigidBodyCount * this.particlesPerBody;
            this.shader.SetInt("particleCount", particleCount);

            // Get Kernels
            this.kernelGenerateParticleValues = this.shader.FindKernel("GenerateParticleValues");
            this.kernelCollisionDetection = this.shader.FindKernel("CollisionDetection");
            this.kernelComputeMomenta = this.shader.FindKernel("ComputeMomenta");
            this.kernelComputePositionAndRotation = this.shader.FindKernel("ComputePositionAndRotation");

            // Count Thread Groups
            this.groupsPerRigidBody = Mathf.CeilToInt(this.rigidBodyCount / 8.0f);
            this.groupsPerParticle = Mathf.CeilToInt(particleCount / 8f);

            // Bind buffers

            // kernel 0 GenerateParticleValues
            this.shader.SetBuffer(this.kernelGenerateParticleValues, "rigidBodiesBuffer", this.rigidBodiesBuffer);
            this.shader.SetBuffer(this.kernelGenerateParticleValues, "particlesBuffer", this.particlesBuffer);

            // kernel 1 Collision Detection
            this.shader.SetBuffer(this.kernelCollisionDetection, "particlesBuffer", this.particlesBuffer);

            // kernel 2 Computation of Momenta
            this.shader.SetBuffer(this.kernelComputeMomenta, "rigidBodiesBuffer", this.rigidBodiesBuffer);
            this.shader.SetBuffer(this.kernelComputeMomenta, "particlesBuffer", this.particlesBuffer);

            // kernel 3 Compute Position and Rotation
            this.shader.SetBuffer(this.kernelComputePositionAndRotation, "rigidBodiesBuffer", this.rigidBodiesBuffer);
        }

        private void InitInstancing()
        {
            // Setup Indirect Renderer
            this.cubeMaterial.SetBuffer("rigidBodiesBuffer", this.rigidBodiesBuffer);

            this.argsArray[0] = this.cubeMesh.GetIndexCount(0);
            this.argsBuffer = new(1, this.argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.argsArray);
        }

        private void Update()
        {
            if (this.activeCount < this.rigidBodyCount && this.frameCounter++ > 5)
            {
                this.activeCount++;
                this.frameCounter = 0;
                this.shader.SetInt("activeCount", this.activeCount);
                this.argsArray[1] = (uint)this.activeCount;
                this.argsBuffer.SetData(this.argsArray);
            }

            float dt = Time.deltaTime / this.stepsPerUpdate;
            this.shader.SetFloat(this.deltaTimeID, dt);

            for (int i = 0; i < this.stepsPerUpdate; i++)
            {
                this.shader.Dispatch(this.kernelGenerateParticleValues, this.groupsPerRigidBody, 1, 1);
                this.shader.Dispatch(this.kernelCollisionDetection, this.groupsPerParticle, 1, 1);
                this.shader.Dispatch(this.kernelComputeMomenta, this.groupsPerRigidBody, 1, 1);
                this.shader.Dispatch(this.kernelComputePositionAndRotation, this.groupsPerRigidBody, 1, 1);
            }

            Graphics.DrawMeshInstancedIndirect(this.cubeMesh, 0, this.cubeMaterial, this.bounds, this.argsBuffer);
        }

        private void OnDestroy()
        {
            this.rigidBodiesBuffer.Release();
            this.particlesBuffer.Release();

            this.argsBuffer?.Release();
        }
    }
}