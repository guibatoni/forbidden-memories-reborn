using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Presentation
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        private float yaw = 0f;
        private float elevation = 40f;
        private float distance = 22.5f;

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
                // Right click is reserved for card inspection. Keep the
                // approved stable arena view and existing wheel/R controls.
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
