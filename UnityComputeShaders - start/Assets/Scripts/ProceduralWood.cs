using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class ProceduralWood : MonoBehaviour
    {
        private const string KERNEL = "CSMain";

        private static readonly int TextureResolutionID = Shader.PropertyToID("textureResolution");
        private static readonly int ResultID            = Shader.PropertyToID("result");
        private static readonly int PaleColourID        = Shader.PropertyToID("paleColour");
        private static readonly int DarkColourID        = Shader.PropertyToID("darkColour");
        private static readonly int NoiseScaleID        = Shader.PropertyToID("noiseScale");
        private static readonly int RingScaleID         = Shader.PropertyToID("ringScale");
        private static readonly int ContrastID          = Shader.PropertyToID("contrast");
        private static readonly int MainTexID           = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private int textureResolution = 1024;
        [SerializeField]
        private Color paleColour = new(0.733f, 0.565f, 0.365f, 1f);
        [SerializeField]
        private Color darkColour = new(0.49f, 0.286f, 0.043f, 1f);
        [SerializeField, Range(0.1f, 10f)]
        private float noiseScale = 6f;
        [SerializeField, Range(0.1f, 3f)]
        private float ringScale = 0.6f;
        [SerializeField, Range(0.1f, 5f)]
        private float contrast = 4f;

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
            this.shader.SetVector(PaleColourID, this.paleColour);
            this.shader.SetVector(DarkColourID, this.darkColour);
            this.shader.SetFloat(NoiseScaleID, this.noiseScale);
            this.shader.SetFloat(RingScaleID, this.ringScale);
            this.shader.SetFloat(ContrastID, this.contrast);

            int count = this.textureResolution / 8;
            this.shader.Dispatch(this.kernelHandle, count, count, 1);
        }
    }
}

