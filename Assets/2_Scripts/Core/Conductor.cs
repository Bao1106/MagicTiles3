using UnityEngine;

public class Conductor : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;

        [Tooltip("Compensates device audio output latency. Positive shifts judgement later. " +
                 "Android ranges from 40 to 150 ms — tune this on the target device, not in the editor.")]
        [SerializeField] private float latencyOffset = 0f;

        private double dspSongStart;
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

            // Advance by real elapsed time every frame, always. The audio clock stays the
            // authority, but it only moves once per DSP block — 21.3 ms at this project's
            // 1024-sample buffer, which is LONGER than a 16.7 ms frame. Snapping onto it
            // whenever it ticked made song time lurch 4.7 ms one frame and 21.3 ms the next
            // while frames were a steady 16.7. Tile Y is a pure function of song time, so the
            // tiles juddered at a locked 60 FPS: the frame counter was fine, the motion wasn't.
            smoothedDspTime += Time.unscaledDeltaTime;

            // Clamp, never assign. dspTime reports the start of the block being processed, so
            // true song time is somewhere in [dsp, dsp + block]. Staying inside that window
            // corrects real drift without throwing away a frame of motion on every tick.
            if (smoothedDspTime < dsp) smoothedDspTime = dsp;
            else if (smoothedDspTime > dsp + maxInterpolation) smoothedDspTime = dsp + maxInterpolation;

            SongPosition = (float)(smoothedDspTime - dspSongStart) - latencyOffset;
        }
    }