using UnityEngine;

namespace UnityComputeShaders
{
    public class ProceduralWood : MonoBehaviour
    {

        public ComputeShader shader;
        public int texResolution = 256;

        private Renderer rend;
        private RenderTexture outputTexture;

        private int kernelHandle;

        public Color paleColor = new(0.733f, 0.565f, 0.365f, 1);
        public Color darkColor = new(0.49f, 0.286f, 0.043f, 1);
        public float frequency = 2.0f;
        public float noiseScale = 6.0f;
        public float ringScale = 0.6f;
        public float contrast = 4.0f;

        // Use this for initialization
        private void Start()
        {
            this.outputTexture = new(this.texResolution, this.texResolution, 0)
            {
                enableRandomWrite = true
            };
            this.outputTexture.Create();

            this.rend = GetComponent<Renderer>();
            this.rend.enabled = true;

            InitShader();
        }

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel("CSMain");

            this.shader.SetInt("texResolution", this.texResolution);

            this.shader.SetVector("paleColor", this.paleColor);
            this.shader.SetVector("darkColor", this.darkColor);
            this.shader.SetFloat("frequency", this.frequency);
            this.shader.SetFloat("noiseScale", this.noiseScale);
            this.shader.SetFloat("ringScale", this.ringScale);
            this.shader.SetFloat("contrast", this.contrast);

            this.shader.SetTexture(this.kernelHandle, "Result", this.outputTexture);

            this.rend.material.SetTexture("_MainTex", this.outputTexture);

            DispatchShader(this.texResolution / 8, this.texResolution / 8);
        }

        private void DispatchShader(int x, int y)
        {
            this.shader.Dispatch(this.kernelHandle, x, y, 1);
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.U))
            {
                DispatchShader(this.texResolution / 8, this.texResolution / 8);
            }
        }
    }
}

