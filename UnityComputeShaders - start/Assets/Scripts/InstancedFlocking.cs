using UnityEngine;

namespace UnityComputeShaders
{
    public class InstancedFlocking : MonoBehaviour
    {
        public struct Boid
        {
            public Vector3 position;
            public Vector3 direction;
            public float noise_offset;

            public Boid(Vector3 pos, Vector3 dir, float offset)
            {
                this.position.x = pos.x;
                this.position.y = pos.y;
                this.position.z = pos.z;
                this.direction.x = dir.x;
                this.direction.y = dir.y;
                this.direction.z = dir.z;
                this.noise_offset = offset;
            }
        }

        private const int SIZE_BOID = 7 * sizeof(float);
    
        public ComputeShader shader;

        public float rotationSpeed = 1f;
        public float boidSpeed = 1f;
        public float neighbourDistance = 1f;
        public float boidSpeedVariation = 1f;
        public Mesh boidMesh;
        public Material boidMaterial;
        public int boidsCount;
        public float spawnRadius;
        public Transform target;

        private int kernelHandle;
        private ComputeBuffer boidsBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private Boid[] boidsArray;
        private int groupSizeX;
        private int numOfBoids;
        private Bounds bounds;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel("CSMain");

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.boidsCount / (float)x);
            this.numOfBoids = this.groupSizeX * (int)x;

            this.bounds = new(Vector3.zero, Vector3.one * 1000);

            InitBoids();
            InitShader();
        }

        private void InitBoids()
        {
            this.boidsArray = new Boid[this.numOfBoids];

            for (int i = 0; i < this.numOfBoids; i++)
            {
                Vector3 pos = this.transform.position + Random.insideUnitSphere * this.spawnRadius;
                Quaternion rot = Quaternion.Slerp(this.transform.rotation, Random.rotation, 0.3f);
                float offset = Random.value * 1000.0f;
                this.boidsArray[i] = new(pos, rot.eulerAngles, offset);
            }
        }

        private void InitShader()
        {
            this.boidsBuffer = new(this.numOfBoids, SIZE_BOID);
            this.boidsBuffer.SetData(this.boidsArray);

            //Initialize args buffer


            this.shader.SetBuffer(this.kernelHandle, "boidsBuffer", this.boidsBuffer);
            this.shader.SetFloat("rotationSpeed", this.rotationSpeed);
            this.shader.SetFloat("boidSpeed", this.boidSpeed);
            this.shader.SetFloat("boidSpeedVariation", this.boidSpeedVariation);
            this.shader.SetVector("flockPosition", this.target.transform.position);
            this.shader.SetFloat("neighbourDistance", this.neighbourDistance);
            this.shader.SetInt("boidsCount", this.numOfBoids);

            this.boidMaterial.SetBuffer("boidsBuffer", this.boidsBuffer);
        }

        private void Update()
        {
            this.shader.SetFloat("time", Time.time);
            this.shader.SetFloat("deltaTime", Time.deltaTime);

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

