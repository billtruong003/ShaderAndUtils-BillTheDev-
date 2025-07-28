#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities.Editor;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleDamageZone))]
	public class VehicleDamageZoneEditor : VehicleZoneEditor
	{
		#region Variables

		private VehicleZonesContainer ContainerInstance
		{
			get
			{
				if (Instance && !containerInstance)
					containerInstance = Instance.GetComponentInParent<VehicleZonesContainer>();

				return containerInstance;
			}
		}
		private VehicleDamageZone Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleDamageZone;

				return instance;
			}
		}
		private VehicleZonesContainer containerInstance;
		private VehicleDamageZone instance;

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
					FixInternalProblems(Instance.gameObject);

				EditorGUILayout.Space();

				return;
			}
			else if (!IsSetupDone)
			{
				GUI.backgroundColor = Color.green;

				EditorGUILayout.HelpBox("It seems like the Multiversal Vehicle Controller is not ready for use yet!", MessageType.Info);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("What's going on?"))
					ToolkitSettingsEditor.OpenWindow();

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.ContainerInstance)
			{
				GUI.backgroundColor = Color.yellow;

				EditorGUILayout.HelpBox("This zone instance doesn't belong to any Zones Container in this scene!", MessageType.Warning);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				ToolkitEditorUtility.SelectObject(ContainerInstance.gameObject);

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Damage Zone Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("General", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			VehicleZone.ZoneShape newShape = (VehicleZone.ZoneShape)EditorGUILayout.EnumPopup(new GUIContent("Shape", "The damage zone trigger collider shape"), Instance.Shape);

			if (Instance.Shape != newShape)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Shape");

				Instance.Shape = newShape;

				EditorUtility.SetDirty(Instance);
			}

			VehicleDamageZone.DamageZoneType newDamageType = (VehicleDamageZone.DamageZoneType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The damage zone behaviour type"), Instance.zoneType);

			if (Instance.zoneType != newDamageType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				Instance.zoneType = newDamageType;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndVertical();
			base.OnInspectorGUI();
			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

		[MenuItem("GameObject/MVC/Zone/Repair Vehicle", false, 19)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Repair Vehicle", false, 19)]
		public static VehicleDamageZone CreateNewRepairVehicleZone()
		{
			if (!CreateNewDamageZoneCheck())
				return null;

			VehicleDamageZone zone = CreateNewZone<VehicleDamageZone, VehicleZonesContainer>();

			if (zone)
				zone.zoneType = VehicleDamageZone.DamageZoneType.RepairVehicle;

			return zone;
		}
		[MenuItem("GameObject/MVC/Zone/Wheel Damage", false, 19)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Wheel Damage", false, 19)]
		public static VehicleDamageZone CreateNewWheelDamageZone()
		{
			if (!CreateNewDamageZoneCheck())
				return null;

			VehicleDamageZone zone = CreateNewZone<VehicleDamageZone, VehicleZonesContainer>();

			if (zone)
				zone.zoneType = VehicleDamageZone.DamageZoneType.Wheel;

			return zone;
		}

		[MenuItem("GameObject/MVC/Zone/Repair Vehicle", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Repair Vehicle", true)]
		[MenuItem("GameObject/MVC/Zone/Wheel Damage", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Wheel Damage", true)]
		protected static bool CreateNewDamageZoneCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#endregion
	}
}
