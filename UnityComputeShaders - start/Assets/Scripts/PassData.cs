using UnityEngine;

namespace UnityComputeShaders
{
    public class PassData : MonoBehaviour
    {

        public ComputeShader shader;
        public int texResolution = 1024;

        private Renderer rend;
        private RenderTexture outputTexture;

        private int circlesHandle;

        public Color clearColor;
        public Color circleColor;

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
            this.circlesHandle = this.shader.FindKernel("Circles");

            this.shader.SetInt( "texResolution", this.texResolution);
            this.shader.SetTexture( this.circlesHandle, "Result", this.outputTexture);

            this.rend.material.SetTexture("_MainTex", this.outputTexture);
        }
 
        private void DispatchKernel(int count)
        {
            this.shader.Dispatch(this.circlesHandle, count, 1, 1);
        }

        private void Update()
        {
            DispatchKernel(1);
        }
    }
}

