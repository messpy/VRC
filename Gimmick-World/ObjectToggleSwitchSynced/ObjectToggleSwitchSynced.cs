using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// ファイル名: ObjectToggleSwitchSynced.cs
// クラス名:  ObjectToggleSwitchSynced（ファイル名と完全一致）
public class ObjectToggleSwitchSynced : UdonSharpBehaviour
{
    [Header("初期状態（オーナーが決めて同期）")]
    public bool initialOn = true;

    [Header("ON/OFFするGameObject（複数OK）")]
    public GameObject[] toggleObjects;

    [Header("enabled切替（Udonで触れるものだけ）")]
    public Renderer[] toggleRenderers;
    public Collider[] toggleColliders;
    public Light[] toggleLights;

    [Header("Material切替（Rendererを指定）")]
    public Renderer[] materialTargets;
    public Material onMaterial;
    public Material offMaterial;

    [Header("Animator（boolパラメータをON/OFFに合わせて切替）")]
    public Animator[] animators;
    public string animatorBoolParam = "On";

    [Header("ON/OFF時にUdon CustomEventを呼ぶ（任意）")]
    public UdonBehaviour[] onEventTargets;
    public string onEventName = "OnSwitchOn";

    public UdonBehaviour[] offEventTargets;
    public string offEventName = "OnSwitchOff";

    [UdonSynced] private bool syncedOn;

    private bool initialized;

    private void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            syncedOn = initialOn;
            RequestSerialization();
        }

        ApplyState(syncedOn);
        initialized = true;
    }

    public override void Interact()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        syncedOn = !syncedOn;
        RequestSerialization();

        ApplyState(syncedOn);
    }

    public override void OnDeserialization()
    {
        if (!initialized) return;
        ApplyState(syncedOn);
    }

    private void ApplyState(bool on)
    {
        // GameObject active
        if (toggleObjects != null)
        {
            for (int i = 0; i < toggleObjects.Length; i++)
            {
                GameObject obj = toggleObjects[i];
                if (obj != null) obj.SetActive(on);
            }
        }

        // Renderer.enabled
        if (toggleRenderers != null)
        {
            for (int i = 0; i < toggleRenderers.Length; i++)
            {
                Renderer r = toggleRenderers[i];
                if (r != null) r.enabled = on;
            }
        }

        // Collider.enabled
        if (toggleColliders != null)
        {
            for (int i = 0; i < toggleColliders.Length; i++)
            {
                Collider c = toggleColliders[i];
                if (c != null) c.enabled = on;
            }
        }

        // Light.enabled
        if (toggleLights != null)
        {
            for (int i = 0; i < toggleLights.Length; i++)
            {
                Light l = toggleLights[i];
                if (l != null) l.enabled = on;
            }
        }

        // Material swap
        if (materialTargets != null)
        {
            Material m = on ? onMaterial : offMaterial;
            if (m != null)
            {
                for (int i = 0; i < materialTargets.Length; i++)
                {
                    Renderer r = materialTargets[i];
                    if (r != null) r.sharedMaterial = m;
                }
            }
        }

        // Animator bool param
        if (animators != null && !string.IsNullOrEmpty(animatorBoolParam))
        {
            for (int i = 0; i < animators.Length; i++)
            {
                Animator a = animators[i];
                if (a != null) a.SetBool(animatorBoolParam, on);
            }
        }

        // Udon Custom Events
        if (on) CallEvents(onEventTargets, onEventName);
        else    CallEvents(offEventTargets, offEventName);
    }

    private void CallEvents(UdonBehaviour[] targets, string eventName)
    {
        if (targets == null) return;
        if (string.IsNullOrEmpty(eventName)) return;

        for (int i = 0; i < targets.Length; i++)
        {
            UdonBehaviour ub = targets[i];
            if (ub != null) ub.SendCustomEvent(eventName);
        }
    }
}
