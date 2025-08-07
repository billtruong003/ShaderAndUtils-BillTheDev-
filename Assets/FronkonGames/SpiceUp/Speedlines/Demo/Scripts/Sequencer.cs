using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FronkonGames.SpiceUp.Speedlines
{
  /// <summary> Sequence of actions. </summary>
  /// <remarks> This code is designed for a simple demo, not for production environments. </remarks>
  public sealed class Sequence
  {
    /// <summary> Sequence status. </summary>
    public enum States
    {
      /// <summary> Ready to be executed. </summary>
      Ready,
      
      /// <summary> Running. </summary>
      Running,
      
      /// <summary> Sequence completed. </summary>
      Done
    }

    /// <summary> Current status of the sequence. </summary>
    public States State { get; private set; } = States.Ready;

    /// <summary> Step within a task. </summary>
    private class Step
    {
      /// <summary> Duration in seconds. </summary>
      public float Duration { get; }
      
      /// <summary> Action that is executed at the start of the step. </summary>
      public Action OnStart { get; }
      
      /// <summary> Action to be executed for the duration of step [0, 1]. </summary>
      public Action<float> OnProgress { get; }
      
      /// <summary> Action to be executed at the end of the step. </summary>
      public Action OnEnd { get; }

      /// <summary> Condition being evaluated. </summary>
      public Func<bool> ConditionWhile { get; }
      
      /// <summary> Action to be executed during standby [0, duration]. </summary>
      public Action<float> Waiting { get; }

      /// <summary> New step. </summary>
      /// <param name="duration">Duration in seconds [>0, ...].</param>
      /// <param name="start">Action to be performed at the beginning of the step.</param>
      /// <param name="progress">Action to be performed during step [0, 1].</param>
      /// <param name="end">Action to be performed at the end of the step.</param>
      public Step(float duration, Action start = null, Action<float> progress = null, Action end = null)
      {
        Duration = Mathf.Max(0.0f, duration);
        OnStart = start;
        OnProgress = progress;
        OnEnd = end;
      }

      /// <summary> New step. </summary>
      /// <param name="conditionWhile">Condition to evaluate. As long as it is true, the step will be executed.</param>
      /// <param name="start">Action to be performed at the beginning of the step.</param>
      /// <param name="waiting">Action to be performed during the step (seconds).</param>
      /// <param name="end">Action to be performed at the end of the step.</param>
      public Step(Func<bool> conditionWhile, Action start = null, Action<float> waiting = null, Action end = null)
      {
        ConditionWhile = conditionWhile;
        OnStart = start;
        Waiting = waiting;
        OnEnd = end;
      }
      
      /// <summary> New step. </summary>
      /// <param name="conditionWhile">Condition to evaluate. As long as it is true, the step will be executed.</param>
      /// <param name="waiting">Action to be performed during the step (seconds).</param>
      public Step(Func<bool> conditionWhile, Action<float> waiting = null)
      {
        ConditionWhile = conditionWhile;
        Waiting = waiting;
      }
    }

    private readonly List<Step> steps = new();

    private static readonly WaitForEndOfFrame WaitForEndOfFrame = new();

    private Sequence() {}

    /// <summary> New sequence to be executed. </summary>
    /// <returns>A sequence.</returns>
    public static Sequence New()
    {
      Sequence sequence = new();
      
      Sequencer.Add(sequence);

      return sequence;
    }
    
    /// <summary> Add a step of waiting a few seconds. </summary>
    /// <param name="seconds">Seconds to wait [>0, ...].</param>
    /// <returns>The sequence.</returns>
    public Sequence Wait(float seconds)
    {
      steps.Add(new Step(seconds));
      return this;
    }
    
    /// <summary> Adds a step that evaluates waiting as long as a condition is true. </summary>
    /// <param name="conditionWhile">Condition to be evaluated.</param>
    /// <param name="start">Action to be performed at the beginning of the step.</param>
    /// <param name="waiting">Action to be performed during the step (seconds).</param>
    /// <param name="end">Action to be performed at the end of the step.</param>
    /// <returns>The sequence.</returns>
    public Sequence While(Func<bool> conditionWhile, Action start = null, Action<float> waiting = null, Action end = null)
    {
      steps.Add(new Step(conditionWhile, start, waiting, end));
      return this;
    }
    
    /// <summary> Adds a step that evaluates waiting as long as a condition is true. </summary>
    /// <param name="conditionWhile">Condition to be evaluated.</param>
    /// <param name="waiting">Action to be performed during the step (seconds).</param>
    /// <returns>The sequence.</returns>
    public Sequence While(Func<bool> conditionWhile, Action<float> waiting = null)
    {
      steps.Add(new Step(conditionWhile, waiting));
      return this;
    }
    
    /// <summary> Execute an action. </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns>The sequence.</returns>
    public Sequence Then(Action action)
    {
      steps.Add(new Step(0.0f, action));
      return this;
    }

    /// <summary> Execute an action. </summary>
    /// <param name="duration">Duration in seconds [>0, ...].</param>
    /// <param name="start">Action to be performed at the beginning of the step.</param>
    /// <param name="progress">Action to be performed during step [0, 1].</param>
    /// <param name="end">Action to be performed at the end of the step.</param>
    /// <returns>The sequence.</returns>
    public Sequence Then(float duration, Action start = null, Action<float> progress = null, Action end = null)
    {
      steps.Add(new Step(Mathf.Max(0.0f, duration), start, progress, end));
      return this;
    }

    /// <summary> Update steps. Internal use. </summary>
    /// <returns>IEnumerator.</returns>
    internal IEnumerator Update()
    {
      State = States.Running;
      
      for (int i = 0; i < steps.Count; ++i)
      {
        steps[i].OnStart?.Invoke();
        
        float time = 0.0f;
        if (steps[i].ConditionWhile != null)
        {
          while (steps[i].ConditionWhile.Invoke() == true)
          {
            steps[i].Waiting?.Invoke(time);
            time += Time.deltaTime;

            yield return WaitForEndOfFrame;
          }
        }
        else if (steps[i].Duration > 0.0f)
        {
          float wait = steps[i].Duration;
          while (time <= wait)
          {
            steps[i].OnProgress?.Invoke(Mathf.Clamp01(time / wait));
            time += Time.deltaTime;
            
            yield return WaitForEndOfFrame;
          }

          if (time - wait > 0.0f)
            steps[i].OnProgress?.Invoke(1.0f);
        }

        steps[i].OnEnd?.Invoke();
      }

      State = States.Done;
    }
  }

  /// <summary> Sequencer. </summary>
  /// <remarks>
  /// This code is designed for a simple demo, not for production environments.
  /// </remarks>
  public sealed class Sequencer : MonoBehaviour
  {
    private static readonly List<Sequence> sequences = new();
    
    private static Coroutine coroutine;
    
    /// <summary> Adds a sequence. Internal use. </summary>
    /// <param name="sequence">A sequence.</param>
    internal static void Add(Sequence sequence) => sequences.Add(sequence);

    private static IEnumerator UpdateSequences()
    {
      while (true)
      {
        int i = 0;
        for (; i < sequences.Count; ++i)
        {
          if (sequences[i].State == Sequence.States.Ready)
            yield return sequences[i].Update();
        }

        i = sequences.Count - 1;
        for (; i >= 0; i--)
        {
          if (sequences[i].State == Sequence.States.Done)
            sequences.RemoveAt(i);
        }

        yield return null;
      }
    }

    private void Reset()
    {
      if (coroutine != null)
        StopCoroutine(coroutine);

      sequences.Clear();
    }

    private void OnDestroy() => Reset();

#if UNITY_EDITOR 
    private void OnValidate() => Reset();
#endif
    
    [RuntimeInitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
      GameObject gameObject = new("Sequencer") { hideFlags = HideFlags.HideInHierarchy };
      gameObject.AddComponent<Sequencer>().StartCoroutine(UpdateSequences());

      DontDestroyOnLoad(gameObject);
    }
  }
}