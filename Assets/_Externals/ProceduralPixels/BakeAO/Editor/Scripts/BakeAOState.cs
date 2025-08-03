/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;

namespace ProceduralPixels.BakeAO.Editor
{
    internal abstract class BakeAOState
	{
		public event Action<BakeAOState> OnTransitionToNextStateRequested;
		public BakeAOState nextState;

		public abstract string StateDescription { get; }
		public abstract float Progress { get; }

		public void TransitionToNextState()
		{
			OnTransitionToNextStateRequested?.Invoke(nextState); 
		}

		public virtual void Start()
		{}

		public abstract void Update();

		public virtual void End()
		{}
	}
}
