using UnityEngine;
using UnityEngine.Serialization;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class Challenge1 : MonoBehaviour
    {
        private const string KERNEL = "Square";

        private static readonly int ResultID  = Shader.PropertyToID("Result");
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField, FormerlySerializedAs("texResolution")]
        private int textureResolution = 1024;

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

            //Create a Vector4 with parameters x, y, width, height
            //Pass this to the shader using SetVector

            this.shader.SetTexture(this.kernelHandle, ResultID, this.outputTexture);

            this.renderer.material.SetTexture(MainTexID, this.outputTexture);

            DispatchShader(this.textureResolution / 8, this.textureResolution / 8);
        }

        private void DispatchShader(int x, int y)
        {
            this.shader.Dispatch(this.kernelHandle, x, y, 1);
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.U))
            {
                DispatchShader(this.textureResolution / 8, this.textureResolution / 8);
            }
        }
    }
}

