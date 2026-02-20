using UnityEngine;
using System.IO;
using System.Text;

public class ExportObjectInfo : MonoBehaviour
{
    void Start()
    {
        // 現在の時間を取得してフォーマット
        string timestamp = System.DateTime.Now.ToString("MMdd_HHmm");

        // ファイル名をアタッチしたオブジェクトの名前に現在の時間を追加して設定
        string fileName = $"{gameObject.name}_{timestamp}.txt";

        // ファイルパスを設定
        string filePath = Application.dataPath + "/" + fileName;

        // StreamWriterを使ってテキストファイルに書き出す
        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            // オブジェクト構成の見出しを書き出す
            writer.WriteLine("オブジェクト構成:");

            // オブジェクト構成を書き出す
            WriteObjectStructure(writer, gameObject.transform, 0);

            // 各オブジェクトの詳細情報を書き出す
            writer.WriteLine("\n詳細情報:");
            WriteObjectDetails(writer, gameObject.transform, 0);
        }

        Debug.Log("Object information has been written to: " + filePath);
    }

    void WriteObjectStructure(StreamWriter writer, Transform parent, int indentLevel)
    {
        // インデントを設定
        string indent = new string('-', indentLevel * 2);

        // 現在のオブジェクトを書き出す
        writer.WriteLine(indent + " " + parent.name);

        // 子オブジェクトの情報を書き出す
        foreach (Transform child in parent)
        {
            WriteObjectStructure(writer, child, indentLevel + 1);
        }
    }

    void WriteObjectDetails(StreamWriter writer, Transform parent, int level)
    {
        string indent = new string('-', level * 2);
        writer.WriteLine("\n" + indent + " " + parent.name + " " + indent);

        // オブジェクトのすべてのコンポーネントを取得して書き出す
        Component[] components = parent.GetComponents<Component>();
        foreach (Component comp in components)
        {
            writer.WriteLine("・" + comp.GetType().Name);
            WriteComponentDetails(writer, comp);
        }

        // 親オブジェクトの取得
        if (parent.parent != null)
        {
            writer.WriteLine("Parent object of this GameObject: " + parent.parent.name);
        }
        else
        {
            writer.WriteLine("This GameObject has no parent.");
        }

        // 子オブジェクトの情報も再帰的に書き出す
        foreach (Transform child in parent)
        {
            WriteObjectDetails(writer, child, level + 1);
        }
    }

    void WriteComponentDetails(StreamWriter writer, Component comp)
    {
        foreach (var field in comp.GetType().GetFields())
        {
            writer.WriteLine("  " + field.Name + ": " + field.GetValue(comp));
        }
        foreach (var property in comp.GetType().GetProperties())
        {
            // 読み取り可能なプロパティとインデクサを除外
            if (property.CanRead && property.GetIndexParameters().Length == 0)
            {
                object value;
                try
                {
                    value = property.GetValue(comp, null);
                }
                catch
                {
                    value = "N/A";
                }
                writer.WriteLine("  " + property.Name + ": " + value);
            }
        }
    }
}
