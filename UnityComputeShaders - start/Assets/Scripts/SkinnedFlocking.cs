using UnityEngine;

namespace UnityComputeShaders
{
    public class SkinnedFlocking : MonoBehaviour {
        public struct Boid
        {
            public Vector3 position;
            public Vector3 direction;
            public float noise_offset;
            public float frame;
        
            public Boid(Vector3 pos, Vector3 dir, float offset)
            {
                this.position.x = pos.x;
                this.position.y = pos.y;
                this.position.z = pos.z;
                this.direction.x = dir.x;
                this.direction.y = dir.y;
                this.direction.z = dir.z;
                this.noise_offset = offset;
                this.frame = 0;
            }
        }

        public ComputeShader shader;

        private SkinnedMeshRenderer boidSMR;
        public GameObject boidObject;
        private Animator animator;
        public AnimationClip animationClip;

        private int numOfFrames;
        public int boidsCount;
        public float spawnRadius;
        public Transform target;
        public float rotationSpeed = 1f;
        public float boidSpeed = 1f;
        public float neighbourDistance = 1f;
        public float boidSpeedVariation = 1f;
        public float boidFrameSpeed = 10f;
        public bool frameInterpolation = true;

        private Mesh boidMesh;
    
        private int kernelHandle;
        private ComputeBuffer boidsBuffer;
        private ComputeBuffer vertexAnimationBuffer;
        public Material boidMaterial;
        private ComputeBuffer argsBuffer;
        private MaterialPropertyBlock props;
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

            // This property block is used only for avoiding an instancing bug.
            this.props = new();
            this.props.SetFloat("_UniqueID", Random.value);

            InitBoids();
            GenerateVertexAnimationBuffer();
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
            // Initialize the indirect draw args buffer.
            this.argsBuffer = new(
                1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments
            );

            if (this.boidMesh) //Set by the GenerateSkinnedAnimationForGPUBuffer
            {
                this.args[0] = this.boidMesh.GetIndexCount(0);
                this.args[1] = (uint)this.numOfBoids;
                this.argsBuffer.SetData(this.args);
            }

            this.boidsBuffer = new(this.numOfBoids, 8 * sizeof(float));
            this.boidsBuffer.SetData(this.boidsArray);

            this.shader.SetFloat("rotationSpeed", this.rotationSpeed);
            this.shader.SetFloat("boidSpeed", this.boidSpeed);
            this.shader.SetFloat("boidSpeedVariation", this.boidSpeedVariation);
            this.shader.SetVector("flockPosition", this.target.transform.position);
            this.shader.SetFloat("neighbourDistance", this.neighbourDistance);
            this.shader.SetFloat("boidFrameSpeed", this.boidFrameSpeed);
            this.shader.SetInt("boidsCount", this.numOfBoids);
            this.shader.SetInt("numOfFrames", this.numOfFrames);
            this.shader.SetBuffer(this.kernelHandle, "boidsBuffer", this.boidsBuffer);

            this.boidMaterial.SetBuffer("boidsBuffer", this.boidsBuffer);
            this.boidMaterial.SetInt("numOfFrames", this.numOfFrames);

            if (this.frameInterpolation && !this.boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
                this.boidMaterial.EnableKeyword("FRAME_INTERPOLATION");
            if (!this.frameInterpolation && this.boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
                this.boidMaterial.DisableKeyword("FRAME_INTERPOLATION");
        }

        private void Update()
        {
            this.shader.SetFloat("time", Time.time);
            this.shader.SetFloat("deltaTime", Time.deltaTime);

            this.shader.Dispatch(this.kernelHandle, this.groupSizeX, 1, 1);

            Graphics.DrawMeshInstancedIndirect( this.boidMesh, 0, this.boidMaterial, this.bounds, this.argsBuffer, 0, this.props);
        }

        private void OnDestroy()
        {
            this.boidsBuffer?.Release();
            this.argsBuffer?.Release();
            this.vertexAnimationBuffer?.Release();
        }

        private void GenerateVertexAnimationBuffer()
        {
            this.boidSMR = this.boidObject.GetComponentInChildren<SkinnedMeshRenderer>();

            this.boidMesh = this.boidSMR.sharedMesh;

            this.boidObject.SetActive(false);
        }
    }
}
