using UnityEngine;
using UnityEngine.Serialization;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class Challenge2 : MonoBehaviour
    {
        private const string KERNEL = "CSMain";

        private static readonly int FillColourID    = Shader.PropertyToID("fillColor");
        private static readonly int ClearColourID   = Shader.PropertyToID("clearColor");
        private static readonly int TexResolutionID = Shader.PropertyToID("texResolution");
        private static readonly int ResultID        = Shader.PropertyToID("Result");
        private static readonly int MainTexID       = Shader.PropertyToID("_MainTex");
        private static readonly int TimeID          = Shader.PropertyToID("time");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField, FormerlySerializedAs("texResolution")]
        private int textureResolution = 1024;
        [FormerlySerializedAs("fillColor"), SerializeField]
        private Color fillColour = new(1f, 1f, 0f, 1f);
        [FormerlySerializedAs("clearColor"), SerializeField]
        private Color clearColour = new(0f, 0f, 0.3f, 1f);

        private new Renderer renderer;
        private RenderTexture outputTexture;

        private int kernelHandle;

        // Use this for initialization
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

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);

            this.shader.SetVector(FillColourID, this.fillColour);
            this.shader.SetVector(ClearColourID, this.clearColour);

            this.shader.SetInt(TexResolutionID, this.textureResolution);
            this.shader.SetTexture(this.kernelHandle, ResultID, this.outputTexture);

            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
        }

        private void DispatchShader(int x, int y)
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.Dispatch(this.kernelHandle, x, y, 1);
        }

        private void Update()
        {
            DispatchShader(this.textureResolution / 8, this.textureResolution / 8);
        }
    }
}

