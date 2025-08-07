using UnityEngine;
using Random = UnityEngine.Random;

namespace FronkonGames.SpiceUp.Speedlines
{
  /// <summary> Simple player controller. </summary>
  /// <remarks>
  /// This code is designed for a simple demo, not for production environments.
  /// </remarks>
  [RequireComponent(typeof(CharacterController))]
  [RequireComponent(typeof(AudioSource))]
  public sealed class Player : MonoBehaviour
  {
    [Space, Header("Gameplay")]

    [SerializeField, Range(0.0f, 50.0f)]
    private float respawnDeep;
    
    [Space, Header("Camera")]

    [SerializeField]
    private Camera head;

    [SerializeField]
    private float sensitivityX = 1.0f;

    [SerializeField]
    private float sensitivityY = 1.0f;

    [Space, Header("Movement")]

    [SerializeField]
    private float walkSpeed = 6.0f;

    [SerializeField]
    private float runSpeed = 11.0f;

    [SerializeField]
    private float jumpSpeed = 8.0f;

    [SerializeField]
    private float impulseStrength = 2.0f;

    [SerializeField]
    private float impulseLost = 1.0f;

    [SerializeField]
    private float gravity = 20.0f; 

    [Space, Header("Input")]

    [SerializeField]
    private string horizontalInput = "Horizontal";

    [SerializeField]
    private string verticalInput = "Vertical";

    [SerializeField]
    private string horizontalLookAt = "Mouse X";

    [SerializeField]
    private string verticalLookAt = "Mouse Y";

    [Space, Header("Audio")]

    [SerializeField, Range(0.0f, 1.0f)]
    private float volume = 0.5f;

    [SerializeField]
    private AudioClip stepsClip;

    [SerializeField]
    private AudioClip impulseClip;

    private AudioSource audioSource;
    private AudioSource audioSourceImpulse;

    private CharacterController characterController;

    private float inputX, inputY;
    private float speed;
    private bool crouch;
    private int jumpTimer;

    private float impulse = 0.0f;

    private readonly float antiBumpFactor = 0.75f;
    private readonly int antiBunnyHopFactor = 1;

    private Vector3 moveDirection = Vector3.zero;
    private bool grounded = false;
    private Vector3 startPosition, lastPosition;
    private Quaternion startRotation;

    public void ReturnToStart()
    {
      impulse = 0.0f;
      this.gameObject.transform.position = startPosition;
      this.gameObject.transform.rotation = startRotation;
    }

    public void Impulse(float impulse, SpeedlineEffect effect)
    {
      this.impulse += impulse;
      audioSourceImpulse.Play();

      Speedlines.Settings settings = Speedlines.GetSettings();
      switch (effect)
      {
        case SpeedlineEffect._1:
          settings.ResetDefaultValues();
          break;
        case SpeedlineEffect._2:
          settings.ResetDefaultValues();
          settings.colorBlend = ColorBlends.Burn;
          settings.colorBorder = settings.colorCenter = Color.red;
          break;
        case SpeedlineEffect._3:
          settings.ResetDefaultValues();
          settings.colorOffset = 0.8f;
          settings.colorDefinition = 5.0f;
          settings.colorBorder = Color.red;
          settings.colorCenter = Color.yellow;
          break;
        case SpeedlineEffect._4:
          settings.ResetDefaultValues();
          settings.colorBrightness = 10.0f;
          settings.length = 2.5f;
          settings.frequency = 3.0f;
          settings.softness = 0.25f;
          settings.colorBorder = Color.red;
          settings.colorCenter = Color.green;
          settings.colorBlend = ColorBlends.Divide;
          break;
        case SpeedlineEffect._5:
          settings.ResetDefaultValues();
          settings.radius = 0.85f;
          settings.length = 0.0f;
          settings.speed = 3.5f;
          settings.frequency = 15.0f;
          settings.softness = 0.5f;
          settings.aspect = true;
          settings.colorBorder = Color.magenta;
          settings.colorCenter = Color.cyan;
          settings.colorBlend = ColorBlends.Screen;
          break;
        case SpeedlineEffect._6:
          settings.ResetDefaultValues();
          settings.radius = 1.0f;
          settings.length = 2.0f;
          settings.speed = 1.5f;
          settings.frequency = 0.0f;
          settings.softness = 0.15f;
          settings.noise = 1.0f;
          settings.aspect = true;
          settings.colorBorder = Color.yellow;
          settings.colorCenter = Color.green;
          settings.colorBlend = ColorBlends.Overlay;
          break;
        case SpeedlineEffect._7:
          settings.ResetDefaultValues();
          settings.colorBrightness = 5.0f;
          settings.radius = 2.0f;
          settings.length = 4.0f;
          settings.speed = 1.0f;
          settings.frequency = 8.5f;
          settings.softness = 0.15f;
          settings.noise = 0.3f;
          settings.aspect = true;
          settings.colorBorder = settings.colorCenter = Color.red;
          settings.colorBlend = ColorBlends.Burn;
          break;
      }
    }

