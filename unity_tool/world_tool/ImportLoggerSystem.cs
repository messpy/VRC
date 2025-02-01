using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ImportLoggerSystem : MonoBehaviour
{
    private static ImportLoggerSystem instance;
    private string filePath;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += HandleLog;
            
            // 日付をファイル名に含める
            string folderPath = Path.Combine(Application.dataPath, "Logs");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            filePath = Path.Combine(folderPath, "Assetsimport_log_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            WriteToFile("エラー", logString, stackTrace);
        }
    }

    void WriteToFile(string logType, string logString, string stackTrace = "")
    {
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"[{logType}] {DateTime.Now}");
            writer.WriteLine(logString);
            if (!string.IsNullOrEmpty(stackTrace))
            {
                writer.WriteLine("スタックトレース: " + stackTrace);
            }
            writer.WriteLine();
        }
    }
}

public class AssetLogger : AssetPostprocessor
{
    static string folderPath = Path.Combine(Application.dataPath, "Logs");
    static string filePath = Path.Combine(folderPath, "combined_log_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine("[アセット変更] " + DateTime.Now);
            writer.WriteLine("インポートされたアセット: ");
            foreach (string asset in importedAssets)
            {
                writer.WriteLine(" - " + asset);
            }

            writer.WriteLine("削除されたアセット: ");
            foreach (string asset in deletedAssets)
            {
                writer.WriteLine(" - " + asset);
            }

            writer.WriteLine("移動されたアセット: ");
            for (int i = 0; i < movedAssets.Length; i++)
            {
                writer.WriteLine(" - 移動元: " + movedFromAssetPaths[i] + " 移動先: " + movedAssets[i]);
            }

            writer.WriteLine();
        }
    }
}
