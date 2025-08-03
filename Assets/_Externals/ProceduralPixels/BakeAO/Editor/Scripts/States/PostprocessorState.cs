/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using UnityEditor;

namespace ProceduralPixels.BakeAO.Editor
{
	internal class PostprocessorState : BakeAOState
	{
		public override float Progress => frameCount / (float)4;
		public override string StateDescription => "Postprocessing bake";

		private BakingData bakingData;
		private BakingSetup bakingSetup;
		private BakePostprocessor bakePostprocessor;

		private int frameCount = 0;

		public void Setup(BakingData bakingData, BakingSetup bakingSetup, BakePostprocessor bakePostprocessor)
		{
			this.bakingData = bakingData;
			this.bakingSetup = bakingSetup;
			this.bakePostprocessor = bakePostprocessor;
		}

		public override void Start()
		{
			base.Start();
			frameCount = 0;
		}

		public override void Update()
		{
			switch(frameCount)
			{
				case 3:
					bakePostprocessor.AfterBake(bakingSetup, bakingData.aoRT);
					break;
				case 5:
					TransitionToNextState();
					break;
			}

			frameCount++;
		}

		public override void End()
		{
			base.End();
			AssetDatabase.Refresh();
		}
	}
}
