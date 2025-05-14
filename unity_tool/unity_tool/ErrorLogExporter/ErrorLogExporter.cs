using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

public class ErrorLogExporter : EditorWindow
{
    private static bool isLogging = false;
    private static readonly string logFolderPath = Path.Combine(Application.dataPath, "Logs");
    private static readonly string logFilePath = Path.Combine(logFolderPath, "ErrorLog.txt");

    private static HashSet<UnityEngine.Object> recentErrorObjects = new HashSet<UnityEngine.Object>();
    private static Dictionary<UnityEngine.Object, double> errorTimestamps = new Dictionary<UnityEngine.Object, double>();
    private const double highlightDuration = 300.0; // 300 seconds

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HighlightHierarchy;
        EditorApplication.projectWindowItemOnGUI += HighlightProject;
    }

    [MenuItem("kennyTool/エラーログ記録を有効化")]
    private static void EnableLogging()
    {
        if (isLogging)
        {
            Debug.Log("既にログ記録は有効です。");
            return;
        }

        if (!Directory.Exists(logFolderPath))
            Directory.CreateDirectory(logFolderPath);

        File.WriteAllText(logFilePath, $"=== Error Log Start ({DateTime.Now}) ===\n");

        Application.logMessageReceived += HandleLog;
        isLogging = true;

        Debug.Log("エラーログ記録を開始しました。");
        Debug.Log($"出力先: {logFilePath}");
    }

    [MenuItem("kennyTool/エラーログ記録を無効化")]
    private static void DisableLogging()
    {
        if (!isLogging)
        {
            Debug.Log("ログ記録は既に無効です。");
            return;
        }

        Application.logMessageReceived -= HandleLog;
        isLogging = false;

        Debug.Log("エラーログ記録を停止しました。");
    }

    [MenuItem("kennyTool/ログファイルを初期化して有効化")]
    private static void ResetAndEnable()
    {
        DisableLogging();
        EnableLogging();
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!isLogging)
            return;

        if (type != LogType.Error && type != LogType.Exception && type != LogType.Warning)
            return;

        UnityEngine.Object contextObject = null;

        // Parse context from known log format
        if (stackTrace.Contains("UnityEngine.Object"))
        {
            contextObject = Selection.activeObject;
        }

        if (contextObject != null)
        {
            recentErrorObjects.Add(contextObject);
            errorTimestamps[contextObject] = EditorApplication.timeSinceStartup;
        }

        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            writer.WriteLine($"[{timestamp}] [{type}] {logString}");

            string location = "(場所不明)";
            if (!string.IsNullOrEmpty(stackTrace))
            {
                string[] lines = stackTrace.Split('\n');
                if (lines.Length > 0)
                    location = lines[0].Trim();
            }
            writer.WriteLine($"  ↳ 発生箇所: {location}");

            string advice = GetAdvice(logString);
            if (!string.IsNullOrEmpty(advice))
            {
                string[] parts = advice.Split('|');
                writer.WriteLine($"  💡 推定原因: {parts[0]}");
                writer.WriteLine($"  💡 対処例: {parts[1]}");
            }

            if (!string.IsNullOrEmpty(stackTrace))
            {
                writer.WriteLine("  ▼ 詳細スタックトレース:");
                foreach (var line in stackTrace.Split('\n'))
                    writer.WriteLine("    " + line.Trim());
            }

            writer.WriteLine(new string('-', 50));
        }
    }

    private static void HighlightHierarchy(int instanceID, Rect selectionRect)
    {
        UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj == null || !recentErrorObjects.Contains(obj)) return;

        if (EditorApplication.timeSinceStartup - errorTimestamps[obj] > highlightDuration)
        {
            recentErrorObjects.Remove(obj);
            errorTimestamps.Remove(obj);
            return;
        }

        Color prev = GUI.color;
        GUI.color = Color.grey;
        GUI.Label(selectionRect, EditorGUIUtility.ObjectContent(obj, obj.GetType()).text);
        GUI.color = prev;

        if (Event.current.type == EventType.MouseDown && selectionRect.Contains(Event.current.mousePosition))
        {
            Selection.activeObject = obj;
            Event.current.Use();
        }
    }

    private static void HighlightProject(string guid, Rect selectionRect)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
        if (obj == null || !recentErrorObjects.Contains(obj)) return;

        if (EditorApplication.timeSinceStartup - errorTimestamps[obj] > highlightDuration)
        {
            recentErrorObjects.Remove(obj);
            errorTimestamps.Remove(obj);
            return;
        }

        Color prev = GUI.color;
        GUI.color = Color.grey;
        GUI.Label(selectionRect, Path.GetFileName(path));
        GUI.color = prev;

        if (Event.current.type == EventType.MouseDown && selectionRect.Contains(Event.current.mousePosition))
        {
            Selection.activeObject = obj;
            Event.current.Use();
        }
    }

    private static string GetAdvice(string message)
    {
        if (message.Contains("NullReferenceException"))
            return "オブジェクトが null のまま使用されています|if (obj != null) でチェックするか、Inspector に正しく割り当ててください";
        if (message.Contains("IndexOutOfRangeException"))
            return "配列やリストの範囲外にアクセスしています|Index が Count や Length 未満か確認してください";
        if (message.Contains("MissingReferenceException"))
            return "Destroy 済みのオブジェクトを使おうとしています|null チェックしてからアクセスするか、参照保持の見直しをしてください";
        if (message.Contains("ArgumentException"))
            return "不正な引数が渡されています|呼び出し元の引数が適切か確認してください";
        if (message.Contains("KeyNotFoundException"))
            return "Dictionary に存在しないキーでアクセスしています|ContainsKey(key) で事前にチェックしてください";
        if (message.Contains("UnassignedReferenceException"))
            return "Inspector に必要なオブジェクトが割り当てられていません|該当フィールドに正しく設定してください";
        if (message.Contains("manual sync while VRCObjectSync is on"))
            return "VRCObjectSync と Manual Sync が競合しています|UdonBehaviour の Sync Mode を 'None' か 'Continuous' に変更するか、VRCObjectSync を削除してください";

        return "";
    }
}
