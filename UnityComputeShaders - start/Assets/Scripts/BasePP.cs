using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Camera))]
    public class BasePP : MonoBehaviour
    {
        protected static readonly int SourceID = Shader.PropertyToID("source");
        protected static readonly int OutputID = Shader.PropertyToID("output");

        [SerializeField]
        protected ComputeShader shader;

        protected Vector2Int textureSize;
        protected Vector2Int groupSize;

        protected RenderTexture initial;
        protected RenderTexture output;

        protected int kernelHandle;
        protected bool init;

        private new Camera camera;
        protected Camera Camera
        {
            get
            {
                if (!this.camera)
                {
                    this.camera = GetComponent<Camera>();
                }

                return this.camera;
            }
        }

        protected virtual string KernelName => "CSMain";

        protected virtual void OnEnable()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("It seems your target Hardware does not support Compute Shaders.");
                return;
            }

            if (!this.shader)
            {
                Debug.LogError("No shader");
                return;
            }

            Init();
        }

        protected virtual void OnDisable() => ClearTextures();

        protected virtual void OnDestroy() => ClearTextures();

        protected virtual void OnValidate()
        {
            if (!this.init)
            {
                Init();
            }

            SetProperties();
        }

        protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!this.init || !this.shader)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (CheckResolutionChanged())
            {
                OnResolutionChanged();
            }

            DispatchWithSource(source, destination);
        }

        protected virtual void Init()
        {
            this.kernelHandle = this.shader.FindKernel(this.KernelName);

            CreateTextures();
            this.init = true;
        }

        protected virtual void CreateTextures()
        {
            if (!this.shader) return;

            this.textureSize = new(this.Camera.pixelWidth, this.Camera.pixelHeight);

            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out uint x, out uint y, out _);
            this.groupSize = new(Mathf.CeilToInt(this.textureSize.x / (float)x),
                                 Mathf.CeilToInt(this.textureSize.y / (float)y));

            CreateTexture(out this.output);
            CreateTexture(out this.initial);

            this.shader.SetTexture(this.kernelHandle, OutputID, this.output);
            this.shader.SetTexture(this.kernelHandle, SourceID, this.initial);
        }

        protected void CreateTexture(out RenderTexture texture)
        {
            texture = new(this.textureSize.x, this.textureSize.y, 0)
            {
                enableRandomWrite = true
            };
            texture.Create();
        }

        protected virtual void ClearTextures()
        {
            ClearTexture(ref this.initial);
            ClearTexture(ref this.output);
            this.init = false;
        }

        protected void ClearTexture(ref RenderTexture textureToClear)
        {
            if (!textureToClear) return;

            textureToClear.Release();
            textureToClear = null;
        }

        protected virtual void DispatchWithSource(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, this.initial);
            this.shader.Dispatch(this.kernelHandle, this.groupSize.x, this.groupSize.y, 1);
            Graphics.Blit(this.output, destination);
        }

        protected bool CheckResolutionChanged()
        {
            if (this.textureSize.x == this.camera.pixelWidth && this.textureSize.y == this.camera.pixelHeight) return false;

            CreateTextures();
            return true;
        }

        protected virtual void OnResolutionChanged() => SetProperties();

        protected virtual void SetProperties() { }
    }
}
