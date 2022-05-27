using UnityEngine;

namespace UnityComputeShaders
{
    public class GrassTrample : MonoBehaviour
    {
        private struct GrassClump
        {
            public Vector3 position;
            public float lean;
            public float trample;
            public Quaternion quaternion;
            public float noise;

            public GrassClump( Vector3 pos)
            {
                this.position.x = pos.x;
                this.position.y = pos.y;
                this.position.z = pos.z;
                this.lean = 0;
                this.noise = Random.Range(0.5f, 1);
                if (Random.value < 0.5f) this.noise = -this.noise;
                this.trample = 0;
                this.quaternion = Quaternion.identity;
            }
        }

        private int SIZE_GRASS_CLUMP = 10 * sizeof(float);

        public Mesh mesh;
        public Material material;
        public ComputeShader shader;
        [Range(0,1)]
        public float density;
        [Range(0.1f,3)]
        public float scale;
        [Range(0.5f, 3)]
        public float speed;
        [Range(10, 45)]
        public float maxLean;
        public Transform trampler;
        [Range(0.1f,2)]
        public float trampleRadius = 0.5f;

        private ComputeBuffer clumpsBuffer;
        private ComputeBuffer argsBuffer;
        private GrassClump[] clumpsArray;
        private uint[] argsArray = { 0, 0, 0, 0, 0 };
        private Bounds bounds;
        private int timeID;
        private int tramplePosID;
        private int groupSize;
        private int kernelUpdateGrass;
        private Vector4 pos;

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
            Vector2 size = new(bounds.extents.x * this.transform.localScale.x, bounds.extents.z * this.transform.localScale.z);
        
            Vector2 clumps = size;
            Vector3 vec = this.transform.localScale / 0.1f * this.density;
            clumps.x *= vec.x;
            clumps.y *= vec.z;

            int total = (int)clumps.x * (int)clumps.y;

            this.kernelUpdateGrass = this.shader.FindKernel("UpdateGrass");

            this.shader.GetKernelThreadGroupSizes(this.kernelUpdateGrass, out uint threadGroupSize, out _, out _);
            this.groupSize = Mathf.CeilToInt(total / (float)threadGroupSize);
            int count = this.groupSize * (int)threadGroupSize;

            this.clumpsArray = new GrassClump[count];

            for(int i=0; i<count; i++)
            {
                Vector3 pos = new(Random.value * size.x * 2 - size.x, 0, Random.value * size.y * 2 - size.y);
                this.clumpsArray[i] = new(pos);
            }

            this.clumpsBuffer = new(count, this.SIZE_GRASS_CLUMP);
            this.clumpsBuffer.SetData(this.clumpsArray);

            this.shader.SetBuffer(this.kernelUpdateGrass, "clumpsBuffer", this.clumpsBuffer);
            this.shader.SetFloat("maxLean", this.maxLean * Mathf.PI / 180);
            this.shader.SetFloat("trampleRadius", this.trampleRadius);
            this.shader.SetFloat("speed", this.speed);
            this.timeID = Shader.PropertyToID("time");
            this.tramplePosID = Shader.PropertyToID("tramplePos");

            this.argsArray[0] = this.mesh.GetIndexCount(0);
            this.argsArray[1] = (uint)count;
            this.argsBuffer = new(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.argsArray);

            this.material.SetBuffer("clumpsBuffer", this.clumpsBuffer);
            this.material.SetFloat("_Scale", this.scale);
        }

        // Update is called once per frame
        private void Update()
        {
            this.shader.SetFloat(this.timeID, Time.time);
            this.pos = this.trampler.position;
            this.shader.SetVector(this.tramplePosID, this.pos);

            this.shader.Dispatch(this.kernelUpdateGrass, this.groupSize, 1, 1);

            Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, this.bounds, this.argsBuffer);
        }

        private void OnDestroy()
        {
            this.clumpsBuffer.Release();
            this.argsBuffer.Release();
        }
    }
}
