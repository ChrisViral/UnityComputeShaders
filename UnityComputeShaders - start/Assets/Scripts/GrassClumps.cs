using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(MeshFilter))]
    public class GrassClumps : MonoBehaviour
    {
        private struct GrassClump
        {
            public Vector3 position;
            public float lean;
            public float noise;
        }

        private const int CLUMP_STRIDE    = 5 * sizeof(float);
        private const int ARGS_STRIDE     = 5 * sizeof(uint);
        private const string GRASS_KERNEL = "LeanGrass";

        private static readonly int ClumpsBufferID = Shader.PropertyToID("clumps");
        private static readonly int MaxLeanID      = Shader.PropertyToID("maxLean");
        private static readonly int TimeID         = Shader.PropertyToID("time");
        private static readonly int ScaleID        = Shader.PropertyToID("_Scale");

        [SerializeField]
        public Mesh mesh;
        [SerializeField]
        public Material material;
        [SerializeField]
        public ComputeShader shader;
        [SerializeField, Range(0f, 1f)]
        public float density = 0.8f;
        [SerializeField, Range(0.1f, 3f)]
        public float scale = 0.2f;
        [SerializeField, Range(10f, 45f)]
        public float maxLean = 25f;

        private ComputeBuffer clumpsBuffer;
        private ComputeBuffer argsBuffer;
        private GrassClump[] clumps;
        private readonly uint[] args   = new uint[5];
        private Bounds bounds;
        private int groupSize;
        private int leanGrassHandle;

        private void Start()
        {
            Random.InitState(new System.Random().Next());
            this.bounds = new(Vector3.zero, new(30f, 30f, 30f));
            InitShader();
        }

        private void OnDestroy()
        {
            this.clumpsBuffer?.Release();
            this.argsBuffer?.Release();
        }

        private void Update()
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.Dispatch(this.leanGrassHandle, this.groupSize, 1, 1);

            Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, this.bounds, this.argsBuffer);
        }

        private void InitShader()
        {
            // ReSharper disable once LocalVariableHidesMember
            Transform transform = this.transform;
            MeshFilter filter   = GetComponent<MeshFilter>();
            Bounds meshBounds   = filter.mesh.bounds;
            Vector3 size        = transform.localScale * 10f * this.density;
            Vector2 dimensions  = new(meshBounds.extents.x * size.x, meshBounds.extents.z * size.z);
            int count           = (int)dimensions.x * (int)dimensions.y;

            this.leanGrassHandle = this.shader.FindKernel(GRASS_KERNEL);
            this.shader.GetKernelThreadGroupSizes(this.leanGrassHandle, out uint x, out _, out _);
            this.groupSize = Mathf.CeilToInt(count / (float)x);
            count = this.groupSize * (int)x;

            this.clumps = new GrassClump[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 position = new(Random.Range(-meshBounds.extents.x, meshBounds.extents.x) + meshBounds.center.x,
                                       0f,
                                       Random.Range(-meshBounds.extents.z, meshBounds.extents.z) + meshBounds.center.z);
                GrassClump clump = new()
                {
                    position = transform.TransformPoint(position),
                    noise    = Random.value
                };
                clump.noise    = clump.noise < 0.5f ? clump.noise - 1f: clump.noise;
                this.clumps[i] = clump;
            }

            this.clumpsBuffer = new(count, CLUMP_STRIDE);
            this.clumpsBuffer.SetData(this.clumps);

            this.shader.SetBuffer(this.leanGrassHandle, ClumpsBufferID, this.clumpsBuffer);
            this.shader.SetFloat(MaxLeanID, this.maxLean * Mathf.Deg2Rad);

            this.material.SetBuffer(ClumpsBufferID, this.clumpsBuffer);
            this.material.SetFloat(ScaleID, this.scale);

            this.args[0] = this.mesh.GetIndexCount(0);
            this.args[0] = (uint)count;
            this.argsBuffer = new(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.args);
        }
    }
}
