using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

public class ErrorLogExporter : EditorWindow
{
    private static readonly string logFolderPath = Path.Combine(Application.dataPath, "Logs");
    private static readonly string logFilePath = Path.Combine(logFolderPath, "ErrorLog.txt");
    private static HashSet<UnityEngine.Object> recentErrorAssets = new HashSet<UnityEngine.Object>();
    private Vector2 scroll;

    [MenuItem("KennyTools/エラーログ記録UI")]
    public static void ShowWindow()
    {
        GetWindow<ErrorLogExporter>("エラーログ記録UI");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("エラー対象ファイル・フォルダ一覧", EditorStyles.boldLabel);

        if (GUILayout.Button("ログファイル初期化"))
        {
            // フォルダがなければ作成
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
                Debug.Log("Logsフォルダを作成しました: " + logFolderPath);
            }
            else
            {
                Debug.Log("Logsフォルダは既に存在します: " + logFolderPath);
            }

            // ファイルがなければ作成
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, "");
                Debug.Log("ErrorLog.txtファイルを作成しました: " + logFilePath);
            }
            else
            {
                Debug.Log("ErrorLog.txtファイルは既に存在します: " + logFilePath);
            }

            // 初期化
            File.WriteAllText(logFilePath, $"=== Error Log Start ({DateTime.Now}) ===\n");
            recentErrorAssets.Clear();
            Debug.Log("ログファイルを初期化しました。");
        }

        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(300));
        foreach (var obj in recentErrorAssets)
        {
            if (obj == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(obj);
            string displayName = string.IsNullOrEmpty(assetPath) ? obj.name : assetPath;
            bool isFolder = AssetDatabase.IsValidFolder(assetPath);

            Color boxColor = isFolder
                ? new Color(0.8f, 0.9f, 1.0f, 1f)
                : new Color(0.85f, 0.85f, 0.85f, 1f);

            EditorGUILayout.BeginHorizontal();
            Rect rect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, boxColor);

            if (GUILayout.Button("🔍", GUILayout.Width(30)))
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            EditorGUI.LabelField(new Rect(rect.x + 35, rect.y, rect.width - 35, rect.height), displayName);

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("ログファイルを開く"))
        {
            EditorUtility.RevealInFinder(logFilePath);
        }
    }
}
