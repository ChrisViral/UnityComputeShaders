using UnityEngine;

namespace UnityComputeShaders
{
    public class SPHFluid : MonoBehaviour
    {
        private struct SPHParticle
        {
            public Vector3 position;

            public Vector3 velocity;
            public Vector3 force;
        
            public float density;
            public float pressure;

            public SPHParticle(Vector3 pos)
            {
                this.position = pos;
                this.velocity = Vector3.zero;
                this.force = Vector3.zero;
                this.density = 0.0f;
                this.pressure = 0.0f;
            }
        }

        private int SIZE_SPHPARTICLE = 11 * sizeof(float);

        private struct SPHCollider
        {
            public Vector3 position;
            public Vector3 right;
            public Vector3 up;
            public Vector2 scale;

            public SPHCollider(Transform _transform)
            {
                this.position = _transform.position;
                this.right = _transform.right;
                this.up = _transform.up;
                this.scale = new(_transform.lossyScale.x / 2f, _transform.lossyScale.y / 2f);
            }
        }

        private int SIZE_SPHCOLLIDER = 11 * sizeof(float);

        public float particleRadius = 1;
        public float smoothingRadius = 1;
        public float restDensity = 15;
        public float particleMass = 0.1f;
        public float particleViscosity = 1;
        public float particleDrag = 0.025f;
        public Mesh particleMesh;
        public int particleCount = 5000;
        public int rowSize = 100;
        public ComputeShader shader;
        public Material material;

        // Consts
        private static Vector4 GRAVITY = new(0.0f, -9.81f, 0.0f, 2000.0f);
        private const float DT = 0.0008f;
        private const float BOUND_DAMPING = -0.5f;
        private const float GAS = 2000.0f;

        private float smoothingRadiusSq;

        // Data
        private SPHParticle[] particlesArray;
        private ComputeBuffer particlesBuffer;
        private SPHCollider[] collidersArray;
        private ComputeBuffer collidersBuffer;
        private uint[] argsArray = { 0, 0, 0, 0, 0 };
        private ComputeBuffer argsBuffer;

        private Bounds bounds = new(Vector3.zero, Vector3.one * 0);

        private int kernelComputeDensityPressure;
        private int kernelComputeForces;
        private int kernelIntegrate;
        private int kernelComputeColliders;

        private int groupSize;
    
        private void Start()
        {
            InitSPH();
            InitShader();
        }

        private void UpdateColliders()
        {
            // Get colliders
            GameObject[] collidersGO = GameObject.FindGameObjectsWithTag("SPHCollider");
            if (this.collidersArray == null || this.collidersArray.Length != collidersGO.Length)
            {
                this.collidersArray = new SPHCollider[collidersGO.Length];
                this.collidersBuffer?.Dispose();
                this.collidersBuffer = new(this.collidersArray.Length, this.SIZE_SPHCOLLIDER);
            }
            for (int i = 0; i < this.collidersArray.Length; i++)
            {
                this.collidersArray[i] = new(collidersGO[i].transform);
            }
            this.collidersBuffer.SetData(this.collidersArray);
            this.shader.SetBuffer(this.kernelComputeColliders, "colliders", this.collidersBuffer);
        }

        private void Update()
        {
            UpdateColliders();

            this.shader.Dispatch(this.kernelComputeDensityPressure, this.groupSize, 1, 1);
            this.shader.Dispatch(this.kernelComputeForces, this.groupSize, 1, 1);
            this.shader.Dispatch(this.kernelIntegrate, this.groupSize, 1, 1);
            this.shader.Dispatch(this.kernelComputeColliders, this.groupSize, 1, 1);

            Graphics.DrawMeshInstancedIndirect(this.particleMesh, 0, this.material, this.bounds, this.argsBuffer);
        }

        private void InitShader()
        {
            this.kernelComputeForces = this.shader.FindKernel("ComputeForces");
            this.kernelIntegrate = this.shader.FindKernel("Integrate");
            this.kernelComputeColliders = this.shader.FindKernel("ComputeColliders");

            float smoothingRadiusSq = this.smoothingRadius * this.smoothingRadius;

            this.particlesBuffer = new(this.particlesArray.Length, this.SIZE_SPHPARTICLE);
            this.particlesBuffer.SetData(this.particlesArray);

            UpdateColliders();

            this.argsArray[0] = this.particleMesh.GetIndexCount(0);
            this.argsArray[1] = (uint)this.particlesArray.Length;
            this.argsBuffer = new(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.argsArray);

            this.shader.SetInt("particleCount", this.particlesArray.Length);
            this.shader.SetInt("colliderCount", this.collidersArray.Length);
            this.shader.SetFloat("smoothingRadius", this.smoothingRadius);
            this.shader.SetFloat("smoothingRadiusSq", smoothingRadiusSq);
            this.shader.SetFloat("gas", GAS);
            this.shader.SetFloat("restDensity", this.restDensity);
            this.shader.SetFloat("radius", this.particleRadius);
            this.shader.SetFloat("mass", this.particleMass);
            this.shader.SetFloat("particleDrag", this.particleDrag);
            this.shader.SetFloat("particleViscosity", this.particleViscosity);
            this.shader.SetFloat("damping", BOUND_DAMPING);
            this.shader.SetFloat("deltaTime", DT);
            this.shader.SetVector("gravity", GRAVITY);

            this.shader.SetBuffer(this.kernelComputeDensityPressure, "particles", this.particlesBuffer);
            this.shader.SetBuffer(this.kernelComputeForces, "particles", this.particlesBuffer);
            this.shader.SetBuffer(this.kernelIntegrate, "particles", this.particlesBuffer);
            this.shader.SetBuffer(this.kernelComputeColliders, "particles", this.particlesBuffer);
            this.shader.SetBuffer(this.kernelComputeColliders, "colliders", this.collidersBuffer);

            this.material.SetBuffer("particles", this.particlesBuffer);
            this.material.SetFloat("_Radius", this.particleRadius);
        }

        private void InitSPH()
        {
            this.kernelComputeDensityPressure = this.shader.FindKernel("ComputeDensityPressure");

            this.shader.GetKernelThreadGroupSizes(this.kernelComputeDensityPressure, out uint numThreadsX, out _, out _);
            this.groupSize = Mathf.CeilToInt(this.particleCount / (float)numThreadsX);
            int amount = (int)numThreadsX * this.groupSize;

            this.particlesArray = new SPHParticle[amount];
            float size = this.particleRadius * 1.1f;
            float center = this.rowSize * 0.5f;

            for (int i = 0; i < amount; i++)
            {
                Vector3 pos = new()
                {
                    x = (i % this.rowSize) + Random.Range(-0.1f, 0.1f) - center,
                    y = 2 + (i / this.rowSize) / this.rowSize * 1.1f,
                    z = ((i / this.rowSize) % this.rowSize) + Random.Range(-0.1f, 0.1f) - center
                };
                pos *= this.particleRadius;

                this.particlesArray[i] = new( pos );
            }
        }

        private void OnDestroy()
        {
            this.particlesBuffer.Dispose();
            this.collidersBuffer.Dispose();
            this.argsBuffer.Dispose();
        }
    }
}
