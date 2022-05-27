using UnityEngine;

namespace UnityComputeShaders
{
    public class SimplePhysics : MonoBehaviour
    {
        public struct Ball
        {
            public Vector3 position;
            public Vector3 velocity;
            public Color color;

            public Ball(float posRange, float maxVel)
            {
                this.position.x = Random.value * posRange - posRange/2;
                this.position.y = Random.value * posRange;
                this.position.z = Random.value * posRange - posRange / 2;
                this.velocity.x = Random.value * maxVel - maxVel/2;
                this.velocity.y = Random.value * maxVel - maxVel / 2;
                this.velocity.z = Random.value * maxVel - maxVel / 2;
                this.color.r = Random.value;
                this.color.g = Random.value;
                this.color.b = Random.value;
                this.color.a = 1;
            }
        }

        public ComputeShader shader;

        public Mesh ballMesh;
        public Material ballMaterial;
        public int ballsCount;
        public float radius = 0.08f;

        private int kernelHandle;
        private ComputeBuffer ballsBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private Ball[] ballsArray;
        private int groupSizeX;
        private int numOfBalls;
        private Bounds bounds;

        private MaterialPropertyBlock props;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel("CSMain");

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.ballsCount / (float)x);
            this.numOfBalls = this.groupSizeX * (int)x;

            this.props = new();
            this.props.SetFloat("_UniqueID", Random.value);

            this.bounds = new(Vector3.zero, Vector3.one * 1000);

            InitBalls();
            InitShader();
        }

        private void InitBalls()
        {
            this.ballsArray = new Ball[this.numOfBalls];

            for (int i = 0; i < this.numOfBalls; i++)
            {
                this.ballsArray[i] = new(4, 1.0f);
            }
        }

        private void InitShader()
        {
            this.ballsBuffer = new(this.numOfBalls, 10 * sizeof(float));
            this.ballsBuffer.SetData(this.ballsArray);

            this.argsBuffer = new(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            if (this.ballMesh != null)
            {
                this.args[0] = this.ballMesh.GetIndexCount(0);
                this.args[1] = (uint)this.numOfBalls;
                this.args[2] = this.ballMesh.GetIndexStart(0);
                this.args[3] = this.ballMesh.GetBaseVertex(0);
            }
            this.argsBuffer.SetData(this.args);

            this.shader.SetBuffer(this.kernelHandle, "ballsBuffer", this.ballsBuffer);
            this.shader.SetInt("ballsCount", this.numOfBalls);
            this.shader.SetVector("limitsXZ", new(-2.5f+this.radius, 2.5f-this.radius, -2.5f+this.radius, 2.5f-this.radius));
            this.shader.SetFloat("floorY", -2.5f+this.radius);
            this.shader.SetFloat("radius", this.radius);

            this.ballMaterial.SetFloat("_Radius", this.radius*2);
            this.ballMaterial.SetBuffer("ballsBuffer", this.ballsBuffer);
        }

        private void Update()
        {
            int iterations = 5;
            this.shader.SetFloat("deltaTime", Time.deltaTime/iterations);

            for (int i = 0; i < iterations; i++)
            {
                this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);
            }

            Graphics.DrawMeshInstancedIndirect(this.ballMesh, 0, this.ballMaterial, this.bounds, this.argsBuffer, 0, this.props);
        }

        private void OnDestroy()
        {
            this.ballsBuffer?.Dispose();

            this.argsBuffer?.Dispose();
        }
    }
}

