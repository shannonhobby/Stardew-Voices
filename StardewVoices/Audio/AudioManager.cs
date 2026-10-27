using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewVoices.Pathing;

namespace StardewVoices.Audio
{
    public class AudioManager
    {
        internal VoiceResolver Resolver { get; } = new VoiceResolver();

        private FileVoiceLine lastLine { get; set; }
        private FileVoiceLine currentLine { get; set; }

        // Lines evicted from currentLine/lastLine while still playing (see Release) — kept
        // here so SweepFinishedLines can still Stop()/Dispose() them once they finish, instead
        // of leaking a SoundEffectInstance and eventually hitting MonoGame's instance cap.
        private readonly List<FileVoiceLine> _pendingDisposal = new();

        private float _volume = 1f;
        private bool _muted;

        /// <summary>0 (silent) to 1 (full). Applies to the playing line and every line after it.</summary>
        public float Volume
        {
            get => _volume;
            set { _volume = value; ApplyVolume(); }
        }

        /// <summary>Silences voices without losing <see cref="Volume"/>. Used while validation runs.</summary>
        public bool Muted
        {
            get => _muted;
            set { _muted = value; ApplyVolume(); }
        }

        private float EffectiveVolume => Muted ? 0f : Volume;

        /// <param name="voicePackManager">Decides which source (a pack or the built-in voices) is active for each character.</param>
        public AudioManager(PathManager pathManager, VoicePackManager voicePackManager) {
            UseVoices(voicePackManager);

            FolderVoiceSource builtInVoices = voicePackManager.BuiltInVoices;
            if (builtInVoices.LineCount == 0)
                ErrorLogger.Warn($"No voice files found in {builtInVoices.RootPath}, so voices won't play.");
            else
                ErrorLogger.Info($"Built-in voices: {builtInVoices.Characters.Count} characters, {builtInVoices.LineCount} lines.");

            WarnAboutOldXactFiles(pathManager.ModPath.ToString());
        }

        /// <summary>
        /// Re-applies the player's choice of voice per character, e.g. after they change one in the
        /// config menu. Call <see cref="VoicePackManager.ApplyChoices"/> first. Takes effect from the
        /// next line; a line already playing finishes as it was.
        /// </summary>
        public void ApplyVoiceChoices(VoicePackManager voicePackManager)
        {
            UseVoices(voicePackManager);
        }

        private void UseVoices(VoicePackManager voicePackManager)
        {
            foreach (string character in voicePackManager.Characters)
                Resolver.SetSource(character, voicePackManager.GetActiveSource(character));
        }

        /// <summary>
        /// Before 3.1 voices shipped as XACT banks in an Audio folder. Updating the mod doesn't
        /// always delete them, and they're about 3 GB, so tell the player they're safe to remove.
        /// </summary>
        private static void WarnAboutOldXactFiles(string modPath)
        {
            string oldAudioFolder = Path.Combine(modPath, "Audio");
            if (!Directory.Exists(oldAudioFolder))
                return;

            string[] waveBanks = Directory.GetFiles(oldAudioFolder, "*.xwb");
            if (waveBanks.Length == 0)
                return;

            double sizeInGb = waveBanks.Sum(file => new FileInfo(file).Length) / 1_000_000_000d;
            ErrorLogger.Warn($"Found old voice files from an earlier Stardew Voices version in {oldAudioFolder}. " +
                $"They're no longer used. You can delete that folder to free {sizeInGb:0.#} GB.");
        }

        public void Play(FileVoiceLine line)
        {
            SweepFinishedLines();
            StopLastLine();

            if (line != null)
            {
                ErrorLogger.Trace($"Playing line");
                SetCurrentLine(line);
                line.Play();
            } else
            {
                ErrorLogger.Warn($"Unable to play line");
            }
        }


        public void Play(string lineId, string characterName)
        {
            SweepFinishedLines();
            SetCurrentLine(GetVoiceLine(lineId, characterName));

            StopLastLine();

            if(currentLine != null)
            {
                ErrorLogger.Trace($"Playing line: {characterName}: {lineId}");
                currentLine.Play();
            }
        }

        public void StopAllLines()
        {
            if (currentLine != null)
            {
                ErrorLogger.Trace("Current line stopped");
                currentLine.Stop();
                currentLine = null;

            }

            StopLastLine();

            // A deliberate "silence everything" request, so pending lines get cut off here
            // even if still playing (SweepFinishedLines only disposes ones that already finished).
            foreach (FileVoiceLine pending in _pendingDisposal)
                pending.Stop();
            _pendingDisposal.Clear();
        }

        /// <summary>Disposes any pending line that has finished playing. Called every tick and on every Play().</summary>
        public void SweepFinishedLines()
        {
            for (int i = _pendingDisposal.Count - 1; i >= 0; i--)
            {
                if (!_pendingDisposal[i].IsPlaying)
                {
                    _pendingDisposal[i].Stop();
                    _pendingDisposal.RemoveAt(i);
                }
            }
        }

        public void StopLastLine()
        {

            if (lastLine != null && lastLine.IsPlaying)
            {
                ErrorLogger.Trace("Last line stopped");
                lastLine.Stop();
            }
        }

        public Boolean LastLineHasFinished() {
            return (this.lastLine == null || (this.lastLine != null && !this.lastLine.IsPlaying));
        }

        public void NextLine()
        {
            if (this.currentLine != null)
            {
                ErrorLogger.Trace("Last line set to current line");
                FileVoiceLine previousLastLine = lastLine;
                this.lastLine = currentLine;
                Release(previousLastLine);
            }
        }

        /// <summary>
        /// Returns the audio for a line (falling back to the un-suffixed id, see
        /// <see cref="VoiceResolver.Resolve"/>), set to the current volume. Null if the
        /// character has no recording for it. Nothing is loaded until the line plays.
        /// </summary>
        public FileVoiceLine GetVoiceLine(string lineId, string characterName)
        {
            FileVoiceLine line = Resolver.Resolve(characterName, lineId);
            if (line == null)
            {
                ErrorLogger.WarnOnce($"No audio for {characterName}: {lineId}");
                return null;
            }

            line.Volume = EffectiveVolume;
            return line;
        }

        private void SetCurrentLine(FileVoiceLine line)
        {
            FileVoiceLine previousLine = currentLine;
            currentLine = line;
            if (line != null)
                line.Volume = EffectiveVolume;
            Release(previousLine);
        }

        /// <summary>
        /// Frees the audio of a line we've stopped tracking. A still-playing line is left to
        /// finish rather than cut off, but goes into _pendingDisposal so it still gets disposed
        /// once it's done instead of leaking.
        /// </summary>
        private void Release(FileVoiceLine line)
        {
            if (line == null || line == currentLine || line == lastLine)
                return;

            if (line.IsPlaying)
                _pendingDisposal.Add(line);
            else
                line.Stop();
        }

        private void ApplyVolume()
        {
            if (currentLine != null)
                currentLine.Volume = EffectiveVolume;
            if (lastLine != null)
                lastLine.Volume = EffectiveVolume;
        }
    }
}
