using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RandomPlayerFlyOnEnable : UdonSharpBehaviour
{
    public float delayTime = 1.0f; // 実行を遅らせる時間 (秒)
    public GameObject object1; // 開始位置
    public GameObject object2; // 目標位置
    public float moveDuration = 3.0f; // 移動時間 (秒)
    public float returnDelay = 3.0f; // 元の位置に戻るまでの待機時間 (秒)

    private VRCPlayerApi selectedPlayer;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float moveStartTime;

    void OnEnable()
    {
        // 指定時間後に処理を実行
        SendCustomEventDelayedSeconds("SelectAndMovePlayer", delayTime);
    }

    public void SelectAndMovePlayer()
    {
        // プレイヤー数を取得
        int playerCount = VRCPlayerApi.GetPlayerCount();
        if (playerCount == 0)
        {
            Debug.Log("プレイヤーがいません");
            return;
        }

        // プレイヤーリストを取得
        VRCPlayerApi[] players = new VRCPlayerApi[playerCount];
        VRCPlayerApi.GetPlayers(players);

        // ランダムに1人選択
        int randomIndex = Random.Range(0, playerCount);
        selectedPlayer = players[randomIndex];

        if (selectedPlayer == null)
        {
            Debug.Log("選ばれたプレイヤーが無効です。");
            return;
        }

        Debug.Log("選ばれたプレイヤー: " + selectedPlayer.displayName);

        // `object1` から `object2` に移動
        if (object1 == null || object2 == null)
        {
            Debug.Log("オブジェクトの参照が設定されていません！");
            return;
        }

        originalPosition = object1.transform.position;
        targetPosition = object2.transform.position;

        // プレイヤーを `object1` の位置にテレポート
        selectedPlayer.TeleportTo(originalPosition, Quaternion.identity);

        // 移動開始
        isMoving = true;
        moveStartTime = Time.time;
        SendCustomEvent("MovePlayer");
    }

    public void MovePlayer()
    {
        if (isMoving && selectedPlayer != null)
        {
            float elapsedTime = Time.time - moveStartTime;
            float progress = elapsedTime / moveDuration; // 進行度 (0.0～1.0)

            if (progress < 1.0f)
            {
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, progress);
                selectedPlayer.TeleportTo(newPosition, selectedPlayer.GetRotation());

                // 次のフレームも移動を続ける
                SendCustomEventDelayedFrames("MovePlayer", 1);
            }
            else
            {
                // 移動完了
                selectedPlayer.TeleportTo(targetPosition, selectedPlayer.GetRotation());
                isMoving = false;

                // 一定時間後に元の位置に戻す
                SendCustomEventDelayedSeconds("ReturnPlayer", returnDelay);
            }
        }
    }

    public void ReturnPlayer()
    {
        if (selectedPlayer != null)
        {
            selectedPlayer.TeleportTo(originalPosition, selectedPlayer.GetRotation());
            Debug.Log("プレイヤーが元の位置に戻りました。");
        }
    }
}
