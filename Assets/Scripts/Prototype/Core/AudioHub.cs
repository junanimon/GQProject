using System.Collections;
using UnityEngine;

namespace GuildProto
{
    public enum Sfx { Click, Stamp, Paper, Coin, Heart, Bell, Whoosh, Thud, Fail }
    public enum Bgm { None, Title, Night, Day, Raid, Ending }

    // 효과음 · 배경음 (Core 씬). 클립은 Assets/Audio 의 파일을 인스펙터에서 바꿔 끼우면 된다.
    public class AudioHub : MonoBehaviour
    {
        public static AudioHub Instance { get; private set; }

        public AudioSource sfxSource;
        public AudioSource musicSource;
        [Range(0, 1)] public float sfxVolume = 0.8f;
        [Range(0, 1)] public float musicVolume = 0.35f;
        public float musicFade = 0.8f;

        [Header("효과음")]
        public AudioClip click;
        public AudioClip stamp;
        public AudioClip paper;
        public AudioClip coin;
        public AudioClip heart;
        public AudioClip bell;
        public AudioClip whoosh;
        public AudioClip thud;
        public AudioClip fail;

        [Header("배경음")]
        public AudioClip titleMusic;
        public AudioClip nightMusic;
        public AudioClip dayMusic;
        public AudioClip raidMusic;
        public AudioClip endingMusic;

        Bgm current;
        Coroutine fading;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        readonly float[] lastPlayed = new float[System.Enum.GetValues(typeof(Sfx)).Length];

        public static void Play(Sfx s)
        {
            if (Instance == null) return;
            // 같은 소리가 한순간에 겹쳐 쌓이지 않게
            float now = Time.unscaledTime;
            if (now - Instance.lastPlayed[(int)s] < 0.05f) return;
            Instance.lastPlayed[(int)s] = now;
            var clip = Instance.Clip(s);
            if (clip != null) Instance.sfxSource.PlayOneShot(clip, Instance.sfxVolume);
        }

        public static void Music(Bgm b)
        {
            if (Instance == null || Instance.current == b) return;
            Instance.current = b;
            if (Instance.fading != null) Instance.StopCoroutine(Instance.fading);
            Instance.fading = Instance.StartCoroutine(Instance.Crossfade(Instance.Clip(b)));
        }

        AudioClip Clip(Sfx s) => s switch
        {
            Sfx.Click => click, Sfx.Stamp => stamp, Sfx.Paper => paper, Sfx.Coin => coin, Sfx.Heart => heart,
            Sfx.Bell => bell, Sfx.Whoosh => whoosh, Sfx.Thud => thud, _ => fail,
        };

        AudioClip Clip(Bgm b) => b switch
        {
            Bgm.Title => titleMusic, Bgm.Night => nightMusic, Bgm.Day => dayMusic, Bgm.Raid => raidMusic, Bgm.Ending => endingMusic, _ => null,
        };

        IEnumerator Crossfade(AudioClip next)
        {
            float from = musicSource.volume;
            for (float t = 0; t < musicFade && musicSource.isPlaying; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(from, 0, t / musicFade);
                yield return null;
            }
            musicSource.Stop();
            if (next == null) yield break;
            musicSource.clip = next;
            musicSource.loop = true;
            musicSource.Play();
            for (float t = 0; t < musicFade; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0, musicVolume, t / musicFade);
                yield return null;
            }
            musicSource.volume = musicVolume;
        }
    }
}
