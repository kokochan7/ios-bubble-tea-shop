using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Writes each shop's background music as a looping WAV file, composed and played by code
    /// (no downloads, no licences). The Boba Shop gets a bouncy pop tune; the Matcha House a
    /// slow, mellow lo-fi one. The scene builder calls this, and you can rebuild the tracks from
    /// Bubble Tea > Audio > Rebuild Music.
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
            public float Swing;              // 0 = straight eighths
            public float RestChance;         // melody density
            public bool Bouncy;              // pop drums and strummed chords, else lo-fi
            public int Seed;
        }

        private static readonly Song BobaSong = new()
        {
            FileName = "Music_BobaShop",
            Bpm = 108f,
            Root = 60, // C
            Chords = new[]
            {
                (0, Quality.Major), (9, Quality.Minor), (5, Quality.Major), (7, Quality.Major),
                (0, Quality.Major), (4, Quality.Minor), (5, Quality.Major), (7, Quality.Major),
                (5, Quality.Major), (7, Quality.Major), (4, Quality.Minor), (9, Quality.Minor),
                (2, Quality.Minor), (7, Quality.Major), (0, Quality.Major), (7, Quality.Major),
            },
            Scale = new[] { 0, 2, 4, 7, 9, 12, 14, 16 },
            Swing = 0f,
            RestChance = 0.3f,
            Bouncy = true,
            Seed = 7,
        };

        private static readonly Song MatchaSong = new()
        {
            FileName = "Music_MatchaHouse",
            Bpm = 76f,
            Root = 53, // F
            Chords = new[]
            {
                (0, Quality.Major7), (-1, Quality.Minor7), (-3, Quality.Minor7), (-5, Quality.Major7),
                (5, Quality.Major7), (4, Quality.Minor7), (2, Quality.Minor7), (-5, Quality.Dominant7),
            },
            Scale = new[] { 0, 2, 4, 7, 9, 12, 14, 16, 19 },
            Swing = 0.16f,
            RestChance = 0.5f,
            Bouncy = false,
            Seed = 11,
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
            int bars = song.Chords.Length * (song.Bouncy ? 1 : 2); // the lo-fi tune plays its chords twice
            int length = Mathf.RoundToInt(bars * 4 * beat * SampleRate);
            int tail = SampleRate * 3; // notes ringing past the end wrap round to the start, so the loop is seamless
            var mix = new float[length + tail];
            var random = new System.Random(song.Seed);

            int previous = 2; // index into the scale
            for (int bar = 0; bar < bars; bar++)
            {
                (int offset, Quality quality) = song.Chords[bar % song.Chords.Length];
                int[] chord = ChordTones(quality);
                float barStart = bar * 4 * beat;

                // Bass: the chord's root, two octaves down.
                int bassNote = song.Root - 24 + offset;
                if (song.Bouncy)
                {
                    Pluck(mix, barStart, bassNote, beat * 0.9f, 0.32f, decay: 3.5f);
                    Pluck(mix, barStart + 2 * beat, bassNote, beat * 0.9f, 0.28f, decay: 3.5f);
                    Pluck(mix, barStart + 3.5f * beat, bassNote + 12, beat * 0.4f, 0.16f, decay: 6f);
                }
                else
                {
                    Pluck(mix, barStart, bassNote, beat * 2.5f, 0.32f, decay: 1.2f);
                    Pluck(mix, barStart + 2.5f * beat, bassNote + 7, beat * 1.2f, 0.18f, decay: 1.8f);
                }

                // Chords: a strummed ukulele on the off-beats, or a soft electric piano.
                if (song.Bouncy)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        float at = barStart + (b + 0.5f) * beat;
                        for (int n = 0; n < chord.Length; n++)
                        {
                            Pluck(mix, at + n * 0.012f, song.Root + offset + chord[n], beat * 0.45f, 0.07f, decay: 7f, bright: 0.5f);
                        }
                    }
                }
                else
                {
                    for (int n = 0; n < chord.Length; n++)
                    {
                        EPiano(mix, barStart + n * 0.02f, song.Root - 12 + offset + chord[n] + 12, beat * 3.8f, 0.07f);
                    }
                }

                // Melody: eighth notes picked from the scale, landing on chord tones on the beat.
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    bool onBeat = eighth % 2 == 0;
                    if (random.NextDouble() < song.RestChance + (onBeat ? -0.15 : 0.1)) continue;
                    if (bar % 4 == 3 && eighth >= 5) continue; // breathe at the end of each phrase

                    int step;
                    if (onBeat) step = NearestChordTone(song, previous, offset, chord);
                    else step = Mathf.Clamp(previous + random.Next(-2, 3), 0, song.Scale.Length - 1);
                    previous = step;

                    float at = barStart + eighth * 0.5f * beat;
                    if (!onBeat) at += song.Swing * beat;
                    int note = song.Root + 12 + song.Scale[step];
                    if (song.Bouncy) Marimba(mix, at, note, beat * 0.9f, 0.16f);
                    else EPiano(mix, at, note, beat * 1.4f, 0.12f);
                }

                // Drums.
                for (int b = 0; b < 4; b++)
                {
                    float at = barStart + b * beat;
                    if (song.Bouncy)
                    {
                        if (b % 2 == 0) Kick(mix, at, 0.45f);
                        else Clap(mix, at, 0.16f, random);
                        Hat(mix, at + 0.5f * beat, 0.05f, random);
                        Hat(mix, at, 0.03f, random);
                    }
                    else
                    {
                        if (b == 0 || (b == 2 && bar % 2 == 1)) Kick(mix, at, 0.32f);
                        if (b == 2) Rim(mix, at, 0.10f);
                        Hat(mix, at + (0.5f + song.Swing) * beat, 0.025f, random);
                    }
                }
            }

            // Wrap the tail round, add a little vinyl hiss to the lo-fi track, then normalize.
            var output = new float[length];
            for (int i = 0; i < mix.Length; i++) output[i % length] += mix[i];
            if (!song.Bouncy)
            {
                for (int i = 0; i < length; i++) output[i] += ((float)random.NextDouble() - 0.5f) * 0.006f;
            }
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

        private static void Marimba(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            Voice(mix, start, seconds, t =>
            {
                float phase = 2f * Mathf.PI * f * t;
                float tone = Mathf.Sin(phase) + 0.35f * Mathf.Sin(4f * phase) * Mathf.Exp(-t * 18f);
                return tone * Mathf.Exp(-t * 5f) * Attack(t, 0.003f) * volume;
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
                float pitch = 50f + 90f * Mathf.Exp(-t * 30f);
                return Mathf.Sin(2f * Mathf.PI * pitch * t) * Mathf.Exp(-t * 9f) * volume;
            });
        }

        private static void Clap(float[] mix, float start, float volume, System.Random random)
        {
            float low = 0f;
            Voice(mix, start, 0.2f, t =>
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.35f; // soften the hiss
                return (noise - low * 0.5f) * Mathf.Exp(-t * 22f) * volume;
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

        private static void Rim(float[] mix, float start, float volume)
        {
            Voice(mix, start, 0.08f, t => Mathf.Sin(2f * Mathf.PI * 820f * t) * Mathf.Exp(-t * 60f) * volume);
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
