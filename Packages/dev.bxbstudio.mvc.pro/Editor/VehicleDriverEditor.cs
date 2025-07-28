#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities.Editor;
using MVC.Editor;
using MVC.Core.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.IK.Editor
{
	[CustomEditor(typeof(VehicleDriver))]
	public class VehicleDriverEditor : ToolkitBehaviourEditor
	{
		#region Variables

		#region Editor Variables

		public static bool animationsFoldout;

		private static Texture2D WarningIcon => EditorUtilities.Icons.Warning;
		private static Texture2D ErrorIcon => EditorUtilities.Icons.Error;
		private static Texture2D IssueIcon => EditorUtilities.Icons.Info;
		private static Texture2D CheckIcon => EditorUtilities.Icons.CheckCircleColored;

		#endregion

		#region Global Variables

		private VehicleDriver Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleDriver;

				return instance;
			}
		}
		private Animator AnimatorInstance
		{
			get
			{
				return Instance.Animation.Animator;
			}
		}
		private VehicleDriver instance;

		#endregion

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (HasInternalErrors)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller is facing some internal problems that need to be fixed!", MessageType.Error);
				EditorGUILayout.Space();

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("Try some quick fixes"))
					FixInternalProblems(Instance.gameObject);

				return;
			}

			EditorGUILayout.BeginHorizontal();

			if (Instance.TargetVehicle)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ToolkitEditorUtility.SelectObject(Instance.TargetVehicle.gameObject);

				GUILayout.Space(5f);
			}

			EditorGUILayout.LabelField("Driver Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
#if MVC_COMMUNITY

			GUI.backgroundColor = Color.green;
				
			EditorGUILayout.HelpBox("This feature is only included with Multiversal Vehicle Controller Pro!", MessageType.Info);

			GUI.backgroundColor = Color.yellow;

			EditorGUILayout.HelpBox("This component exist only for the purpose of not losing references when switching between versions.", MessageType.Warning);

			GUI.backgroundColor = orgGUIBackgroundColor;

			if (GUILayout.Button("Upgrade to Pro"))
				ToolkitEditorWindow.VisitWebsite();

			EditorGUILayout.Space();
#else
			#region Animations Foldout

			var hideFlag = Settings.useHideFlags ? HideFlags.HideInHierarchy : HideFlags.None;

			if (Instance.Animation.Animator && Instance.Animation.Animator.hideFlags != hideFlag)
				Instance.Animation.Animator.hideFlags = hideFlag;

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Animations", "Control your driver animations"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasAnimationsWarnings ? WarningIcon : Instance.Problems.HasAnimationsErrors ? ErrorIcon : Instance.Problems.HasAnimationsIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(animationsFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				animationsFoldout = !animationsFoldout;

			EditorGUILayout.EndHorizontal();

			if (animationsFoldout)
				AnimationsEditor();

			EditorGUILayout.EndVertical();

			#endregion

			MessagesEditor();

			if (Instance.TargetVehicle)
				EditorGUILayout.HelpBox($"This driver is currently linked to '{Instance.TargetVehicle.name}'", MessageType.None);

			EditorGUILayout.Space();
			Repaint();
#endif
		}

		#endregion

		#region Static Methods

		#region Utilities

		public static bool EnableFoldout()
		{
			animationsFoldout = false;

			return true;
		}

		#endregion

		#region Menu Items

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/Driver", false, 23)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Driver", false, 23)]
#endif
		public static VehicleDriver CreateNewDriver()
		{
			if (!CreateNewDriverCheck())
				return null;

			if (!Selection.activeGameObject)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "To add a Driver component, please select a GameObject within the inspector window.", "Okay");

				return null;
			}

			VehicleDriver driver = Selection.activeGameObject.GetComponent<VehicleDriver>();

			if (!driver)
				driver = Selection.activeGameObject.GetComponentInParent<VehicleDriver>();

			if (!driver)
			{
				Undo.RegisterCompleteObjectUndo(Selection.activeGameObject, "Add Driver");

				driver = Undo.AddComponent<VehicleDriver>(Selection.activeGameObject);

				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "We have successfully added a Driver component to the selected GameObject!", "Okay");
			}
			else
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The selected GameObject have a Driver component attached to it already!", "Okay");

			EditorUtility.SetDirty(driver.gameObject);

			return driver;
		}

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/Driver", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Driver", true)]
#endif
		protected static bool CreateNewDriverCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#endregion

		#region Global Methods

		#region Editor

