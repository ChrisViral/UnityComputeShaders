using UnityEngine;

namespace UnityComputeShaders
{
    public class PassData : MonoBehaviour
    {
        private const string CIRCLES_KERNEL = "Circles";
        private const string CLEAR_KERNEL   = "Clear";

        private static readonly int OutputID            = Shader.PropertyToID("output");
        private static readonly int TextureResolutionID = Shader.PropertyToID("textureResolution");
        private static readonly int ClearColourID       = Shader.PropertyToID("clearColour");
        private static readonly int CircleColourID      = Shader.PropertyToID("circleColour");
        private static readonly int TimeID              = Shader.PropertyToID("time");
        private static readonly int MainTexID           = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private int textureResolution = 1024;
        [SerializeField]
        private Color clearColour;
        [SerializeField]
        private Color circleColour;

        private new Renderer renderer;
        private RenderTexture outputTexture;
        private int circlesHandle;
        private int clearHandle;

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
            this.shader.SetFloat(TimeID, Time.time);
            DispatchKernels(10);
        }

        private void InitShader()
        {
            this.circlesHandle = this.shader.FindKernel(CIRCLES_KERNEL);
            this.clearHandle   = this.shader.FindKernel(CLEAR_KERNEL);

            this.shader.SetTexture(this.circlesHandle, OutputID, this.outputTexture);
            this.shader.SetTexture(this.clearHandle, OutputID, this.outputTexture);

            this.shader.SetInt(TextureResolutionID, this.textureResolution);
            this.shader.SetVector(ClearColourID, this.clearColour);
            this.shader.SetVector(CircleColourID, this.circleColour);

            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
        }

        private void DispatchKernels(int count = 1)
        {
            int sides = this.textureResolution / 8;
            this.shader.Dispatch(this.clearHandle, sides, sides, 1);

            this.shader.Dispatch(this.circlesHandle, count, 1, 1);
        }
    }
}

