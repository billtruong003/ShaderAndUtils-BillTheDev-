//#define VIDEO_MODE
using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FronkonGames.SpiceUp.Speedlines
{
#if UNITY_EDITOR
  [CustomEditor(typeof(Demo))]
  public class DemoWarning : UnityEditor.Editor
  {
    private GUIStyle Style => style ??= new GUIStyle(GUI.skin.GetStyle("HelpBox")) { richText = true, fontSize = 14, alignment = TextAnchor.MiddleCenter };
    private GUIStyle style;
    public override void OnInspectorGUI()
    {
      EditorGUILayout.TextArea($"\nThis code is only for the demo\n\n<b>DO NOT USE</b> it in your projects\n\nIf you have any questions,\ncheck the <a href='{Constants.Support.Documentation}'>online help</a> or use the <a href='mailto:{Constants.Support.Email}'>support email</a>,\n<b>thanks!</b>\n", Style);
      this.DrawDefaultInspector();
    }
  }
#endif

  /// <summary> Spice Up: Speed lines demo. </summary>
  /// <remarks>
  /// This code is designed for a simple demo, not for production environments.
  /// </remarks>
  [RequireComponent(typeof(AudioSource))]
  public class Demo : MonoBehaviour
  {
    [Header("Gameplay")]

    [SerializeField]
    private Player player;

    [Header("UI")]

    [SerializeField]
    private CanvasGroup fade;

    [SerializeField]
    private CanvasGroup logo;

    [SerializeField]
    private CanvasGroup start;

    [SerializeField]
    private CanvasGroup credits;

    [SerializeField]
    private CanvasGroup timer;

    [SerializeField]
    private Text chrono;

    [SerializeField]
    private CanvasGroup end;

    [Header("Audio")]

    [SerializeField, Range(0.0f, 1.0f)]
    private float volume = 0.5f;

    [SerializeField]
    private AudioClip music;

    [SerializeField]
    private AudioClip beep;

    [Header("Internal")]

    private Speedlines.Settings settings;

    private AudioSource audioSource;

    private float time;

    private void Countdown()
    {
      Sequence.New()
        .Then(1.0f, () => timer.gameObject.SetActive(true), progress => timer.alpha = progress, () => audioSource.PlayOneShot(beep, 0.5f))
        .Wait(0.75f)
        .Then(0.25f, null, progress => chrono.color = Color.Lerp(Color.red, Color.yellow, progress), () => audioSource.PlayOneShot(beep, 0.75f))
        .Wait(0.75f)
        .Then(0.25f, null, progress => chrono.color = Color.Lerp(Color.yellow, Color.green, progress), () => audioSource.PlayOneShot(beep, 1.0f))
        .Then(() =>
        {
          audioSource.Play();
          player.enabled = true;
        })
        .Wait(2.0f)
        .Then(1.0f, null, progress => chrono.color = Color.Lerp(Color.green, Color.white, progress));
    }

    private void EndDemo()
    {
      Sequence.New()
        .Then(() =>
        {
          player.enabled = false;
          settings.strength = 0.0f;
        })
        .Then(2.0f, () => fade.gameObject.SetActive(true),
          progress => fade.alpha = progress)
        .Then(1.0f,
          () =>
          {
            end.gameObject.GetComponentInChildren<Text>().text =
#if VIDEO_MODE
              "Thank you for watching this video!\n\nFollow me at <color=orange>@FronkonGames</color>";
#else
              $"You have completed the demo in only <color=orange>{SecondsToHuman(time)}</color>!";
#endif
            end.gameObject.SetActive(true);
          }, progress => end.alpha = progress)
        .Wait(5.0f)
        .Then(1.0f, null, progress =>
        {
          end.alpha = 1.0f - progress;
          audioSource.volume = volume * (1.0f - progress);
        })
#if VIDEO_MODE
        .Wait(0.5f)
        .Then(() => UnityEditor.EditorApplication.isPlaying = false);
#else
        .Then(2.0f,
          () =>
          {
            audioSource.Stop();
            settings.ResetDefaultValues();
            time = 0.0f;
            timer.gameObject.SetActive(true);
            timer.alpha = 0.0f;
            chrono.text = "00:00:00";
            chrono.color = Color.red;
            player.ReturnToStart();
          },
          progress => fade.alpha = 1.0f - progress,
          () =>
          {
            audioSource.volume = volume;
            fade.gameObject.SetActive(false);
            Countdown();
          });
#endif
    }

    private string SecondsToHuman(float seconds) => TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\:ff");

    private void OnTriggerEnter(Collider collider)
    {
      if (collider.gameObject.tag == "Player")
        EndDemo();
    }

    private void Awake()
    {
      if (Speedlines.IsInRenderFeatures() == false)
      {
        Debug.LogWarning($"Effect '{Constants.Asset.Name}' not found. You must add it as a Render Feature.");
#if UNITY_EDITOR
        if (EditorUtility.DisplayDialog($"Effect '{Constants.Asset.Name}' not found", $"You must add '{Constants.Asset.Name}' as a Render Feature.", "Quit") == true)
          EditorApplication.isPlaying = false;
#endif
      }

      this.enabled = Speedlines.IsInRenderFeatures();
    }

    private void Start()
    {
      settings = Speedlines.GetSettings();

      audioSource = this.gameObject.GetComponent<AudioSource>();
      if (music != null)
      {
        audioSource.clip = music;
        audioSource.volume = volume;
        audioSource.loop = true;
      }

      Random.InitState((int)DateTime.Now.Ticks);

      player.enabled = false;

      Sequence.New()
        .Then(2.0f, () =>
          {
            fade.alpha = 1.0f;
            fade.gameObject.SetActive(true);
            timer.alpha = 0.0f;
            chrono.text = "00:00:00";
            chrono.color = Color.red;
          },
          progress => fade.alpha = 1.0f - progress,
          () => fade.gameObject.SetActive(false))
        .Then(1.0f, () => logo.gameObject.SetActive(true), progress => logo.alpha = progress)
        .Wait(2.0f)
        .Then(1.0f, null, progress => logo.alpha = 1.0f - progress, () => logo.gameObject.SetActive(false))
        .Wait(1.0f)
#if !VIDEO_MODE
        .Then(1.0f,
          () =>
          {
            credits.gameObject.SetActive(true);
            start.gameObject.SetActive(true);
            start.alpha = credits.alpha = 0.0f;
          },
          progress => credits.alpha = start.alpha = progress)
        .Wait(4.0f)
        .Then(1.0f, null,
          progress => credits.alpha = start.alpha = 1.0f - progress,
          () =>
          {
            credits.gameObject.SetActive(false);
            start.gameObject.SetActive(false);
          })
#endif
        ;
        Countdown();
    }

    private void Update()
    {
      if (player.enabled == true)
      {
        time += Time.deltaTime;
        chrono.text = SecondsToHuman(time);
      }

#if UNITY_EDITOR && VIDEO_MODE
      if (Input.GetKey(KeyCode.Escape) == true)
        EndDemo();
#endif
    }

    private void OnDestroy()
    {
      settings?.ResetDefaultValues();
    }
  }
}
