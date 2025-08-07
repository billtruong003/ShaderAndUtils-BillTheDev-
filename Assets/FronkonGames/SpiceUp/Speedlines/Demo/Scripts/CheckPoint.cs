using UnityEngine;

namespace FronkonGames.SpiceUp.Speedlines
{
  /// <summary> Checkpoint. </summary>
  /// <remarks>
  /// This code is designed for a simple demo, not for production environments.
  /// </remarks>
  [RequireComponent(typeof(BoxCollider))]
  public sealed class CheckPoint : MonoBehaviour
  {
    public static Vector3 LastPosition { get; private set; }
    public static Quaternion LastRotation { get; private set; }

    private void Awake() => this.gameObject.GetComponent<BoxCollider>().isTrigger = true;

    private void OnTriggerEnter(Collider collider)
    {
      if (collider.gameObject.tag == "Player")
      {
        LastPosition = collider.gameObject.transform.position;
        LastRotation = this.gameObject.transform.rotation;
      }
    }
  }
}