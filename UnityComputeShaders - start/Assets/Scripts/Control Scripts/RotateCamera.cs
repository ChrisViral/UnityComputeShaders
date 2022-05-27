using UnityEngine;

namespace UnityComputeShaders
{
    public class RotateCamera : MonoBehaviour
    {
        [SerializeField]
        private Transform target;    //the target object
        [SerializeField]
        private float speed = 10f; //a speed modifier

        private Vector3 point;      //the coord to the point where the camera looks at

        private void Start()
        {
            //Set up things on the start method
            this.point = this.target.transform.position; //get target's coords
            this.transform.LookAt(this.point);           //makes the camera look to it
        }

        private void Update()
        {
            //makes the camera rotate around "point" coords, rotating around its Y axis, 20 degrees per second times the speed modifier
            this.transform.RotateAround(this.point, Vector3.up, 20f * Time.deltaTime * this.speed);
        }
    }
}