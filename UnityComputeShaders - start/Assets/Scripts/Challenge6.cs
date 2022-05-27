using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer), typeof(MeshFilter))]
    public class Challenge6 : MonoBehaviour
    {
        private struct GrassClump
        {
            public Vector3 position;
            public float lean;
            public float trample;
            public Quaternion quaternion;
            public float noise;

            public GrassClump(Vector3 position)
            {
                this.position = position;
                this.lean = 0f;
                this.noise = Random.value;
                if (this.noise < 0.5f)
                {
                    this.noise--;
                }
                this.quaternion = Quaternion.identity;
                this.trample = 0f;
            }
        }

        private const int SIZE_GRASS_CLUMP = 10 * sizeof(float);
        private const string GRASS_KERNEL  = "UpdateGrass";

        private static readonly int WindID          = Shader.PropertyToID("wind");
        private static readonly int ScaleID         = Shader.PropertyToID("_Scale");
        private static readonly int ClumpsBufferID  = Shader.PropertyToID("clumpsBuffer");
        private static readonly int MaxLeanID       = Shader.PropertyToID("maxLean");
        private static readonly int TrampleRadiusID = Shader.PropertyToID("trampleRadius");
        private static readonly int TramplePosID    = Shader.PropertyToID("tramplePos");
        private static readonly int TimeID          = Shader.PropertyToID("time");

        [SerializeField]
        private Mesh mesh;
        [SerializeField]
        private Material material;
        [SerializeField]
        private Material visualizeNoise;
        [SerializeField]
        private bool viewNoise;
        [SerializeField]
        private ComputeShader shader;
        [SerializeField, Range(0f, 1f)]
        private float density;
        [SerializeField, Range(0.1f, 3f)]
        private float scale;
        [SerializeField, Range(10f, 45f)]
        private float maxLean;
        [SerializeField]
        private Transform trampler;
        [SerializeField, Range(0.1f, 2f)]
        private float trampleRadius = 0.5f;
        //TO DO: Add wind direction (0-360), speed (0-2)  and scale (10-1000)

        private ComputeBuffer clumpsBuffer;
        private ComputeBuffer argsBuffer;
        private GrassClump[] clumpsArray;
        private readonly uint[] argsArray = new uint[5];
        private Bounds bounds;
        private int groupSize;
        private int kernelUpdateGrass;
        private Vector4 pos;
        private Material groundMaterial;

        // Start is called before the first frame update
        private void Start()
        {
            this.bounds = new(Vector3.zero, new(30f, 30f, 30f));

            this.groundMaterial = GetComponent<MeshRenderer>().material;

            InitShader();
        }

        private void OnValidate()
        {
            if (!this.groundMaterial) return;

            GetComponent<MeshRenderer>().material = this.viewNoise ? this.visualizeNoise : this.groundMaterial;

            //TO DO: Set wind vector
            Vector4 wind = new();
            this.shader.SetVector(WindID, wind);
            this.visualizeNoise.SetVector(WindID, wind);
        }

        private void InitShader()
        {
            Bounds meshBounds = GetComponent<MeshFilter>().sharedMesh.bounds;
            // ReSharper disable once LocalVariableHidesMember
            Vector3 localScale = this.transform.localScale;
            Vector2 size = new(meshBounds.extents.x * localScale.x, meshBounds.extents.z * localScale.z);

            Vector2 clumps = size;
            Vector3 vec = localScale * 10f * this.density;
            clumps.x *= vec.x;
            clumps.y *= vec.z;

            int total = (int)clumps.x * (int)clumps.y;

            this.kernelUpdateGrass = this.shader.FindKernel(GRASS_KERNEL);

            this.shader.GetKernelThreadGroupSizes(this.kernelUpdateGrass, out uint threadGroupSize, out _, out _);
            this.groupSize = Mathf.CeilToInt(total / (float)threadGroupSize);
            int count = this.groupSize * (int)threadGroupSize;

            this.clumpsArray = new GrassClump[count];

            for(int i = 0; i < count; i++)
            {
                Vector3 position = new((Random.value * size.x * 2f) - size.x, 0, (Random.value * size.y * 2f) - size.y);
                this.clumpsArray[i] = new(position);
            }

            this.clumpsBuffer = new(count, SIZE_GRASS_CLUMP);
            this.clumpsBuffer.SetData(this.clumpsArray);

            this.shader.SetBuffer(this.kernelUpdateGrass, ClumpsBufferID, this.clumpsBuffer);
            this.shader.SetFloat(MaxLeanID, this.maxLean * Mathf.PI / 180);
            this.shader.SetFloat(TrampleRadiusID, this.trampleRadius);
            //TO DO: Set wind vector
            Vector4 wind = new();
            this.shader.SetVector(WindID, wind);

            this.argsArray[0] = this.mesh.GetIndexCount(0);
            this.argsArray[1] = (uint)count;
            this.argsBuffer = new(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.argsArray);

            this.material.SetBuffer(ClumpsBufferID, this.clumpsBuffer);
            this.material.SetFloat(ScaleID, this.scale);

            this.visualizeNoise.SetVector(WindID, wind);
        }

        // Update is called once per frame
        private void Update()
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.pos = this.trampler.position;
            this.shader.SetVector(TramplePosID, this.pos);

            this.shader.Dispatch(this.kernelUpdateGrass, this.groupSize, 1, 1);

            if (!this.viewNoise)
            {
                Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, this.bounds, this.argsBuffer);
            }
        }

        private void OnDestroy()
        {
            this.clumpsBuffer?.Release();
            this.argsBuffer?.Release();
        }
    }
}
