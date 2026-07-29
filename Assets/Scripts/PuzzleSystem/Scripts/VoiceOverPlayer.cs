using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Single entry point for playing a voiceover clip by its server-side
    /// voiceover_title. Guarantees only one VO plays at a time - starting a
    /// new one always stops whatever was already playing.
    ///
    /// Real playback goes through AudioManager.Instance.PlayFromServer,
    /// which in turn asks RuntimeAudioLoader for the downloaded clip (that
    /// call also stops its shared AudioSource before playing, so overlap is
    /// covered on both ends). Optional local test clips can still be
    /// assigned in the Inspector to preview VO without a downloaded clip.
    /// </summary>
    public class VoiceOverPlayer : MonoBehaviour
    {
        [System.Serializable]
        public struct VOEntry
        {
            public string voiceoverTitle;
            public AudioClip clip;
        }

        [Header("Local test clips (optional - used instead of the server until it's wired up)")]
        [SerializeField] private VOEntry[] testClips;
        [SerializeField] private AudioSource audioSource;

        private string _currentTitle;

        /// <summary>Plays the voiceover matching this title, cutting off any VO already playing.</summary>
        public void Play(string voiceoverTitle)
        {
            if (string.IsNullOrEmpty(voiceoverTitle))
            {
                Debug.Log("[VoiceOverPlayer] Play called with empty title - ignoring");
                return;
            }

            Stop();

            Debug.Log($"[VoiceOverPlayer] Playing '{voiceoverTitle}'");
            _currentTitle = voiceoverTitle;

            var clip = FindTestClip(voiceoverTitle);
            if (clip != null && audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                PlayFromServer(voiceoverTitle);
            }
        }

        /// <summary>Stops whatever VO is currently playing, if any.</summary>
        public void Stop()
        {
            if (_currentTitle == null) return;

            Debug.Log($"[VoiceOverPlayer] Stopping '{_currentTitle}'");
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            _currentTitle = null;
        }

        private AudioClip FindTestClip(string voiceoverTitle)
        {
            if (testClips == null) return null;

            foreach (var entry in testClips)
            {
                if (entry.voiceoverTitle == voiceoverTitle) return entry.clip;
            }
            return null;
        }

        /// <summary>Routes to AudioManager, which owns the real downloaded-clip playback.</summary>
        private void PlayFromServer(string voiceoverTitle)
        {
            if (AudioManager.Instance == null)
            {
                Debug.Log($"[VoiceOverPlayer] AudioManager not available - cannot play '{voiceoverTitle}'");
                return;
            }

            Debug.Log($"[VoiceOverPlayer] Requesting server VO '{voiceoverTitle}' via AudioManager");
            AudioManager.Instance.PlayFromServer(voiceoverTitle);
        }
    }
}
