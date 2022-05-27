// StableFluids - Smoke

using UnityEngine;

namespace UnityComputeShaders
{
    public class Challenge7 : MonoBehaviour
    {
        private const string ADVECT_KERNEL   = "Advect";
        private const string FORCE_KERNEL    = "Force";
        private const string SETUP_KERNEL    = "ProjectSetup";
        private const string PROJECT_KERNEL  = "Project";
        private const string DIFFUSE1_KERNEL = "Diffuse1";
        private const string DIFFUSE2_KERNEL = "Diffuse2";

        // ReSharper disable InconsistentNaming
        private static readonly int U_inID           = Shader.PropertyToID("U_in");
        private static readonly int U_outID          = Shader.PropertyToID("U_out");
        private static readonly int W_inID           = Shader.PropertyToID("W_in");
        private static readonly int W_outID          = Shader.PropertyToID("W_out");
        private static readonly int B1_inID          = Shader.PropertyToID("B1_in");
        private static readonly int B2_inID          = Shader.PropertyToID("B2_in");
        private static readonly int DivW_outID       = Shader.PropertyToID("DivW_out");
        private static readonly int P_inID           = Shader.PropertyToID("P_in");
        private static readonly int P_outID          = Shader.PropertyToID("P_out");
        private static readonly int X1_inID          = Shader.PropertyToID("X1_in");
        private static readonly int X1_outID         = Shader.PropertyToID("X1_out");
        private static readonly int X2_inID          = Shader.PropertyToID("X2_in");
        private static readonly int X2_outID         = Shader.PropertyToID("X2_out");
        private static readonly int ForceExponentID  = Shader.PropertyToID("ForceExponent");
        private static readonly int _ForceExponentID = Shader.PropertyToID("_ForceExponent");
        private static readonly int VelocityFieldID  = Shader.PropertyToID("_VelocityField");
        private static readonly int TimeID           = Shader.PropertyToID("Time");
        private static readonly int DeltaTimeID      = Shader.PropertyToID("DeltaTime");
        private static readonly int AlphaID          = Shader.PropertyToID("Alpha");
        private static readonly int BetaID           = Shader.PropertyToID("Beta");
        // ReSharper restore InconsistentNaming

        #region
        [SerializeField]
        private int resolution  = 512;
        [SerializeField]
        private float viscosity = 1E-6f;
        [SerializeField]
        private float force     = 300f;
        [SerializeField]
        private float exponent  = 200f;
        [SerializeField]
        private ComputeShader compute;
        [SerializeField]
        private Shader shader;
        [SerializeField]
        private Vector2 forceOrigin;
        [SerializeField]
        private Vector2 forceVector;

        private Material material;

        private int kernelAdvect;
        private int kernelForce;
        private int kernelProjectSetup;
        private int kernelProject;
        private int kernelDiffuse1;
        private int kernelDiffuse2;

        private int ThreadCountX => (this.resolution + 7) / 8;

        private int ThreadCountY => (this.resolution * Screen.height) / (Screen.width + 7) / 8;

        private int ResolutionX  => this.ThreadCountX * 8;

        private int ResolutionY  => this.ThreadCountY * 8;

        // Vector field buffers
        private RenderTexture vfbRTV1;
        private RenderTexture vfbRTV2;
        private RenderTexture vfbRTV3;
        private RenderTexture vfbRTP1;
        private RenderTexture vfbRTP2;

        // Color buffers (for double buffering)
        private RenderTexture colorRT1;
        private RenderTexture colorRT2;

        private RenderTexture CreateRenderTexture(int componentCount, int width = 0, int height = 0)
        {
            RenderTextureFormat format = componentCount switch
            {
                1 => RenderTextureFormat.RHalf,
                2 => RenderTextureFormat.RGHalf,
                _ => RenderTextureFormat.ARGBHalf
            };

            if (width == 0)
            {
                width = this.ResolutionX;
            }

            if (height == 0)
            {
                height = this.ResolutionY;
            }

            RenderTexture renderTexture = new(width, height, 0, format)
            {
                enableRandomWrite = true
            };
            renderTexture.Create();
            return renderTexture;
        }

        private void OnValidate()
        {
            this.resolution = Mathf.Max(this.resolution, 8);
        }

        private void Start()
        {
            this.material = new(this.shader);

            InitBuffers();
            InitShader();
        }

        private void InitBuffers()
        {
            this.vfbRTV1 = CreateRenderTexture(2);
            this.vfbRTV2 = CreateRenderTexture(2);
            this.vfbRTV3 = CreateRenderTexture(2);
            this.vfbRTP1 = CreateRenderTexture(1);
            this.vfbRTP2 = CreateRenderTexture(1);

            this.colorRT1 = CreateRenderTexture(4, Screen.width, Screen.height);
            this.colorRT2 = CreateRenderTexture(4, Screen.width, Screen.height);
        }
        #endregion

