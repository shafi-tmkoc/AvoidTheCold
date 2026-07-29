using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace AvoidTheCold
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] AudioClip bgMusic;
        [SerializeField] bool manualLanguageSelection = false;
        [SerializeField]protected AudioLocalizationSO audioScript;
        public AudioSource voiceOverSource;
        public AudioSource sfxSource;
        public AudioSource backgroundSource;

        public AudioClip dragSFX, winSFX, connectSFX;

        protected override void Awake()
        {
            base.Awake();
            PlayBG();
            //RuntimeAudioLoader.Instance.PlayRuntimeAudio("Intro");
            Debug.Log("Audio Manager Play");
        }

        private void OnEnable()
        {
            //GameManager.OnLevelFailed += PlayLevelFailed;
        }
        void OnDisable()
        {
            //GameManager.OnLevelFailed -= PlayLevelFailed;
        }

        public void PlayBG()
        {
            PlayAudio(backgroundSource, bgMusic, false, null, true);
        }

        /// <summary>Stops the looping background track - e.g. while the storyboard is showing.</summary>
        public void StopBG()
        {
            if (backgroundSource != null && backgroundSource.isPlaying) backgroundSource.Stop();
        }

        public void PlaySFX(AudioClip clip)
        {
            PlayAudio(sfxSource, clip);
        }

        public void PlayLevelIntro(int levelIndex, Action onComplete = null)
        {
            //PlayAudio(voiceOverSource, audioScript.levelAudio[levelIndex].Intro, false, onComplete);
            float clipLength = RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioScript.levelAudio[levelIndex].Intro);
            if (onComplete != null)
            {
                if (clipLength < 0) clipLength = 0;
                DOVirtual.DelayedCall(clipLength, () => onComplete.Invoke());
            }

        }

        public void PlayLevelOutro(int levelIndex, Action onComplete = null)
        {
            // PlayAudio(voiceOverSource, audioScript.levelAudio[levelIndex].Outro, false, onComplete);
            float clipLength = RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioScript.levelAudio[levelIndex].Outro);
            if (onComplete != null)
            {
                if (clipLength < 0) clipLength = 0;
                DOVirtual.DelayedCall(clipLength, () => onComplete.Invoke());
            }
        }

        public void PlayLevelHint(int levelIndex, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(audioScript.levelAudio[levelIndex].Ingame)) return;
            // if (audioScript.levelAudio[levelIndex].Ingame == null) return;
            // PlayAudio(voiceOverSource, audioScript.levelAudio[levelIndex].Ingame, false, onComplete);
            float clipLength = RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioScript.levelAudio[levelIndex].Ingame);
            if (onComplete != null)
            {
                if (clipLength < 0) clipLength = 0;
                DOVirtual.DelayedCall(clipLength, () => onComplete.Invoke());
            }
        }

        public void PlayLevelFailed()
        {
            // PlayAudio(voiceOverSource, audioScript.timeUp[UnityEngine.Random.Range(0, audioScript.timeUp.Count - 1)], false);
            RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioScript.timeUp[UnityEngine.Random.Range(0, audioScript.timeUp.Count - 1)]);
        }

        public void PlayRetry()
        {
            //if (GameManager.Instance.gameState == GameState.Stop)   return;
            // PlayAudio(voiceOverSource, audioScript.retry[UnityEngine.Random.Range(0, audioScript.retry.Count - 1)], false);
            RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioScript.timeUp[UnityEngine.Random.Range(0, audioScript.retry.Count - 1)]);
        }

        public void PlayAudio(AudioSource _source, AudioClip _clip, bool isOneShot = true, Action onClipEnd = null, bool loop = false)
        {
            if (_clip == null || _source == null) return;
            if (isOneShot)
            {
                _source.PlayOneShot(_clip);

            }
            else
            {
                _source.Stop();
                _source.loop = loop;
                _source.clip = _clip;
                _source.Play();
            }

            if (onClipEnd != null)
            {
                Debug.Log("audio action trigger");
                StartCoroutine(waitForClipEnd_CO(_clip, onClipEnd));
            }

        }


        IEnumerator waitForClipEnd_CO(AudioClip _clip, Action action = null)
        {

            yield return new WaitForSeconds(_clip.length);
            action?.Invoke();
            Debug.Log("audio action invoked");
        }

        public void PlayRandomVO(List<AudioClip> audioClips, Action callback = null)
        {
            PlayAudio(voiceOverSource, audioClips[UnityEngine.Random.Range(0, audioClips.Count)], false, callback);
        }

        public  void Connect()
        {
            AudioManager.Instance.PlayAudio(AudioManager.Instance.sfxSource, AudioManager.Instance.connectSFX);
        }

        public void Win()
        {
            AudioManager.Instance.PlayAudio(AudioManager.Instance.sfxSource, AudioManager.Instance.winSFX);
        }

        public void PlayFromServer(string voiceoverTitle)
        {
            if (RuntimeAudioLoader.Instance != null)
            {
                float clipLength = RuntimeAudioLoader.Instance.PlayRuntimeAudio(voiceoverTitle);
            }
            Debug.Log($"[VoiceOverPlayer] (stub) Would request server VO for '{voiceoverTitle}' - wire up the real package call in PlayFromServer()");
        }
    }
}
