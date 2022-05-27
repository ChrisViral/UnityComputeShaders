using UnityEngine;

namespace UnityComputeShaders
{
    public class GrassClumps : MonoBehaviour
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
        [Range(0,1)]
        public float density = 0.8f;
        [Range(0.1f,3)]
        public float scale = 0.2f;
        [Range(10, 45)]
        public float maxLean = 25;

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
            this.clumpsBuffer?.Release();
            this.argsBuffer?.Release();
        }
    }
}
