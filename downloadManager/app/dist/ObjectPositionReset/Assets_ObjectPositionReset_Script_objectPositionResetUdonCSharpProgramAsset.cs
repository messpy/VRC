using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class objectPositionResetUdonCSharpProgramAsset : UdonSharpBehaviour
{
    [SerializeField] GameObject[] targetObject;
    [SerializeField] AudioSource audio;
    Vector3[] defaultPosition;
    Quaternion[] defaultRotation;

    private void Start()
    {
        if (audio == null)
        {
            Debug.Log($"[<color=#4169e1>objectPositionReset</color>]" + " audio == null on root: " + transform.root.gameObject.name);
        }

        defaultPosition = new Vector3[targetObject.Length];
        defaultRotation = new Quaternion[targetObject.Length];
        for (int i=0;  i< targetObject.Length; i++)
        {
            if (targetObject[i] != null)
            {
                defaultPosition[i] = targetObject[i].transform.localPosition;
                defaultRotation[i] = targetObject[i].transform.localRotation;
            }
            else
            {
                Debug.Log($"[<color=#4169e1>objectPositionReset</color>]" + " : targetObject [ " + i + " ] が空です");
            }
        }
    }
    public override void Interact()
    {
        if (audio != null)
        {
            audio.Play();
        }
        for (int i = 0; i < targetObject.Length; i++)
        {
            if (targetObject[i] != null)
            {
                targetObject[i].transform.localPosition = defaultPosition[i];
                targetObject[i].transform.localRotation = defaultRotation[i];
            }
            else
            {
                Debug.Log($"[<color=#4169e1>objectPositionReset</color>]" + " : targetObject [ " + i + " ] が空です");
            }
        }
    }
}
