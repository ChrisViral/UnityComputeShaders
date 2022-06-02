using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class BlurHighlight : BasePP
    {
        private const string HORIZONTAL_KERNEL = "HorizontalPass";

        private static readonly int RadiusID         = Shader.PropertyToID("radius");
        private static readonly int EdgeID           = Shader.PropertyToID("edge");
        private static readonly int ShadeID          = Shader.PropertyToID("shade");
        private static readonly int CenterID         = Shader.PropertyToID("center");
        private static readonly int HorizontalPassID = Shader.PropertyToID("horizontalPass");
        private static readonly int BlurRadiusID     = Shader.PropertyToID("blurRadius");

        [SerializeField, Range(0.01f, 1f)]
        private float radius = 0.1f;
        [SerializeField, Range(0f, 1f)]
        private float smoothing = 0.1f;
        [SerializeField, Range(0f, 1f)]
        private float shade = 0.7f;
        [SerializeField]
        private Transform trackedObject;
        [SerializeField, Range(1, 50)]
        private int blurRadius = 20;

        private RenderTexture horizontalOutput;
        private int horizontalHandle;

        protected override string KernelName => "Highlight";

        private void OnValidate()
        {
            if (!this.init)
            {
                Init();
            }

            SetProperties();
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

        protected override void OnResolutionChanged() => SetProperties();

        protected void SetProperties()
        {
            // ReSharper disable once LocalVariableHidesMember
            float radius = this.radius * this.textureSize.y;
            this.shader.SetFloat(RadiusID, radius);
            this.shader.SetFloat(EdgeID, radius * this.smoothing);
            this.shader.SetFloat(ShadeID, this.shade);
            this.shader.SetInt(BlurRadiusID, this.blurRadius);
        }
    }
}
