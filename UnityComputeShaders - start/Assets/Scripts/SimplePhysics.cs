using UnityEngine;

namespace UnityComputeShaders
{
    public class SimplePhysics : MonoBehaviour
    {
        private struct Ball
        {
            public Vector3 position;
            public Vector3 velocity;
            public Color color;
        }

        private const string KERNEL   = "CSMain";
        private const int BALL_STRIDE = 10 * sizeof(float);
        private const int ARGS_STRIDE = 5  * sizeof(uint);
        private const int ITERATIONS  = 5;

        private static readonly int UniqueID      = Shader.PropertyToID("_UniqueID");
        private static readonly int BallsBufferID = Shader.PropertyToID("ballsBuffer");
        private static readonly int BallsCountID  = Shader.PropertyToID("ballsCount");
        private static readonly int LimitsID      = Shader.PropertyToID("limits");
        private static readonly int FloorID       = Shader.PropertyToID("floorY");
        private static readonly int RadiusID      = Shader.PropertyToID("radius");
        private static readonly int DeltaTimeID   = Shader.PropertyToID("deltaTime");
        // ReSharper disable once InconsistentNaming
        private static readonly int _RadiusID     = Shader.PropertyToID("_Radius");

        [SerializeField]
        public ComputeShader shader;
        [SerializeField]
        public Mesh mesh;
        [SerializeField]
        public Material ballMaterial;
        [SerializeField]
        public int ballsCount;
        [SerializeField, Range(0.01f, 3f)]
        public float radius = 0.08f;

        private int kernelHandle;
        private ComputeBuffer ballsBuffer;
        private ComputeBuffer argsBuffer;
        private readonly uint[] args = new uint[5];
        private Ball[] balls;
        private int groupSizeX;
        private Bounds bounds;
        private MaterialPropertyBlock props;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.ballsCount / (float)x);
            this.ballsCount = this.groupSizeX * (int)x;

            this.props = new();
            this.props.SetFloat(UniqueID, Random.value);
            this.bounds = new(Vector3.zero, new(1000f, 1000f, 1000f));

            InitBalls();
            InitShader();
        }

        private void OnDestroy()
        {
            this.ballsBuffer?.Dispose();
            this.argsBuffer?.Dispose();
        }

        private void Update()
        {
            this.shader.SetFloat(DeltaTimeID, Time.deltaTime / ITERATIONS);

            for (int i = 0; i < ITERATIONS; i++)
            {
                this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);
            }

            Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.ballMaterial, this.bounds, this.argsBuffer, 0, this.props);
        }

        private void InitBalls()
        {
            this.balls = new Ball[this.ballsCount];
            Random.InitState(new System.Random().Next());
            for (int i = 0; i < this.ballsCount; i++)
            {
                Vector3 position = Random.insideUnitSphere * 2f;
                position.y       = Mathf.Abs(position.y);
                this.balls[i] = new()
                {
                    position = position,
                    velocity = Random.onUnitSphere,
                    color    = Random.ColorHSV(0f, 1f, 0.75f, 1f, 0.5f, 1f)
                };
            }
        }

        private void InitShader()
        {
            this.ballsBuffer = new(this.ballsCount, BALL_STRIDE);
            this.ballsBuffer.SetData(this.balls);

            this.shader.SetBuffer(this.kernelHandle, BallsBufferID, this.ballsBuffer);
            this.ballMaterial.SetBuffer(BallsBufferID, this.ballsBuffer);

            this.shader.SetInt(BallsCountID, this.ballsCount);
            this.shader.SetVector(LimitsID, new(-2.5f + this.radius, 2.5f - this.radius, -2.5f + this.radius, 2.5f - this.radius));
            this.shader.SetFloat(FloorID, -2.5f + this.radius);
            this.shader.SetFloat(RadiusID, this.radius);

            this.ballMaterial.SetFloat(_RadiusID, this.radius * 2f);

            this.argsBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.args[0]    = this.mesh.GetIndexCount(0);
            this.args[1]    = (uint)this.ballsCount;
            this.args[2]    = this.mesh.GetIndexStart(0);
            this.args[3]    = this.mesh.GetBaseVertex(0);
            this.argsBuffer.SetData(this.args);
        }
    }
}

