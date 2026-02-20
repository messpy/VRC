using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameCubeController : UdonSharpBehaviour
{
    public GameObject[] targetObjects; // ON/OFFを切り替えるオブジェクトのリスト
    public float waitTime = 5.0f; // 何秒後に開始するか
    public float onDuration = 3.0f; // 何秒間ONにするか
    public bool loop = false; // 繰り返すかどうか

    private bool isRunning = false; // 実行中フラグ

    void Start()
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogError("[GameCubeController] ターゲットオブジェクトが設定されていません！");
            return;
        }

        // 指定時間後に処理を開始
        SendCustomEventDelayedSeconds("ActivateAllObjects", waitTime);
    }

    public void ActivateAllObjects()
    {
        if (isRunning) return;
        isRunning = true;

        Debug.Log("[GameCubeController] すべてのオブジェクトを ON");

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null)
            {
                targetObjects[i].SetActive(true);
            }
        }

        // onDuration 秒後にすべてのオブジェクトを OFF にする
        SendCustomEventDelayedSeconds("DeactivateAllObjects", onDuration);
    }

    public void DeactivateAllObjects()
    {
        Debug.Log("[GameCubeController] すべてのオブジェクトを OFF");

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null)
            {
                targetObjects[i].SetActive(false);
            }
        }

        isRunning = false;

        // ループする場合は再び開始
        if (loop)
        {
            SendCustomEventDelayedSeconds("ActivateAllObjects", waitTime);
        }
    }
}