using UnityEngine;

namespace UnityComputeShaders
{
    public class Spinner : MonoBehaviour
    {
        private void Update()
        {
            this.transform.Rotate(Vector3.up, 2f);
        }
    }
}
