using UnityEngine;
using AssetKits.ParticleImage;

namespace AvoidTheCold
{
    /// <summary>
    /// Enables this GameObject and plays every ParticleImage effect under it
    /// once every puzzle slot has been filled (e.g. a celebratory confetti
    /// burst), then stops and disables it again via ResetForNewAttempt()
    /// when a fresh level attempt starts. Subscribes in Awake/OnDestroy
    /// rather than OnEnable/OnDisable because this object is expected to
    /// START disabled - OnEnable would never fire on its own to catch the
    /// very first "all slots filled" event.
    /// </summary>
    public class ShowOnPuzzleComplete : MonoBehaviour
    {
        [SerializeField] private PuzzleProgressTracker progressTracker;

        [SerializeField] private ParticleImage[] _particleEffects;

        private void Awake()
        {
            _particleEffects = GetComponentsInChildren<ParticleImage>(true);
            if (progressTracker != null) progressTracker.OnAllSlotsFilled += HandleAllSlotsFilled;
        }

        private void OnDestroy()
        {
            if (progressTracker != null) progressTracker.OnAllSlotsFilled -= HandleAllSlotsFilled;
        }

        private void HandleAllSlotsFilled()
        {
            Debug.Log($"[ShowOnPuzzleComplete] All slots filled - enabling {name}");
            gameObject.SetActive(true);

            foreach (var p in _particleEffects)
            {
                if (p != null) p.Play();
            }
        }

        /// <summary>Call when a fresh level attempt starts (next level or restart) to stop and hide this effect again.</summary>
        public void ResetForNewAttempt()
        {
            Debug.Log($"[ShowOnPuzzleComplete] New attempt - disabling {name}");

            foreach (var p in _particleEffects)
            {
                if (p != null) p.Stop();
            }

            gameObject.SetActive(false);
        }
    }
}
