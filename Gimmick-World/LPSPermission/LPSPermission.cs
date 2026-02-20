using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[DefaultExecutionOrder(-1000)]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LPSPermissionAll : UdonSharpBehaviour
{

    [Header("権限を与える対象アカウントの名前リスト")]
    [SerializeField] private string[] targetAccountNames;
    [Header("権限持ちのみ ON にするレンダラー（スタッフのみ見えるなど）")]
    [SerializeField] private Renderer[] restrictedRenderers;
    [Header("権限持ちのみ ON にするコライダー/トリガー（スタッフのみ乗れる/持てる・使えるなど）")]
    [SerializeField] private Collider[] restrictedColliders;
    [Header("権限持ちのみ ON にするオブジェクト")]
    [SerializeField] private GameObject[] restrictedObjects;

    [Header("権限を持つユーザーのリスポーン位置")]
    [SerializeField] private Transform privilegedRespawnPoint;

    private bool hasPermission = false;
    private Vector3 lastPosition;

    void Start()
    {
        foreach (string name in targetAccountNames)
        {
            if (Networking.LocalPlayer.displayName == name)
            {
                hasPermission = true;
                break;
            }
        }
        Apply();
        SetRespawnPoint(); // 初期リスポーン位置を設定
        lastPosition = Networking.LocalPlayer.GetPosition(); // 現在の位置を記録
    }

    private void Update()
    {
        if (!hasPermission) return;

        // プレイヤーの位置を監視
        Vector3 currentPosition = Networking.LocalPlayer.GetPosition();
        if (Vector3.Distance(currentPosition, lastPosition) > 10f) // 位置が大きく変わった場合
        {
            SetRespawnPoint(); // カスタムリスポーン位置に移動
        }
        lastPosition = currentPosition; // 現在の位置を更新
    }

    private void Apply()
    {
        if (!hasPermission) return;

        // 権限を持つユーザー向けのオブジェクトを有効化
        foreach (Renderer renderer in restrictedRenderers)
        {
            if (renderer != null) renderer.enabled = true;
        }
        foreach (Collider collider in restrictedColliders)
        {
            if (collider != null) collider.enabled = true;
        }
        foreach (GameObject obj in restrictedObjects)
        {
            if (obj != null) obj.SetActive(true);
        }
    }

    private void SetRespawnPoint()
    {
        if (!hasPermission || Networking.LocalPlayer == null || privilegedRespawnPoint == null) return;

        // 権限を持つユーザーを指定されたリスポーン位置に移動
        Networking.LocalPlayer.TeleportTo(privilegedRespawnPoint.position, privilegedRespawnPoint.rotation);
    }
    public override void OnPlayerRespawn(VRCPlayerApi player)
         {
        Networking.LocalPlayer.TeleportTo(privilegedRespawnPoint.position, privilegedRespawnPoint.rotation);
         }
}
