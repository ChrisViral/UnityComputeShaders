using UnityEngine;

namespace UnityComputeShaders
{
    public class StarGlow : MonoBehaviour
    {
        #region Field

        [Range(0, 1)]
        public float threshold = 1;

        [Range(0, 10)]
        public float intensity = 1;

        [Range(1, 20)]
        public int divide = 3;

        [Range(1, 5)]
        public int iteration = 5;

        [Range(0, 1)]
        public float attenuation = 1;

        [Range(0, 360)]
        public float angleOfStreak;

        [Range(1, 16)]
        public int numOfStreaks = 4;

        public Material material;

        public Color color = Color.white;

        private int compositeTexID;
        private int compositeColorID;
        private int brightnessSettingsID;
        private int iterationID;
        private int offsetID;

        #endregion Field

        #region Method
        private void Start()
        {
            this.compositeTexID   = Shader.PropertyToID("_CompositeTex");
            this.compositeColorID = Shader.PropertyToID("_CompositeColor");
            this.brightnessSettingsID   = Shader.PropertyToID("_BrightnessSettings");
            this.iterationID      = Shader.PropertyToID("_Iteration");
            this.offsetID         = Shader.PropertyToID("_Offset");
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
        }

        #endregion Method
    }
}