    private void ControllerUpdate()
    {
      lastPosition = this.transform.position;

      inputX = Input.GetAxis(horizontalInput);
      inputY = Input.GetAxis(verticalInput);
      float inputModifyFactor = inputX != 0.0f && inputY != 0.0f ? 0.7071f : 1.0f;

      if (grounded)
      {
        speed = Input.GetKey(KeyCode.LeftShift) == true ? runSpeed : walkSpeed;
        crouch = Input.GetKey(KeyCode.C);

        if (crouch == true)
        {
          characterController.height = 1.0f;
          characterController.center = Vector3.up * 0.5f;
        }
        else
        {
          characterController.height = 2.0f;
          characterController.center = Vector3.up;
        }

        moveDirection = new Vector3(inputX * inputModifyFactor, -antiBumpFactor, inputY * inputModifyFactor);
        moveDirection = this.transform.TransformDirection(moveDirection) * speed;

        if (Input.GetKeyDown(KeyCode.Space) == false)
          jumpTimer++;
        else if (jumpTimer >= antiBunnyHopFactor)
        {
          moveDirection.y = jumpSpeed;
          jumpTimer = 0;
        }
      }
      else
      {
          moveDirection.x = inputX * speed * inputModifyFactor;
          moveDirection.z = inputY * speed * inputModifyFactor;
          moveDirection = this.transform.TransformDirection(moveDirection);
      }

      moveDirection.y -= gravity * Time.deltaTime;
      grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    private void CameraUpdate()
    {
      Vector3 characterEuler = transform.eulerAngles;
      Vector3 cameraEuler = head.transform.localEulerAngles;

      characterEuler.y += Input.GetAxis(horizontalLookAt) * sensitivityX;
      transform.eulerAngles = characterEuler;
      cameraEuler.x -= Input.GetAxis(verticalLookAt) * sensitivityY;
      cameraEuler = NormalizeAngle(cameraEuler);
      cameraEuler.x = Mathf.Clamp(cameraEuler.x, -45.0f, 45.0f);
      head.transform.localEulerAngles = cameraEuler;
    }

    private void AudioUpdate()
    {
      if ((this.transform.position - lastPosition).magnitude >= 0.005f && characterController.isGrounded == true)
      {
        if (audioSource.isPlaying == false)
        {
          audioSource.pitch = Random.Range(0.9f, 1.1f);
          audioSource.Play();
        }

        lastPosition = this.transform.position;
      }
      else
        audioSource.Stop();
    }

    private void RespawnUpdate()
    {
      if (respawnDeep > 0.0f && this.gameObject.transform.position.y < 0.0f)
      {
        impulse -= Time.deltaTime * impulseLost * 5.0f;

        if (this.gameObject.transform.position.y <= -respawnDeep)
        {
          impulse = 0.0f;
          this.gameObject.transform.position = CheckPoint.LastPosition;
          this.gameObject.transform.rotation = CheckPoint.LastRotation;
        }
      }
    }

    private void ImpulseUpdate()
    {
      Speedlines.GetSettings().strength = Mathf.Clamp01((impulse * 1.25f) / SpeedBoost.MaxBoost);
      audioSourceImpulse.volume = volume * Speedlines.GetSettings().strength;

      if (impulse > 0.0f)
      {
        characterController.Move(characterController.velocity.normalized * impulse * impulseStrength * Time.deltaTime);
        impulse -= Time.deltaTime * impulseLost;
      }
    }

    private void ConfigAudio()
    {
      audioSource = this.gameObject.GetComponent<AudioSource>();
      audioSource.clip = stepsClip;
      audioSource.loop = true;
      audioSource.volume = volume;
      audioSource.spatialBlend = 1;
      audioSource.playOnAwake = false;

      audioSourceImpulse = this.gameObject.AddComponent<AudioSource>();
      audioSourceImpulse.clip = impulseClip;
      audioSourceImpulse.volume = volume;
    }

    private Vector3 NormalizeAngle(Vector3 eulerAngle)
    {
      var delta = eulerAngle;

      if (delta.x > 180.0f)
        delta.x -= 360.0f;
      else if (delta.x < -180.0f)
        delta.x += 360.0f;

      if (delta.y > 180.0f)
        delta.y -= 360.0f;
      else if (delta.y < -180.0f)
        delta.y += 360.0f;

      if (delta.z > 180.0f)
        delta.z -= 360.0f;
      else if (delta.z < -180.0f)
        delta.z += 360.0f;

      return new Vector3(delta.x, delta.y, delta.z);
    }

    private void Awake()
    {
      characterController = this.gameObject.GetComponent<CharacterController>();
      startPosition = lastPosition = this.transform.position;
      startRotation = this.transform.rotation;

      speed = walkSpeed;
      jumpTimer = antiBunnyHopFactor;

      ConfigAudio();

      this.enabled = characterController != null && head != null && Speedlines.IsInRenderFeatures();
    }

    private void OnEnable() => Cursor.lockState = CursorLockMode.Locked;

    private void OnDisable()
    {
      audioSourceImpulse.Stop();
      audioSource.Stop();

      Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
      ControllerUpdate();

      CameraUpdate();

      ImpulseUpdate();

      RespawnUpdate();
    }

    private void FixedUpdate() => AudioUpdate();
  }
}