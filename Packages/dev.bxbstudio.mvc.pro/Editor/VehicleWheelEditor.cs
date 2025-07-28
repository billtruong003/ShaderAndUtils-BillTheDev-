#region Namespaces

using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Editor;
using MVC.Utilities.Editor;
using MVC.Base;

#endregion

namespace MVC.Core.Editor
{
	[CustomEditor(typeof(VehicleWheel))]
	public class VehicleWheelEditor : ToolkitBehaviourEditor
	{
		#region Variables

		public VehicleWheel Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleWheel;

				return instance;
			}
		}

		private VehicleWheel instance;

		#endregion

		#region Methods

		public override void OnInspectorGUI()
		{
			#region Messages

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (HasInternalErrors)
			{
				EditorGUILayout.Space();

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
				EditorGUILayout.Space();

				GUI.backgroundColor = Color.green;

				EditorGUILayout.HelpBox("It seems like the Multiversal Vehicle Controller is not ready for use yet!", MessageType.Info);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("What's going on?"))
					ToolkitSettingsEditor.OpenWindow();

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.VehicleInstance)
			{
				EditorGUILayout.Space();

				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This wheel component is invalid cause it's Vehicle parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.Module)
			{
				EditorGUILayout.Space();

				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This wheel component is invalid because it is not included in the wheels list!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			Repaint();
		}

		/*private static void DrawCurve(Func<float, float> function, float step, Utility.Interval xInterval, Utility.Interval yInterval, Rect rect, Color backgroundColor, Color secondaryColor, Color curveColor, float innerMargin, bool interactable, bool showExtremumPoints)
		{
			EditorGUI.DrawRect(rect, backgroundColor);

			rect.xMin += innerMargin;
			rect.xMax -= innerMargin;
			rect.yMin += innerMargin;
			rect.yMax -= innerMargin;

			Color orgColor = Handles.color;

			Handles.color = secondaryColor;

			Handles.DrawLine(new(rect.xMin + xInterval.InverseLerp(0f) * rect.width, rect.yMax), new(rect.xMin + xInterval.InverseLerp(0f) * rect.width, rect.yMin));
			Handles.DrawLine(new(rect.xMin, rect.yMax - yInterval.InverseLerp(0f) * rect.height), new(rect.xMax, rect.yMax - yInterval.InverseLerp(0f) * rect.height));

			for (int i = (int)xInterval.Min; i <= xInterval.Max; i++)
			{
				if (i == 0)
					continue;

				float x = rect.xMin + xInterval.InverseLerp(i) * rect.width;

				Handles.DrawDottedLine(new(x, rect.yMin), new(x, rect.yMax), 2f);
			}

			for (int i = (int)yInterval.Min; i <= yInterval.Max; i++)
			{
				if (i == 0)
					continue;

				float y = rect.yMin + yInterval.InverseLerp(i) * rect.height;

				Handles.DrawDottedLine(new(rect.xMin, y), new(rect.xMax, y), 8f * 256f / rect.width);
			}

			Handles.color = curveColor;

			Vector3 prevPos = new(xInterval.Min, function(xInterval.Min));
			Vector3? t0 = null, t1 = null, t2 = null;

			for (float t = xInterval.Min + step; t <= xInterval.Max; t += step)
			{
				Vector3 pos = new(t, function(t));

				if (pos.y >= yInterval.Min && pos.y <= yInterval.Max)
				{
					Vector3 invPrevPos = new(rect.xMin + xInterval.InverseLerp(prevPos.x) * rect.width, rect.yMax - yInterval.InverseLerp(prevPos.y) * rect.height);
					Vector3 invPos = new(rect.xMin + xInterval.InverseLerp(pos.x) * rect.width, rect.yMax - yInterval.InverseLerp(pos.y) * rect.height);

					Handles.DrawLine(invPrevPos, invPos);

					if (Mathf.Approximately(t, 0f))
						t0 = pos;
					else
					{
						if ((t1 == null || t1.Value.y < pos.y) && t > 0f)
							t1 = pos;

						if ((t2 == null || t2.Value.y > pos.y) && t < 0f)
							t2 = pos;
					}
				}

				prevPos = pos;
			}

			if (showExtremumPoints)
			{
				if (t0 != null)
				{
					Vector3 invT0 = new(rect.xMin + xInterval.InverseLerp(t0.Value.x) * rect.width, rect.yMax - yInterval.InverseLerp(t0.Value.y) * rect.height);

					Handles.DrawSolidDisc(invT0, Vector3.forward, 2.5f);
				}

				if (t1 != null)
				{
					Vector3 invT1 = new(rect.xMin + xInterval.InverseLerp(t1.Value.x) * rect.width, rect.yMax - yInterval.InverseLerp(t1.Value.y) * rect.height);

					Handles.DrawSolidDisc(invT1, Vector3.forward, 2.5f);
				}

				if (t2 != null)
				{
					Vector3 invT2 = new(rect.xMin + xInterval.InverseLerp(t2.Value.x) * rect.width, rect.yMax - yInterval.InverseLerp(t2.Value.y) * rect.height);

					Handles.DrawSolidDisc(invT2, Vector3.forward, 2.5f);
				}
			}

			Handles.color = orgColor;
		}*/
		#endregion
	}
}
