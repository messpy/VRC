using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class effecttimes : UdonSharpBehaviour
{
    [SerializeField] private GameObject firework;
    [SerializeField] private GameObject lightEffects;
    [SerializeField] private GameObject fire;
    [SerializeField] private GameObject smoke;

    // 各エフェクトの発動時間リスト（配列）
    private float[] fireworkTimes = {12, 30, 250, 379, 493, 596, 601, 749, 765, 928, 934, 961, 1045,1095, 1101};
    private float[] lightEffectTimes = {100, 1000};
    private float[] fireTimes = {1515, 1815, 1821, 1045,1095};
    private float[] smokeTimes = {1, 10,15, 25, 30, 410, 1095,1101,1130};

    private float effectDuration = 10.0f;
    private float startTime;

    private void Start()
    {
        startTime = Time.time;
        Debug.Log("[effecttimes] スクリプト開始時刻: " + startTime);
    }

    private void Update()
    {
        float elapsedTime = Time.time - startTime;

        // 配列の中身を直接変更する方式に変更
        CheckAndTriggerEffects(fireworkTimes, firework, "DeactivateFirework", elapsedTime);
        CheckAndTriggerEffects(lightEffectTimes, lightEffects, "DeactivateLightEffects", elapsedTime);
        CheckAndTriggerEffects(fireTimes, fire, "DeactivateFire", elapsedTime);
        CheckAndTriggerEffects(smokeTimes, smoke, "DeactivateSmoke", elapsedTime);
    }

    private void CheckAndTriggerEffects(float[] times, GameObject obj, string deactivateEvent, float elapsedTime)
    {
        if (obj == null) return;

        for (int i = 0; i < times.Length; i++)
        {
            if (times[i] < 0) continue; // すでに発動したエフェクトはスキップ

            if (elapsedTime >= times[i])
            {
                obj.SetActive(true);
                Debug.Log("[effecttimes] " + obj.name + " 発動！ (" + elapsedTime + " 秒)");

                SendCustomEventDelayedSeconds(deactivateEvent, effectDuration);

                times[i] = -1; // 発動済みフラグとして -1 に変更
                return;
            }
        }
    }

    public void DeactivateFirework() { if (firework != null) firework.SetActive(false); Debug.Log("[effecttimes] firework 消灯"); }
    public void DeactivateLightEffects() { if (lightEffects != null) lightEffects.SetActive(false); Debug.Log("[effecttimes] lightEffects 消灯"); }
    public void DeactivateFire() { if (fire != null) fire.SetActive(false); Debug.Log("[effecttimes] fire 消灯"); }
    public void DeactivateSmoke() { if (smoke != null) smoke.SetActive(false); Debug.Log("[effecttimes] smoke 消灯"); }
}
