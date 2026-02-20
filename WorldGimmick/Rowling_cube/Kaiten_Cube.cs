using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class KaitenCube : UdonSharpBehaviour
{
    // プレイヤーとの距離を計算する中心となるオブジェクト
    public Transform detectionCenter;
    // 検知する半径（単位：メートル）
    public float detectionRadius = 5f;
    // 回転速度（Z軸、角度/秒）
    public float rotationSpeedZ = 30f;

    void Update()
    {
        // ローカルプレイヤーを取得
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;
        
        // プレイヤーと検知中心との距離を計算
        float distance = Vector3.Distance(localPlayer.GetPosition(), detectionCenter.position);
        
        // プレイヤーが検知範囲内なら回転させる
        if (distance <= detectionRadius)
        {
            transform.Rotate(0f, 0f, rotationSpeedZ * Time.deltaTime);
        }
    }
}
