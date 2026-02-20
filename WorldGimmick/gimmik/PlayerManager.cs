
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
// 3. プレイヤーの情報を取得:
//    string playerName = PlayerDataUtils.Instance.GetPlayerName(allPlayers[0]);
//    Debug.Log("プレイヤー名: " + playerName);
//
//    Vector3 position = PlayerDataUtils.Instance.GetPlayerPosition(allPlayers[0]);
//    Debug.Log("プレイヤーの位置: " + position);
//
//    Quaternion rotation = PlayerDataUtils.Instance.GetPlayerRotation(allPlayers[0]);
//    Debug.Log("プレイヤーの回転: " + rotation);
