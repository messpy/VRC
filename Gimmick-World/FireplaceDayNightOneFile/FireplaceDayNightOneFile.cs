using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FireplaceDayNightOneFile : UdonSharpBehaviour
{
    public const int STATE_DAY = 0;
    public const int STATE_NIGHT = 1;

    [Header("トリガー検出設定")]
    [Tooltip("対象のオブジェクト（これが触れたら発動）")]
    public GameObject targetObject;

    [Tooltip("トリガー後のクールダウン時間（秒）")]
    public float triggerCooldownSeconds = 2f;

    [Tooltip("薪を元の位置に戻すまでの時間（秒）")]
    public float resetDelaySeconds = 15f;

    [Header("スカイボックス")]
    public Material skyboxDay;
    public Material skyboxNight;

    [Header("炎エフェクト（夜ON / 昼OFF）")]
    public GameObject[] fireObjects;
    public Light fireLight;

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioClip[] bgmListDay;
    public AudioClip[] bgmListNight;
    public float bgmIntervalMin = 300f;
    public float bgmIntervalMax = 900f;
    public int bgmPlayCount = 3;

    [Header("昼夜で表示切替するオブジェクト")]
    public GameObject[] showAtNightObjects;
    public GameObject[] showAtDayObjects;

    // 状態
    private int currentState = STATE_DAY;
    private bool busy = false;

    // 薪リセット用
    private Vector3 targetInitialPosition;
    private Quaternion targetInitialRotation;

    // BGM
    private bool bgmPlaying = false;
    private int lastPlayedIndex = -1;
    private int bgmCurrentPlayCount = 0;
    private float bgmWaitTimer = 0f;
    private float bgmCurrentInterval = 0f;
    private bool bgmWaiting = false;
    private float bgmPlayCooldown = 0f;

    void Start()
    {
        // 薪の初期位置を保存
        if (targetObject != null)
        {
            targetInitialPosition = targetObject.transform.position;
            targetInitialRotation = targetObject.transform.rotation;
        }

        // 初期状態反映
        ApplyVisualState(currentState);
        ApplyFireState(currentState);
        ApplyObjectVisibility(currentState);

        // BGM開始
        StartBGMShuffle();
    }

    // ===== トリガー発火 =====
    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (targetObject == null) return;
        if (other.gameObject != targetObject) return;
        if (busy) return;

        busy = true;

        // 昼夜トグル
        if (currentState == STATE_DAY)
        {
            currentState = STATE_NIGHT;
            ApplyVisualState(STATE_NIGHT);
            ApplyFireState(STATE_NIGHT);
            ApplyObjectVisibility(STATE_NIGHT);
        }
        else
        {
            currentState = STATE_DAY;
            ApplyVisualState(STATE_DAY);
            ApplyFireState(STATE_DAY);
            ApplyObjectVisibility(STATE_DAY);
        }
        SwitchBGMForState();

        // クールダウン後にロック解除
        SendCustomEventDelayedSeconds(nameof(_Unlock), triggerCooldownSeconds);

        // 薪リセット
        SendCustomEventDelayedSeconds(nameof(_ResetTarget), resetDelaySeconds);
    }

    public void _Unlock() { busy = false; }

    public void _ResetTarget()
    {
        if (targetObject == null) return;

        // 持っていたらスキップして再試行
        VRC_Pickup pickup = (VRC_Pickup)targetObject.GetComponent(typeof(VRC_Pickup));
        if (pickup != null && pickup.currentPlayer != null)
        {
            SendCustomEventDelayedSeconds(nameof(_ResetTarget), 1f);
            return;
        }

        // 速度リセット
        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 位置リセット
        targetObject.transform.position = targetInitialPosition;
        targetObject.transform.rotation = targetInitialRotation;
    }

    void Update()
    {
        TickBGM();
    }

    // ===== 状態適用 =====
    private void ApplyVisualState(int state)
    {
        if (state == STATE_DAY && skyboxDay != null)
            RenderSettings.skybox = skyboxDay;
        if (state == STATE_NIGHT && skyboxNight != null)
            RenderSettings.skybox = skyboxNight;
    }

    private void ApplyFireState(int state)
    {
        bool on = (state == STATE_NIGHT);
        if (fireObjects != null)
        {
            foreach (GameObject obj in fireObjects)
                if (obj != null) obj.SetActive(on);
        }
        if (fireLight != null) fireLight.enabled = on;
    }

    private void ApplyObjectVisibility(int state)
    {
        if (showAtNightObjects != null)
        {
            foreach (GameObject obj in showAtNightObjects)
                if (obj != null) obj.SetActive(state == STATE_NIGHT);
        }
        if (showAtDayObjects != null)
        {
            foreach (GameObject obj in showAtDayObjects)
                if (obj != null) obj.SetActive(state == STATE_DAY);
        }
    }

    // ===== BGM =====
    private void StartBGMShuffle()
    {
        if (bgmSource == null) return;
        AudioClip[] list = GetCurrentBGMList();
        if (list == null || list.Length == 0) return;

        bgmPlaying = true;
        bgmWaiting = false;
        bgmCurrentPlayCount = 0;
        PlayRandomBGM();
    }

    private void TickBGM()
    {
        if (!bgmPlaying || bgmSource == null) return;

        if (bgmPlayCooldown > 0f)
        {
            bgmPlayCooldown -= Time.deltaTime;
            return;
        }

        AudioClip[] list = GetCurrentBGMList();
        if (list == null || list.Length == 0) return;

        if (bgmWaiting)
        {
            bgmWaitTimer += Time.deltaTime;
            if (bgmWaitTimer >= bgmCurrentInterval)
            {
                bgmWaiting = false;
                bgmCurrentPlayCount = 0;
                PlayRandomBGM();
            }
            return;
        }

        if (!bgmSource.isPlaying)
        {
            bgmCurrentPlayCount++;
            if (bgmCurrentPlayCount >= bgmPlayCount)
            {
                bgmWaiting = true;
                bgmWaitTimer = 0f;
                bgmCurrentInterval = Random.Range(bgmIntervalMin, bgmIntervalMax);
            }
            else
            {
                PlayRandomBGM();
            }
        }
    }

    private AudioClip[] GetCurrentBGMList()
    {
        return (currentState == STATE_NIGHT) ? bgmListNight : bgmListDay;
    }

    private void PlayRandomBGM()
    {
        if (bgmSource == null) return;
        AudioClip[] list = GetCurrentBGMList();
        if (list == null || list.Length == 0) return;

        int next;
        if (list.Length == 1)
        {
            next = 0;
        }
        else
        {
            do { next = Random.Range(0, list.Length); }
            while (next == lastPlayedIndex);
        }

        lastPlayedIndex = next;
        bgmSource.clip = list[next];
        bgmSource.loop = false;
        bgmSource.Play();
        bgmPlayCooldown = 0.5f;
    }

    private void SwitchBGMForState()
    {
        if (bgmSource == null) return;
        lastPlayedIndex = -1;
        bgmCurrentPlayCount = 0;
        bgmWaiting = false;
        bgmSource.Stop();
        PlayRandomBGM();
    }
}
