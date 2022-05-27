using CjLib;
using UnityEngine;

namespace UnityComputeShaders
{
    public class GPUPhysics : MonoBehaviour {
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

        public Mesh cubeMesh {
            get {
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
        private Vector3 cubeScale;

        private int particlesPerBody;
        private float particleDiameter;

        private RigidBody[] rigidBodiesArray;
        private Particle[] particlesArray;
        private uint[] argsArray;

        private ComputeBuffer rigidBodiesBuffer;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer voxelGridBuffer;               
	
        private int kernelGenerateParticleValues;
        private int kernelCollisionDetection;
        private int kernelComputeMomenta;
        private int kernelComputePositionAndRotation;
	
        private int groupsPerRigidBody;
        private int groupsPerParticle;

        private int deltaTimeID;

        private int activeCount = 0;

        private int frameCounter;

        private void Start() {

            InitArrays();

            InitRigidBodies();

            InitParticles();

            InitBuffers();

            InitShader();
		
            InitInstancing();
		
        }

        private void InitArrays()
        {
		
        }

        private void InitRigidBodies()
        {
		
        }

        private void InitParticles()
        {
		
        }

        private void InitBuffers()
        {
		
        }

        private void InitShader()
        {
			
        }

        private void InitInstancing() {
		
        }

        private void Update() {
            Graphics.DrawMeshInstancedIndirect(this.cubeMesh, 0, this.cubeMaterial, this.bounds, this.argsBuffer);
        }

        private void OnDestroy() {
            this.rigidBodiesBuffer.Release();
            this.particlesBuffer.Release();

            this.argsBuffer?.Release();
        }
    }
}