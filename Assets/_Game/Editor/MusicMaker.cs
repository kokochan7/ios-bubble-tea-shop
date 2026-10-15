using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Writes each shop's background music as a looping WAV file, composed and played by code
    /// (no downloads, no licences). Both shops get laid-back hip hop beats: a boom-bap kick and
    /// snare on swung sixteenths, hi-hat rolls, a deep 808 bass, warm electric-piano seventh chords
    /// and a short plucked hook on a pentatonic scale (a nod to the guzheng). The Boba Shop plays a
    /// minor groove in A, the Matcha House a mellow jazzy one in D. The scene builder calls this,
    /// and you can rebuild the tracks from Bubble Tea > Audio > Rebuild Music.
    /// </summary>
    internal static class MusicMaker
    {
        private const string Folder = "Assets/_Game/Audio";
        private const int SampleRate = 32000;

        internal enum Track { BobaShop, MatchaHouse }

        private enum Quality { Major, Minor, Major7, Minor7, Dominant7 }

        private sealed class Song
        {
            public string FileName;
            public float Bpm;
            public int Root;                 // MIDI note of the key
            public (int offset, Quality quality)[] Chords; // one per bar
            public int[] Scale;              // melody notes, semitones above the root
            public float Swing;              // how late the off sixteenths land, as a share of a sixteenth
            public float RestChance;         // melody density
            public int HookBars = 2;         // the melody's rhythm and shape repeat every this many bars
            public int Seed;
        }

        private static readonly Song BobaSong = new()
        {
            FileName = "Music_BobaShop_HipHop",
            Bpm = 90f,
            Root = 57, // A minor
            Chords = new[]
            {
                (0, Quality.Minor7), (5, Quality.Minor7), (8, Quality.Major7), (7, Quality.Minor7),
                (0, Quality.Minor7), (5, Quality.Minor7), (3, Quality.Major7), (7, Quality.Dominant7),
            },
            Scale = new[] { 0, 3, 5, 7, 10, 12, 15, 17, 19 }, // minor pentatonic
            Swing = 0.32f,
            RestChance = 0.45f,
            HookBars = 2,
            Seed = 11,
        };

        private static readonly Song MatchaSong = new()
        {
            FileName = "Music_MatchaHouse_HipHop",
            Bpm = 86f,
            Root = 50, // D major, jazzy
            Chords = new[]
            {
                (2, Quality.Minor7), (7, Quality.Dominant7), (0, Quality.Major7), (9, Quality.Minor7),
                (2, Quality.Minor7), (7, Quality.Dominant7), (4, Quality.Minor7), (9, Quality.Dominant7),
            },
            Scale = new[] { 0, 2, 4, 7, 9, 12, 14, 16, 19 }, // major pentatonic
            Swing = 0.36f,
            RestChance = 0.5f,
            HookBars = 2,
            Seed = 29,
        };

        [MenuItem("Bubble Tea/Audio/Rebuild Music")]
        public static void RebuildAll()
        {
            Write(BobaSong);
            Write(MatchaSong);
            Debug.Log("Bubble Tea: music rebuilt in " + Folder);
        }

        /// <summary>The track's clip, written first if it doesn't exist yet.</summary>
        public static AudioClip Get(Track track)
        {
            Song song = track == Track.BobaShop ? BobaSong : MatchaSong;
            string path = $"{Folder}/{song.FileName}.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            return clip != null ? clip : Write(song);
        }

        private static AudioClip Write(Song song)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            float[] samples = Render(song);
            string path = $"{Folder}/{song.FileName}.wav";
            WriteWav(path, samples);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---------- Composing and playing ----------

        private static float[] Render(Song song)
        {
            float beat = 60f / song.Bpm;
            float sixteenth = beat / 4f;
            int bars = song.Chords.Length * 2; // the progression plays twice per loop
            int length = Mathf.RoundToInt(bars * 4 * beat * SampleRate);
            int tail = SampleRate * 3; // notes ringing past the end wrap round to the start, so the loop is seamless
            var mix = new float[length + tail];
            var random = new System.Random(song.Seed);

            // When the n-th sixteenth of a bar lands: the off ones are pushed late for the swing.
            float At(float barStart, int n) => barStart + n * sixteenth + (n % 2 == 1 ? song.Swing * sixteenth : 0f);

            int previous = 3; // index into the scale
            for (int bar = 0; bar < bars; bar++)
            {
                (int offset, Quality quality) = song.Chords[bar % song.Chords.Length];
                int[] chord = ChordTones(quality);
                float barStart = bar * 4 * beat;
                bool turnaround = bar % 4 == 3;

                // Boom-bap drums: kicks on 1 and the "and" of 3, snares on 2 and 4, a ghost kick now and then.
                int[] kicks = bar % 2 == 0 ? new[] { 0, 7, 10 } : new[] { 0, 3, 10, 14 };
                foreach (int n in kicks) Kick(mix, At(barStart, n), n == 0 ? 0.55f : 0.42f);
                Snare(mix, At(barStart, 4), 0.32f, random);
                Snare(mix, At(barStart, 12), 0.32f, random);
                if (turnaround) Snare(mix, At(barStart, 15), 0.1f, random);

                // Hi-hats on every eighth, quiet ghost hats on the sixteenths, and a quick roll at the end of a phrase.
                for (int n = 0; n < 16; n++)
                {
                    if (turnaround && n >= 12)
                    {
                        for (int r = 0; r < 3; r++) Hat(mix, barStart + n * sixteenth + r * sixteenth / 3f, 0.035f + 0.01f * r, random);
                        continue;
                    }
                    Hat(mix, At(barStart, n), n % 2 == 0 ? (n % 4 == 2 ? 0.06f : 0.045f) : 0.018f, random);
                }

                // 808 bass: a deep, long note following the kick, on the chord's root.
                int bassNote = song.Root - 24 + offset;
                while (bassNote < 31) bassNote += 12;
                while (bassNote > 42) bassNote -= 12;
                Bass808(mix, At(barStart, 0), bassNote, beat * 1.7f, 0.42f);
                Bass808(mix, At(barStart, 10), bassNote, beat * 1.3f, 0.34f);
                if (bar % 2 == 1) Bass808(mix, At(barStart, 14), bassNote + 7, beat * 0.5f, 0.24f);

                // Warm electric-piano chords: held on the one, pushed again just before beat 3.
                for (int n = 0; n < chord.Length; n++)
                {
                    int note = song.Root + offset + chord[n];
                    while (note > song.Root + 14) note -= 12;
                    EPiano(mix, At(barStart, 0) + n * 0.015f, note, beat * 2.2f, 0.06f);
                    EPiano(mix, At(barStart, 7) + n * 0.015f, note, beat * 1.8f, 0.045f);
                }

                // Hook: a plucked melody on eighths, landing on chord tones on the beat. The choices
                // repeat every few bars, which is what makes it stick.
                var hook = new System.Random(song.Seed * 31 + bar % Mathf.Max(1, song.HookBars));
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    bool onBeat = eighth % 2 == 0;
                    double roll = hook.NextDouble();
                    int wander = hook.Next(-2, 3);
                    if (roll < song.RestChance + (onBeat ? -0.15 : 0.1)) continue;
                    if (turnaround && eighth >= 4) continue; // leave room for the hat roll

                    int step;
                    if (onBeat) step = NearestChordTone(song, previous, offset, chord);
                    else step = Mathf.Clamp(previous + wander, 0, song.Scale.Length - 1);
                    previous = step;

                    int note = song.Root + 12 + song.Scale[step];
                    Pluck(mix, At(barStart, eighth * 2), note, beat * 1.2f, 0.12f, decay: 4f, bright: 0.6f);
                }
            }

            // Wrap the tail round, add a little vinyl hiss, then normalize.
            var output = new float[length];
            for (int i = 0; i < mix.Length; i++) output[i % length] += mix[i];
            for (int i = 0; i < length; i++) output[i] += ((float)random.NextDouble() - 0.5f) * 0.005f;
            float peak = 0.0001f;
            foreach (float s in output) peak = Mathf.Max(peak, Mathf.Abs(s));
            float gain = 0.85f / peak;
            for (int i = 0; i < length; i++) output[i] = (float)Math.Tanh(output[i] * gain * 1.1f) * 0.9f;
            return output;
        }

        private static int[] ChordTones(Quality quality) => quality switch
        {
            Quality.Minor => new[] { 0, 3, 7 },
            Quality.Major7 => new[] { 0, 4, 7, 11 },
            Quality.Minor7 => new[] { 0, 3, 7, 10 },
            Quality.Dominant7 => new[] { 0, 4, 7, 10 },
            _ => new[] { 0, 4, 7 },
        };

        // The scale step closest to the previous note that belongs to the current chord.
        private static int NearestChordTone(Song song, int previous, int chordOffset, int[] chord)
        {
            int best = previous, bestDistance = int.MaxValue;
            for (int i = 0; i < song.Scale.Length; i++)
            {
                int pitchClass = ((song.Scale[i] - chordOffset) % 12 + 12) % 12;
                if (Array.IndexOf(chord, pitchClass) < 0) continue;
                int distance = Mathf.Abs(i - previous);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        // ---------- Instruments ----------

        private static float Frequency(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        private static void Pluck(float[] mix, float start, int midi, float seconds, float volume, float decay, float bright = 0.25f)
        {
            float f = Frequency(midi);
            Voice(mix, start, seconds, t =>
            {
                float phase = 2f * Mathf.PI * f * t;
                float tone = Mathf.Sin(phase) + bright * Mathf.Sin(2f * phase) * Mathf.Exp(-t * 8f) + 0.1f * Mathf.Sin(3f * phase);
                return tone * Mathf.Exp(-t * decay) * Attack(t, 0.004f) * volume;
            });
        }

        // A deep sine with a short pitch drop and some drive, so it is still heard on phone speakers.
        private static void Bass808(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            float phase = 0f;
            Voice(mix, start, seconds, t =>
            {
                phase += 2f * Mathf.PI * f * (1f + 0.6f * Mathf.Exp(-t * 40f)) / SampleRate;
                float tone = (float)Math.Tanh(2.2f * Mathf.Sin(phase));
                return tone * Mathf.Exp(-t * 1.4f) * Attack(t, 0.005f) * volume;
            });
        }

        private static void EPiano(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            Voice(mix, start, seconds, t =>
            {
                float phase = 2f * Mathf.PI * f * t;
                float bell = Mathf.Sin(phase + 1.2f * Mathf.Exp(-t * 5f) * Mathf.Sin(2f * phase)); // a touch of FM
                float tremolo = 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 4.5f * t);
                return bell * tremolo * Mathf.Exp(-t * 1.6f) * Attack(t, 0.01f) * volume;
            });
        }

        private static void Kick(float[] mix, float start, float volume)
        {
            Voice(mix, start, 0.35f, t =>
            {
                float pitch = 48f + 100f * Mathf.Exp(-t * 32f);
                return Mathf.Sin(2f * Mathf.PI * pitch * t) * Mathf.Exp(-t * 9f) * volume;
            });
        }

        private static void Snare(float[] mix, float start, float volume, System.Random random)
        {
            float low = 0f;
            Voice(mix, start, 0.25f, t =>
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.35f; // soften the hiss
                float body = Mathf.Sin(2f * Mathf.PI * 185f * t) * Mathf.Exp(-t * 30f);
                return ((noise - low * 0.5f) * Mathf.Exp(-t * 16f) + 0.6f * body) * volume;
            });
        }

        private static void Hat(float[] mix, float start, float volume, System.Random random)
        {
            float previous = 0f;
            Voice(mix, start, 0.06f, t =>
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                float high = noise - previous; // keep only the high end
                previous = noise;
                return high * Mathf.Exp(-t * 70f) * volume;
            });
        }

        private static float Attack(float t, float seconds) => Mathf.Clamp01(t / seconds);

        private static void Voice(float[] mix, float start, float seconds, Func<float, float> sample)
        {
            int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
            int count = Mathf.RoundToInt(seconds * SampleRate);
            int fade = Mathf.Min(count, SampleRate / 100); // 10 ms fade-out avoids clicks
            for (int i = 0; i < count && from + i < mix.Length; i++)
            {
                float t = i / (float)SampleRate;
                float value = sample(t);
                if (i > count - fade) value *= (count - i) / (float)fade;
                mix[from + i] += value;
            }
        }

        private static void WriteWav(string path, float[] samples)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            int dataBytes = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)1); // mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);
            foreach (float s in samples) writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
        }
    }
}
