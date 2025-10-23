using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EventPeriodActivator : UdonSharpBehaviour
{
    [Header("Nightmode Time Settings (例: 22なら夜10時開始、6なら朝6時終了)")]
    [Range(0, 23)]
    public int NightmodeStart = 22;
    [Range(0, 23)]
    public int NightmodeEnd = 6;

    [Header("Nightmode Date Range (inclusive)")]
    [Tooltip("例: 2025年7月1日から2025年7月3日までONにしたい場合。年0で毎年、月0で毎月、日0で毎日")]
    public int StartYear = 0;
    public int StartMonth = 10;
    public int StartDay = 1;
    public int EndYear = 0;
    public int EndMonth = 10;
    public int EndDay = 29;

    [Header("Objects to Enable during Nightmode")]
    [Tooltip("ナイトモード中にONにしたいオブジェクト")]
    public GameObject[] nightmodeOnObjects;

    [Header("Objects to Disable during Nightmode")]
    [Tooltip("ナイトモード中にOFFにしたいオブジェクト")]
    public GameObject[] nightmodeOffObjects;

    private void Update()
    {
        bool isNight = Nightmode();

        foreach (var obj in nightmodeOnObjects)
        {
            if (obj != null) obj.SetActive(isNight);
        }
        foreach (var obj in nightmodeOffObjects)
        {
            if (obj != null) obj.SetActive(!isNight);
        }

        var now = Networking.GetNetworkDateTime();
        Debug.Log(
            $"[Birthday1] Now: {now.Year}/{now.Month}/{now.Day} {now.Hour}:{now.Minute} | " +
            $"Nightmode Range: {StartYear}/{StartMonth}/{StartDay} - {EndYear}/{EndMonth}/{EndDay} | " +
            $"Nightmode Time: {NightmodeStart} - {NightmodeEnd} | IsNight: {isNight}"
        );
    }

    public bool Nightmode()
    {
        var now = Networking.GetNetworkDateTime();

        // 日付範囲判定（0はワイルドカード）
        bool afterStart = CompareDate(now, StartYear, StartMonth, StartDay) >= 0;
        bool beforeEnd  = CompareDate(now, EndYear, EndMonth, EndDay) <= 0;

        if (afterStart && beforeEnd)
        {
            // 時刻判定
            if (NightmodeStart == 0 && NightmodeEnd == 0)
            {
                return true; // 24時間ON
            }
            else
            {
                int hour = now.Hour;
                if (NightmodeStart < NightmodeEnd)
                {
                    if (hour >= NightmodeStart && hour <= NightmodeEnd) return true;
                }
                else if (NightmodeStart > NightmodeEnd)
                {
                    if (hour >= NightmodeStart || hour <= NightmodeEnd) return true;
                }
                else
                {
                    if (hour == NightmodeStart) return true;
                }
            }
        }
        return false;
    }

    // 0はワイルドカードとして判定
    private int CompareDate(System.DateTime now, int y, int m, int d)
    {
        if (y > 0 && now.Year != y)
        {
            return now.Year < y ? -1 : 1;
        }
        if (m > 0 && now.Month != m)
        {
            return now.Month < m ? -1 : 1;
        }
        if (d > 0 && now.Day != d)
        {
            return now.Day < d ? -1 : 1;
        }
        return 0;
    }
}
