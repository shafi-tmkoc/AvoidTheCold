using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AvoidTheCold
{
    // public enum AudioLanguage
    // {
    //     EnglishUS,
    //     Bengali,
    //     Hindi,
    //     Tamil,
    //     Marathi,
    //     French,
    //     Malayalam,
    //     Punjabi
    // }

    [CreateAssetMenu(fileName = "LocalizedAudio", menuName = "ScriptableObject/LocalizedAudio")]

    public class AudioLocalizationSO : ScriptableObject
    {
        // public AudioLanguage audioLanguage;
        // public AudioClip background;
        public List<string> timeUp, retry;
        public List<LevelAudios> levelAudio;
    }

    [Serializable]
    public struct LevelAudios
    {
        public string Intro, Ingame, Outro;
    }

}