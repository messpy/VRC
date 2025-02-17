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
    public Vector3 rotationAmount = Vector3.zero; // ユーザーが設定する回転量 (X, Y, Z)

    private VRCPlayerApi selectedPlayer;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;
    private float moveStartTime;

    void OnEnable()
    {
        SendCustomEventDelayedSeconds("SelectAndMovePlayer", delayTime);
    }

    public void SelectAndMovePlayer()
    {
        int playerCount = VRCPlayerApi.GetPlayerCount();
        if (playerCount == 0)
        {
            Debug.Log("プレイヤーがいません");
            return;
        }

        VRCPlayerApi[] players = new VRCPlayerApi[playerCount];
        VRCPlayerApi.GetPlayers(players);

        int randomIndex = Random.Range(0, playerCount);
        selectedPlayer = players[randomIndex];

        if (selectedPlayer == null)
        {
            Debug.Log("選ばれたプレイヤーが無効です。");
            return;
        }

        Debug.Log("選ばれたプレイヤー: " + selectedPlayer.displayName);

        if (object1 == null || object2 == null)
        {
            Debug.Log("オブジェクトの参照が設定されていません！");
            return;
        }

        // プレイヤーの現在位置と回転を記録
        originalPosition = selectedPlayer.GetPosition();
        originalRotation = selectedPlayer.GetRotation();

        // 開始地点と目標地点の位置・回転を取得
        Vector3 startPosition = object1.transform.position;
        Quaternion startRotation = object1.transform.rotation;
        targetPosition = object2.transform.position;
        targetRotation = object2.transform.rotation * Quaternion.Euler(rotationAmount);

        // プレイヤーを開始位置にテレポート
        selectedPlayer.TeleportTo(startPosition, startRotation);

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
            float progress = elapsedTime / moveDuration;

            if (progress < 1.0f)
            {
                Vector3 newPosition = Vector3.Lerp(object1.transform.position, targetPosition, progress);
                Quaternion newRotation = Quaternion.Slerp(object1.transform.rotation, targetRotation, progress);
                selectedPlayer.TeleportTo(newPosition, newRotation);

                SendCustomEventDelayedFrames("MovePlayer", 1);
            }
            else
            {
                selectedPlayer.TeleportTo(targetPosition, targetRotation);
                isMoving = false;

                SendCustomEventDelayedSeconds("ReturnPlayer", returnDelay);
            }
        }
    }

    public void ReturnPlayer()
    {
        if (selectedPlayer != null)
        {
            selectedPlayer.TeleportTo(originalPosition, originalRotation);
            Debug.Log("プレイヤーが元の位置に戻りました。");
        }
    }
}
