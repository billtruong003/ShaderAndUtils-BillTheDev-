using UnityEngine;

public class VATFrameCounter : MonoBehaviour
{
    public Material material;

    void Update()
    {
        float time = (Time.time % material.GetFloat("_VAT_AnimationLength"));
        material.SetFloat("_AnimTime", time);
    }
}