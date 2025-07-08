using UnityEngine;

namespace AllIn13DShader
{
	public class URPSettingsDrawer : AssetWindowTabDrawer
	{
		public URPSettingsDrawer(CommonStyles commonStyles, AllIn13DShaderWindow parentWindow) : base(commonStyles, parentWindow)
		{
		}

		public override void Draw()
		{
			GUILayout.Label("Configure AllIn13D to work correctly with URP", commonStyles.bigLabel);
			if (GUILayout.Button("Configure"))
			{
#if ALLIN13DSHADER_URP
				URPConfigurator.Configure(forceConfigure: true);
#endif
			}
		}

		public override void Hide()
		{

		}

		public override void Show()
		{
			
		}

		public override void OnDisable()
		{
		
		}

		public override void OnEnable()
		{
		
		}

		public override void EnteredPlayMode()
		{
		
		}
	}
}