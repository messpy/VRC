using UdonSharp;
using UnityEngine;

public class UdonManager : UdonSharpBehaviour
{
    public UdonUtility utility; // UdonUtility の参照を設定する（Inspector からアタッチ可能）

    void Start()
    {
        // ✅ 1. インスペクターでアタッチしている場合、そのまま使う
        if (utility != null)
        {
            utility.SetActive(gameObject, false);
        }

        // ✅ 2. 自動で UdonUtility を探して取得
        if (utility == null)
        {
            utility = GetComponent<UdonUtility>();
        }

        // ✅ 3. 別のオブジェクトにある UdonUtility を探す
        if (utility == null)
        {
            GameObject utilityObject = GameObject.Find("UdonUtilityObject");
            if (utilityObject != null)
            {
                utility = utilityObject.GetComponent<UdonUtility>();
            }
        }
    }
}
