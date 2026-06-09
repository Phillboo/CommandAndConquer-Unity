using UnityEngine;

namespace RTS
{
    public class RTSCamera : MonoBehaviour
    {
        public float panSpeed = 32f;
        public float edgeSize = 14f;
        public float zoomStep = 90f;
        public float minY = 14f, maxY = 58f;
        public Vector2 limit = new Vector2(62f, 62f);

        void LateUpdate()
        {
            Vector3 pos = transform.position;
            float dx = Input.GetAxisRaw("Horizontal");
            float dz = Input.GetAxisRaw("Vertical");

            Vector3 m = Input.mousePosition;
            if (m.x >= 0f && m.x <= Screen.width && m.y >= 0f && m.y <= Screen.height)
            {
                if (m.x < edgeSize) dx -= 1f;
                else if (m.x > Screen.width - edgeSize) dx += 1f;
                if (m.y < edgeSize) dz -= 1f;
                else if (m.y > Screen.height - edgeSize) dz += 1f;
            }

            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = transform.right; right.y = 0f; right.Normalize();
            Vector3 move = right * Mathf.Clamp(dx, -1f, 1f) + fwd * Mathf.Clamp(dz, -1f, 1f);
            if (move.sqrMagnitude > 1f) move.Normalize();
            pos += move * panSpeed * Time.unscaledDeltaTime;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                pos += transform.forward * scroll * zoomStep;

            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            pos.x = Mathf.Clamp(pos.x, -limit.x, limit.x);
            pos.z = Mathf.Clamp(pos.z, -limit.y, limit.y);
            transform.position = pos;
        }
    }
}
