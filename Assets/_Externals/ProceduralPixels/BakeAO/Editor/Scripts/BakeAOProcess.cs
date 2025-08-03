/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class BakeAOProcess
	{
		public bool IsBaking => stateMachine.HasActiveState;
		public bool IsPostprocessing => stateMachine.ActiveState is PostprocessorState;
		public bool IsComplete => !stateMachine.HasActiveState;

		public long EstimatedMemorySize => bakingData == null ? 0 : bakingData.GetEstimatedMemorySize();

		public BakingSetup bakingSetup;

		public BakingData UvBuffer => bakingData;
		private BakingData bakingData;

		//States
		private BakeAOStateMachine stateMachine = new BakeAOStateMachine();

		private WaitingState waitingState = new WaitingState();
		private PrepareRayGenerationDataState prepareRayGenerationDataState = new PrepareRayGenerationDataState();
		private TracingProgramState tracingProgramState = new TracingProgramState();
		private PostprocessorState postprocessorState = new PostprocessorState();

		private BakePostprocessor postprocessor;

		public UnityEngine.Object Context { get; set; }
		public object Result => postprocessor == null ? null : postprocessor.Result;

		private float lastProgress = 0.0f;

		public float Progress
		{
			get
			{
				if (stateMachine != null)
				{
					if (stateMachine.HasActiveState)
                    {
                        if (stateMachine.ActiveState is PrepareRayGenerationDataState)
							lastProgress = Mathf.Max(lastProgress, Mathf.Lerp(0.0f, 0.1f, stateMachine.ActiveState.Progress));
						else if (stateMachine.ActiveState is TracingProgramState)
							lastProgress = Mathf.Max(lastProgress, Mathf.Lerp(0.1f, 1.0f, stateMachine.ActiveState.Progress));
                    }

					return lastProgress;
                }
				else
					return 0.0f;
			}
		}

		public string ActiveOperationDescription
		{
			get
			{
				if (stateMachine != null)
				{
					if (stateMachine.HasActiveState)
						return stateMachine.ActiveState.StateDescription;
					else
						return null;
				}
				else
					return null;
			}
		}

		public float ActiveOperationProgress
		{
			get
			{
				if (stateMachine != null)
				{
					if (stateMachine.HasActiveState)
						return stateMachine.ActiveState.Progress;
					else
						return 1.0f;
				}
				else
					return 1.0f;
			}
		}

		public float? LastBakeTimeInMs => lastBakeTimeInMs;
		private float? lastBakeTimeInMs = null;

		private BakeAOProcess() { }

		public static BakeAOProcess Create()
		{
			var process = new BakeAOProcess();
			return process;
		}

		public void Destroy()
		{
		}

		public void OnUpdate()
		{
			if (stateMachine != null)
			{
				if (stateMachine.HasActiveState)
				{
					stateMachine.Update();
				}
			}
		}

		public void Bake(BakingSetup bakingSetup, BakePostprocessor postprocessor)
		{
			if (IsBaking)
				Debug.LogWarning("Bake AO Process stops baking to start the new one! Make sure that the process is not in baking state to not lose the baking progress.");

			Stop();

			lastBakeTimeInMs = null;

			this.bakingSetup = bakingSetup;
			this.postprocessor = postprocessor;

			if (bakingData != null)
				bakingData.Release();

			bakingData = new BakingData(bakingSetup.quality.TextureSize);

			waitingState.nextState = prepareRayGenerationDataState;
			prepareRayGenerationDataState.nextState = tracingProgramState;
			tracingProgramState.nextState = postprocessorState;
			postprocessorState.nextState = null;

			prepareRayGenerationDataState.Setup(bakingData, bakingSetup);
			tracingProgramState.Setup(bakingData, bakingSetup);
			postprocessorState.Setup(bakingData, bakingSetup, postprocessor);

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			stateMachine.ChangeState(waitingState);
			stateMachine.OnStateChanged += OnBakeEnd;

			void OnBakeEnd(BakeAOState state)
			{
				if (state == null)
				{
					stateMachine.OnStateChanged -= OnBakeEnd;
					lastBakeTimeInMs = stopwatch.ElapsedMilliseconds;
					bakingData.Release();
				}
			}
		}

		public void Stop()
		{
			if (IsBaking)
            {
                stateMachine.ChangeState(null);
            }
        }
	}
}