using UnityEngine;

namespace Sapphire.Presentation.Camera
{
    /// <summary>
    /// Follows a target transform on the XY plane. Pixel snapping and
    /// integer zoom are entirely delegated to Unity's PixelPerfectCamera
    /// component (com.unity.2d.pixel-perfect) placed on the same GameObject -
    /// this class never rounds/snaps positions itself.
    /// </summary>
    public class CameraFollowRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            float smoothing = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, smoothing);
        }
    }
}
