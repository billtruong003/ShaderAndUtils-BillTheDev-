#region Namespaces

using UnityEngine;
using UnityEditor;

#endregion

namespace MVC.Base.Editor
{
	[CustomPropertyDrawer(typeof(VehicleTireCompound.WidthFrictionModifier))]
	public class WidthFrictionModifierDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			
			int indentLevel = EditorGUI.indentLevel;
			
			EditorGUIUtility.labelWidth *= .25f;
			EditorGUI.indentLevel = 0;

			position.width *= .5f;
			position.width -= 5f;

			EditorGUI.PropertyField(position, property.FindPropertyRelative("width"), new GUIContent("Width", "Tire width threshold"));

			position.x += position.width + 10f;

			EditorGUI.PropertyField(position, property.FindPropertyRelative("frictionMultiplier"), new GUIContent("Multiplier", "Tire friction multiplier"));

			EditorGUIUtility.labelWidth *= 4f;
			EditorGUI.indentLevel = indentLevel;

			EditorGUI.EndProperty();
		}
	}
}
