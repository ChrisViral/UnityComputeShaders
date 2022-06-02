using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class Challenge2 : MonoBehaviour
    {
        private const string KERNEL = "CSMain";

        private static readonly int FillColourID    = Shader.PropertyToID("fillColour");
        private static readonly int ClearColourID   = Shader.PropertyToID("clearColour");
        private static readonly int TexResolutionID = Shader.PropertyToID("textureResolution");
        private static readonly int ResultID        = Shader.PropertyToID("result");
        private static readonly int TimeID          = Shader.PropertyToID("time");
        private static readonly int SidesID         = Shader.PropertyToID("sides");
        private static readonly int MainTexID       = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private int textureResolution = 1024;
        [SerializeField]
        private Color fillColour = new(1f, 1f, 0f, 1f);
        [SerializeField]
        private Color clearColour = new(0f, 0f, 0.3f, 1f);
        [SerializeField, Range(3, 12)]
        private int sides = 5;

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

            this.shader.SetInt(TexResolutionID, this.textureResolution);
            this.shader.SetTexture(this.kernelHandle, ResultID, this.outputTexture);

            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
        }

        private void Dispatch()
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.SetVector(FillColourID, this.fillColour);
            this.shader.SetVector(ClearColourID, this.clearColour);
            this.shader.SetInt(SidesID, this.sides);

            int count = this.textureResolution / 8;
            this.shader.Dispatch(this.kernelHandle, count, count, 1);
        }
    }
}
