using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(VoxelizeMesh))]
    public class Voxelization : MonoBehaviour
    {
        private void Start()
        {
            VoxelizeMesh voxelizer = GetComponent<VoxelizeMesh>();
            voxelizer.Voxelize();

            // ReSharper disable once LocalVariableHidesMember
            Transform transform = this.transform;
            Vector3 scale       = new(voxelizer.ParticleSize, voxelizer.ParticleSize, voxelizer.ParticleSize);
            foreach (Vector3 position in voxelizer.Positions)
            {
                Transform particle  = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                particle.position   = position;
                particle.localScale = scale;
                particle.parent     = transform;
            }
        }
    }
}
