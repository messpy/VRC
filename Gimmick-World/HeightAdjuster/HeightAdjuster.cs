using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HeightAdjuster : UdonSharpBehaviour
{
    [Header("座る位置（空のTransform）")]
    [SerializeField] private Transform entryPoint;

    [Header("最大上下補正（安全のため0.5以内推奨）")]
    [SerializeField] private float maxOffset = 0.5f;

    [Header("Headの高さに応じた目標Y高さ")]
    [Tooltip("headRanges.Length + 1 == targetHeights.Length にしてください")]
    [SerializeField] private float[] headRanges = { 1.0f, 1.5f, 2.0f };

    [SerializeField] private float[] targetHeights = { 1.3f, 1.6f, 1.9f, 2.0f };

    private Vector3 initialPos;

    void Start()
    {
        if (entryPoint != null)
        {
            initialPos = entryPoint.position;
        }
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (!player.isLocal || entryPoint == null) return;

        Vector3 headPos = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        float currentY = headPos.y;

        float target = GetTargetHeight(currentY);
        float offset = Mathf.Clamp(target - currentY, -maxOffset, maxOffset);

        Vector3 newPos = initialPos;
        newPos.y += offset;
        entryPoint.position = newPos;
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player.isLocal && entryPoint != null)
        {
            entryPoint.position = initialPos;
        }
    }

    private float GetTargetHeight(float headY)
    {
        int len = headRanges.Length;
        for (int i = 0; i < len; i++)
        {
            if (headY < headRanges[i])
            {
                return targetHeights[i];
            }
        }
        return targetHeights[targetHeights.Length - 1];
    }
}
