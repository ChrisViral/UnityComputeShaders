// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// adapted from https://github.com/keijiro/StableFluids

using UnityEngine;

namespace UnityComputeShaders
{
    public class StableFluids : MonoBehaviour
    {
        public int resolution = 512;
        public float viscosity = 1e-6f;
        public float force = 300;
        public float exponent = 200;
        public Texture2D initial;
        public ComputeShader compute;
        public Material material;

        private Vector2 previousInput;

        private int kernelAdvect;
        private int kernelForce;
        private int kernelProjectSetup;
        private int kernelProject;
        private int kernelDiffuse1;
        private int kernelDiffuse2;

        private int threadCountX { get { return (this.resolution + 7) / 8; } }

        private int threadCountY { get { return (this.resolution * Screen.height / Screen.width + 7) / 8; } }

        private int resolutionX { get { return this.threadCountX * 8; } }

        private int resolutionY { get { return this.threadCountY * 8; } }

        // Vector field buffers
        private RenderTexture vfbRTV1;
        private RenderTexture vfbRTV2;
        private RenderTexture vfbRTV3;
        private RenderTexture vfbRTP1;
        private RenderTexture vfbRTP2;

        // Color buffers (for double buffering)
        private RenderTexture colorRT1;
        private RenderTexture colorRT2;

        private RenderTexture CreateRenderTexture(int componentCount, int width = 0, int height = 0)
        {
            RenderTexture rt = new(width, height, 0)
            {
                enableRandomWrite = true
            };
            rt.Create();
            return rt;
        }

        private void OnValidate()
        {
            this.resolution = Mathf.Max(this.resolution, 8);
        }

        private void Start()
        {
            InitBuffers();
            InitShader();

            Graphics.Blit(this.initial, this.colorRT1);
        }

        private void InitBuffers()
        {
        
        }

        private void InitShader()
        {
        
        }

        private void OnDestroy()
        {
        
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float dx = 1.0f / this.resolutionY;

            // Input point
            Vector2 input = new(
                (Input.mousePosition.x - Screen.width * 0.5f) / Screen.height,
                (Input.mousePosition.y - Screen.height * 0.5f) / Screen.height
            );

            // Common variables
            this.compute.SetFloat("Time", Time.time);
            this.compute.SetFloat("DeltaTime", dt);

            //Add code here



            this.previousInput = input;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(this.colorRT1, destination, this.material, 1);
        }
    }
}
