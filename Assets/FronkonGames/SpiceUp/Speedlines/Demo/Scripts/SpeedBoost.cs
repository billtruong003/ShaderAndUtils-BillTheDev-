using UnityEngine;

namespace FronkonGames.SpiceUp.Speedlines
{
  public enum SpeedlineEffect
  {
    _1,
    _2,
    _3,
    _4,
    _5,
    _6,
    _7,
  }

  /// <summary> Speed boost. </summary>
  /// <remarks>
  /// This code is designed for a simple demo, not for production environments.
  /// </remarks>
  [RequireComponent(typeof(BoxCollider))]
  public sealed class SpeedBoost : MonoBehaviour
  {
    [SerializeField]
    private SpeedlineEffect effect = SpeedlineEffect._1;

    [SerializeField, Range(0.0f, MaxBoost)]
    private float speedBoost = 15.0f;
    
    public const float MaxBoost = 15.0f;

    private void Awake() => this.gameObject.GetComponent<BoxCollider>().isTrigger = true;
    
    void OnTriggerExit(Collider collider)
    {
      if (collider.gameObject.tag == "Player")
        collider.gameObject.GetComponent<Player>().Impulse(speedBoost, effect);
    }
  }
}