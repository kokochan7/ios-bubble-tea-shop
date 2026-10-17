using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Writes each shop's background music as a looping WAV file, composed and played by code
    /// (no downloads, no licences). Each shop has its own style:
    ///  - Boba Shop: cozy café bossa nova. Nylon-guitar chord comping on the bossa rhythm, a soft
    ///    upright bass on root and fifth, brushes and a rim-click clave, and a mellow vibraphone tune.
    ///  - Matcha House: East Asian folk fusion. Rolling guzheng arpeggios with little pitch bends,
    ///    a breathy bamboo flute singing long notes on the pentatonic scale, warm pads and a soft,
    ///    modern beat underneath.
    /// The scene builder calls this, and you can rebuild the tracks from Bubble Tea > Audio > Rebuild Music.
    /// </summary>
    internal static class MusicMaker
    {
        private const string Folder = "Assets/_Game/Audio";
        private const int SampleRate = 32000;

        internal enum Track { BobaShop, MatchaHouse, NightMarket }

        private enum Quality { Major, Minor, Major7, Minor7, Dominant7, Major6 }

        private enum Style { Bossa, Folk }

        private sealed class Song
        {
            public string FileName;
            public Style Style;
            public float Bpm;
            public int Root;                 // MIDI note of the key
            public (int offset, Quality quality)[] Chords; // one per bar
            public int[] Scale;              // melody notes, semitones above the root
            public float RestChance;         // melody density
            public int HookBars = 2;         // the melody's rhythm and shape repeat every this many bars
            public int Seed;
        }

        // A bossa in F: Fmaj7 D7 | Gm7 C7 | Am7 D7 | Gm7 C7, then a turn through Bbmaj7.
        private static readonly Song BobaSong = new()
        {
            FileName = "Music_BobaShop_Bossa",
            Style = Style.Bossa,
            Bpm = 126f,
            Root = 53, // F
            Chords = new[]
            {
                (0, Quality.Major7), (9, Quality.Dominant7), (2, Quality.Minor7), (7, Quality.Dominant7),
                (4, Quality.Minor7), (9, Quality.Dominant7), (5, Quality.Major7), (7, Quality.Dominant7),
            },
            Scale = new[] { 0, 2, 4, 7, 9, 11, 12, 14, 16, 19, 21 }, // major pentatonic plus the major seventh
            RestChance = 0.5f,
            HookBars = 2,
            Seed = 17,
        };

        // Folk fusion on the D gong (major pentatonic) scale: D | Bm | G | A, with a Em7 turn.
        private static readonly Song MatchaSong = new()
        {
            FileName = "Music_MatchaHouse_Folk",
            Style = Style.Folk,
            Bpm = 80f,
            Root = 50, // D
            Chords = new[]
            {
                (0, Quality.Major), (9, Quality.Minor7), (5, Quality.Major), (7, Quality.Major),
                (0, Quality.Major), (4, Quality.Minor7), (5, Quality.Major6), (7, Quality.Major),
            },
            Scale = new[] { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 }, // D E F# A B
            RestChance = 0.35f,
            HookBars = 4,
            Seed = 41,
        };

        // A lazy evening tune for the Night Market, on A minor pentatonic: Am | F | C | G, with an Em7 turn.
        private static readonly Song NightSong = new()
        {
            FileName = "Music_NightMarket_Folk",
            Style = Style.Folk,
            Bpm = 92f,
            Root = 57, // A
            Chords = new[]
            {
                (0, Quality.Minor), (8, Quality.Major), (3, Quality.Major), (10, Quality.Major),
                (0, Quality.Minor7), (7, Quality.Minor7), (8, Quality.Major7), (10, Quality.Major),
            },
            Scale = new[] { 0, 3, 5, 7, 10, 12, 15, 17, 19, 22, 24 }, // A C D E G
            RestChance = 0.4f,
            HookBars = 4,
            Seed = 73,
        };

        [MenuItem("Bubble Tea/Audio/Rebuild Music")]
        public static void RebuildAll()
        {
            Write(BobaSong);
            Write(MatchaSong);
            Write(NightSong);
            Debug.Log("Bubble Tea: music rebuilt in " + Folder);
        }

        /// <summary>The track's clip, written first if it doesn't exist yet.</summary>
        public static AudioClip Get(Track track)
        {
            Song song = track == Track.BobaShop ? BobaSong : track == Track.NightMarket ? NightSong : MatchaSong;
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
            int bars = song.Chords.Length * 2; // the progression plays twice per loop
            int length = Mathf.RoundToInt(bars * 4 * beat * SampleRate);
            int tail = SampleRate * 4; // notes ringing past the end wrap round to the start, so the loop is seamless
            var mix = new float[length + tail];
            var random = new System.Random(song.Seed);

            if (song.Style == Style.Bossa) ComposeBossa(song, mix, bars, beat, random);
            else ComposeFolk(song, mix, bars, beat, random);

            // Wrap the tail round, then normalize with a gentle soft clip.
            var output = new float[length];
            for (int i = 0; i < mix.Length; i++) output[i % length] += mix[i];
            float peak = 0.0001f;
            foreach (float s in output) peak = Mathf.Max(peak, Mathf.Abs(s));
            float gain = 0.85f / peak;
            for (int i = 0; i < length; i++) output[i] = (float)Math.Tanh(output[i] * gain * 1.15f) * 0.92f;
            return output;
        }

        // Cozy café bossa nova: straight eighths, guitar comping on the syncopated bossa rhythm.
        private static void ComposeBossa(Song song, float[] mix, int bars, float beat, System.Random random)
        {
            float sixteenth = beat / 4f;
            // The guitar's comping rhythm over two bars (in sixteenths), and the rim-click clave (3-2).
            int[][] comp = { new[] { 0, 6, 12 }, new[] { 2, 8, 12 } };
            int[][] clave = { new[] { 0, 6, 12 }, new[] { 4, 8 } };
            int previous = 4;
            for (int bar = 0; bar < bars; bar++)
            {
                (int offset, Quality quality) = song.Chords[bar % song.Chords.Length];
                int[] chord = ChordTones(quality);
                float barStart = bar * 4 * beat;
                float At(int n) => barStart + n * sixteenth;

                // Brushes: a soft swish on every eighth, a little louder on 2 and 4; a quiet kick on 1 and 3.
                for (int n = 0; n < 16; n += 2) Brush(mix, At(n), n == 4 || n == 12 ? 0.05f : 0.028f, random);
                Kick(mix, At(0), 0.22f);
                Kick(mix, At(8), 0.16f);
                foreach (int n in clave[bar % 2]) Rim(mix, At(n), 0.07f);

                // Upright bass: root on 1, fifth on 3, with a pickup on the "and" of 4.
                int bass = song.Root - 12 + offset;
                while (bass > 45) bass -= 12;
                Upright(mix, At(0), bass, beat * 1.5f, 0.45f);
                Upright(mix, At(8), bass + 7, beat * 1.5f, 0.38f);
                Upright(mix, At(14), bass + (bar % 2 == 0 ? 0 : 5), beat * 0.5f, 0.3f);

                // Nylon guitar: the chord's upper tones strummed softly on the comping rhythm.
                foreach (int n in comp[bar % 2])
                {
                    for (int c = 1; c < chord.Length; c++)
                    {
                        int note = song.Root + offset + chord[c];
                        while (note > song.Root + 16) note -= 12;
                        while (note < song.Root + 2) note += 12;
                        Pluck(mix, At(n) + c * 0.012f, note, beat * 1.4f, 0.05f, decay: 3.5f, bright: 0.2f);
                    }
                }

                // Vibraphone tune on quarters and eighths, leaning on chord tones on the beat.
                var hook = new System.Random(song.Seed * 31 + bar % Mathf.Max(1, song.HookBars));
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    bool onBeat = eighth % 2 == 0;
                    double roll = hook.NextDouble();
                    int wander = hook.Next(-2, 3);
                    if (roll < song.RestChance + (onBeat ? -0.2 : 0.15)) continue;
                    int step = onBeat ? NearestChordTone(song, previous, offset, chord)
                                      : Mathf.Clamp(previous + wander, 0, song.Scale.Length - 1);
                    previous = step;
                    Vibes(mix, At(eighth * 2), song.Root + 12 + song.Scale[step], beat * 1.6f, 0.09f);
                }
            }
        }

        // East Asian folk fusion: guzheng arpeggios and a bamboo flute over a soft modern beat.
        private static void ComposeFolk(Song song, float[] mix, int bars, float beat, System.Random random)
        {
            float sixteenth = beat / 4f;
            int previous = 5;
            for (int bar = 0; bar < bars; bar++)
            {
                (int offset, Quality quality) = song.Chords[bar % song.Chords.Length];
                int[] chord = ChordTones(quality);
                float barStart = bar * 4 * beat;
                float At(int n) => barStart + n * sixteenth;
                bool phraseEnd = bar % 4 == 3;
                bool intro = bar < 2; // the beat comes in after two bars of guzheng and flute

                // A soft, modern beat: round kick, a gentle clap on 2 and 4, quiet shaker eighths.
                if (!intro)
                {
                    Kick(mix, At(0), 0.34f);
                    Kick(mix, At(10), 0.26f);
                    Rim(mix, At(4), 0.05f);
                    Brush(mix, At(12), 0.07f, random);
                    for (int n = 0; n < 16; n += 2) Hat(mix, At(n), n % 4 == 2 ? 0.035f : 0.02f, random);
                }

                // Warm sub bass and a slow string pad holding the chord.
                int bass = song.Root - 12 + offset;
                while (bass > 45) bass -= 12;
                SoftBass(mix, At(0), bass, beat * 3.5f, 0.3f);
                for (int c = 0; c < chord.Length; c++)
                {
                    int note = song.Root + offset + chord[c];
                    while (note > song.Root + 12) note -= 12;
                    Pad(mix, barStart, note, beat * 4.4f, 0.035f);
                }

                // Guzheng: rolling up-and-down arpeggios over the chord on sixteenths, with a sweeping
                // glissando down the scale at the end of each phrase.
                if (phraseEnd)
                {
                    for (int i = 0; i < 10; i++)
                        Guzheng(mix, At(8) + i * sixteenth * 0.5f, song.Root + 12 + song.Scale[song.Scale.Length - 1 - i], beat, 0.07f);
                }
                int[] shape = { 0, 1, 2, 3, 2, 1, 2, 3 };
                for (int n = 0; n < (phraseEnd ? 8 : 16); n++)
                {
                    if (n % 4 == 3 && n != 15) continue; // little breaths in the pattern
                    int c = shape[n % shape.Length] % chord.Length;
                    int note = song.Root + offset + chord[c] + (n >= 8 ? 12 : 0);
                    while (note > song.Root + 26) note -= 12;
                    Guzheng(mix, At(n), note, beat * 1.2f, n % 4 == 0 ? 0.075f : 0.05f);
                }

                // Bamboo flute: long, singing notes (halves and quarters) on the pentatonic scale.
                if (intro && bar == 0) continue;
                var hook = new System.Random(song.Seed * 31 + bar % Mathf.Max(1, song.HookBars));
                for (int quarter = 0; quarter < 4; quarter++)
                {
                    double roll = hook.NextDouble();
                    int wander = hook.Next(-2, 3);
                    bool held = hook.NextDouble() < 0.45;
                    if (roll < song.RestChance + (quarter == 0 ? -0.25 : 0.05)) continue;
                    int step = quarter % 2 == 0 ? NearestChordTone(song, previous, offset, chord)
                                                : Mathf.Clamp(previous + wander, 2, song.Scale.Length - 1);
                    previous = step;
                    float length = held && quarter < 3 ? beat * 2f : beat;
                    Flute(mix, barStart + quarter * beat, song.Root + 12 + song.Scale[step], length, 0.1f, random);
                    if (held) quarter++;
                }
            }
        }

        private static int[] ChordTones(Quality quality) => quality switch
        {
            Quality.Minor => new[] { 0, 3, 7 },
            Quality.Major7 => new[] { 0, 4, 7, 11 },
            Quality.Minor7 => new[] { 0, 3, 7, 10 },
            Quality.Dominant7 => new[] { 0, 4, 7, 10 },
            Quality.Major6 => new[] { 0, 4, 7, 9 },
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

        // A soft upright bass: a round sine with a little second harmonic and a thumpy start.
        private static void Upright(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            Voice(mix, start, seconds, t =>
            {
                float phase = 2f * Mathf.PI * f * t;
                float tone = Mathf.Sin(phase) + 0.35f * Mathf.Sin(2f * phase) * Mathf.Exp(-t * 6f) + 0.15f * Mathf.Sin(3f * phase) * Mathf.Exp(-t * 12f);
                return tone * Mathf.Exp(-t * 2.2f) * Attack(t, 0.008f) * volume;
            });
        }

        // A warm sub bass with a slow swell, for under the folk tune.
        private static void SoftBass(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            Voice(mix, start, seconds, t =>
            {
                float phase = 2f * Mathf.PI * f * t;
                return (Mathf.Sin(phase) + 0.2f * Mathf.Sin(2f * phase)) * Mathf.Exp(-t * 0.6f) * Attack(t, 0.03f) * volume;
            });
        }

        // Vibraphone: a pure tone with a bell overtone and a slow shimmer.
        private static void Vibes(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            Voice(mix, start, seconds, t =>
            {
                float phase = 2f * Mathf.PI * f * t;
                float tone = Mathf.Sin(phase) + 0.25f * Mathf.Sin(4f * phase) * Mathf.Exp(-t * 6f);
                float shimmer = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                return tone * shimmer * Mathf.Exp(-t * 1.8f) * Attack(t, 0.003f) * volume;
            });
        }

        // Guzheng: a bright, quickly fading pluck whose pitch settles from just above the note.
        private static void Guzheng(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            float phase = 0f;
            Voice(mix, start, seconds, t =>
            {
                float bend = 1f + 0.012f * Mathf.Exp(-t * 18f);
                phase += 2f * Mathf.PI * f * bend / SampleRate;
                float tone = Mathf.Sin(phase) + 0.5f * Mathf.Sin(2f * phase) * Mathf.Exp(-t * 7f)
                             + 0.3f * Mathf.Sin(3f * phase) * Mathf.Exp(-t * 11f) + 0.12f * Mathf.Sin(5f * phase) * Mathf.Exp(-t * 20f);
                return tone * Mathf.Exp(-t * 3.2f) * Attack(t, 0.002f) * volume;
            });
        }

        // Bamboo flute (dizi): a soft swell, gentle vibrato that grows as the note is held, and a breath of air.
        private static void Flute(float[] mix, float start, int midi, float seconds, float volume, System.Random random)
        {
            float f = Frequency(midi);
            float phase = 0f;
            float air = 0f;
            Voice(mix, start, seconds, t =>
            {
                float vibrato = 1f + 0.006f * Mathf.Clamp01(t * 2f) * Mathf.Sin(2f * Mathf.PI * 5.2f * t);
                float scoop = 1f - 0.02f * Mathf.Exp(-t * 25f); // slides up into the note
                phase += 2f * Mathf.PI * f * vibrato * scoop / SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                air += (noise - air) * 0.08f;
                float tone = Mathf.Sin(phase) + 0.18f * Mathf.Sin(2f * phase) + 0.06f * Mathf.Sin(3f * phase);
                float swell = Attack(t, 0.08f) * Mathf.Clamp01((seconds - t) / 0.12f);
                return (tone + air * 0.6f) * swell * volume;
            });
        }

        // A slow string pad: a few detuned sines fading in and out.
        private static void Pad(float[] mix, float start, int midi, float seconds, float volume)
        {
            float f = Frequency(midi);
            Voice(mix, start, seconds, t =>
            {
                float a = 2f * Mathf.PI * f * t;
                float tone = Mathf.Sin(a) + Mathf.Sin(a * 1.004f) + 0.5f * Mathf.Sin(a * 2.002f);
                float envelope = Attack(t, 0.6f) * Mathf.Clamp01((seconds - t) / 0.8f);
                return tone * envelope * volume;
            });
        }

        // A brush swish: soft, darker noise with a slower fade than a hi-hat.
        private static void Brush(float[] mix, float start, float volume, System.Random random)
        {
            float low = 0f;
            Voice(mix, start, 0.18f, t =>
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.5f;
                return low * Mathf.Exp(-t * 18f) * Attack(t, 0.01f) * volume;
            });
        }

        // A rim click: a short wooden knock.
        private static void Rim(float[] mix, float start, float volume)
        {
            Voice(mix, start, 0.06f, t =>
                (Mathf.Sin(2f * Mathf.PI * 1700f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 820f * t)) * Mathf.Exp(-t * 90f) * volume);
        }

        private static void Kick(float[] mix, float start, float volume)
        {
            Voice(mix, start, 0.35f, t =>
            {
                float pitch = 48f + 100f * Mathf.Exp(-t * 32f);
                return Mathf.Sin(2f * Mathf.PI * pitch * t) * Mathf.Exp(-t * 9f) * volume;
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
