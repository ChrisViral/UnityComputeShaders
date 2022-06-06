using UnityEngine;

namespace UnityComputeShaders
{
    public class QuadParticles : MonoBehaviour
    {
        #pragma warning disable 0649
        // ReSharper disable NotAccessedField.Local
        private struct Particle
        {
            public Vector3 position;
            public Vector3 velocity;
            public float life;
        }

        private struct Vertex
        {
            public Vector3 position;
            public Vector2 uv;
            public float life;
        }
        // ReSharper restore NotAccessedField.Local
        #pragma warning restore 0649

        private const string KERNEL     = "CSParticle";
        private const int SIZE_PARTICLE = 7 * sizeof(float);
        private const int SIZE_VERTEX   = 6 * sizeof(float);

        private static readonly int ParticleBufferID = Shader.PropertyToID("particles");
        private static readonly int VertexBufferID   = Shader.PropertyToID("vertices");
        private static readonly int DeltaTimeID      = Shader.PropertyToID("deltaTime");
        private static readonly int MousePositionID  = Shader.PropertyToID("mousePosition");
        private static readonly int SizeID           = Shader.PropertyToID("size");

        [SerializeField]
        private int particleCount = 1000;
        [SerializeField]
        private Material material;
        [SerializeField]
        private ComputeShader shader;
        [SerializeField, Range(0.01f, 1f)]
        private float quadSize = 0.3f;

        private readonly float[] cursorPosition = new float[2];
        private int kernelID;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer verticesBuffer;
        private int groupSizeX;
        private int verticesCount;

        private new Camera camera;
        private Camera Camera
        {
            get
            {
                if (!this.camera)
                {
                    this.camera = Camera.main;
                }

                return this.camera;
            }
        }

        private void Start()
        {
            // Initialize the particles and vertices
            Particle[] particles = new Particle[this.particleCount];
            Vertex[] vertices    = new Vertex[this.particleCount * 6];
            for (int i = 0, j = 0; i < this.particleCount; i++)
            {
                ref Particle particle = ref particles[i];
                particle.position     = Random.insideUnitSphere / 2f;
                particle.position.z  += 3f;
                particle.life         = Random.Range(1f, 4f);

                // Tri 1: bl, tl, tr
                vertices[j++].uv = Vector2.zero;
                vertices[j++].uv = Vector2.up;
                vertices[j++].uv = Vector2.one;

                // Tri 2: bl, tr, br
                vertices[j++].uv = Vector2.zero;
                vertices[j++].uv = Vector2.one;
                vertices[j++].uv   = Vector2.right;
            }

            // Create compute buffers
            this.particlesBuffer = new(this.particleCount, SIZE_PARTICLE);
            this.particlesBuffer.SetData(particles);
            this.verticesBuffer  = new(vertices.Length, SIZE_VERTEX);
            this.verticesBuffer.SetData(vertices);


            // Initialize shader
            this.kernelID = this.shader.FindKernel(KERNEL);
            this.shader.GetKernelThreadGroupSizes(this.kernelID, out uint threadsX, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.particleCount / (float)threadsX);

            // Bind the compute buffer to the shader and the compute shader
            this.shader.SetBuffer(this.kernelID, ParticleBufferID, this.particlesBuffer);
            this.shader.SetBuffer(this.kernelID, VertexBufferID, this.verticesBuffer);
            this.material.SetBuffer(ParticleBufferID, this.particlesBuffer);
            this.material.SetBuffer(VertexBufferID, this.verticesBuffer);
            this.shader.SetFloat(SizeID, this.quadSize);
        }

        private void OnDestroy()
        {
            this.particlesBuffer?.Release();
            this.verticesBuffer?.Release();
        }

        private void Update()
        {
            // Send data to the compute shader
            this.shader.SetFloat(DeltaTimeID, Time.deltaTime);
            this.shader.SetFloats(MousePositionID, this.cursorPosition);

            // Update the Particles
            this.shader.Dispatch(this.kernelID, this.groupSizeX, 1, 1);

            Vector2 worldPosition = this.Camera.ScreenToWorldPoint(new(Input.mousePosition.x, Input.mousePosition.y, this.Camera.nearClipPlane + 14f)); // z = 3.
            this.cursorPosition[0] = worldPosition.x;
            this.cursorPosition[1] = worldPosition.y;
        }

        private void OnRenderObject()
        {
            this.material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, this.particleCount);
        }
    }
}
