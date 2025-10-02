using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ObjectsReset : UdonSharpBehaviour
{
    [Header("リセット対象のオブジェクト")]
    [SerializeField] private GameObject[] objectsToReset; // リセット対象のオブジェクトを格納する配列

    private Vector3[] initialPositions; // 各オブジェクトの初期位置を保存
    private Quaternion[] initialRotations; // 各オブジェクトの初期回転を保存

    // 例：1つのオブジェクトだけ同期する場合
    [UdonSynced] private Vector3 syncedPosition;
    [UdonSynced] private Quaternion syncedRotation;

    void Start()
    {
        // 有効なオブジェクトの数をカウント
        int validCount = 0;
        for (int i = 0; i < objectsToReset.Length; i++)
        {
            if (objectsToReset[i] != null && objectsToReset[i].GetComponent<VRC_Pickup>() != null)
            {
                validCount++;
            }
            else
            {
                string objectName = objectsToReset[i] != null ? objectsToReset[i].name : "null";
                Debug.LogWarning($"リセット対象から除外: {objectName}（VRC_Pickup がありません）");
            }
        }

        // 有効なオブジェクトだけを格納する配列を作成
        GameObject[] validObjects = new GameObject[validCount];
        initialPositions = new Vector3[validCount];
        initialRotations = new Quaternion[validCount];

        int index = 0;
        for (int i = 0; i < objectsToReset.Length; i++)
        {
            if (objectsToReset[i] != null && objectsToReset[i].GetComponent<VRC_Pickup>() != null)
            {
                validObjects[index] = objectsToReset[i];
                initialPositions[index] = objectsToReset[i].transform.position;
                initialRotations[index] = objectsToReset[i].transform.rotation;
                index++;
            }
        }

        // 有効なオブジェクトだけをリセット対象として設定
        objectsToReset = validObjects;
    }

    public void ResetObjects()
    {
        // オブジェクトを初期位置と回転に戻す
        for (int i = 0; i < objectsToReset.Length; i++)
        {
            if (objectsToReset[i] != null)
            {
                // オブジェクトの所有権を設定
                Networking.SetOwner(Networking.LocalPlayer, objectsToReset[i]);

                // 初期位置と回転を設定
                objectsToReset[i].transform.position = initialPositions[i];
                objectsToReset[i].transform.rotation = initialRotations[i];

                // 同期をリクエスト
                RequestSerialization();

                // 例：1つのオブジェクトだけ同期する場合
                syncedPosition = initialPositions[i];
                syncedRotation = initialRotations[i];
                RequestSerialization();
            }
        }
    }

    // OnDeserializationで反映
    public override void OnDeserialization()
    {
        objectsToReset[0].transform.position = syncedPosition;
        objectsToReset[0].transform.rotation = syncedRotation;
    }

    public override void Interact()
    {
        // インタラクト時にリセットを実行
        ResetObjects();
    }
}
