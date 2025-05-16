using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

public class ComponentHighlighterUI : EditorWindow
{
    private static List<Type> componentTypes;
    private static Type selectedType;
    private static HashSet<GameObject> matchedObjects = new HashSet<GameObject>();

    private string searchTerm = "";
    private Vector2 scrollPos;

    [MenuItem("KennyTools/コンポーネントハイライト")]
    public static void ShowWindow()
    {
        GetWindow<ComponentHighlighterUI>("コンポーネント検索");
    }

    private void OnEnable()
    {
        componentTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm =>
            {
                try { return asm.GetTypes(); } catch { return new Type[0]; }
            })
            .Where(t => typeof(Component).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)
            .OrderBy(t => t.Name)
            .ToList();

        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🔍 コンポーネント名で検索", EditorStyles.boldLabel);
        string newSearch = EditorGUILayout.TextField("名前:", searchTerm);

        if (newSearch != searchTerm)
        {
            searchTerm = newSearch;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

        var filtered = componentTypes
            .Where(t => t.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            .Take(100); // 表示数制限（パフォーマンス対策）

        foreach (var type in filtered)
        {
            if (GUILayout.Button(type.FullName, EditorStyles.miniButton))
            {
                selectedType = type;
                RefreshMatches();
            }
        }

        EditorGUILayout.EndScrollView();

        if (selectedType != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"🎯 選択中: {selectedType.FullName}", EditorStyles.helpBox);
            EditorGUILayout.LabelField($"ヒット数: {matchedObjects.Count}");

            if (GUILayout.Button("🔄 再スキャン"))
            {
                RefreshMatches();
            }
        }
    }

    private static void RefreshMatches()
    {
        matchedObjects.Clear();
        if (selectedType == null) return;

        foreach (var go in GameObject.FindObjectsOfType<GameObject>())
        {
            if (go.GetComponent(selectedType))
                matchedObjects.Add(go);
        }

        SceneView.RepaintAll();
        EditorApplication.RepaintHierarchyWindow();
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        if (selectedType == null) return;

        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        // 自身または親がmatchedObjectsに含まれていれば色付け
        GameObject current = go;
        bool isMatchedOrParent = false;
        while (current != null)
        {
            if (matchedObjects.Contains(current))
            {
                isMatchedOrParent = true;
                break;
            }
            current = current.transform.parent ? current.transform.parent.gameObject : null;
        }

        if (isMatchedOrParent)
        {
            EditorGUI.DrawRect(selectionRect, new Color(0.4f, 0.6f, 1f, 0.3f)); // 青
        }
    }

    private static void OnSceneGUI(SceneView view)
    {
        if (selectedType == null) return;

        Handles.color = new Color(1f, 0.2f, 0.2f, 0.8f); // 赤

        foreach (var go in matchedObjects)
        {
            if (go == null) continue;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Handles.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
            }
            else
            {
                Handles.DrawWireCube(go.transform.position, Vector3.one);
            }
        }
    }
}
