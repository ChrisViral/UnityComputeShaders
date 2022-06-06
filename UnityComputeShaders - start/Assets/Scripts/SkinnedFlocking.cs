using UnityEngine;

namespace UnityComputeShaders
{
    public class SkinnedFlocking : MonoBehaviour
    {
        public struct Boid
        {
            public Vector3 position;
            public Vector3 direction;
            public float noise;
            public float frame;
        }

        private const string KERNEL              = "CSMain";
        private const string FRAME_INTERPOLATION = "FRAME_INTERPOLATION";
        private const int BOID_STRIDE            = 8 * sizeof(float);
        private const int ARGS_STRIDE            = 5 * sizeof(uint);
        private const int VERTEX_STRIDE          = 4 * sizeof(uint);

        private static readonly int BoidsBufferID        = Shader.PropertyToID("boidsBuffer");
        private static readonly int VertexAnimationID    = Shader.PropertyToID("vertexAnimation");
        private static readonly int RotationSpeedID      = Shader.PropertyToID("rotationSpeed");
        private static readonly int BoidSpeedID          = Shader.PropertyToID("boidSpeed");
        private static readonly int BoidSpeedVariationID = Shader.PropertyToID("boidSpeedVariation");
        private static readonly int FlockPositionID      = Shader.PropertyToID("flockPosition");
        private static readonly int NeighbourDistanceID  = Shader.PropertyToID("neighbourDistance");
        private static readonly int BoidFrameSpeedID     = Shader.PropertyToID("boidFrameSpeed");
        private static readonly int BoidsCountID         = Shader.PropertyToID("boidsCount");
        private static readonly int FrameCountID         = Shader.PropertyToID("frameCount");
        private static readonly int TimeID               = Shader.PropertyToID("time");
        private static readonly int DeltaTimeID          = Shader.PropertyToID("deltaTime");
        private static readonly int UniqueID             = Shader.PropertyToID("_UniqueID");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private GameObject boidObject;
        [SerializeField]
        private AnimationClip animationClip;
        [SerializeField]
        private int boidsCount;
        [SerializeField, Range(0.5f, 10f)]
        private float spawnRadius = 10f;
        [SerializeField, Range(0.5f, 10f)]
        private float rotationSpeed = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float boidSpeed = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float neighbourDistance = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float boidSpeedVariation = 1f;
        [SerializeField, Range(0.5f, 10f)]
        private float boidFrameSpeed = 10f;
        [SerializeField]
        private bool frameInterpolation = true;

        private int frameCount;
        private Mesh mesh;
        public Material boidMaterial;
        private int kernelHandle;
        private ComputeBuffer boidsBuffer;
        private ComputeBuffer vertexAnimationBuffer;
        private ComputeBuffer argsBuffer;
        private MaterialPropertyBlock props;
        private readonly uint[] args = new uint[5];
        private Boid[] boids;
        private int groupSizeX;
        private Bounds bounds;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.boidsCount / (float)x);
            this.boidsCount = this.groupSizeX * (int)x;

            this.bounds = new(Vector3.zero, new(1000f, 1000f, 1000f));

            // This property block is used only for avoiding an instancing bug.
            this.props = new();
            this.props.SetFloat(UniqueID, Random.value);

            InitBoids();
            GenerateVertexAnimationBuffer();
            InitShader();
        }

        private void OnDestroy()
        {
            this.boidsBuffer?.Release();
            this.argsBuffer?.Release();
            this.vertexAnimationBuffer?.Release();
        }

        private void Update()
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.SetFloat(DeltaTimeID, Time.deltaTime);

            this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);

            Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.boidMaterial, this.bounds, this.argsBuffer, 0, this.props);
        }

        private void InitBoids()
        {
            this.boids = new Boid[this.boidsCount];
            for (int i = 0; i < this.boidsCount; i++)
            {
                Random.InitState(new System.Random().Next());
                Transform parent    = this.transform;
                Vector3 position    = parent.position + (Random.insideUnitSphere * this.spawnRadius);
                Quaternion rotation = Quaternion.Slerp(parent.rotation, Random.rotation, 0.3f);
                float offset        = Random.Range(0f, 1000f);
                this.boids[i]       = new()
                {
                    position  = position,
                    direction = rotation.eulerAngles,
                    noise     = offset
                };
            }
        }

        private void GenerateVertexAnimationBuffer()
        {
            SkinnedMeshRenderer boidRenderer = this.boidObject.GetComponentInChildren<SkinnedMeshRenderer>();
            this.mesh                        = boidRenderer.sharedMesh;

            Animator boidAnimator   = this.boidObject.GetComponentInChildren<Animator>();
            AnimatorStateInfo state = boidAnimator.GetCurrentAnimatorStateInfo(0);
            Mesh bakedMesh          = new();
            this.frameCount         = Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(this.animationClip.frameRate * this.animationClip.length));
            float frameTime         = this.animationClip.length / this.frameCount;
            float sampleTime        = 0f;

            Vector4[] animationData = new Vector4[this.mesh.vertexCount * this.frameCount];
            for (int i = 0; i < this.frameCount; i++, sampleTime += frameTime)
            {
                boidAnimator.Play(state.shortNameHash, 0, sampleTime);
                boidAnimator.Update(0f);
                boidRenderer.BakeMesh(bakedMesh);

                for (int j = 0; j < bakedMesh.vertexCount; j++)
                {
                    Vector4 vertex = bakedMesh.vertices[j];
                    vertex.w = 1f;
                    animationData[(j * this.frameCount) + i] = vertex;

                }
            }

            this.vertexAnimationBuffer = new(animationData.Length, VERTEX_STRIDE);
            this.vertexAnimationBuffer.SetData(animationData);
            this.boidMaterial.SetBuffer(VertexAnimationID, this.vertexAnimationBuffer);
            this.boidObject.SetActive(false);
        }

        private void InitShader()
        {
            this.boidsBuffer = new(this.boidsCount, BOID_STRIDE);
            this.boidsBuffer.SetData(this.boids);

            this.shader.SetBuffer(this.kernelHandle, BoidsBufferID, this.boidsBuffer);
            this.boidMaterial.SetBuffer(BoidsBufferID, this.boidsBuffer);

            this.shader.SetFloat(RotationSpeedID, this.rotationSpeed);
            this.shader.SetFloat(BoidSpeedID, this.boidSpeed);
            this.shader.SetFloat(BoidSpeedVariationID, this.boidSpeedVariation);
            this.shader.SetVector(FlockPositionID, this.transform.position);
            this.shader.SetFloat(NeighbourDistanceID, this.neighbourDistance);
            this.shader.SetFloat(BoidFrameSpeedID, this.boidFrameSpeed);
            this.shader.SetInt(BoidsCountID, this.boidsCount);
            this.shader.SetInt(FrameCountID, this.frameCount);

            this.boidMaterial.SetInt(FrameCountID, this.frameCount);

            this.argsBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.args[0] = this.mesh.GetIndexCount(0);
            this.args[1] = (uint)this.boidsCount;
            this.argsBuffer.SetData(this.args);



            // Initialize the indirect draw args buffer.

            if (this.boidMaterial.IsKeywordEnabled(FRAME_INTERPOLATION))
            {
                if (!this.frameInterpolation)
                {
                    this.boidMaterial.DisableKeyword(FRAME_INTERPOLATION);
                }
            }
            else if (this.frameInterpolation)
            {
                this.boidMaterial.EnableKeyword(FRAME_INTERPOLATION);
            }
        }
    }
}
