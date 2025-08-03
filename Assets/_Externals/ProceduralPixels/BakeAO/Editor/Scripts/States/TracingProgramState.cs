/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿namespace ProceduralPixels.BakeAO.Editor
{
    internal class TracingProgramState : BakeAOState
	{
		private BakeAOPass aoBakePass;
		private BakingData bakingData;

		public override string StateDescription => "Baking...";
		public override float Progress => aoBakePass != null ? aoBakePass.Progress : 0.0f;

		public void Setup(BakingData bakingData, BakingSetup bakingSetup)
		{
			this.bakingData = bakingData;
			aoBakePass = new BakeAOPass();
			aoBakePass.Setup(bakingData, bakingSetup);
		}

		public override void Update()
		{
            if (aoBakePass.Status == BakeAOPass.BakeStatus.Completed)
            {
				aoBakePass.ReleaseAllocatedResources();
				bakingData.ReleaseRayGenerationData(); // Baking is done, so we don't need ray data anymore
                TransitionToNextState();
            }
            else
				aoBakePass.Execute();
		}
	}
}
