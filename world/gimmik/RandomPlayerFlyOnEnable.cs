using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RandomPlayerFlyOnEnable : UdonSharpBehaviour
{
    public float delayTime = 10.0f; // 実行を遅らせる時間 (秒)
    public Vector3 teleportOffset = new Vector3(0, 2, 0); // テレポート時のオフセット
    public Vector3 moveDirection = new Vector3(0, 0, 1); // 移動方向 (例: 前方)
    public float moveDistance = 5.0f; // 移動距離 (m)
    public float moveSpeed = 1.0f; // 移動速度 (m/s)
    public float returnDelay = 3.0f; // 元の位置に戻るまでの待機時間 (秒)

    private VRCPlayerApi selectedPlayer;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float moveStartTime;

    void OnEnable()
    {
        // 10秒後に処理を実行
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

        // 現在の位置を保存（元の位置に戻すため）
        originalPosition = selectedPlayer.GetPosition();

        // テレポート位置を計算（元の位置 + teleportOffset）
        Vector3 teleportPosition = originalPosition + teleportOffset;
        selectedPlayer.TeleportTo(teleportPosition, Quaternion.identity);

        // 移動の準備
        targetPosition = teleportPosition + moveDirection.normalized * moveDistance;
        isMoving = true;
        moveStartTime = Time.time;

        // 移動開始
        SendCustomEvent("MovePlayer");
    }

    public void MovePlayer()
    {
        if (isMoving && selectedPlayer != null)
        {
            float elapsedTime = Time.time - moveStartTime;
            float progress = elapsedTime * moveSpeed / moveDistance; // 進行度 (0.0～1.0)

            if (progress < 1.0f)
            {
                Vector3 newPosition = Vector3.Lerp(originalPosition + teleportOffset, targetPosition, progress);
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
