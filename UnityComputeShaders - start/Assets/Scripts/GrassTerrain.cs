using UnityEngine;

namespace UnityComputeShaders
{
    public class GrassTerrain : MonoBehaviour
    {
        private struct GrassClump
        {
            public Vector3 position;
            public float lean;
            public float noise;

            public GrassClump( Vector3 pos)
            {
                this.position.x = pos.x;
                this.position.y = pos.y;
                this.position.z = pos.z;
                this.lean = 0;
                this.noise = Random.Range(0.5f, 1);
                if (Random.value < 0.5f) this.noise = -this.noise;
            }
        }

        private int SIZE_GRASS_CLUMP = 5 * sizeof(float);

        public Mesh mesh;
        public Material material;
        public ComputeShader shader;
        [Range(0,3)]
        public float density = 0.8f;
        [Range(0.1f,3)]
        public float scale = 0.2f;
        [Range(10, 45)]
        public float maxLean = 25;
        [Range(0, 1)]
        public float heightAffect = 0.5f;

        private ComputeBuffer clumpsBuffer;
        private ComputeBuffer argsBuffer;
        private GrassClump[] clumpsArray;
        private uint[] argsArray = { 0, 0, 0, 0, 0 };
        private Bounds bounds;
        private int timeID;
        private int groupSize;
        private int kernelLeanGrass;

        // Start is called before the first frame update
        private void Start()
        {
            this.bounds = new(Vector3.zero, new(30, 30, 30));
            InitShader();
        }

        private void InitShader()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            Bounds bounds = mf.sharedMesh.bounds;

            Vector3 clumps = bounds.extents;
            Vector3 vec = this.transform.localScale / 0.1f * this.density;
            clumps.x *= vec.x;
            clumps.z *= vec.z;

            int total = (int)clumps.x * (int)clumps.z;

            this.kernelLeanGrass = this.shader.FindKernel("LeanGrass");

            this.shader.GetKernelThreadGroupSizes(this.kernelLeanGrass, out uint threadGroupSize, out _, out _);
            this.groupSize = Mathf.CeilToInt(total / (float)threadGroupSize);
            int count = this.groupSize * (int)threadGroupSize;

            InitPositionsArray(count, bounds);

            count = this.clumpsArray.Length;

            this.clumpsBuffer = new(count, this.SIZE_GRASS_CLUMP);
            this.clumpsBuffer.SetData(this.clumpsArray);

            this.shader.SetBuffer(this.kernelLeanGrass, "clumpsBuffer", this.clumpsBuffer);
            this.shader.SetFloat("maxLean", this.maxLean * Mathf.PI / 180);
            this.timeID = Shader.PropertyToID("time");

            this.argsArray[0] = this.mesh.GetIndexCount(0);
            this.argsArray[1] = (uint)count;
            this.argsBuffer = new(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.argsArray);

            this.material.SetBuffer("clumpsBuffer", this.clumpsBuffer);
            this.material.SetFloat("_Scale", this.scale);
        }

        private void InitPositionsArray(int count, Bounds bounds)
        {
            this.clumpsArray = new GrassClump[count];
        }

        // Update is called once per frame
        private void Update()
        {
            this.shader.SetFloat(this.timeID, Time.time);
            this.shader.Dispatch(this.kernelLeanGrass, this.groupSize, 1, 1);

            Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, this.bounds, this.argsBuffer);
        }

        private void OnDestroy()
        {
            this.clumpsBuffer.Release();
            this.argsBuffer.Release();
        }
    }
}
