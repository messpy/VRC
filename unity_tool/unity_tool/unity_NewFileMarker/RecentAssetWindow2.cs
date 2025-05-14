using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class RecentAssetWindow : EditorWindow
{
    private static TimeSpan threshold = TimeSpan.FromHours(1);
    private static List<string> recentAssets = new List<string>();
    private Vector2 scroll;

    [MenuItem("Tools/最近のアセット一覧を表示")]
    public static void ShowWindow()
    {
        GetWindow<RecentAssetWindow>("最近のアセット");
        RefreshAssetList();
    }

    private void OnGUI()
    {
        // 緑色のスタイルを作成
        GUIStyle greenLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        greenLabelStyle.normal.textColor = Color.green;

        // 通常のラベルスタイル
        GUIStyle defaultLabelStyle = new GUIStyle(EditorStyles.label);

        EditorGUILayout.LabelField("◉ 何時間以内のファイルを表示するか", defaultLabelStyle); // 通常のラベル
        float hours = (float)threshold.TotalHours;
        hours = EditorGUILayout.Slider("しきい値（時間）", hours, 0.1f, 24f);
        threshold = TimeSpan.FromHours(hours);

        if (GUILayout.Button("🔄 再読み込み"))
        {
            RefreshAssetList();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"最近のアセット（{recentAssets.Count}件）", defaultLabelStyle); // 通常のラベル

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string assetPath in recentAssets)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍", GUILayout.Width(30)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                EditorGUIUtility.PingObject(obj);
            }

            // アセットパスを緑色で表示
            EditorGUILayout.LabelField(assetPath, greenLabelStyle);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private static void RefreshAssetList()
    {
        recentAssets.Clear();
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

        string root = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);

        foreach (string path in allAssetPaths)
        {
            if (!path.StartsWith("Assets")) continue;

            string fullPath = Path.Combine(root, path);

            // ファイルまたはフォルダが存在するかを確認
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath)) continue;

            DateTime lastWrite = File.GetLastWriteTime(fullPath);
            if ((DateTime.Now - lastWrite) < threshold)
            {
                recentAssets.Add(path);
            }
        }
    }
}
