using System.Collections.Generic;
using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Devices;
using UnityEngine;

namespace ChargeRush.Audio
{
    /// <summary>Lightweight audio cue router with placeholder clip support.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private List<AudioCueClip> clips = new List<AudioCueClip>();

        private readonly Dictionary<AudioCue, AudioClip> lookup = new Dictionary<AudioCue, AudioClip>();

        [System.Serializable]
        public sealed class AudioCueClip
        {
            public AudioCue Cue;
            public AudioClip Clip;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            RebuildLookup();
            ApplyVolumes();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetClips(List<AudioCueClip> cueClips)
        {
            clips = cueClips ?? new List<AudioCueClip>();
            RebuildLookup();
        }

        public void ApplyVolumes()
        {
            var save = Save.SaveManager.Instance;
            if (save == null)
            {
                return;
            }

            musicSource.volume = save.Data.MusicVolume;
            sfxSource.volume = save.Data.SfxVolume;
        }

        public void Play(AudioCue cue)
        {
            if (sfxSource == null)
            {
                return;
            }

            if (lookup.TryGetValue(cue, out var clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private void RebuildLookup()
        {
            lookup.Clear();
            for (var i = 0; i < clips.Count; i++)
            {
                lookup[clips[i].Cue] = clips[i].Clip;
            }
        }

        private void Subscribe()
        {
            GameEvents.CustomerArrived += OnCustomerArrived;
            GameEvents.DevicePickedUp += OnDevicePickedUp;
            GameEvents.DevicePlacedCorrectly += OnPlaced;
            GameEvents.IncorrectConnector += OnIncorrect;
            GameEvents.ChargingStarted += OnChargeStart;
            GameEvents.ChargingCompleted += OnChargeComplete;
            GameEvents.DeviceReturned += OnReturned;
            GameEvents.PaymentReceived += OnPayment;
            GameEvents.AchievementUnlocked += OnAchievement;
            GameEvents.LevelCompleted += OnComplete;
            GameEvents.LevelFailed += OnFailed;
        }

        private void Unsubscribe()
        {
            GameEvents.CustomerArrived -= OnCustomerArrived;
            GameEvents.DevicePickedUp -= OnDevicePickedUp;
            GameEvents.DevicePlacedCorrectly -= OnPlaced;
            GameEvents.IncorrectConnector -= OnIncorrect;
            GameEvents.ChargingStarted -= OnChargeStart;
            GameEvents.ChargingCompleted -= OnChargeComplete;
            GameEvents.DeviceReturned -= OnReturned;
            GameEvents.PaymentReceived -= OnPayment;
            GameEvents.AchievementUnlocked -= OnAchievement;
            GameEvents.LevelCompleted -= OnComplete;
            GameEvents.LevelFailed -= OnFailed;
        }

        private void OnCustomerArrived(string _) => Play(AudioCue.CustomerArrival);
        private void OnDevicePickedUp(DeviceInstance _) => Play(AudioCue.DevicePickup);
        private void OnPlaced(DeviceInstance _) => Play(AudioCue.DevicePlacedCorrectly);
        private void OnIncorrect(DeviceInstance _) => Play(AudioCue.IncorrectConnector);
        private void OnChargeStart(DeviceInstance _) => Play(AudioCue.ChargingStarted);
        private void OnChargeComplete(DeviceInstance _) => Play(AudioCue.ChargingComplete);
        private void OnReturned(DeviceInstance _) => Play(AudioCue.DeviceReturned);
        private void OnPayment(int _) => Play(AudioCue.Payment);
        private void OnAchievement(string _) => Play(AudioCue.AchievementUnlocked);
        private void OnComplete() => Play(AudioCue.LevelComplete);
        private void OnFailed(MistakeReason _) => Play(AudioCue.LevelFailed);
    }
}