        private void InitShader()
        {
            this.kernelAdvect       = this.compute.FindKernel(ADVECT_KERNEL);
            this.kernelForce        = this.compute.FindKernel(FORCE_KERNEL);
            this.kernelProjectSetup = this.compute.FindKernel(SETUP_KERNEL);
            this.kernelProject      = this.compute.FindKernel(PROJECT_KERNEL);
            this.kernelDiffuse1     = this.compute.FindKernel(DIFFUSE1_KERNEL);
            this.kernelDiffuse2     = this.compute.FindKernel(DIFFUSE2_KERNEL);

            this.compute.SetTexture(this.kernelAdvect, U_inID, this.vfbRTV1);
            this.compute.SetTexture(this.kernelAdvect, W_outID, this.vfbRTV2);

            this.compute.SetTexture(this.kernelDiffuse2, B2_inID, this.vfbRTV1);

            this.compute.SetTexture(this.kernelForce, W_inID, this.vfbRTV2);
            this.compute.SetTexture(this.kernelForce, W_outID, this.vfbRTV3);

            this.compute.SetTexture(this.kernelProjectSetup, W_inID, this.vfbRTV3);
            this.compute.SetTexture(this.kernelProjectSetup, DivW_outID, this.vfbRTV2);
            this.compute.SetTexture(this.kernelProjectSetup, P_outID, this.vfbRTP1);

            this.compute.SetTexture(this.kernelDiffuse1, B1_inID, this.vfbRTV2);

            this.compute.SetTexture(this.kernelProject, W_inID, this.vfbRTV3);
            this.compute.SetTexture(this.kernelProject, P_inID, this.vfbRTP1);
            this.compute.SetTexture(this.kernelProject, U_outID, this.vfbRTV1);
            this.compute.SetFloat(ForceExponentID, this.exponent);

            //TO DO: 1 - Setup the correct force origin.
            //The StableFluids.compute shader wants the input to have the origin at the centre of the quad.
            //The public property forceOrigin has uv coordinates, with the origin at bottom left

            this.material.SetFloat(_ForceExponentID, this.exponent);
            this.material.SetTexture(VelocityFieldID, this.vfbRTV1);

            //TO DO: 2 - Get the material attached to this object and set colorRT1 as its _MainTex property

        }

        private void OnDestroy()
        {
            Destroy(this.vfbRTV1);
            Destroy(this.vfbRTV2);
            Destroy(this.vfbRTV3);
            Destroy(this.vfbRTP1);
            Destroy(this.vfbRTP2);

            Destroy(this.colorRT1);
            Destroy(this.colorRT2);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float dx = 1f / this.ResolutionY;

            // Common variables
            this.compute.SetFloat(TimeID, Time.time);
            this.compute.SetFloat(DeltaTimeID, dt);

            // Advection
            this.compute.Dispatch(this.kernelAdvect, this.ThreadCountX, this.ThreadCountY, 1);

            // Diffuse setup
            float diffAlpha = (dx * dx) / (this.viscosity * dt);
            this.compute.SetFloat(AlphaID, diffAlpha);
            this.compute.SetFloat(BetaID, 4 + diffAlpha);
            Graphics.CopyTexture(this.vfbRTV2, this.vfbRTV1);

            // Jacobi iteration
            for (int i = 0; i < 20; i++)
            {
                this.compute.SetTexture(this.kernelDiffuse2, X2_inID, this.vfbRTV2);
                this.compute.SetTexture(this.kernelDiffuse2, X2_outID, this.vfbRTV3);
                this.compute.Dispatch(this.kernelDiffuse2, this.ThreadCountX, this.ThreadCountY, 1);

                this.compute.SetTexture(this.kernelDiffuse2, X2_inID, this.vfbRTV3);
                this.compute.SetTexture(this.kernelDiffuse2, X2_outID, this.vfbRTV2);
                this.compute.Dispatch(this.kernelDiffuse2, this.ThreadCountX, this.ThreadCountY, 1);
            }

            //TO DO: 3 - Add random vector to the forceVector

            // Add external force
            this.compute.Dispatch(this.kernelForce, this.ThreadCountX, this.ThreadCountY, 1);

            // Projection setup
            this.compute.Dispatch(this.kernelProjectSetup, this.ThreadCountX, this.ThreadCountY, 1);

            // Jacobi iteration
            this.compute.SetFloat(AlphaID, -dx * dx);
            this.compute.SetFloat(BetaID, 4f);

            for (int i = 0; i < 20; i++)
            {
                this.compute.SetTexture(this.kernelDiffuse1, X1_inID, this.vfbRTP1);
                this.compute.SetTexture(this.kernelDiffuse1, X1_outID, this.vfbRTP2);
                this.compute.Dispatch(this.kernelDiffuse1, this.ThreadCountX, this.ThreadCountY, 1);

                this.compute.SetTexture(this.kernelDiffuse1, X1_inID, this.vfbRTP2);
                this.compute.SetTexture(this.kernelDiffuse1, X1_outID, this.vfbRTP1);
                this.compute.Dispatch(this.kernelDiffuse1, this.ThreadCountX, this.ThreadCountY, 1);
            }

            // Projection finish
            this.compute.Dispatch(this.kernelProject, this.ThreadCountX, this.ThreadCountY, 1);

            // Apply the velocity field to the color buffer.
            Graphics.Blit(this.colorRT1, this.colorRT2, this.material, 0);

            // Swap the color buffers.
            (this.colorRT1, this.colorRT2) = (this.colorRT2, this.colorRT1);
        }
    }
}
