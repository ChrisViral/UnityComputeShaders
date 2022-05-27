using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class SolidColor : MonoBehaviour
    {
        private const string RED_KERNEL    = "SolidRed";
        private const string YELLOW_KERNEL = "SolidYellow";
        private const string SPLIT_KERNEL  = "SplitScreen";
        private const string CIRCLE_KERNEL = "Circle";

        private static readonly int OutputID            = Shader.PropertyToID("output");
        private static readonly int TextureResolutionID = Shader.PropertyToID("textureResolution");
        private static readonly int MainTexID           = Shader.PropertyToID("_MainTex");
        private static readonly string[] Kernels        = { RED_KERNEL, YELLOW_KERNEL, SPLIT_KERNEL, CIRCLE_KERNEL };

        [SerializeField]
        public ComputeShader shader;
        [SerializeField]
        public int textureResolution = 256;

        private new Renderer renderer;
        private RenderTexture outputTexture;
        private int kernelHandle;
        private int side;
        private int currentKernel;

        private void Start()
        {
            this.outputTexture = new(this.textureResolution, this.textureResolution, 0)
            {
                enableRandomWrite = true
            };
            this.outputTexture.Create();

            this.renderer = GetComponent<Renderer>();
            this.renderer.enabled = true;
            this.side = this.textureResolution / 8;
            this.shader.SetInt(TextureResolutionID, this.textureResolution);
            InitShader();
        }

        private void OnDestroy()
        {
            this.outputTexture.Release();
        }

        private void Update()
        {
            if (!Input.GetKeyUp(KeyCode.U)) return;

            this.currentKernel = (this.currentKernel + 1) % Kernels.Length;
            InitShader();
        }

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel(Kernels[this.currentKernel]);
            this.shader.SetTexture(this.kernelHandle, OutputID, this.outputTexture);
            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
            DispatchShader();
        }

        private void DispatchShader()
        {
            this.shader.Dispatch(this.kernelHandle, this.side, this.side, 1);
        }
    }
}

