using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Birthday1 - Nightmode Controller
/// 指定した日付や時刻の間だけ、Inspectorで指定したオブジェクトをON/OFFにします。
/// </summary>
public class Birthday1 : UdonSharpBehaviour
{
    [Header("Nightmode Time Settings (例: 22なら夜10時開始、6なら朝6時終了)")]
    [Range(0, 23)]
    public int NightmodeStart = 22;
    [Range(0, 23)]
    public int NightmodeEnd = 6;

    [Header("Nightmode Date Range (inclusive)")]
    [Tooltip("例: 2025年7月1日から2025年7月3日までONにしたい場合")]
    public int StartYear = 2025;
    public int StartMonth = 7;
    public int StartDay = 1;
    public int EndYear = 2025;
    public int EndMonth = 7;
    public int EndDay = 3;

    [Header("Objects to Enable during Nightmode")]
    [Tooltip("ナイトモード中にONにしたいオブジェクト")]
    public GameObject[] nightmodeOnObjects;

    [Header("Objects to Disable during Nightmode")]
    [Tooltip("ナイトモード中にOFFにしたいオブジェクト")]
    public GameObject[] nightmodeOffObjects;

    private void Update()
    {
        bool isNight = Nightmode();

        // ONにするオブジェクト
        foreach (var obj in nightmodeOnObjects)
        {
            if (obj != null) obj.SetActive(isNight);
        }
        // OFFにするオブジェクト
        foreach (var obj in nightmodeOffObjects)
        {
            if (obj != null) obj.SetActive(!isNight);
        }
    }

    /// <summary>
    /// ナイトモード判定
    /// 日付範囲内なら常にON、範囲外なら時刻でON/OFF
    /// </summary>
    public bool Nightmode()
    {
        var now = Networking.GetNetworkDateTime();

        // 日付判定（0の項目はスキップ＝ワイルドカード）
        bool skipDate = (StartYear == 0 && StartMonth == 0 && StartDay == 0 && EndYear == 0 && EndMonth == 0 && EndDay == 0);
        bool dateMatch = true;

        if (!skipDate)
        {
            // 0や不正値が混じっていたら日付判定をスキップ
            if (IsValidDate(StartYear, StartMonth, StartDay) && IsValidDate(EndYear, EndMonth, EndDay))
            {
                System.DateTime nightStart = new System.DateTime(StartYear, StartMonth, StartDay);
                System.DateTime nightEnd = new System.DateTime(EndYear, EndMonth, EndDay);
                if (now.Date < nightStart.Date || now.Date > nightEnd.Date)
                {
                    dateMatch = false;
                }
            }
            else
            {
                // 不正な日付はスキップ（時刻判定のみ）
                dateMatch = false;
            }
        }

        if (dateMatch)
        {
            // 時刻判定
            if (NightmodeStart == 0 && NightmodeEnd == 0)
            {
                // その日すべてON
                return true;
            }
            else
            {
                int hour = now.Hour;
                if (NightmodeStart < NightmodeEnd)
                {
                    // 例: 8～20時
                    if (hour >= NightmodeStart && hour <= NightmodeEnd) return true;
                }
                else if (NightmodeStart > NightmodeEnd)
                {
                    // 例: 22～翌6時
                    if (hour >= NightmodeStart || hour <= NightmodeEnd) return true;
                }
                else
                {
                    // Start==End（0以外）→その1時間だけON
                    if (hour == NightmodeStart) return true;
                }
            }
        }
        return false;
    }

    // 日付が有効かどうかを判定
    private bool IsValidDate(int y, int m, int d)
    {
        if (y <= 0 || m <= 0 || d <= 0) return false;
        if (m > 12 || d > 31) return false;
        // 2月の日数やうるう年などの厳密な判定は省略（必要なら追加）
        return true;
    }
}
