using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class AssignTexture : MonoBehaviour
    {
        private const string KERNEL = "CSMain";

        private static readonly int OutputID  = Shader.PropertyToID("output");
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private int textureResolution = 256;

        private new Renderer renderer;
        private RenderTexture outputTexture;
        private int kernelHandle;
        private int side;

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
            if (!Input.GetKeyDown(KeyCode.U)) return;

            DispatchShader();
        }

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);
            this.shader.SetTexture(this.kernelHandle, OutputID, this.outputTexture);
            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
            this.side = this.textureResolution / 8;
            DispatchShader(this.textureResolution / 16);
        }

        private void DispatchShader() => DispatchShader(this.side);

        private void DispatchShader(int threads)
        {
            this.shader.Dispatch(this.kernelHandle, threads, threads, 1);
        }
    }
}
