// ===============================================================
// Copyright (c) HoliGimmick. All rights reserved.
// Created: 2025/06/01
// Updated: 2025/06/25
// ===============================================================

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VoiceFilter : UdonSharpBehaviour
{
    [Header("リスト内のプレイヤー同士が会話できる最大距離（0=完全遮断）")]
    [SerializeField, Min(0)] private float voiceDistance = 25.0f;

    [UdonSynced] private int[] syncedPlayerIds = new int[0];

    [Header("MaterialChanger の参照")]
    [SerializeField] private MaterialChanger materialChanger; // MaterialChanger スクリプトの参照

    private void Start()
    {
        // 初期化処理が必要であればここに記述
    }

    public override void Interact()
    {
        ToggleVoiceForMe();

        // MaterialChanger のマテリアル切り替えを呼び出す
        if (materialChanger != null)
        {
            materialChanger.ChangeMaterial();
        }
        else
        {
            Debug.LogError("MaterialChanger が設定されていません。");
        }
    }

    public void ToggleVoiceForMe()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        int localId = Networking.LocalPlayer.playerId;

        if (!IsPlayerInArray(syncedPlayerIds, localId))
        {
            syncedPlayerIds = AddToArray(syncedPlayerIds, localId);
            Debug.Log($"プレイヤーID {localId} がリストに追加されました。");
        }
        else
        {
            syncedPlayerIds = RemoveFromArray(syncedPlayerIds, localId);
            Debug.Log($"プレイヤーID {localId} がリストから削除されました。");
        }

        RequestSerialization();
        UpdateVoiceSettings();
    }

    public override void OnDeserialization()
    {
        UpdateVoiceSettings();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        // 誰かがJoinするたびに全員分リフレッシュ
        UpdateVoiceSettings();
    }

    private void UpdateVoiceSettings()
    {
        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        bool amCast = IsPlayerInArray(syncedPlayerIds, Networking.LocalPlayer.playerId);

        foreach (var player in players)
        {
            if (!Utilities.IsValid(player)) continue;

            bool isCast = IsPlayerInArray(syncedPlayerIds, player.playerId);

            if (amCast)
            {
                if (isCast)
                {
                    player.SetVoiceGain(10.0f);
                    player.SetVoiceDistanceNear(voiceDistance);
                    player.SetVoiceDistanceFar(voiceDistance);
                }
                else
                {
                    player.SetVoiceGain(0.0f);
                    player.SetVoiceDistanceNear(0.0f);
                    player.SetVoiceDistanceFar(0.0f);
                }
            }
            else
            {
                player.SetVoiceGain(10.0f);
                player.SetVoiceDistanceNear(0.0f);
                player.SetVoiceDistanceFar(voiceDistance);
            }
        }
    }

    private int[] AddToArray(int[] array, int value)
    {
        int[] newArray = new int[array.Length + 1];
        for (int i = 0; i < array.Length; i++) newArray[i] = array[i];
        newArray[array.Length] = value;
        return newArray;
    }

    private int[] RemoveFromArray(int[] array, int value)
    {
        int count = 0;
        foreach (int v in array) if (v != value) count++;
        int[] newArray = new int[count];
        int index = 0;
        foreach (int v in array) if (v != value) newArray[index++] = v;
        return newArray;
    }

    private bool IsPlayerInArray(int[] array, int playerId)
    {
        foreach (int id in array) if (id == playerId) return true;
        return false;
    }
}
