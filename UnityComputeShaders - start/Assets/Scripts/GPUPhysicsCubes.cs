using CjLib;
using UnityEngine;

namespace UnityComputeShaders
{
    public class GPUPhysicsCubes : MonoBehaviour {
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

        public bool debugWireframe;

        // set from editor
        public Mesh cubeMesh {
            get {
                return this.debugWireframe ? PrimitiveMeshFactory.BoxWireframe() : PrimitiveMeshFactory.BoxFlatShaded();
            }
        }
        public Mesh sphereMesh {
            get {
                return PrimitiveMeshFactory.SphereWireframe(6, 6);
            }
        }
        public Mesh lineMesh {
            get {
                return PrimitiveMeshFactory.Line(Vector3.zero, new(1.0f, 1.0f, 1.0f));
            }
        }

        public ComputeShader shader;
        public Material cubeMaterial;
        public Material sphereMaterial;
        public Material lineMaterial;
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
        public Vector3Int gridSize = new(5, 5, 5);
        public Vector3 gridPosition; //centre of grid
        public bool useGrid = true;
        public int rigidBodyCount = 1000;
        [Range(1, 20)]
        public int stepsPerUpdate = 10;

        // calculated
        private Vector3 cubeScale;

        private int particlesPerBody;
        private float particleDiameter;

        private RigidBody[] rigidBodiesArray;
        private Particle[] particlesArray;

        private ComputeBuffer rigidBodiesBuffer;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer argsSphereBuffer;
        private ComputeBuffer argsLineBuffer;
        private ComputeBuffer voxelGridBuffer; // int4

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
        private int deltaTimeID;

        private int activeCount;

        private int frameCounter;

