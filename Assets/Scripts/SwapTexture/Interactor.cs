using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField]
    float radius;

    void Update()
    {
        Shader.SetGlobalVector("_Position", transform.position);
        Shader.SetGlobalFloat("_Radius", radius);
    }
}