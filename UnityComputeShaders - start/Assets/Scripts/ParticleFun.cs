using UnityEngine;

#pragma warning disable 0649

namespace UnityComputeShaders
{
    public class ParticleFun : MonoBehaviour
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

        public int particleCount = 1000000;
        public Material material;
        public ComputeShader shader;
        [Range(1, 10)]
        public int pointSize = 2;

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
            // initialize the particles
            Particle[] particleArray = new Particle[this.particleCount];

            for (int i = 0; i < this.particleCount; i++)
            {
                //TO DO: Initialize particle
            }

            // create compute buffer
            this.particleBuffer = new(this.particleCount, SIZE_PARTICLE);

            this.particleBuffer.SetData(particleArray);

            // find the id of the kernel
            this.kernelID = this.shader.FindKernel("CSParticle");

            this.shader.GetKernelThreadGroupSizes(this.kernelID, out uint threadsX, out _, out _);
            this.groupSizeX = Mathf.CeilToInt(this.particleCount / (float)threadsX);

            // bind the compute buffer to the shader and the compute shader
            this.shader.SetBuffer(this.kernelID, "particleBuffer", this.particleBuffer);
            this.material.SetBuffer("particleBuffer", this.particleBuffer);

            this.material.SetInt("_PointSize", this.pointSize);
        }

        private void OnRenderObject()
        {
            this.material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, this.particleCount);
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

            p = c.ScreenToWorldPoint(new(mousePos.x, mousePos.y, c.nearClipPlane + 14)); // z = 3.

            this.cursorPos.x = p.x;
            this.cursorPos.y = p.y;
        
        }
    }
}
