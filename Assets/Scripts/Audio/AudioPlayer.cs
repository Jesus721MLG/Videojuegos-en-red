using System;
using UnityEngine;

namespace Battleship
{
    public class AudioPlayer : MonoBehaviour
    {
        public static AudioPlayer Instance;

        public Sound[] sounds;
        [SerializeField] float _fadeInTime = 5f;
        [SerializeField] float _fadeOutTime = 1f;

        void Awake()
        {
            Instance = this;
            SoundSetup();
        }

        void Start()
        {
            FadeIn("Theme");
            PlayerTurn.OnWin += PlayWinMusic;
        }

        void SoundSetup()
        {
            foreach (Sound sound in sounds)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;
            }
        }

        public void Play(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);

            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found!");
                return;
            }

            s.source.Play();
        }

        public void Stop(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);

            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found!");
                return;
            }

            s.source.Stop();
        }

        public void FadeIn(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);

            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found!");
                return;
            }

            StartCoroutine(AudioFader.CO_FadeIn(s, _fadeInTime));
        }

        public void FadeOut(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);

            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found!");
                return;
            }

            StartCoroutine(AudioFader.CO_FadeOut(s, _fadeOutTime));
        }

        void PlayWinMusic()
        {
            FadeOut("Theme");
            Play("Victory");
        }

        private void OnDisable()
        {
            PlayerTurn.OnWin -= PlayWinMusic;
        }
    }
}
