using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections.Generic;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class effecttimes : UdonSharpBehaviour
{
    [SerializeField] private GameObject firework;
    [SerializeField] private GameObject lightEffects;
    [SerializeField] private GameObject fire;
    [SerializeField] private GameObject smoke;

    // 各エフェクトの発動時間リスト（リストに変換）
    private List<float> fireworkTimes = new List<float> {5, 12, 30, 250, 410};
    private List<float> lightEffectTimes = new List<float> {10, 20, 50};
    private List<float> fireTimes = new List<float> {15, 100, 200};
    private List<float> smokeTimes = new List<float> {3, 10, 25};

    private float effectDuration = 10.0f;
    private float startTime;

    private void Start()
    {
        startTime = Time.time;
        Debug.Log("[effecttimes] スクリプト開始時刻: " + startTime);
    }

    private void Update()
    {
        float elapsedTime = Time.time - startTime; // 経過時間

        // ここで不要なループを回さないように最適化
        CheckAndTriggerEffects(ref fireworkTimes, firework, "DeactivateFirework", elapsedTime);
        CheckAndTriggerEffects(ref lightEffectTimes, lightEffects, "DeactivateLightEffects", elapsedTime);
        CheckAndTriggerEffects(ref fireTimes, fire, "DeactivateFire", elapsedTime);
        CheckAndTriggerEffects(ref smokeTimes, smoke, "DeactivateSmoke", elapsedTime);
    }

    private void CheckAndTriggerEffects(ref List<float> times, GameObject obj, string deactivateEvent, float elapsedTime)
    {
        if (obj == null || times.Count == 0) return;

        for (int i = 0; i < times.Count; i++)
        {
            if (elapsedTime >= times[i]) // 発動タイミングを過ぎたら
            {
                obj.SetActive(true);
                Debug.Log("[effecttimes] " + obj.name + " 発動！ (" + elapsedTime + " 秒)");

                SendCustomEventDelayedSeconds(deactivateEvent, effectDuration);
                
                times.RemoveAt(i); // リストから削除して無駄なチェックを減らす
                return; // 1つ発動したら次のフレームで処理
            }
        }
    }

    public void DeactivateFirework() { if (firework != null) firework.SetActive(false); Debug.Log("[effecttimes] firework 消灯"); }
    public void DeactivateLightEffects() { if (lightEffects != null) lightEffects.SetActive(false); Debug.Log("[effecttimes] lightEffects 消灯"); }
    public void DeactivateFire() { if (fire != null) fire.SetActive(false); Debug.Log("[effecttimes] fire 消灯"); }
    public void DeactivateSmoke() { if (smoke != null) smoke.SetActive(false); Debug.Log("[effecttimes] smoke 消灯"); }
}