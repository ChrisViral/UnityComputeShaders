using UnityEngine;

namespace UnityComputeShaders
{
    public class GrassBlades : MonoBehaviour
    {
        private struct GrassBlade
        {
            public Vector3 position;
            public float bend;
            public float noise;
            public float fade;

            public GrassBlade( Vector3 pos)
            {
                this.position.x = pos.x;
                this.position.y = pos.y;
                this.position.z = pos.z;
                this.bend = 0;
                this.noise = Random.Range(0.5f, 1) * 2 - 1;
                this.fade = Random.Range(0.5f, 1);
            }
        }

        private int SIZE_GRASS_BLADE = 6 * sizeof(float);

        public Material material;
        public ComputeShader shader;
        public Material visualizeNoise;
        public bool viewNoise;
        [Range(0,1)]
        public float density;
        [Range(0.1f,3)]
        public float scale;
        [Range(10, 45)]
        public float maxBend;
        [Range(0, 2)]
        public float windSpeed;
        [Range(0, 360)]
        public float windDirection;
        [Range(10, 1000)]
        public float windScale;

        private ComputeBuffer bladesBuffer;
        private ComputeBuffer argsBuffer;
        private GrassBlade[] bladesArray;
        private uint[] argsArray = { 0, 0, 0, 0, 0 };
        private Bounds bounds;
        private int timeID;
        private int groupSize;
        private int kernelBendGrass;
        private Mesh blade;
        private Material groundMaterial;

        private Mesh Blade
        {
            get
            {
                Mesh mesh;

                if (this.blade != null)
                {
                    mesh = this.blade;
                }
                else
                {
                    mesh = new();

                    float height = 0.2f;
                    float rowHeight = height / 4;
                    float halfWidth = height / 10;

                    //1. Use the above variables to define the vertices array

                    //2. Define the normals array, hint: each vertex uses the same normal
                    Vector3 normal = new(0, 0, -1);

                    //3. Define the uvs array

                    //4. Define the indices array

                    //5. Assign the mesh properties using the arrays
                    //   for indices use
                    //   mesh.SetIndices( indices, MeshTopology.Triangles, 0 );

                }

                return mesh;
            }
        }
        // Start is called before the first frame update
        private void Start()
        {
            this.bounds = new(Vector3.zero, new(30, 30, 30));
            this.blade = this.Blade;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            this.groundMaterial = renderer.material;

            InitShader();
        }

        private void OnValidate()
        {
            if (this.groundMaterial != null)
            {
                MeshRenderer renderer = GetComponent<MeshRenderer>();

                renderer.material = (this.viewNoise) ? this.visualizeNoise : this.groundMaterial;

                //TO DO: set wind using wind direction, speed and noise scale
                Vector4 wind = new();
                this.shader.SetVector("wind", wind);
                this.visualizeNoise.SetVector("wind", wind);
            }
        }

        private void InitShader()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            Bounds bounds = mf.sharedMesh.bounds;

            Vector3 blades = bounds.extents;
            Vector3 vec = this.transform.localScale / 0.1f * this.density;
            blades.x *= vec.x;
            blades.z *= vec.z;

            int total = (int)blades.x * (int)blades.z * 20;

            this.kernelBendGrass = this.shader.FindKernel("BendGrass");

            this.shader.GetKernelThreadGroupSizes(this.kernelBendGrass, out uint threadGroupSize, out _, out _);
            this.groupSize = Mathf.CeilToInt(total / (float)threadGroupSize);
            int count = this.groupSize * (int)threadGroupSize;

            this.bladesArray = new GrassBlade[count];

            for(int i=0; i<count; i++)
            {
                Vector3 pos = new( Random.value * bounds.extents.x * 2 - bounds.extents.x + bounds.center.x,
                                   0,
                                   Random.value * bounds.extents.z * 2 - bounds.extents.z + bounds.center.z);
                pos = this.transform.TransformPoint(pos);
                this.bladesArray[i] = new(pos);
            }

            this.bladesBuffer = new(count, this.SIZE_GRASS_BLADE);
            this.bladesBuffer.SetData(this.bladesArray);

            this.shader.SetBuffer(this.kernelBendGrass, "bladesBuffer", this.bladesBuffer);
            this.shader.SetFloat("maxBend", this.maxBend * Mathf.PI / 180);
            //TO DO: set wind using wind direction, speed and noise scale
            Vector4 wind = new();
            this.shader.SetVector("wind", wind);

            this.timeID = Shader.PropertyToID("time");

            this.argsArray[0] = this.blade.GetIndexCount(0);
            this.argsArray[1] = (uint)count;
            this.argsBuffer = new(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.argsArray);

            this.material.SetBuffer("bladesBuffer", this.bladesBuffer);
            this.material.SetFloat("_Scale", this.scale);
        }

        // Update is called once per frame
        private void Update()
        {
            this.shader.SetFloat(this.timeID, Time.time);
            this.shader.Dispatch(this.kernelBendGrass, this.groupSize, 1, 1);

            if (!this.viewNoise)
            {
                Graphics.DrawMeshInstancedIndirect(this.blade, 0, this.material, this.bounds, this.argsBuffer);
            }
        }

        private void OnDestroy()
        {
            this.bladesBuffer.Release();
            this.argsBuffer.Release();
        }
    }
}
