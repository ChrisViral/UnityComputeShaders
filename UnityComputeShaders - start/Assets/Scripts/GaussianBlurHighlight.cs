using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class GaussianBlurHighlight : BasePP
    {
        private const string HORIZONTAL_KERNEL = "HorizontalPass";

        private static readonly int RadiusID         = Shader.PropertyToID("radius");
        private static readonly int EdgeID           = Shader.PropertyToID("edge");
        private static readonly int ShadeID          = Shader.PropertyToID("shade");
        private static readonly int CenterID         = Shader.PropertyToID("center");
        private static readonly int HorizontalPassID = Shader.PropertyToID("horizontalPass");
        private static readonly int BlurRadiusID     = Shader.PropertyToID("blurRadius");
        private static readonly int WeightsID        = Shader.PropertyToID("weights");

        [SerializeField, Range(0.01f, 1f)]
        private float radius = 0.1f;
        [SerializeField, Range(0f, 1f)]
        private float smoothing = 0.3f;
        [SerializeField, Range(0f, 1f)]
        private float shade = 0.7f;
        [SerializeField]
        private Transform trackedObject;
        [SerializeField, Range(1, 50)]
        private int blurRadius = 20;

        private RenderTexture horizontalOutput;
        private int horizontalHandle;
        private ComputeBuffer weightsBuffer;

        protected override string KernelName => "Highlight";

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateWeightsBuffer();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            this.weightsBuffer?.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.weightsBuffer?.Dispose();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateWeightsBuffer();
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (this.trackedObject)
            {
                Vector2 position = this.Camera.WorldToScreenPoint(this.trackedObject.position);
                this.shader.SetVector(CenterID, position);
            }

            base.OnRenderImage(source, destination);
        }

        protected override void Init()
        {
            this.horizontalHandle = this.shader.FindKernel(HORIZONTAL_KERNEL);
            base.Init();
        }

        protected override void CreateTextures()
        {
            if (!this.shader) return;

            base.CreateTextures();
            this.shader.SetTexture(this.horizontalHandle, SourceID, this.initial);

            CreateTexture(out this.horizontalOutput);
            this.shader.SetTexture(this.kernelHandle, HorizontalPassID, this.horizontalOutput);
            this.shader.SetTexture(this.horizontalHandle, HorizontalPassID, this.horizontalOutput);
        }

        protected override void ClearTextures()
        {
            ClearTexture(ref this.horizontalOutput);
            base.ClearTextures();
        }

        protected override void DispatchWithSource(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, this.initial);
            this.shader.Dispatch(this.horizontalHandle, this.groupSize.x, this.groupSize.y, 1);
            this.shader.Dispatch(this.kernelHandle, this.groupSize.x, this.groupSize.y, 1);
            Graphics.Blit(this.output, destination);
        }

        protected override void SetProperties()
        {
            // ReSharper disable once LocalVariableHidesMember
            float radius = this.radius * this.textureSize.y;
            this.shader.SetFloat(RadiusID, radius);
            this.shader.SetFloat(EdgeID, radius * this.smoothing);
            this.shader.SetFloat(ShadeID, this.shade);
            this.shader.SetInt(BlurRadiusID, this.blurRadius);
        }

        private void UpdateWeightsBuffer()
        {
            this.weightsBuffer?.Dispose();

            float sigma = this.blurRadius / 1.5f;

            this.weightsBuffer = new((this.blurRadius * 2) + 1, sizeof(float));
            float[] blurWeights = GetWeightsArray(this.blurRadius, sigma);
            this.weightsBuffer.SetData(blurWeights);

            this.shader.SetBuffer(this.horizontalHandle, WeightsID, this.weightsBuffer);
            this.shader.SetBuffer(this.kernelHandle, WeightsID, this.weightsBuffer);
        }

        private static float[] GetWeightsArray(int radius, float sigma)
        {
            int total = (radius * 2) + 1;
            float[] weights = new float[total];
            float c = 1f / Mathf.Sqrt(2f * Mathf.PI * sigma * sigma);

            for (int n = 0; n < radius; n++)
            {
                float weight = c * Mathf.Exp(n * n / -(2f * sigma * sigma));
                weights[radius + n] = weight;
                weights[radius - n] = weight;
            }

            return weights;
        }
    }
}
