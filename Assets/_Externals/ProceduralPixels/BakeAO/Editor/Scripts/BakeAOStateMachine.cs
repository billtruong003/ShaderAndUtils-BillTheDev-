/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using UnityEditor;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class BakeAOStateMachine
	{
		public event Action<BakeAOState> OnStateChanged;

		public BakeAOState ActiveState => activeState;
		private BakeAOState activeState = null;

		public bool HasActiveState => activeState != null;

		public void ChangeState(BakeAOState nextState)
		{
			if (activeState != null)
			{
				activeState.End();
				activeState.OnTransitionToNextStateRequested -= ChangeState;
			}

			if (nextState != null)
			{
				nextState.Start();
				nextState.OnTransitionToNextStateRequested += ChangeState;
			}

			activeState = nextState;
			OnStateChanged?.Invoke(nextState);
		}

		public void Update()
		{
			if (activeState != null)
			{
				activeState.Update();
			}
		}
	}
}
