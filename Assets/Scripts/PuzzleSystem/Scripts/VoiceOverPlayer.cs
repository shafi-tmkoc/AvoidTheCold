using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Single entry point for playing a voiceover clip by its server-side
    /// voiceover_title. Guarantees only one VO plays at a time - starting a
    /// new one always stops whatever was already playing.
    ///
    /// The actual "load and play audio from the server" call is not wired up
    /// yet (no such package exists in this project). PlayFromServer is the
    /// one place to replace once it is imported - see the TODO there. Until
    /// then, optional local test clips can be assigned in the Inspector so
    /// this can be exercised/tested without the server.
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

        /// <summary>
        /// TODO: replace this with the real VO package call once it's
        /// imported into the project, e.g.:
        ///   YourVOPackage.Instance.PlayVoiceOver(voiceoverTitle);
        /// Make sure whatever that call is also stops any VO it has in
        /// flight first, same as Stop() does for the local test path above.
        /// </summary>
        private void PlayFromServer(string voiceoverTitle)
        {
            /*float clipLength = RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioScript.levelAudio[levelIndex].Outro);
            if (onComplete != null)
            {
                if (clipLength < 0) clipLength = 0;
                DOVirtual.DelayedCall(clipLength, () => onComplete.Invoke());
            }
            Debug.Log($"[VoiceOverPlayer] (stub) Would request server VO for '{voiceoverTitle}' - wire up the real package call in PlayFromServer()");
        */}
    }
}
