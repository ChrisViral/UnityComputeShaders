using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Camera))]
    public class BasePP : MonoBehaviour
    {
        [SerializeField]
        protected ComputeShader shader;

        protected virtual string KernelName => "CSMain";

        protected Vector2Int textureSize;
        protected Vector2Int groupSize;
        protected new Camera camera;

        protected RenderTexture output;
        protected RenderTexture renderedSource;

        protected int kernelHandle = -1;
        protected bool init;

        protected virtual void Init()
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

            this.kernelHandle = this.shader.FindKernel(this.KernelName);

            this.camera = GetComponent<Camera>();

            if (!this.camera)
            {
                Debug.LogError("Object has no Camera");
                return;
            }

            CreateTextures();

            this.init = true;
        }

        protected void ClearTexture(ref RenderTexture textureToClear)
        {
            if (!textureToClear) return;

            textureToClear.Release();
            textureToClear = null;
        }

        protected virtual void ClearTextures()
        {
            ClearTexture(ref this.output);
            ClearTexture(ref this.renderedSource);
        }

        protected void CreateTexture(out RenderTexture textureToMake, int divide=1)
        {
            textureToMake = new(this.textureSize.x/divide, this.textureSize.y/divide, 0)
            {
                enableRandomWrite = true
            };
            textureToMake.Create();
        }


        protected virtual void CreateTextures()
        {

        }

        protected virtual void OnEnable()
        {
            Init();
        }

        protected virtual void OnDisable()
        {
            ClearTextures();
            this.init = false;
        }

        protected virtual void OnDestroy()
        {
            ClearTextures();
            this.init = false;
        }

        protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
        {

        }

        protected void CheckResolution(out bool resChange )
        {
            resChange = false;
        }

        protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!this.init || !this.shader)
            {
                Graphics.Blit(source, destination);
                return;
            }

            CheckResolution(out _);
            DispatchWithSource(ref source, ref destination);
        }

    }
}
