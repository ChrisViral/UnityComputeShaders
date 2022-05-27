﻿using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(Renderer))]
    public class Challenge1 : MonoBehaviour
    {
        private const string SQUARE_KERNEL = "Square";

        private static readonly int OutputID            = Shader.PropertyToID("output");
        private static readonly int TextureResolutionID = Shader.PropertyToID("textureResolution");
        private static readonly int MainTexID           = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private int textureResolution = 1024;

        private new Renderer renderer;
        private RenderTexture outputTexture;
        private int kernelHandle;

        private void Start()
        {
            this.outputTexture = new(this.textureResolution, this.textureResolution, 0)
            {
                enableRandomWrite = true
            };
            this.outputTexture.Create();

            this.renderer = GetComponent<Renderer>();
            this.renderer.enabled = true;
            this.shader.SetInt(TextureResolutionID, this.textureResolution);
            InitShader();
        }

        private void OnDestroy()
        {
            this.outputTexture.Release();
        }

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel(SQUARE_KERNEL);
            this.shader.SetTexture(this.kernelHandle, OutputID, this.outputTexture);
            this.renderer.material.SetTexture(MainTexID, this.outputTexture);
            DispatchShader();
        }

        private void DispatchShader()
        {
            int side = this.textureResolution / 8;
            this.shader.Dispatch(this.kernelHandle, side, side, 1);
        }
    }
}

