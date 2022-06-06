using UnityEngine;

namespace UnityComputeShaders
{
    public class ParticleFun : MonoBehaviour
    {
        #pragma warning disable 0649
        // ReSharper disable NotAccessedField.Local
        private struct Particle
        {
            public Vector3 position;
            public Vector3 velocity;
            public float life;
        }
        // ReSharper restore NotAccessedField.Local
        #pragma warning restore 0649

        private const string KERNEL     = "CSParticle";
        private const int SIZE_PARTICLE = 7 * sizeof(float);

        private static readonly int ParticleBufferID = Shader.PropertyToID("particles");
        private static readonly int PointSizeID      = Shader.PropertyToID("_PointSize");
        private static readonly int DeltaTimeID      = Shader.PropertyToID("deltaTime");
        private static readonly int MousePositionID  = Shader.PropertyToID("mousePosition");

        [SerializeField]
        private int particleCount = 1_000_000;
        [SerializeField]
        private Material material;
        [SerializeField]
        private ComputeShader shader;
        [SerializeField, Range(1, 10)]
        private int pointSize = 2;

        private readonly float[] cursorPosition = new float[2];
        private int kernelID;
        private ComputeBuffer buffer;
        private int groupSizeX;

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
            // Initialize the particles
            Particle[] particles = new Particle[this.particleCount];
            for (int i = 0; i < this.particleCount; i++)
            {
                ref Particle particle = ref particles[i];
                particle.position     = Random.insideUnitSphere / 2f;
                particle.position.z  += 3f;
                particle.life         = Random.Range(1f, 4f);
            }

            // Create compute buffer
            this.buffer = new(this.particleCount, SIZE_PARTICLE);
            this.buffer.SetData(particles);

            // Initialize shader
            this.kernelID = this.shader.FindKernel(KERNEL);
            this.shader.GetKernelThreadGroupSizes(this.kernelID, out uint threadsX, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.particleCount / (float)threadsX);

            // bind the compute buffer to the shader and the compute shader
            this.shader.SetBuffer(this.kernelID, ParticleBufferID, this.buffer);
            this.material.SetBuffer(ParticleBufferID, this.buffer);
            this.material.SetInt(PointSizeID, this.pointSize);
        }

        private void OnDestroy()
        {
            this.buffer?.Release();
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
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, this.particleCount);
        }
    }
}
