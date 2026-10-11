using System;
using UnityEngine;

namespace SlotMachine.Audio
{
    /// <summary>
    /// Central audio manager coordinating all sound effects, reel loops,
    /// mechanical clacks, win chimes, and coin tick counters.
    /// Includes procedural audio synthesis fallback if audio clips are not assigned.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource spinLoopSource;
        [SerializeField] private AudioSource winSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip leverPullClip;
        [SerializeField] private AudioClip reelSpinLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip coinCountClip;
        [SerializeField] private AudioClip winSmallClip;
        [SerializeField] private AudioClip winJackpotClip;
        [SerializeField] private AudioClip bonusTriggerClip;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupAudioSources();
            EnsureAudioClips();
        }

        private void SetupAudioSources()
        {
            // Ensure an AudioListener exists in the scene so audio can actually be heard!
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.gameObject.AddComponent<AudioListener>();
                }
                else
                {
                    gameObject.AddComponent<AudioListener>();
                }
            }

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (spinLoopSource == null) spinLoopSource = gameObject.AddComponent<AudioSource>();
            if (winSource == null) winSource = gameObject.AddComponent<AudioSource>();

            sfxSource.spatialBlend = 0f; // Pure 2D
            spinLoopSource.spatialBlend = 0f;
            winSource.spatialBlend = 0f;

            spinLoopSource.loop = true;
            spinLoopSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            winSource.playOnAwake = false;
        }

        private void EnsureAudioClips()
        {
            if (buttonClickClip == null) buttonClickClip = GenerateClickClip();
            if (leverPullClip == null) leverPullClip = GenerateLeverClip();
            if (reelSpinLoopClip == null) reelSpinLoopClip = GenerateSpinLoopClip();
            if (reelStopClip == null) reelStopClip = GenerateStopClip();
            if (coinCountClip == null) coinCountClip = GenerateCoinClip();
            if (winSmallClip == null) winSmallClip = GenerateWinSmallClip();
            if (winJackpotClip == null) winJackpotClip = GenerateJackpotClip();
        }

        public void PlayButtonClick()
        {
            if (buttonClickClip != null && sfxSource != null)
                sfxSource.PlayOneShot(buttonClickClip, 1.0f);
        }

        public void PlayLeverPull()
        {
            if (leverPullClip != null && sfxSource != null)
                sfxSource.PlayOneShot(leverPullClip, 1.0f);
        }

        public void StartReelSpinSound()
        {
            if (reelSpinLoopClip != null && spinLoopSource != null)
            {
                spinLoopSource.clip = reelSpinLoopClip;
                spinLoopSource.volume = 0.85f;
                spinLoopSource.Play();
            }
        }

        public void StopReelSpinSound()
        {
            if (spinLoopSource != null && spinLoopSource.isPlaying)
                spinLoopSource.Stop();
        }

        public void PlayReelStop()
        {
            if (reelStopClip != null && sfxSource != null)
                sfxSource.PlayOneShot(reelStopClip, 1.0f);
        }

        public void PlayCoinCount()
        {
            if (coinCountClip != null && sfxSource != null)
                sfxSource.PlayOneShot(coinCountClip, 0.9f);
        }

        public void PlayWin(bool isJackpot)
        {
            if (winSource == null) return;
            AudioClip clip = isJackpot ? winJackpotClip : winSmallClip;
            if (clip != null)
                winSource.PlayOneShot(clip, 1.0f);
        }

        public void PlayBonusTrigger()
        {
            if (winSource == null) return;
            if (bonusTriggerClip != null)
                winSource.PlayOneShot(bonusTriggerClip, 1.0f);
            else if (winJackpotClip != null)
                winSource.PlayOneShot(winJackpotClip, 1.0f);
        }

        #region Procedural Audio Clip Synthesizers

        private AudioClip GenerateClickClip()
        {
            int sr = 44100;
            int length = (int)(sr * 0.05f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sr;
                samples[i] = Mathf.Sin(2 * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 80f);
            }
            var clip = AudioClip.Create("proc_click", length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateLeverClip()
        {
            int sr = 44100;
            int length = (int)(sr * 0.25f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sr;
                float click = Mathf.Sin(2 * Mathf.PI * 320f * t) * Mathf.Sin(2 * Mathf.PI * 45f * t);
                samples[i] = click * Mathf.Exp(-t * 12f) * 0.8f;
            }
            var clip = AudioClip.Create("proc_lever", length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateSpinLoopClip()
        {
            int sr = 44100;
            int length = (int)(sr * 0.4f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sr;
                float phase = (t % 0.08f) / 0.08f;
                samples[i] = Mathf.Sin(2 * Mathf.PI * 350f * phase) * Mathf.Exp(-phase * 20f) * 0.4f;
            }
            var clip = AudioClip.Create("proc_spin_loop", length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateStopClip()
        {
            int sr = 44100;
            int length = (int)(sr * 0.15f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sr;
                samples[i] = (Mathf.Sin(2 * Mathf.PI * 160f * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * 80f * t)) * Mathf.Exp(-t * 25f) * 0.9f;
            }
            var clip = AudioClip.Create("proc_stop", length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateCoinClip()
        {
            int sr = 44100;
            int length = (int)(sr * 0.08f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sr;
                samples[i] = Mathf.Sin(2 * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 40f) * 0.6f;
            }
            var clip = AudioClip.Create("proc_coin", length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateWinSmallClip()
        {
            int sr = 44100;
            int length = (int)(sr * 0.8f);
            float[] samples = new float[length];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDur = 0.2f;

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sr;
                int nIdx = Mathf.Min(3, (int)(t / noteDur));
                float localT = t - (nIdx * noteDur);
                float f = notes[nIdx];
                samples[i] = Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-localT * 8f) * 0.6f;
            }
            var clip = AudioClip.Create("proc_win_small", length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateJackpotClip()
        {
            int sr = 44100;
            int length = (int)(sr * 1.5f);
            float[] samples = new float[length];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f, 1567.98f };
            float noteDur = 0.22f;

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sr;
                int nIdx = Mathf.Min(notes.Length - 1, (int)(t / noteDur));
                float localT = t - (nIdx * noteDur);
                float f = notes[nIdx];
                float tone = Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * (f * 2) * t);
                samples[i] = tone * Mathf.Exp(-localT * 6f) * 0.7f;
            }
            var clip = AudioClip.Create("proc_jackpot", length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
