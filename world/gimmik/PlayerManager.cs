using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerDataUtils : UdonSharpBehaviour
{
    public static PlayerDataUtils Instance;

    void Start()
    {
        Instance = this;
    }

    // ========================== 基本情報取得 ==========================

    // 全プレイヤーのリストを取得
    public VRCPlayerApi[] GetAllPlayers()
    {
        if (Instance == null)
        {
            Debug.LogError("PlayerDataUtilsが見つかりません。");
            return new VRCPlayerApi[0];
        }
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        return players;
    }

    // 指定したプレイヤーの名前を取得
    public string GetPlayerName(VRCPlayerApi player)
    {
        return player != null && player.IsValid() ? player.displayName : "Unknown";
    }

    // ホストプレイヤーを取得
    public VRCPlayerApi GetHostPlayer()
    {
        return Networking.GetOwner(gameObject);
    }

    // ========================== 位置・回転情報取得 ==========================

    // 指定したプレイヤーの位置を取得
    public Vector3 GetPlayerPosition(VRCPlayerApi player)
    {
        return player != null && player.IsValid() ? player.GetPosition() : Vector3.zero;
    }

    // 指定したプレイヤーの回転を取得
    public Quaternion GetPlayerRotation(VRCPlayerApi player)
    {
        return player != null && player.IsValid() ? player.GetRotation() : Quaternion.identity;
    }

    // ========================== プレイヤー状態判定 ==========================

    // プレイヤーがPCかどうかを判定
    public bool IsPlayerPC(VRCPlayerApi player)
    {
        return player != null && player.IsValid() && !player.IsUserInVR();
    }

    // プレイヤーがVRかどうかを判定
    public bool IsPlayerVR(VRCPlayerApi player)
    {
        return player != null && player.IsValid() && player.IsUserInVR();
    }

    // プレイヤーが3点トラッキングを使用しているかを判定
    public bool IsPlayerUsing3PointTracking(VRCPlayerApi player)
    {
        return player != null && player.IsValid() && player.IsUserInVR() && player.IsUsing3PointTracking();
    }

    // ========================== プレイヤー操作 ==========================

    // 指定プレイヤーをワープさせる
    public void TeleportPlayer(VRCPlayerApi player, Vector3 position, Quaternion rotation)
    {
        if (player != null && player.IsValid())
        {
            player.TeleportTo(position, rotation);
        }
    }

    // プレイヤーのスケールを変更
    public void SetPlayerScale(VRCPlayerApi player, float scale)
    {
        if (player != null && player.IsValid())
        {
            player.SetAvatarScale(scale);
        }
    }
}

// ========================== 他のスクリプトからの参照方法 ==========================
// PlayerDataUtils を他のスクリプトから利用する場合は、以下の方法で呼び出してください。
//
// 必要な using:
// using UdonSharp;
// using UnityEngine;
// using VRC.SDKBase;
// using VRC.Udon;
//
// 1. PlayerDataUtils の null チェック:
//    if (PlayerDataUtils.Instance == null)
//    {
//        Debug.LogError("PlayerDataUtilsが見つかりません。");
//        return;
//    }
//
// 2. 全プレイヤーのリストを取得:
//    VRCPlayerApi[] allPlayers = PlayerDataUtils.Instance.GetAllPlayers();
//    Debug.Log("現在のプレイヤー数: " + allPlayers.Length);
//
// 3. 特定のプレイヤーの情報取得:
//    string playerName = PlayerDataUtils.Instance.GetPlayerName(allPlayers[0]);
//    Debug.Log("プレイヤー名: " + playerName);
//
//    Vector3 position = PlayerDataUtils.Instance.GetPlayerPosition(allPlayers[0]);
//    Debug.Log("プレイヤーの位置: " + position);
//
//    Quaternion rotation = PlayerDataUtils.Instance.GetPlayerRotation(allPlayers[0]);
//    Debug.Log("プレイヤーの回転: " + rotation);
//
// 4. プレイヤーのデバイス情報取得:
//    bool isPC = PlayerDataUtils.Instance.IsPlayerPC(allPlayers[0]);
//    Debug.Log("このプレイヤーはPCユーザーか？ " + isPC);
//
//    bool isVR = PlayerDataUtils.Instance.IsPlayerVR(allPlayers[0]);
//    Debug.Log("このプレイヤーはVRユーザーか？ " + isVR);
//
//    bool isUsing3Point = PlayerDataUtils.Instance.IsPlayerUsing3PointTracking(allPlayers[0]);
//    Debug.Log("このプレイヤーは3点トラッキングを使用しているか？ " + isUsing3Point);
//
// 5. プレイヤーをワープさせる:
//    PlayerDataUtils.Instance.TeleportPlayer(allPlayers[0], new Vector3(0, 1, 0), Quaternion.identity);
//    Debug.Log("プレイヤーをワープさせました。");
//
// 6. プレイヤーのスケールを変更:
//    PlayerDataUtils.Instance.SetPlayerScale(allPlayers[0], 1.5f);
//    Debug.Log("プレイヤーのスケールを変更しました。");
