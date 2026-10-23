using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using StardewVoices.Utils;

namespace StardewVoices.Audio
{
    /// <summary>
    /// One playable voice line, from a loose .ogg or .wav file.
    /// The file is only read when the line plays, and the decoded audio is freed when it
    /// stops, so looking a line up costs nothing and only the playing line uses memory.
    /// </summary>
    public class FileVoiceLine
    {
        private readonly string _path;
        private SoundEffect _clip;
        private SoundEffectInstance _instance;
        private float _volume = 1f;

        public FileVoiceLine(string path, string character, string lineId)
        {
            _path = path;
            Character = character;
            LineId = lineId;
        }

        /// <summary>The line id this audio was found under (the fallback id if one was used).</summary>
        public string LineId { get; }

        public string Character { get; }

        /// <summary>0 (silent) to 1 (full). Takes effect immediately if the line is playing.</summary>
        public float Volume
        {
            get => _volume;
            set
            {
                // SoundEffectInstance throws outside 0..1.
                _volume = Math.Clamp(value, 0f, 1f);
                if (_instance != null)
                    _instance.Volume = _volume;
            }
        }

        public bool IsPlaying => _instance?.State == SoundState.Playing;

        public void Play()
        {
            // Playing again restarts from the beginning.
            Stop();

            _clip = LoadClip(_path);
            if (_clip == null)
                return;

            _instance = _clip.CreateInstance();
            _instance.Volume = _volume;
            _instance.Play();
        }

        /// <summary>Stops playback and frees the decoded audio.</summary>
        public void Stop()
        {
            // The instance has to go before the clip: disposing a clip doesn't stop
            // instances made with CreateInstance, and they'd keep reading freed audio.
            _instance?.Stop();
            _instance?.Dispose();
            _instance = null;

            _clip?.Dispose();
            _clip = null;
        }

        /// <summary>Reads and decodes the whole file. Null (and a warning) if it can't be loaded.</summary>
        private static SoundEffect LoadClip(string path)
        {
            try
            {
                Stopwatch timer = Stopwatch.StartNew();
                using FileStream stream = File.OpenRead(path);
                SoundEffect clip = SoundEffect.FromStream(stream, vorbis: FolderVoiceSource.IsOgg(path));

                // Decoding happens on the game thread, so keep an eye on this in the logs.
                ErrorLogger.Trace($"Loaded {Path.GetFileName(path)} in {timer.ElapsedMilliseconds} ms");
                return clip;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Couldn't load voice file {path}: {e.Message}");
                return null;
            }
        }
    }
}
