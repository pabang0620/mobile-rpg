using UnityEngine;

namespace Sapphire.Presentation.Combat
{
    public class CameraShake : MonoBehaviour
    {
        private float shakeMagnitude;
        private float shakeDuration;
        private float shakeTimer;

        public void Shake(float magnitude = 0.08f, float duration = 0.15f)
        {
            shakeMagnitude = magnitude;
            shakeDuration = duration;
            shakeTimer = duration;
        }

        private void LateUpdate()
        {
            if (shakeTimer <= 0f) return;

            shakeTimer -= Time.deltaTime;
            float t = shakeTimer / shakeDuration;
            float currentMag = shakeMagnitude * t;

            Vector3 offset = new Vector3(
                Random.Range(-currentMag, currentMag),
                Random.Range(-currentMag, currentMag),
                0f
            );

            transform.position += offset;

            if (shakeTimer <= 0f) shakeTimer = 0f;
        }
    }
}
