using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections.Generic;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class EffectTime_Manager : UdonSharpBehaviour
{
    [Header("エフェクトリスト（変更可能）")]
    [SerializeField] private List<GameObject> effects = new List<GameObject>(); // エフェクト一覧
    [Header("各エフェクトの発動時間（秒）")]
    [SerializeField] private List<List<float>> effectTimes = new List<List<float>>(); // 各エフェクトの発動時間リスト
    [Header("エフェクトの持続時間（秒） (-1 で無限ON)")]
    [SerializeField] private List<float> effectDurations = new List<float>(); // 各エフェクトの持続時間
    [Header("エフェクトの初期状態")]
    [SerializeField] private List<bool> effectActiveStates = new List<bool>(); // 初期状態 (ON/OFF)

    private float startTime;

    private void Start()
    {
        startTime = Time.time;

        // 設定された値を一括でログに出力
        Debug.Log("===== [EffectManager] 初期設定 =====");
        Debug.Log("スクリプト開始時刻: " + startTime);

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null)
            {
                string effectName = effects[i].name;
                string times = effectTimes.Count > i ? string.Join(", ", effectTimes[i]) : "なし";
                string duration = effectDurations.Count > i ? (effectDurations[i] == -1 ? "無限" : effectDurations[i] + " 秒") : "未設定";
                string initialState = effectActiveStates.Count > i && effectActiveStates[i] ? "ON" : "OFF";

                Debug.Log($"[{effectName}] 発動時間: {times} | 持続時間: {duration} | 初期状態: {initialState}");
            }
        }
        Debug.Log("===================================");

        // 各エフェクトの初期状態を設定
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null)
            {
                effects[i].SetActive(effectActiveStates.Count > i ? effectActiveStates[i] : false);
            }
        }
    }

    private void Update()
    {
        float elapsedTime = Time.time - startTime; // 経過時間

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null && effectTimes.Count > i)
            {
                CheckAndTriggerEffects(ref effectTimes[i], effects[i], i, elapsedTime);
            }
        }
    }

    private void CheckAndTriggerEffects(ref List<float> times, GameObject obj, int index, float elapsedTime)
    {
        if (times == null || times.Count == 0 || obj == null) return;

        for (int i = 0; i < times.Count; i++)
        {
            if (elapsedTime >= times[i]) // 発動タイミングを過ぎたら
            {
                obj.SetActive(true);
                Debug.Log($"[EffectManager] {obj.name} 発動！（{elapsedTime} 秒）");

                if (effectDurations.Count > index && effectDurations[index] != -1)
                {
                    SendCustomEventDelayedSeconds("DeactivateEffect" + index, effectDurations[index]);
                }

                times.RemoveAt(i); // リストから削除して無駄なチェックを減らす
                return; // 1つ発動したら次のフレームで処理
            }
        }
    }

    public void DeactivateEffect0() { DeactivateEffect(0); }
    public void DeactivateEffect1() { DeactivateEffect(1); }
    public void DeactivateEffect2() { DeactivateEffect(2); }
    public void DeactivateEffect3() { DeactivateEffect(3); }
    public void DeactivateEffect4() { DeactivateEffect(4); }

    private void DeactivateEffect(int index)
    {
        if (effects.Count > index && effects[index] != null)
        {
            effects[index].SetActive(false);
            Debug.Log($"[EffectManager] {effects[index].name} 消灯");
        }
    }
}
