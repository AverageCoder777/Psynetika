using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Mixer")]
    public AudioMixer audioMixer;
    public string musicVolumeParam = "MusicVolume"; // exposed param name

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("Scenes")]
    public string menuSceneName = "MainMenu";

    [Header("Fade")]
    public float fadeTime = 1.5f;

    // Громкость фейда (локальная, 0..1). Общая громкость — через Mixer.
    [Range(0f, 1f)] public float targetFadeVolume = 1f;

    AudioSource a, b, current;
    Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            Debug.LogError("MusicManager: на объекте должно быть 2 AudioSource.");
            return;
        }

        a = sources[0];
        b = sources[1];

        a.playOnAwake = b.playOnAwake = false;
        a.loop = b.loop = true;

        a.volume = 0f;
        b.volume = 0f;
        current = a;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Стартовая громкость в миксере (например 1.0 = 0 dB)
        if (audioMixer != null)
            audioMixer.SetFloat(musicVolumeParam, LinearToDb(1f));

        PlayForScene(SceneManager.GetActiveScene().name, immediate: true);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name, immediate: false);
    }

    void PlayForScene(string sceneName, bool immediate)
    {
        AudioClip nextClip = (sceneName == menuSceneName) ? menuMusic : gameMusic;
        if (nextClip == null) return;

        if (current.clip == nextClip && current.isPlaying) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (immediate)
        {
            AudioSource other = (current == a) ? b : a;
            other.Stop();
            other.volume = 0f;

            current.Stop();
            current.clip = nextClip;
            current.volume = targetFadeVolume;
            current.Play();
        }
        else
        {
            fadeRoutine = StartCoroutine(CrossFadeTo(nextClip));
        }
    }

    IEnumerator CrossFadeTo(AudioClip nextClip)
    {
        AudioSource next = (current == a) ? b : a;

        next.Stop();
        next.clip = nextClip;
        next.volume = 0f;
        next.Play();

        float t = 0f;
        float startVol = current.volume;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);

            current.volume = Mathf.Lerp(startVol, 0f, k);
            next.volume = Mathf.Lerp(0f, targetFadeVolume, k);

            yield return null;
        }

        current.Stop();
        current.volume = 0f;

        current = next;
        fadeRoutine = null;
    }

    // Для UI-слайдера громкости
    public void SetMusicVolume01(float value01)
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat(musicVolumeParam, LinearToDb(value01));
    }

    float LinearToDb(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }
}