using UnityEngine;

namespace UnityComputeShaders
{
    public class ProceduralMarble : MonoBehaviour
    {
        private const string KERNEL = "CSMain";

        private static readonly int TextureResolutionID = Shader.PropertyToID("textureResolution");
        private static readonly int ResultID            = Shader.PropertyToID("result");
        private static readonly int MarbleID            = Shader.PropertyToID("marble");
        private static readonly int MainTexID           = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private int textureResolution = 1024;

        private new Renderer renderer;
        private RenderTexture outputTexture;
        private int kernelHandle;
        private bool marble = true;

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

        private void OnDestroy()
        {
            this.outputTexture.Release();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                this.marble = !this.marble;
                Dispatch();
            }
        }

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);

            this.shader.SetInt(TextureResolutionID, this.textureResolution);
            this.shader.SetBool(MarbleID, this.marble);
            this.shader.SetTexture(this.kernelHandle, ResultID, this.outputTexture);
            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
            Dispatch();
        }

        private void Dispatch()
        {
            this.shader.SetBool(MarbleID, this.marble);
            int count = this.textureResolution / 8;
            this.shader.Dispatch(this.kernelHandle, count, count, 1);
        }
    }
}

