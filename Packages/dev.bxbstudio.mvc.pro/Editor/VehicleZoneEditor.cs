#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities;
using MVC.Editor;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleZone))]
	public class VehicleZoneEditor : ToolkitBehaviourEditor
	{
		#region Variables

		private VehicleZone zoneInstance;

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("To place the selected zone around the scene, use `Shift` + `Ctrl` and move your mouse cursor within the Scene window.", MessageType.None);
		}

		#endregion

		#region Static Methods

		internal static ZoneT CreateNewZone<ZoneT, ContainerT>() where ZoneT : VehicleZone where ContainerT : VehicleZonesContainer
		{
			ContainerT container = CreateNewZoneCheck<ContainerT>();

			if (!container)
			{
				string zoneName = typeof(ZoneT).Name.Replace("Vehicle", "");

				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"To create a{(zoneName.StartsWith("A") ? "n" : "")} {zoneName}, please select an existing Zones Container or create a new one.", "Okay");

				return null;
			}

			GameObject zoneGameObject = new($"{container.ZonesCount:000}_{typeof(ZoneT).Name.Replace("Vehicle", "")}");
			Transform zoneTransform = zoneGameObject.transform;

			zoneTransform.SetParent(container.transform, false);

			zoneTransform.localScale = 10f * Vector3.one;

			ZoneT zone = zoneGameObject.AddComponent<ZoneT>();

			ToolkitEditorUtility.SelectObject(zoneGameObject);
			ToolkitEditorUtility.FocusSceneView();
			SceneView.lastActiveSceneView.Frame(Utility.GetObjectPhysicsBounds(zoneGameObject, true, true), false);
			EditorUtility.SetDirty(container.gameObject);

			return zone;
		}

		private static T CreateNewZoneCheck<T>() where T : VehicleZonesContainer
		{
			if (EditorApplication.isPlaying || !Selection.activeGameObject)
				return null;

			T container = Selection.activeGameObject.GetComponent<T>();

			return container ?? Selection.activeGameObject.GetComponentInParent<T>();
		}

		#endregion

		#region Global Methods

		public virtual void OnSceneGUI()
		{
			var e = Event.current;

			if (e.shift && e.control && (e.type == EventType.MouseMove || e.type == EventType.MouseDrag))
				if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit, Mathf.Infinity, -1, QueryTriggerInteraction.Ignore))
					zoneInstance.transform.position = hit.point;
		}

		private void OnEnable()
		{
			zoneInstance = target as VehicleZone;
		}

		#endregion

		#endregion
	}
}
