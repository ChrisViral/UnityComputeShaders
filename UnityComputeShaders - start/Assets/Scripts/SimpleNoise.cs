using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class SimpleNoise : MonoBehaviour
    {
        private const string KERNEL = "CSMain";

        private static readonly int TextureResolutionID = Shader.PropertyToID("textureResolution");
        private static readonly int ResultID            = Shader.PropertyToID("result");
        private static readonly int MainTexID           = Shader.PropertyToID("_MainTex");
        private static readonly int TimeID              = Shader.PropertyToID("time");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private int textureResolution = 256;

        private new Renderer renderer;
        private RenderTexture outputTexture;
        private int kernelHandle;

        private void Start()
        {
            this.outputTexture = new(this.textureResolution, this.textureResolution, 0)
            {
                enableRandomWrite = true
            };
            this.outputTexture.Create();

            this.renderer = GetComponent<Renderer>();
            this.renderer.enabled = true;
            InitShader();
        }

        private void OnDestroy()
        {
            this.outputTexture.Release();
        }

        private void Update()
        {
            Dispatch();
        }

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);
            this.shader.SetInt(TextureResolutionID, this.textureResolution);
            this.shader.SetTexture(this.kernelHandle, ResultID, this.outputTexture);
            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
        }

        private void Dispatch()
        {
            this.shader.SetFloat(TimeID, Time.time);
            int count = this.textureResolution / 8;
            this.shader.Dispatch(this.kernelHandle, count, count, 1);
        }
    }
}
