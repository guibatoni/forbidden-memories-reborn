using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Presentation
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        private float yaw = 0f;
        private float elevation = 40f;
        private float distance = 22.5f;
        private Vector2 lastPointer;
        private bool rotating;

        public void ResetView()
        {
            yaw = 0f; elevation = 40f; distance = 22.5f;
            UpdateTransform();
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var pointer = mouse.position.ReadValue();
                if (mouse.rightButton.wasPressedThisFrame) { rotating = true; lastPointer = pointer; }
                if (mouse.rightButton.wasReleasedThisFrame) rotating = false;
                if (rotating)
                {
                    var delta = pointer - lastPointer;
                    yaw = Mathf.Clamp(yaw + delta.x * 0.13f,-32f,32f);
                    elevation = Mathf.Clamp(elevation - delta.y*0.10f,29f,65f);
                }
                lastPointer = pointer;
                var wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) distance = Mathf.Clamp(distance-wheel*0.008f,16.5f,30f);
            }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ResetView();
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            var target = new Vector3(0,0,0f);
            var rotation = Quaternion.Euler(elevation,yaw,0f);
            transform.position = target + rotation * new Vector3(0,0,-distance);
            transform.LookAt(target);
        }
    }
}
