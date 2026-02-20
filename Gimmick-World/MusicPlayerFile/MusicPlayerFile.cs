using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class MusicPlayerFile : UdonSharpBehaviour
{
    [Header("音楽設定")]
    [Tooltip("再生する音楽リスト")]
    public AudioClip[] musicList;

    [Tooltip("音楽再生用のAudioSource")]
    public AudioSource audioSource;

    // 状態
    private bool isPlaying = false;
    private int lastPlayedIndex = -1;
    private float playCooldown = 0f;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (!isPlaying) return;
        if (audioSource == null) return;

        if (playCooldown > 0f)
        {
            playCooldown -= Time.deltaTime;
            return;
        }

        // 曲が終わったら次をシャッフル再生
        if (!audioSource.isPlaying)
        {
            PlayRandomMusic();
        }
    }

    public override void Interact()
    {
        isPlaying = !isPlaying;

        if (isPlaying)
        {
            PlayRandomMusic();
        }
        else
        {
            audioSource.Stop();
        }
    }

    private void PlayRandomMusic()
    {
        if (audioSource == null) return;
        if (musicList == null || musicList.Length == 0) return;

        int next;
        if (musicList.Length == 1)
        {
            next = 0;
        }
        else
        {
            do { next = Random.Range(0, musicList.Length); }
            while (next == lastPlayedIndex);
        }

        lastPlayedIndex = next;
        audioSource.clip = musicList[next];
        audioSource.loop = false;
        audioSource.Play();
        playCooldown = 0.5f;
    }
}