#if !MVC_COMMUNIY
		private void AnimationsEditor()
		{
			animationsFoldout = EnableFoldout();

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Animator", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			RuntimeAnimatorController newAnimatorController = EditorGUILayout.ObjectField(new GUIContent("Controller", "The Driver's Animator Controller asset"), AnimatorInstance.runtimeAnimatorController, typeof (RuntimeAnimatorController), false) as RuntimeAnimatorController;

			if (AnimatorInstance.runtimeAnimatorController != newAnimatorController)
			{
				Undo.RegisterCompleteObjectUndo(AnimatorInstance, "Change Controller");

				AnimatorInstance.runtimeAnimatorController = newAnimatorController;

				EditorUtility.SetDirty(AnimatorInstance);
			}

			if (AnimatorInstance.runtimeAnimatorController)
				EditorGUILayout.HelpBox("Please make sure the Animator Controller Layers' IK Pass is set to true, in order for the IK system to work properly", MessageType.Info);

			Avatar newAvatar = EditorGUILayout.ObjectField(new GUIContent("Avatar", "The Driver's skeleton definition"), AnimatorInstance.avatar, typeof(Avatar), false) as Avatar;

			if (AnimatorInstance.avatar != newAvatar)
			{
				Undo.RegisterCompleteObjectUndo(AnimatorInstance, "Change Avatar");

				AnimatorInstance.avatar = newAvatar;

				EditorUtility.SetDirty(AnimatorInstance);
			}

			AnimatorUpdateMode newUpdateMode = (AnimatorUpdateMode)EditorGUILayout.EnumPopup(new GUIContent("Update Mode", "The animator update method"), AnimatorInstance.updateMode);

			if (AnimatorInstance.updateMode != newUpdateMode)
			{
				Undo.RegisterCompleteObjectUndo(AnimatorInstance, "Change Update");

				AnimatorInstance.updateMode = newUpdateMode;

				EditorUtility.SetDirty(AnimatorInstance);
			}

			AnimatorCullingMode newCullingMode = (AnimatorCullingMode)EditorGUILayout.EnumPopup(new GUIContent("Culling Mode", "The animator culling method"), AnimatorInstance.cullingMode);

			if (AnimatorInstance.cullingMode != newCullingMode)
			{
				Undo.RegisterCompleteObjectUndo(AnimatorInstance, "Change Culling");

				AnimatorInstance.cullingMode = newCullingMode;

				EditorUtility.SetDirty(AnimatorInstance);
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

			if (Instance.TargetVehicle)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Parameters", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;

				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				ToolkitEditorUtility.Slider(new GUIContent("Look At Height", "The height factor of the driver's eye sight"), Instance.LookAtPositionHeight, 0f, 1f);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					ToolkitEditorUtility.SelectObject(Instance.TargetVehicle.gameObject);

					VehicleEditor.driverIKFoldout = VehicleEditor.EnableFoldout();
					VehicleEditor.IKPivotsFoldout = VehicleEditor.EnableInternalFoldout();
				}

				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
			}
		}
		private void MessagesEditor()
		{
			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (Instance.isActiveAndEnabled)
			{
				if (Instance.Problems.HasAnimationsWarnings)
					GUI.backgroundColor = Color.yellow;
				else if (Instance.Problems.HasAnimationsErrors)
					GUI.backgroundColor = Color.red;
				else if (Instance.Problems.HasAnimationsIssues)
					GUI.backgroundColor = Color.green;
				else
					GUI.backgroundColor = Color.blue;

				EditorGUILayout.Space();
				EditorGUILayout.HelpBox($"Overall State: {(Instance.Problems.HasAnimationsWarnings ? "This controller instance is going to be disabled at runtime." : Instance.Problems.HasAnimationsErrors ? "This controller has some errors that need to be fixed before switching to play mode or else some features may not work." : Instance.Problems.HasAnimationsIssues ? "Some features may not work as expected." : "Everything works well!")}", MessageType.None);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (Instance.Problems.HasAnimationsWarnings)
				{
					if (!Instance.Animation.Animator.runtimeAnimatorController)
						EditorGUILayout.HelpBox("Please assign the Animator Controller field!", MessageType.Warning);
				}
				else if (Instance.Problems.HasAnimationsErrors)
				{
					if (Instance.Animation.AnimationClips.Length < 1)
						EditorGUILayout.HelpBox("The assigned Animator Controller doesn't have any animations.", MessageType.Error);
				}
				else if (Instance.Problems.HasAnimationsIssues)
				{
					if (!Instance.Animation.Animator.avatar)
						EditorGUILayout.HelpBox("Please assign the Animator Avatar field!", MessageType.Info);
				}
			}
			else
				EditorGUILayout.HelpBox("This driver has been disabled.", MessageType.None);
		}
#endif

		#endregion

		#region GUI

		private void OnSceneGUI()
		{
			if (Instance.TargetVehicle)
			{
				Transform driverContainer = Instance.TargetVehicle.transform.Find("Driver");

				if (Tools.current == Tool.Move)
				{
					if (!Tools.hidden)
						Tools.hidden = true;

					Vector3 newPosition = Handles.PositionHandle(driverContainer.position, driverContainer.rotation);

					if (driverContainer.position != newPosition)
					{
						Undo.RegisterCompleteObjectUndo(driverContainer, "Change Position");

						driverContainer.position = newPosition;

						EditorUtility.SetDirty(driverContainer);
					}
				}
				else if (Tools.current == Tool.Rotate)
				{
					if (!Tools.hidden)
						Tools.hidden = true;

					Quaternion newRotation = Handles.RotationHandle(driverContainer.rotation, driverContainer.position);

					if (driverContainer.rotation != newRotation)
					{
						Undo.RegisterCompleteObjectUndo(driverContainer, "Change Rotation");

						driverContainer.rotation = newRotation;

						EditorUtility.SetDirty(driverContainer);
					}
				}
				else if (Tools.hidden)
					Tools.hidden = false;
			}
		}

		#endregion

		#region Destroy

		private void OnDestroy()
		{
			if (Tools.hidden)
				Tools.hidden = false;

			if (!Instance.TargetVehicle)
				return;

			Transform driverContainer = Instance.TargetVehicle.transform.Find("Driver");

			if ((Instance.transform.localPosition != Vector3.zero || !Instance.transform.parent) && Instance.TargetVehicle)
			{
				if (Instance.transform.parent != driverContainer)
				{
					Instance.transform.parent = driverContainer;

					Instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
				}

				if (Instance.transform.localPosition != Vector3.zero)
				{
					driverContainer.position = Instance.transform.position;
					Instance.transform.localPosition = Vector3.zero;
				}

				if (Instance.transform.localRotation != Quaternion.identity)
				{
					driverContainer.rotation = Instance.transform.rotation;
					Instance.transform.localRotation = Quaternion.identity;
				}
			}
		}

		#endregion

		#endregion

		#endregion
	}
}
