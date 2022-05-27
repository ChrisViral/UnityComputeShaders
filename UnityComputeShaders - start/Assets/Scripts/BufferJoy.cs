using UnityEngine;
using UnityEngine.Serialization;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class BufferJoy : MonoBehaviour
    {
        private const int COUNT             = 10;
        private const string KERNEL_CLEAR   = "Clear";
        private const string KERNEL_CIRCLES = "Circles";

        private static readonly int ClearColourID   = Shader.PropertyToID("clearColor");
        private static readonly int CircleColourID  = Shader.PropertyToID("circleColor");
        private static readonly int TexResolutionID = Shader.PropertyToID("texResolution");
        private static readonly int ResultID        = Shader.PropertyToID("Result");
        private static readonly int MainTexID       = Shader.PropertyToID("_MainTex");
        private static readonly int TimeID          = Shader.PropertyToID("time");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField, FormerlySerializedAs("texResolution")]
        private int textureResolution = 1024;
        [SerializeField, FormerlySerializedAs("clearColor")]
        private Color clearColour = Color.clear;
        [SerializeField, FormerlySerializedAs("circleColor")]
        private Color circleColour = Color.white;

        private new Renderer renderer;
        private RenderTexture outputTexture;

        private int circlesHandle;
        private int clearHandle;

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

            InitData();

            InitShader();
        }

        private void InitData()
        {
            this.circlesHandle = this.shader.FindKernel(KERNEL_CIRCLES);
        }

        private void InitShader()
        {
            this.clearHandle = this.shader.FindKernel(KERNEL_CLEAR);

            this.shader.SetVector(ClearColourID, this.clearColour);
            this.shader.SetVector(CircleColourID, this.circleColour);
            this.shader.SetInt(TexResolutionID, this.textureResolution);

            this.shader.SetTexture(this.clearHandle, ResultID, this.outputTexture);
            this.shader.SetTexture(this.circlesHandle, ResultID, this.outputTexture);

            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
        }

        private void DispatchKernels(int count)
        {
            this.shader.Dispatch(this.clearHandle, this.textureResolution / 8, this.textureResolution / 8, 1);
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.Dispatch(this.circlesHandle, count, 1, 1);
        }

        private void Update()
        {
            DispatchKernels(COUNT);
        }
    }
}
