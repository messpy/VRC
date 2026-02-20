using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MaterialsChanger : UdonSharpBehaviour
{
    public Renderer[] targetRenderers;
    public Material[] materials;
    public float changeInterval = 2.0f;

    private int currentIndex = 0;
    private float timer = 0f;
    private int materialsCount = 0;

    void Start()
    {
        materialsCount = materials.Length;

        if (materialsCount > 0 && targetRenderers != null)
        {
            foreach (var renderer in targetRenderers)
            {
                if (renderer != null)
                    renderer.material = materials[0];
            }
        }
    }

    void Update()
    {
        if (materialsCount == 0 || targetRenderers == null) return;

        timer += Time.deltaTime;
        if (timer >= changeInterval)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % materialsCount;
            foreach (var renderer in targetRenderers)
            {
                if (renderer != null)
                    renderer.material = materials[currentIndex];
            }
        }
    }
}
