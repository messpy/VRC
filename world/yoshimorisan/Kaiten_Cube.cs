using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Kaiten_Cube : UdonSharpBehaviour
{
    public Transform targetObject; // 回転させるオブジェクト
    public float triggerDistance = 3.0f; // 反応する距離
    public float baseSpeed = 3.0f; // 基本の回転速度
    public float maxSpeed = 999.0f; // 最大回転速度
    public float speedIncreaseDuration = 30.0f; // 何秒で最大速度に到達するか
    private VRCPlayerApi localPlayer;
    private float startTime;
    private bool shouldRotate = false;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }

    void Update()
    {
        if (localPlayer == null) return;

        float distance = Vector3.Distance(localPlayer.GetPosition(), targetObject.position);

        if (distance < triggerDistance)
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject); // オブジェクトのオーナーを設定
            }

            if (!shouldRotate)
            {
                shouldRotate = true;
                startTime = Time.time; // 回転開始時間を記録
            }
        }
        else
        {
            shouldRotate = false;
        }

        if (shouldRotate && Networking.IsOwner(gameObject))
        {
            float elapsedTime = Time.time - startTime;
            float speedFactor = Mathf.Clamp01(elapsedTime / speedIncreaseDuration);
            float currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, speedFactor);
            targetObject.Rotate(new Vector3(0, currentSpeed, 0) * Time.deltaTime);
        }
    }
}
