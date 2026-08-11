using UnityEngine;

public class Conductor : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;

        [Tooltip("Compensates device audio output latency. Positive shifts judgement later. " +
                 "Android ranges from 40 to 150 ms — tune this on the target device, not in the editor.")]
        [SerializeField] private float latencyOffset = 0f;

        private double dspSongStart;
        private double lastDspTime;
        private double smoothedDspTime;
        private double maxInterpolation;

        /// <summary>
        /// Seconds since the song's first sample. Negative during the lead-in.
        /// </summary>
        public float SongPosition { get; private set; }

        public bool IsRunning { get; private set; }

        private void Awake()
        {
            // AudioSettings.dspTime only advances once per DSP block. That block length is the
            // physical upper bound on how far ahead interpolation may ever run.
            AudioSettings.GetDSPBufferSize(out int bufferLength, out _);
            maxInterpolation = (double)bufferLength / AudioSettings.outputSampleRate;
        }

        /// <param name="leadInSeconds">
        /// Silence before the first sample, so notes can already be falling when the music starts.
        /// SongPosition is negative during this window.
        /// </param>
        public void Play(AudioClip clip, float leadInSeconds)
        {
            musicSource.Stop();
            musicSource.clip = clip;

            double now = AudioSettings.dspTime;
            dspSongStart = now + leadInSeconds;
            lastDspTime = now;
            smoothedDspTime = now;
            SongPosition = (float)(now - dspSongStart) - latencyOffset;

            // PlayScheduled, not Play: Play starts on the next audio buffer, adding a random
            // offset of up to ~20 ms that differs every run. PlayScheduled is sample-accurate.
            musicSource.PlayScheduled(dspSongStart);
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning) return;

            double dsp = AudioSettings.dspTime;

            if (dsp > lastDspTime)
            {
                // Audio clock ticked — snap to truth.
                lastDspTime = dsp;
                smoothedDspTime = dsp;
            }
            else
            {
                // Same DSP block as last frame. Interpolate so tiles keep moving, but never
                // run further ahead than one block, or the next snap would jump backwards.
                smoothedDspTime = System.Math.Min(
                    smoothedDspTime + Time.unscaledDeltaTime,
                    lastDspTime + maxInterpolation);
            }

            SongPosition = (float)(smoothedDspTime - dspSongStart) - latencyOffset;
        }
    }