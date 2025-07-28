#region Namespaces

using UnityEngine;
using UnityEditor;
#if !MVC_COMMUNITY
using Utilities;
#endif
using Utilities.Editor;
#if !MVC_COMMUNITY
using MVC.Base;
#endif
using MVC.Base.Editor;
#if MVC_COMMUNITY
using MVC.Editor;
#endif
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.AI.Editor
{
	[CustomEditor(typeof(VehicleAIZone))]
	public class VehicleAIZoneEditor : VehicleZoneEditor
	{
		#region Variables

		private VehicleAIZone m_instance;

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			#region Messages

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (HasInternalErrors)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller is facing some internal problems that need to be fixed!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("Try some quick fixes"))
					FixInternalProblems(m_instance.gameObject);

				EditorGUILayout.Space();

				return;
			}
			else if (!m_instance.PathInstance)
			{
				GUI.backgroundColor = Color.yellow;

				EditorGUILayout.HelpBox("This AI Zone instance doesn't belong to any AI Path in this scene!", MessageType.Warning);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				ToolkitEditorUtility.SelectObject(m_instance.PathInstance.gameObject);

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("AI Zone Configurations", EditorStyles.boldLabel);
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

#else
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("General", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			VehicleZone.ZoneShape newShape = (VehicleZone.ZoneShape)EditorGUILayout.EnumPopup(new GUIContent("Shape", "The AI zone trigger collider shape"), m_instance.Shape);

			if (m_instance.Shape != newShape)
			{
				Undo.RegisterCompleteObjectUndo(m_instance, "Change Shape");

				m_instance.Shape = newShape;

				EditorUtility.SetDirty(m_instance);
			}

			VehicleAIZone.AIZoneType newAIType = (VehicleAIZone.AIZoneType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The AI zone behaviour type"), m_instance.zoneType);

			if (m_instance.zoneType != newAIType)
			{
				Undo.RegisterCompleteObjectUndo(m_instance, "Change Type");

				m_instance.zoneType = newAIType;

				EditorUtility.SetDirty(m_instance);
			}

			var path = m_instance.PathInstance;

			switch (m_instance.zoneType)
			{
				case VehicleAIZone.AIZoneType.Brake:
					float newBrakeSpeedTarget = ToolkitEditorUtility.NumberField(new GUIContent("Speed", "The target speed an AI vehicle needs to reach while braking"), m_instance.BrakeSpeedTarget, Utility.Units.Speed, 1, m_instance, "Change Speed");

					if (m_instance.BrakeSpeedTarget != newBrakeSpeedTarget)
						m_instance.BrakeSpeedTarget = newBrakeSpeedTarget;

					EditorGUI.BeginDisabledGroup(!path || !path.snapZones);

					EditorGUI.indentLevel++;

					bool newSnapBrakeSpeedTarget = ToolkitEditorUtility.ToggleButtons(new GUIContent("Snap Speed", "Enabling this allows the path to set the recommended speed target automatically while moving the AI brake zone"), null, "On", "Off", m_instance.snapBrakeSpeedTarget, m_instance, "Switch Snap");

					if (m_instance.snapBrakeSpeedTarget != newSnapBrakeSpeedTarget)
						m_instance.snapBrakeSpeedTarget = newSnapBrakeSpeedTarget;

					EditorGUI.indentLevel--;

					EditorGUI.EndDisabledGroup();

					break;

				case VehicleAIZone.AIZoneType.Handbrake:
					float newHandbrakeSlipTarget = ToolkitEditorUtility.Slider(new GUIContent("Slip Target", "The wheels forward slip target while handbraking"), m_instance.HandbrakeSlipTarget, 0f, 1.5f, m_instance, "Change Slip");

					if (m_instance.HandbrakeSlipTarget != newHandbrakeSlipTarget)
						m_instance.HandbrakeSlipTarget = newHandbrakeSlipTarget;

					break;

				case VehicleAIZone.AIZoneType.NOS:
					float newMinimumNOSTarget = ToolkitEditorUtility.Slider(new GUIContent("NOS Target", "The vehicle minimum NOS quantity inside a bottle to initiate"), m_instance.MinimumNOSTarget, 0f, 1f, m_instance, "Change NOS");

					if (m_instance.MinimumNOSTarget != newMinimumNOSTarget)
						m_instance.MinimumNOSTarget = newMinimumNOSTarget;

					break;
			}
			
			if (m_instance.zoneType == VehicleAIZone.AIZoneType.Brake || m_instance.zoneType == VehicleAIZone.AIZoneType.Handbrake)
			{
				float newInputIntensity = ToolkitEditorUtility.Slider(new GUIContent("Braking Intensity", "The braking input intensity"), m_instance.BrakingIntensity, 0f, 1f, m_instance, "Change Intensity");

				if (m_instance.BrakingIntensity != newInputIntensity)
					m_instance.BrakingIntensity = newInputIntensity;
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			base.OnInspectorGUI();

			if (path && path.snapZones)
			{
				EditorGUILayout.HelpBox("AI Zone snapping to path is enabled", MessageType.Info);

				if (GUILayout.Button("Disable Path Snapping"))
				{
					Undo.RegisterCompleteObjectUndo(path, "Switch Snap");

					path.snapZones = false;
					
					EditorUtility.SetDirty(path);
				}
			}
			else if (GUILayout.Button("Enable Path Snapping"))
			{
				Undo.RegisterCompleteObjectUndo(path, "Switch Snap");

				path.snapZones = true;

				EditorUtility.SetDirty(path);
			}
#endif
			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/AI/Brake Zone", false, 20)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/Brake Zone", false, 20)]
#endif
		public static VehicleAIZone CreateNewBrakeZone()
		{
			if (!CreateNewZoneCheck())
				return null;

			VehicleAIZone zone = CreateNewZone<VehicleAIZone, VehicleAIPath>();

			if (zone)
				zone.zoneType = VehicleAIZone.AIZoneType.Brake;

			return zone;
		}
#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/AI/Handbrake Zone", false, 20)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/Handbrake Zone", false, 20)]
#endif
		public static VehicleAIZone CreateNewHandbrakeZone()
		{
			if (!CreateNewZoneCheck())
				return null;

			VehicleAIZone zone = CreateNewZone<VehicleAIZone, VehicleAIPath>();

			if (zone)
				zone.zoneType = VehicleAIZone.AIZoneType.Handbrake;

			return zone;
		}
#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/AI/NOS Zone", false, 20)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/NOS Zone", false, 20)]
#endif
		public static VehicleAIZone CreateNewNOSZone()
		{
			if (!CreateNewZoneCheck())
				return null;

			VehicleAIZone zone = CreateNewZone<VehicleAIZone, VehicleAIPath>();

			if (zone)
				zone.zoneType = VehicleAIZone.AIZoneType.NOS;

			return zone;
		}

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/AI/Brake Zone", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/Brake Zone", true)]
		[MenuItem("GameObject/MVC/AI/Handbrake Zone", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/Handbrake Zone", true)]
		[MenuItem("GameObject/MVC/AI/NOS Zone", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/NOS Zone", true)]
#endif
		protected static bool CreateNewZoneCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

		public override void OnSceneGUI()
		{
			var path = m_instance.PathInstance;
			var e = Event.current;

			if (path)
				foreach (var bezierCurve in path.BezierCurves)
					Handles.DrawBezier(bezierCurve.P0, bezierCurve.P3, bezierCurve.P1, bezierCurve.P2, Settings.AIPathBezierColor, null, Settings.gizmosSize * 2f);

			if (e.shift && e.control && (e.type == EventType.MouseMove || e.type == EventType.MouseDrag))
				if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit, Mathf.Infinity, -1, QueryTriggerInteraction.Ignore))
				{
					int groundIndex = Undo.GetCurrentGroup();
					Undo.SetCurrentGroupName("Snap Zone");
					Undo.RegisterCompleteObjectUndo(m_instance.transform, "Move Transform");
					if (path.SpacedPointsCount < 1)
					{
						path.GenerateSpacedPoints();
						path.AlignSpacedPointsToGround();
					}

					if (path && path.snapZones && path.TryGetSpacedPoint(path.ClosestSpacedPointIndex(hit.point), out VehicleAIPath.SpacedPathPoint pathPoint))
					{
						m_instance.transform.SetPositionAndRotation(pathPoint.spacedCurvePoint.curvePoint.position, pathPoint.spacedCurvePoint.curvePoint.rotation);

						Undo.RegisterCompleteObjectUndo(m_instance, "Change Speed");

						if (m_instance.snapBrakeSpeedTarget && m_instance.zoneType == VehicleAIZone.AIZoneType.Brake)
							m_instance.BrakeSpeedTarget = pathPoint.spacedCurvePoint.curvePoint.TargetVelocity(VehicleAIPath.DefaultVehicleFrictionForVelocityEvaluation, Physics.gravity.magnitude) * 3.6f;
					}
					else
						m_instance.transform.position = hit.point;

					EditorUtility.SetDirty(m_instance.gameObject);
					Undo.CollapseUndoOperations(groundIndex);
				}
		}
		private void OnEnable()
		{
			m_instance = target as VehicleAIZone;
		}

		#endregion

		#endregion
	}
}
