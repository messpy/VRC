using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

public class ErrorLogExporter : EditorWindow
{
    private static readonly string logFolderPath = Path.Combine(Application.dataPath, "Logs");
    private static readonly string logFilePath = Path.Combine(logFolderPath, "ErrorLog.txt");
    private static readonly string errorAssetsKey = "ErrorLogExporter_Assets";
    private Vector2 scroll;

    private static HashSet<string> recentErrorAssetGuids = new HashSet<string>();

    [MenuItem("KennyTools/エラーログ記録UI")]
    public static void ShowWindow()
    {
        LoadErrorAssets();
        GetWindow<ErrorLogExporter>("エラーログ記録UI");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("エラー対象ファイル・フォルダ一覧", EditorStyles.boldLabel);

        // ドラッグ＆ドロップで追加
        GUILayout.Label("ここにアセットやフォルダをドラッグ＆ドロップしてください。", EditorStyles.helpBox, GUILayout.Height(30));
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetLastRect();
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                        if (!string.IsNullOrEmpty(guid))
                        {
                            recentErrorAssetGuids.Add(guid);
                        }
                    }
                    SaveErrorAssets();
                    Repaint();
                }
                evt.Use();
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("ログファイル初期化（バックアップ作成）"))
        {
            if (!Directory.Exists(logFolderPath))
                Directory.CreateDirectory(logFolderPath);

            // バックアップ
            if (File.Exists(logFilePath))
            {
                string backupPath = logFilePath + "." + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";
                File.Copy(logFilePath, backupPath, true);
            }
            File.WriteAllText(logFilePath, $"=== Error Log Start ({DateTime.Now}) ===\n");
            recentErrorAssetGuids.Clear();
            SaveErrorAssets();
            Debug.Log("ログファイルを初期化し、エラーアセット履歴もクリアしました。");
        }

        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(300));
        foreach (var guid in recentErrorAssetGuids.ToList())
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj == null)
            {
                recentErrorAssetGuids.Remove(guid);
                continue;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍", GUILayout.Width(30)))
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            EditorGUILayout.LabelField(assetPath);
            if (GUILayout.Button("削除", GUILayout.Width(50)))
            {
                recentErrorAssetGuids.Remove(guid);
                SaveErrorAssets();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("ログファイルを開く"))
        {
            EditorUtility.RevealInFinder(logFilePath);
        }
        if (GUILayout.Button("一覧をログに追記"))
        {
            using (StreamWriter sw = File.AppendText(logFilePath))
            {
                sw.WriteLine($"--- Error Assets List ({DateTime.Now}) ---");
                foreach (var guid in recentErrorAssetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    sw.WriteLine(assetPath);
                }
            }
            Debug.Log("ログファイルに一覧を追記しました。");
        }
    }

    // 永続化
    private static void SaveErrorAssets()
    {
        EditorPrefs.SetString(errorAssetsKey, string.Join(",", recentErrorAssetGuids));
    }
    private static void LoadErrorAssets()
    {
        string data = EditorPrefs.GetString(errorAssetsKey, "");
        recentErrorAssetGuids = new HashSet<string>((data ?? "").Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries));
    }
}