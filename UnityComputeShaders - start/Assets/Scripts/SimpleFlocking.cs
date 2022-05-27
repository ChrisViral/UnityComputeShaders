using UnityEngine;

namespace UnityComputeShaders
{
    public class SimpleFlocking : MonoBehaviour
    {
        public struct Boid
        {
            public Vector3 position;
            public Vector3 direction;
        
            public Boid(Vector3 pos)
            {
                this.position.x = pos.x;
                this.position.y = pos.y;
                this.position.z = pos.z;
                this.direction.x = 0;
                this.direction.y = 0;
                this.direction.z = 0;
            }
        }

        public ComputeShader shader;

        public float rotationSpeed = 1f;
        public float boidSpeed = 1f;
        public float neighbourDistance = 1f;
        public float boidSpeedVariation = 1f;
        public GameObject boidPrefab;
        public int boidsCount;
        public float spawnRadius;
        public Transform target;

        private int kernelHandle;
        private ComputeBuffer boidsBuffer;
        private Boid[] boidsArray;
        private GameObject[] boids;
        private int groupSizeX;
        private int numOfBoids;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel("CSMain");

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.boidsCount / (float)x);
            this.numOfBoids = this.groupSizeX * (int)x;

            InitBoids();
            InitShader();
        }

        private void InitBoids()
        {
            this.boids = new GameObject[this.numOfBoids];
            this.boidsArray = new Boid[this.numOfBoids];

            for (int i = 0; i < this.numOfBoids; i++)
            {
                Vector3 pos = this.transform.position + Random.insideUnitSphere * this.spawnRadius;
                this.boidsArray[i] = new(pos);
                this.boids[i] = Instantiate(this.boidPrefab, pos, Quaternion.identity);
                this.boidsArray[i].direction = this.boids[i].transform.forward;
            }
        }

        private void InitShader()
        {
            this.boidsBuffer = new(this.numOfBoids, 6 * sizeof(float));
            this.boidsBuffer.SetData(this.boidsArray);

            this.shader.SetBuffer(this.kernelHandle, "boidsBuffer", this.boidsBuffer);
            this.shader.SetFloat("rotationSpeed", this.rotationSpeed);
            this.shader.SetFloat("boidSpeed", this.boidSpeed);
            this.shader.SetFloat("boidSpeedVariation", this.boidSpeedVariation);
            this.shader.SetVector("flockPosition", this.target.transform.position);
            this.shader.SetFloat("neighbourDistance", this.neighbourDistance);
            this.shader.SetInt("boidsCount", this.boidsCount);
        }

        private void Update()
        {
            this.shader.SetFloat("time", Time.time);
            this.shader.SetFloat("deltaTime", Time.deltaTime);

            this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);

            this.boidsBuffer.GetData(this.boidsArray);

            for (int i = 0; i < this.boidsArray.Length; i++)
            {
                this.boids[i].transform.localPosition = this.boidsArray[i].position;

                if (!this.boidsArray[i].direction.Equals(Vector3.zero))
                {
                    this.boids[i].transform.rotation = Quaternion.LookRotation(this.boidsArray[i].direction);
                }

            }
        }

        private void OnDestroy()
        {
            this.boidsBuffer?.Dispose();
        }
    }
}

