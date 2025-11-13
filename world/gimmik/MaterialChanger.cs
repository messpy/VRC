using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MaterialChanger : UdonSharpBehaviour
{
    [Header("切り替え用のマテリアル")]
    [SerializeField] private Material material1; // 最初のマテリアル
    [SerializeField] private Material material2; // 切り替え先のマテリアル

    [Header("対象オブジェクト")]
    [SerializeField] private Renderer targetRenderer; // マテリアルを変更する対象のRenderer

    private bool isMaterial1Active = true; // 現在のマテリアル状態を追跡

    // 他のスクリプトからも呼び出せるメソッド
    public void ChangeMaterial()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("ターゲットの Renderer が設定されていません。");
            return;
        }

        // 現在のマテリアルを切り替える
        if (isMaterial1Active)
        {
            targetRenderer.material = material2;
            Debug.Log("Material を material2 に切り替えました。");
        }
        else
        {
            targetRenderer.material = material1;
            Debug.Log("Material を material1 に切り替えました。");
        }

        isMaterial1Active = !isMaterial1Active; // 状態を反転
    }

    // Interact メソッドからも ChangeMaterial を呼び出す
    public override void Interact()
    {
        ChangeMaterial();
    }
}