        private void Start() {
            Application.targetFrameRate = 300;

            this.cubeScale = new(this.scale, this.scale, this.scale);

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

            for(int i=0; i<this.rigidBodyCount; i++)
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

            int count = this.rigidBodyCount * this.particlesPerBody;

            this.particlesArray = new Particle[count];

            // initialize buffers
            // initial local particle positions within a rigidbody
            int index = 0;
            float centerer = this.scale * -0.5f + this.particleDiameter * 0.5f;
            Vector3 offset = new(centerer, centerer, centerer);

            for (int x = 0; x < this.particlesPerEdge; x++)
            {
                for (int y = 0; y < this.particlesPerEdge; y++)
                {
                    for (int z = 0; z < this.particlesPerEdge; z++)
                    {
                        Vector3 pos = offset + new Vector3(x, y, z) * this.particleDiameter;
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

            int numGridCells = this.gridSize.x * this.gridSize.y * this.gridSize.z;
            this.voxelGridBuffer = new(numGridCells, 8 * sizeof(int));
        }

        private void InitShader()
        {
            this.deltaTimeID = Shader.PropertyToID("deltaTime");

            int[] gridDimensions = { this.gridSize.x, this.gridSize.y, this.gridSize.z };
            this.shader.SetInts("gridDimensions", gridDimensions);
            this.shader.SetInt("gridMax", this.gridSize.x * this.gridSize.y * this.gridSize.z);
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
            this.shader.SetInt("particleCount", this.rigidBodyCount * this.particlesPerBody);
            Vector3 halfSize = new Vector3(this.gridSize.x, this.gridSize.y, this.gridSize.z) * this.particleDiameter * 0.5f;
            Vector3 pos = this.gridPosition * this.particleDiameter - halfSize;
            this.shader.SetFloats("gridStartPosition", pos.x, pos.y, pos.z);

            int particleCount = this.rigidBodyCount * this.particlesPerBody;
            // Get Kernels
            this.kernelGenerateParticleValues = this.shader.FindKernel("GenerateParticleValues");
            this.kernelClearGrid = this.shader.FindKernel("ClearGrid");
            this.kernelPopulateGrid = this.shader.FindKernel("PopulateGrid");
            this.kernelCollisionDetectionWithGrid = this.shader.FindKernel("CollisionDetectionWithGrid");
            this.kernelComputeMomenta = this.shader.FindKernel("ComputeMomenta");
            this.kernelComputePositionAndRotation = this.shader.FindKernel("ComputePositionAndRotation");
            this.kernelCollisionDetection = this.shader.FindKernel("CollisionDetection");

            // Count Thread Groups
            this.groupsPerRigidBody = Mathf.CeilToInt(this.rigidBodyCount / 8.0f);
            this.groupsPerParticle = Mathf.CeilToInt(particleCount / 8f);
            this.groupsPerGridCell = Mathf.CeilToInt((this.gridSize.x * this.gridSize.y * this.gridSize.z) / 8f);

            // Bind buffers

            // kernel 0 GenerateParticleValues
            this.shader.SetBuffer(this.kernelGenerateParticleValues, "rigidBodiesBuffer", this.rigidBodiesBuffer);
            this.shader.SetBuffer(this.kernelGenerateParticleValues, "particlesBuffer", this.particlesBuffer);

            // kernel 1 ClearGrid
            this.shader.SetBuffer(this.kernelClearGrid, "voxelGridBuffer", this.voxelGridBuffer);

            // kernel 2 Populate Grid
            this.shader.SetBuffer(this.kernelPopulateGrid, "voxelGridBuffer", this.voxelGridBuffer);
            this.shader.SetBuffer(this.kernelPopulateGrid, "particlesBuffer", this.particlesBuffer);

            // kernel 3 Collision Detection using Grid
            this.shader.SetBuffer(this.kernelCollisionDetectionWithGrid, "particlesBuffer", this.particlesBuffer);
            this.shader.SetBuffer(this.kernelCollisionDetectionWithGrid, "voxelGridBuffer", this.voxelGridBuffer);

            // kernel 4 Computation of Momenta
            this.shader.SetBuffer(this.kernelComputeMomenta, "rigidBodiesBuffer", this.rigidBodiesBuffer);
            this.shader.SetBuffer(this.kernelComputeMomenta, "particlesBuffer", this.particlesBuffer);

            // kernel 5 Compute Position and Rotation
            this.shader.SetBuffer(this.kernelComputePositionAndRotation, "rigidBodiesBuffer", this.rigidBodiesBuffer);

            // kernel 6 Collision Detection
            this.shader.SetBuffer(this.kernelCollisionDetection, "particlesBuffer", this.particlesBuffer);
            this.shader.SetBuffer(this.kernelCollisionDetection, "voxelGridBuffer", this.voxelGridBuffer);
        }

        private void InitInstancing() {
            // Setup Indirect Renderer
            this.cubeMaterial.SetBuffer("rigidBodiesBuffer", this.rigidBodiesBuffer);

            uint[] args = { this.cubeMesh.GetIndexCount(0), 1, 0, 0, 0 };
            this.argsBuffer = new(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(args);

            if (this.debugWireframe)
            {
                int numOfParticles = this.rigidBodyCount * this.particlesPerBody;

                uint[] sphereArgs = { this.sphereMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
                this.argsSphereBuffer = new(1, sphereArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                this.argsSphereBuffer.SetData(sphereArgs);

                uint[] lineArgs = { this.lineMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
                this.argsLineBuffer = new(1, lineArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                this.argsLineBuffer.SetData(lineArgs);

                this.sphereMaterial.SetBuffer("particlesBuffer", this.particlesBuffer);
                this.sphereMaterial.SetFloat("scale", this.particleDiameter * 0.5f);

                this.lineMaterial.SetBuffer("particlesBuffer", this.particlesBuffer);
            }
        }

        private void Update() {
            if (this.activeCount<this.rigidBodyCount && this.frameCounter++ > 5) {
                this.activeCount++;
                this.frameCounter = 0;
                this.shader.SetInt("activeCount", this.activeCount);
                uint[] args = { this.cubeMesh.GetIndexCount(0), (uint)this.activeCount, 0, 0, 0 };
                this.argsBuffer.SetData(args);
            }

            float dt = Time.deltaTime/this.stepsPerUpdate;
            this.shader.SetFloat(this.deltaTimeID, dt);

            for (int i=0; i<this.stepsPerUpdate; i++) {
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

            if (this.debugWireframe) {
                Graphics.DrawMeshInstancedIndirect(this.sphereMesh, 0, this.sphereMaterial, this.bounds, this.argsSphereBuffer);
                Graphics.DrawMeshInstancedIndirect(this.lineMesh, 0, this.lineMaterial, this.bounds, this.argsLineBuffer);
            }
            else
            {
                Graphics.DrawMeshInstancedIndirect(this.cubeMesh, 0, this.cubeMaterial, this.bounds, this.argsBuffer);
            }
        }

        private void OnDestroy() {
            this.rigidBodiesBuffer.Release();
            this.particlesBuffer.Release();

            this.voxelGridBuffer.Release();

            this.argsSphereBuffer?.Release();
            this.argsLineBuffer?.Release();
            this.argsBuffer?.Release();
        }
    }
}