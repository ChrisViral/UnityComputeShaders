using UnityEngine;

namespace UnityComputeShaders
{
    public class SolidColor : MonoBehaviour
    {

        public ComputeShader shader;
        public int texResolution = 256;

        private Renderer rend;
        private RenderTexture outputTexture;

        private int kernelHandle;

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

