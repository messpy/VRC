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

    [MenuItem("KennyTools/最近のアセット一覧を表示")]
    public static void ShowWindow()
    {
        GetWindow<RecentAssetWindow>("最近のアセット");
        RefreshAssetList();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("◉ 何時間以内のファイルを表示するか", EditorStyles.boldLabel);
        float hours = (float)threshold.TotalHours;
        hours = EditorGUILayout.Slider("しきい値（時間）", hours, 0.1f, 24f);
        threshold = TimeSpan.FromHours(hours);

        if (GUILayout.Button("🔄 再読み込み"))
        {
            RefreshAssetList();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"最近のアセット（{recentAssets.Count}件）", EditorStyles.boldLabel);

        // 緑色のスタイルを作成
        GUIStyle greenLabelStyle = new GUIStyle(EditorStyles.label);
        greenLabelStyle.normal.textColor = Color.green;

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string assetPath in recentAssets)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍", GUILayout.Width(30)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                EditorGUIUtility.PingObject(obj);
            }

            // ここで色付きスタイルを適用
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
            if (!File.Exists(fullPath)) continue;

            DateTime lastWrite = File.GetLastWriteTime(fullPath);
            if ((DateTime.Now - lastWrite) < threshold)
            {
                recentAssets.Add(path);
            }
        }
    }
}

[InitializeOnLoad]
public class RecentAssetMarker
{
    private static readonly TimeSpan threshold = TimeSpan.FromHours(1);

    static RecentAssetMarker()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets")) return;

        string fullPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length), path);
        if (!File.Exists(fullPath)) return;

        DateTime lastWrite = File.GetLastWriteTime(fullPath);
        if ((DateTime.Now - lastWrite) < threshold)
        {
            // 薄緑色でマーカー
            EditorGUI.DrawRect(selectionRect, new Color(0.4f, 1.0f, 0.4f, 0.4f));
        }
    }
}
