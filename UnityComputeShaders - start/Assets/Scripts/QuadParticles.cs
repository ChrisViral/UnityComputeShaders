using UnityEngine;

#pragma warning disable 0649

namespace UnityComputeShaders
{
    public class QuadParticles : MonoBehaviour
    {

        private Vector2 cursorPos;

        // struct
        private struct Particle
        {
            public Vector3 position;
            public Vector3 velocity;
            public float life;
        }

        private const int SIZE_PARTICLE = 7 * sizeof(float);

        public int particleCount = 10000;
        public Material material;
        public ComputeShader shader;
        [Range(0.01f, 1.0f)]
        public float quadSize = 0.1f;

        private int numParticles;
        private int numVerticesInMesh;
        private int kernelID;
        private ComputeBuffer particleBuffer;

        private int groupSizeX; 
    
        // Use this for initialization
        private void Start()
        {
            Init();
        }

        private void Init()
        {
            // find the id of the kernel
            this.kernelID = this.shader.FindKernel("CSMain");

            this.shader.GetKernelThreadGroupSizes(this.kernelID, out uint threadsX, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.particleCount / (float)threadsX);
            this.numParticles = this.groupSizeX * (int)threadsX;

            // initialize the particles
            Particle[] particleArray = new Particle[this.numParticles];

            int numVertices = this.numParticles * 6;
        
            Vector3 pos = new();
        
            for (int i = 0; i < this.numParticles; i++)
            {
                pos.Set(Random.value * 2 - 1.0f, Random.value * 2 - 1.0f, Random.value * 2 - 1.0f);
                pos.Normalize();
                pos *= Random.value;
                pos *= 0.5f;

                particleArray[i].position.Set(pos.x, pos.y, pos.z + 3);
                particleArray[i].velocity.Set(0,0,0);
          
                // Initial life value
                particleArray[i].life = Random.value * 5.0f + 1.0f;
            }

            // create compute buffers
            this.particleBuffer = new(this.numParticles, SIZE_PARTICLE);
            this.particleBuffer.SetData(particleArray);
        
            // bind the compute buffers to the shader and the compute shader
            this.shader.SetBuffer(this.kernelID, "particleBuffer", this.particleBuffer);
        }

        private void OnRenderObject()
        {
            this.material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, this.numParticles);
        }

        private void OnDestroy()
        {
            this.particleBuffer?.Release();
        }

        // Update is called once per frame
        private void Update()
        {
            float[] mousePosition2D = { this.cursorPos.x, this.cursorPos.y };

            // Send datas to the compute shader
            this.shader.SetFloat("deltaTime", Time.deltaTime);
            this.shader.SetFloats("mousePosition", mousePosition2D);

            // Update the Particles
            this.shader.Dispatch(this.kernelID, this.groupSizeX, 1, 1);
        }

        private void OnGUI()
        {
            Vector3 p = new();
            Camera c = Camera.main;
            Event e = Event.current;
            Vector2 mousePos = new()
            {
                // Note that the y position from Event is inverted.
                // Get the mouse position from Event.
                x = e.mousePosition.x,
                y = c.pixelHeight - e.mousePosition.y
            };

            p = c.ScreenToWorldPoint(new(mousePos.x, mousePos.y, c.nearClipPlane + 14));

            this.cursorPos.x = p.x;
            this.cursorPos.y = p.y;
        
        }
    }
}
