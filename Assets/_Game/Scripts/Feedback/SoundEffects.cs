using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Feedback
{
    /// <summary>
    /// Plays short sounds for game moments. The sounds are generated in code (simple
    /// tones), so there are no audio files yet; drop real clips into the fields to replace them.
    /// Collecting cash plays a tick that climbs in pitch while you keep grabbing bills.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundEffects : MonoBehaviour
    {
        [SerializeField, Tooltip("Played for each bill collected. Leave empty for a generated tick.")]
        private AudioClip cashClip;

        [SerializeField, Tooltip("Played as money drains into an unlock pad. Leave empty for a generated tick.")]
        private AudioClip payClip;

        [SerializeField, Tooltip("Played when a station appears. Leave empty for a generated chime.")]
        private AudioClip unlockClip;

        [SerializeField, Tooltip("Played when a customer buys a drink. Leave empty for a generated cash register.")]
        private AudioClip saleClip;

        [SerializeField, Tooltip("Played when a customer tips. Leave empty for a generated sparkle.")]
        private AudioClip tipClip;

        [SerializeField, Tooltip("Played when a customer walks out. Leave empty for a generated low tone.")]
        private AudioClip upsetClip;

        [SerializeField, Tooltip("Played when an upgrade is bought. Leave empty for a generated chime.")]
        private AudioClip upgradeClip;

        [SerializeField, Range(0f, 1f), Tooltip("Overall volume.")]
        private float volume = 0.5f;

        [SerializeField, Tooltip("Seconds without collecting before the cash tick's pitch resets.")]
        private float streakResetSeconds = 0.4f;

        private AudioSource source;
        private float lastCashTime = -10f;
        private int cashStreak;
        private float lastPayTime;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            if (cashClip == null) cashClip = Tone("Cash", new[] { 1320f }, 0.05f);
            if (payClip == null) payClip = Tone("Pay", new[] { 660f }, 0.04f);
            if (unlockClip == null) unlockClip = Tone("Unlock", new[] { 523f, 659f, 784f, 1047f }, 0.09f);
            if (saleClip == null) saleClip = Register();
            if (tipClip == null) tipClip = Tone("Tip", new[] { 1568f, 2093f, 2637f }, 0.05f);
            if (upsetClip == null) upsetClip = Tone("Upset", new[] { 392f, 311f }, 0.14f);
            if (upgradeClip == null) upgradeClip = Tone("Upgrade", new[] { 659f, 988f }, 0.1f);
        }

        private void OnEnable()
        {
            GameEvents.CashCollected += OnCash;
            GameEvents.PadPaid += OnPay;
            GameEvents.Unlocked += OnUnlock;
            GameEvents.DrinkSold += OnSale;
            GameEvents.UpgradeBought += OnUpgrade;
            GameEvents.BonusCollected += OnBonus;
            GameEvents.Tipped += OnTip;
            GameEvents.CustomerLeftUpset += OnUpset;
            GameEvents.TableCleaned += OnCleaned;
        }

        private void OnDisable()
        {
            GameEvents.CashCollected -= OnCash;
            GameEvents.PadPaid -= OnPay;
            GameEvents.Unlocked -= OnUnlock;
            GameEvents.DrinkSold -= OnSale;
            GameEvents.UpgradeBought -= OnUpgrade;
            GameEvents.BonusCollected -= OnBonus;
            GameEvents.Tipped -= OnTip;
            GameEvents.CustomerLeftUpset -= OnUpset;
            GameEvents.TableCleaned -= OnCleaned;
        }

        // A soft tick, at most about 8 a second, so grabbing a big pile isn't a drum roll.
        private void OnCash(Vector3 _)
        {
            float since = Time.time - lastCashTime;
            if (since < 0.12f) return;
            cashStreak = since < streakResetSeconds ? cashStreak + 1 : 0;
            lastCashTime = Time.time;
            Play(cashClip, 1f + Mathf.Min(cashStreak, 12) * 0.03f, 0.22f);
        }

        // Pads pay 20 times a second; a tick every other payment is plenty.
        private void OnPay(Vector3 _)
        {
            if (Time.time - lastPayTime < 0.09f) return;
            lastPayTime = Time.time;
            Play(payClip, 1f, 0.5f);
        }

        private void OnUnlock(Vector3 _) => Play(unlockClip, 1f, 1f);
        // A single cashier's bell "ding" when a customer pays.
        private void OnSale(Vector3 _, int amount) => Play(saleClip, 1f, 0.7f);
        private void OnUpgrade() => Play(upgradeClip, 1f, 1f);
        private void OnBonus() => Play(unlockClip, 1f, 1f);
        private void OnTip(Vector3 _, int amount) => Play(tipClip, 1f, 0.7f);
        private void OnUpset(Vector3 _) => Play(upsetClip, 1f, 0.8f);
        private void OnCleaned(Vector3 _) => Play(tipClip, 0.8f, 0.6f);

        private void Play(AudioClip clip, float pitch, float loudness)
        {
            if (clip == null || !GameSettings.SoundOn) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume * loudness);
        }

        // A single cashier's bell "ding" that rings out.
        private static AudioClip Register()
        {
            const int sampleRate = 44100;
            int length = Mathf.CeilToInt(0.6f * sampleRate);
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = Mathf.Clamp(Bell(t, 2349f, 0.32f) * Mathf.Clamp01(t / 0.002f) * 0.8f, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create("Ding", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Bell(float t, float pitch, float decay)
        {
            if (t < 0f) return 0f;
            float envelope = Mathf.Exp(-t / decay);
            return (Mathf.Sin(2f * Mathf.PI * pitch * t)
                    + 0.5f * Mathf.Sin(2f * Mathf.PI * pitch * 2.76f * t)
                    + 0.25f * Mathf.Sin(2f * Mathf.PI * pitch * 5.4f * t)) * envelope * 0.4f;
        }

        // Builds a little clip: each note in turn, as a sine wave that fades out quickly.
        private static AudioClip Tone(string name, float[] notes, float secondsPerNote)
        {
            const int sampleRate = 44100;
            int perNote = Mathf.CeilToInt(secondsPerNote * sampleRate);
            var samples = new float[perNote * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / sampleRate;
                    float fade = 1f - (float)i / perNote;
                    samples[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * fade * fade * 0.6f;
                }
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
