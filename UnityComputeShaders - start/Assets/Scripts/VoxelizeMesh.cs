using System.Collections.Generic;
using UnityEngine;

namespace UnityComputeShaders
{
    public class VoxelizeMesh : MonoBehaviour 
    {
        public Mesh meshToVoxelize;
        public int yParticleCount = 4;
        public int layer = 9;

        private float particleSize = 0;

        public float ParticleSize{
            get{
                return this.particleSize; 
            }
        }

        private List<Vector3> positions = new();

        public List<Vector3> PositionList
        {
            get
            {
                return this.positions;
            }
        }

        public void Voxelize(Mesh mesh)
        {
        
        }
    }
}
