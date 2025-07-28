#region Namespaces

using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.AI;
using UnityEditorInternal;
using Utilities;
using Utilities.Editor;
using MVC.Editor;

using Object = UnityEngine.Object;

#endregion

namespace MVC.Utilities.Editor
{
	public static class ToolkitEditorUtility
	{
		#region Variables

		public static GUIStyle IndentedButton => new(GUI.skin.button)
		{
			margin = new()
			{
				top = GUI.skin.button.margin.top,
				right = GUI.skin.button.margin.right,
				left = GUI.skin.button.margin.left + 15 * EditorGUI.indentLevel,
				bottom = GUI.skin.button.margin.bottom
			}
		};
		public static GUIStyle UnstretchableMiniButtonSmall => new(EditorStyles.miniButton)
		{
			stretchWidth = false,
			fixedWidth = miniButtonSmallWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButton => new(EditorStyles.miniButton)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonNormal => new(EditorStyles.miniButton)
		{
			stretchWidth = false,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonWide => new(EditorStyles.miniButton)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWideWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonLeftSmall => new(EditorStyles.miniButtonLeft)
		{
			stretchWidth = false,
			fixedWidth = miniButtonSmallWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonLeft => new(EditorStyles.miniButtonLeft)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonLeftWide => new(EditorStyles.miniButtonLeft)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWideWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonMiddleSmall => new(EditorStyles.miniButtonMid)
		{
			stretchWidth = false,
			fixedWidth = miniButtonSmallWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonMiddle => new(EditorStyles.miniButtonMid)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonMiddleWide => new(EditorStyles.miniButtonMid)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWideWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonRightSmall => new(EditorStyles.miniButtonRight)
		{
			stretchWidth = false,
			fixedWidth = miniButtonSmallWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonRight => new(EditorStyles.miniButtonRight)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle UnstretchableMiniButtonRightWide => new(EditorStyles.miniButtonRight)
		{
			stretchWidth = false,
			fixedWidth = miniButtonWideWidth,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle MiddleLeftAlignedLabel => new(GUI.skin.label)
		{
			stretchWidth = true,
			alignment = TextAnchor.MiddleLeft,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle MiddleLeftAlignedBoldLabel => new(EditorStyles.boldLabel)
		{
			stretchWidth = true,
			alignment = TextAnchor.MiddleLeft,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle MiddleCenterAlignedLabel => new(GUI.skin.label)
		{
			alignment = TextAnchor.MiddleCenter,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle MiddleCenterAlignedBoldLabel => new(EditorStyles.boldLabel)
		{
			alignment = TextAnchor.MiddleCenter,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle MiddleRightAlignedLabel => new(GUI.skin.label)
		{
			alignment = TextAnchor.MiddleRight,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle MiddleRightAlignedBoldLabel => new(EditorStyles.boldLabel)
		{
			alignment = TextAnchor.MiddleRight,
			fixedHeight = EditorGUIUtility.singleLineHeight
		};
		public static GUIStyle MiddleAlignedIcon => new()
		{
			stretchHeight = false,
			stretchWidth = false,
			alignment = TextAnchor.MiddleCenter,
			fixedWidth = EditorGUIUtility.singleLineHeight - 4f,
			fixedHeight = EditorGUIUtility.singleLineHeight + 4f
		};
		public static GUIStyle CenterAlignedNumberField => new(EditorStyles.numberField)
		{
			alignment = TextAnchor.MiddleCenter
		};
		public static GUIStyle NumberFieldUnitText => new(EditorStyles.centeredGreyMiniLabel)
		{
			alignment = TextAnchor.MiddleLeft,
			clipping = TextClipping.Overflow,
			fontStyle = FontStyle.Bold
		};

		private static ToolkitSettings Settings
		{
			get
			{
				return ToolkitBehaviourEditor.Settings;
			}
		}
		private static readonly float miniButtonSmallWidth = 16f;
		private static readonly float miniButtonWidth = 20f;
		private static readonly float miniButtonWideWidth = 25f;
		private static readonly float sliderWidthAddition = 15f;

		#endregion

		#region Methods

		public static bool FoldoutButton(bool foldout, GUIStyle buttonStyle = null)
		{
			
			return GUILayout.Toggle(foldout, foldout ? EditorUtilities.Icons.CaretDown : EditorUtilities.Icons.CaretRight, buttonStyle ?? UnstretchableMiniButtonWide);
		}
		public static bool FoldoutButton(GUIContent label, bool foldout, GUIStyle labelStyle = null, GUIStyle buttonStyle = null)
		{
			EditorGUILayout.BeginHorizontal();

			foldout = FoldoutButton(foldout, buttonStyle);

			EditorGUILayout.LabelField(label, labelStyle ?? EditorStyles.label);
			EditorGUILayout.EndHorizontal();

			return foldout;
		}
		public static bool FoldoutButton(string label, bool foldout, GUIStyle labelStyle = null, GUIStyle buttonStyle = null)
		{
			return FoldoutButton(new GUIContent(label), foldout, labelStyle, buttonStyle);
		}

		public static LayerMask LayerMaskField(GUIContent label, LayerMask mask)
		{
			mask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask);
			mask = EditorGUILayout.MaskField(label, mask, InternalEditorUtility.layers);
			mask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(mask);

			return mask;
		}
		public static LayerMask LayerMaskField(string label, LayerMask mask)
		{
			return LayerMaskField(new GUIContent(label), mask);
		}

		public static T EnumField<T>(GUIContent label, T value, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle style = null, params GUILayoutOption[] options) where T : Enum
		{
			style ??= EditorStyles.popup;

			T newValue = (T)EditorGUILayout.EnumPopup(label, value, style, options);

			if (recordObjectUndo && !newValue.Equals(value))
			{
				RecordEnum(label, recordObjectUndo, undoName);

				if (setDirty)
					EditorUtility.SetDirty(recordObjectUndo);
			}

			return newValue;
		}
		public static T EnumField<T>(string label, T value, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle style = null, params GUILayoutOption[] options) where T : Enum
		{
			return EnumField(new GUIContent(label), value, recordObjectUndo, undoName, setDirty, style, options);
		}

		public static T EnumField<T>(GUIContent label, T value, Func<Enum, bool> checkEnabled, bool includeObsolete, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle style = null, params GUILayoutOption[] options) where T : Enum
		{
			style ??= EditorStyles.popup;

			T newValue = (T)EditorGUILayout.EnumPopup(label, value, checkEnabled, includeObsolete, style, options);

			if (recordObjectUndo && !newValue.Equals(value))
			{
				RecordEnum(label, recordObjectUndo, undoName);

				if (setDirty)
					EditorUtility.SetDirty(recordObjectUndo);
			}

			return newValue;
		}
		public static T EnumField<T>(string label, T value, Func<Enum, bool> checkEnabled, bool includeObsolete, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle style = null, params GUILayoutOption[] options) where T : Enum
		{
			return EnumField(new GUIContent(label), value, checkEnabled, includeObsolete, recordObjectUndo, undoName, setDirty, style, options);
		}

		public static bool ToggleButtons(GUIContent label, GUIStyle labelStyle, GUIContent onContent, GUIContent offContent, bool state, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, GUIStyle leftButtonStyle = null, GUIStyle rightButtonStyle = null, bool setDirty = true)
		{
			bool newState = state;

			EditorGUILayout.BeginHorizontal();

			if (labelStyle != null)
				EditorGUILayout.LabelField(label, labelStyle);
			else
			{
				EditorGUILayout.PrefixLabel(label);
				GUILayout.Space(2f);
			}

			if (flexibleSpace)
				GUILayout.FlexibleSpace();

			bool newOnState = GUILayout.Toggle(newState, onContent, leftButtonStyle ?? EditorStyles.miniButtonLeft);
			bool newOffState = GUILayout.Toggle(!newState, offContent, rightButtonStyle ?? EditorStyles.miniButtonRight);

			EditorGUILayout.EndHorizontal();

			if (newOnState && newOnState != newState)
			{
				newState = true;
				newOffState = false;
			}

			if (newOffState && newOffState != !newState)
				newState = false;

			if (recordObjectUndo && newState != state)
			{
				RecordToggle(label, recordObjectUndo, undoName);

				if (setDirty)
					EditorUtility.SetDirty(recordObjectUndo);
			}

			return newState;
		}
		public static bool ToggleButtons(GUIContent label, GUIStyle labelStyle, string onContent, string offContent, bool state, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, GUIStyle leftButtonStyle = null, GUIStyle rightButtonStyle = null, bool setDirty = true)
		{
			return ToggleButtons(label, labelStyle, new GUIContent(onContent), new GUIContent(offContent), state, recordObjectUndo, undoName, flexibleSpace, leftButtonStyle, rightButtonStyle, setDirty);
		}
		public static bool ToggleButtons(string label, GUIStyle labelStyle, string onContent, string offContent, bool state, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, GUIStyle leftButtonStyle = null, GUIStyle rightButtonStyle = null, bool setDirty = true)
		{
			return ToggleButtons(new GUIContent(label), labelStyle, new GUIContent(onContent), new GUIContent(offContent), state, recordObjectUndo, undoName, flexibleSpace, leftButtonStyle, rightButtonStyle, setDirty);
		}
		public static int ToggleMultipleButtons(GUIContent label, GUIStyle labelStyle, bool horizontal, int stateIndex, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUIContent[] contents)
		{
			if (stateIndex < 0 || stateIndex > contents.Length)
				return default;

			bool changed = false;

			if (horizontal)
				EditorGUILayout.BeginHorizontal();

			if (labelStyle != null)
				EditorGUILayout.LabelField(label, labelStyle);
			else
			{
				EditorGUILayout.PrefixLabel(label);

				if (horizontal)
					GUILayout.Space(2f);
			}

			for (int i = 0; i < contents.Length; i++)
			{
				bool state = i == stateIndex;
				bool newState = GUILayout.Toggle(state, contents[i], horizontal ? (i == 0 ? EditorStyles.miniButtonLeft : i + 1 >= contents.Length ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid) : GUI.skin.box);

				if (newState && state != newState)
				{
					stateIndex = i;
					changed = true;
				}
			}

			if (horizontal)
				EditorGUILayout.EndHorizontal();

			if (changed && recordObjectUndo)
			{
				RecordToggle(label, recordObjectUndo, undoName);

				if (setDirty)
					EditorUtility.SetDirty(recordObjectUndo);
			}

			return stateIndex;
		}
		public static int ToggleMultipleButtons(GUIContent label, GUIStyle labelStyle, bool horizontal, int stateIndex, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params string[] contents)
		{
			GUIContent[] guiContents = contents.Select(content => new GUIContent(content)).ToArray();

			return ToggleMultipleButtons(label, labelStyle, horizontal, stateIndex, recordObjectUndo, undoName, setDirty, guiContents);
		}
		public static int ToggleMultipleButtons(string label, GUIStyle labelStyle, bool horizontal, int stateIndex, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params string[] contents)
		{
			return ToggleMultipleButtons(new GUIContent(label), labelStyle, horizontal, stateIndex, recordObjectUndo, undoName, setDirty, contents);
		}
		public static void ToggleTabButtons(GUIContent tab1, GUIContent tab2, ref bool tab1State, ref bool tab2State, float buttonHeight = default)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUI.indentLevel * 15f);

			GUIStyle leftButtonStyle = new(EditorStyles.miniButtonLeft);
			GUIStyle rightButtonStyle = new(EditorStyles.miniButtonRight);

			if (buttonHeight != default)
			{
				leftButtonStyle.fixedHeight = buttonHeight;
				rightButtonStyle.fixedHeight = buttonHeight;
			}

			bool newTab1State = GUILayout.Toggle(tab1State, tab1, leftButtonStyle);
			bool newTab2State = GUILayout.Toggle(tab2State, tab2, rightButtonStyle);

			if (newTab1State && tab1State != newTab1State)
			{
				tab1State = true;
				tab2State = false;
				newTab2State = false;
			}

			if (newTab2State && tab2State != newTab2State)
			{
				tab1State = false;
				tab2State = true;
			}

			EditorGUILayout.EndHorizontal();
		}
		public static void ToggleTabButtons(string tab1, string tab2, ref bool tab1State, ref bool tab2State, float buttonHeight = default)
		{
			ToggleTabButtons(new GUIContent(tab1), new GUIContent(tab2), ref tab1State, ref tab2State, buttonHeight);
		}
		public static void ToggleTabButtons(GUIContent tab1, GUIContent tab2, GUIContent tab3, ref bool tab1State, ref bool tab2State, ref bool tab3State, float buttonHeight = default)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUI.indentLevel * 15f);

			GUIStyle leftButtonStyle = new(EditorStyles.miniButtonLeft);
			GUIStyle middleButtonStyle = new(EditorStyles.miniButtonMid);
			GUIStyle rightButtonStyle = new(EditorStyles.miniButtonRight);

			if (buttonHeight != default)
			{
				leftButtonStyle.fixedHeight = buttonHeight;
				middleButtonStyle.fixedHeight = buttonHeight;
				rightButtonStyle.fixedHeight = buttonHeight;
			}

			bool newTab1State = GUILayout.Toggle(tab1State, tab1, leftButtonStyle);
			bool newTab2State = GUILayout.Toggle(tab2State, tab2, middleButtonStyle);
			bool newTab3State = GUILayout.Toggle(tab3State, tab3, rightButtonStyle);

			if (newTab1State && tab1State != newTab1State)
			{
				tab1State = true;
				tab2State = false;
				tab3State = false;
				newTab2State = false;
				newTab3State = false;
			}

			if (newTab2State && tab2State != newTab2State)
			{
				tab1State = false;
				tab2State = true;
				tab3State = false;
				newTab3State = false;
			}

			if (newTab3State && tab3State != newTab3State)
			{
				tab1State = false;
				tab2State = false;
				tab3State = true;
			}

			EditorGUILayout.EndHorizontal();
		}
		public static void ToggleTabButtons(string tab1, string tab2, string tab3, ref bool tab1State, ref bool tab2State, ref bool tab3State, float buttonHeight = default)
		{
			ToggleTabButtons(new GUIContent(tab1), new GUIContent(tab2), new GUIContent(tab3), ref tab1State, ref tab2State, ref tab3State, buttonHeight);
		}

		// Interval Field (label, labelStyle, minContent, maxContent, unit, decimals, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, Utility.Units unit, uint decimals, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			Utility.Interval newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, unit, labelStyle, flexibleSpace);

			float newIntervalMin = NumberField(minContent, interval.Min, unit, decimals, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			float newIntervalMax = NumberField(maxContent, interval.Max, unit, decimals, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, uint decimals, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, decimals, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		public static Utility.Interval IntervalField(string label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, uint decimals, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, minContent, maxContent, unit, decimals, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		// Interval Field (label, labelStyle, minContent, maxContent, unit, rounded, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, Utility.Units unit, bool rounded, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			Utility.Interval newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, unit, labelStyle, flexibleSpace);

			float newIntervalMin = NumberField(minContent, interval.Min, unit, rounded, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			float newIntervalMax = NumberField(maxContent, interval.Max, unit, rounded, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, bool rounded, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, rounded, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		public static Utility.Interval IntervalField(string label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, bool rounded, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, minContent, maxContent, unit, rounded, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		// Interval Field (label, labelStyle, minContent, maxContent, unit, fullUnit, decimals, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, string unit, string fullUnit, uint decimals, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			Utility.Interval newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, unit, fullUnit, labelStyle, flexibleSpace);

			float newIntervalMin = NumberField(minContent, interval.Min, unit, fullUnit, decimals, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			float newIntervalMax = NumberField(maxContent, interval.Max, unit, fullUnit, decimals, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, uint decimals, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(label, labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, fullUnit, decimals, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		public static Utility.Interval IntervalField(string label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, uint decimals, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, minContent, maxContent, unit, fullUnit, decimals, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		// Interval Field (label, labelStyle, minContent, maxContent, unit, fullUnit, rounded, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, string unit, string fullUnit, bool rounded, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			Utility.Interval newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, unit, fullUnit, labelStyle, flexibleSpace);

			float newIntervalMin = NumberField(minContent, interval.Min, unit, fullUnit, rounded, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			float newIntervalMax = NumberField(maxContent, interval.Max, unit, fullUnit, rounded, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.Interval IntervalField(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, bool rounded, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(label, labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, fullUnit, rounded, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		public static Utility.Interval IntervalField(string label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, bool rounded, Utility.Interval interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, minContent, maxContent, unit, fullUnit, rounded, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}

		// Interval Field (label, labelStyle, minContent, maxContent, unit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.IntervalInt IntervalField(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, Utility.Units unit, Utility.IntervalInt interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			Utility.IntervalInt newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, unit, labelStyle, flexibleSpace);

			int newIntervalMin = NumberField(minContent, interval.Min, unit, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			int newIntervalMax = NumberField(maxContent, interval.Max, unit, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.IntervalInt IntervalField(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, Utility.IntervalInt interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		public static Utility.IntervalInt IntervalField(string label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, Utility.IntervalInt interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, minContent, maxContent, unit, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		// Interval Field (label, labelStyle, minContent, maxContent, unit, fullUnit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.IntervalInt IntervalField(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, string unit, string fullUnit, Utility.IntervalInt interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			Utility.IntervalInt newInterval = new Utility.IntervalInt(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, unit, fullUnit, labelStyle, flexibleSpace);

			int newIntervalMin = NumberField(minContent, interval.Min, unit, fullUnit, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			int newIntervalMax = NumberField(maxContent, interval.Max, unit, fullUnit, recordObjectUndo, undoName, setDirty, fieldsStyle, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.IntervalInt IntervalField(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, Utility.IntervalInt interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, fullUnit, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}
		public static Utility.IntervalInt IntervalField(string label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, Utility.IntervalInt interval, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, GUIStyle fieldsStyle = null, params GUILayoutOption[] options)
		{
			return IntervalField(new GUIContent(label), labelStyle, minContent, maxContent, unit, fullUnit, interval, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldsStyle, options);
		}

		// Interval Slider (label, labelStyle, minContent, maxContent, unit, fullUnit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.Interval IntervalSlider(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, Utility.Units unit, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			Utility.Interval newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(GUIContentWithMeasureTooltip(label, unit), labelStyle, flexibleSpace, 140f);

			float newIntervalMin = Slider(minContent, interval.Min, min, newInterval.Max, unit, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			float newIntervalMax = Slider(maxContent, interval.Max, newInterval.Min, max, unit, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.Interval IntervalSlider(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}
		public static Utility.Interval IntervalSlider(string label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Units unit, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, minContent, maxContent, unit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}
		// Interval Slider (label, labelStyle, minContent, maxContent, unit, fullUnit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.Interval IntervalSlider(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, string unit, string fullUnit, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			Utility.Interval newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(GUIContentWithMeasureTooltip(label, unit, fullUnit), labelStyle, flexibleSpace, 140f);

			float newIntervalMin = Slider(minContent, interval.Min, min, newInterval.Max, unit, fullUnit, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			float newIntervalMax = Slider(maxContent, interval.Max, newInterval.Min, max, unit, fullUnit, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.Interval IntervalSlider(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), unit, fullUnit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}
		public static Utility.Interval IntervalSlider(string label, GUIStyle labelStyle, string minContent, string maxContent, string unit, string fullUnit, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, minContent, maxContent, unit, fullUnit, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}
		// Interval Slider (label, labelStyle, minContent, maxContent, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.Interval IntervalSlider(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			Utility.Interval newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, labelStyle, flexibleSpace, 140f);

			float newIntervalMin = Slider(minContent, interval.Min, min, newInterval.Max, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			float newIntervalMax = Slider(maxContent, interval.Max, newInterval.Min, max, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.Interval IntervalSlider(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}
		public static Utility.Interval IntervalSlider(string label, GUIStyle labelStyle, string minContent, string maxContent, Utility.Interval interval, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, minContent, maxContent, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}

		// Interval Slider (label, labelStyle, minContent, maxContent, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, fieldStyle, options)
		public static Utility.IntervalInt IntervalSlider(GUIContent label, GUIStyle labelStyle, GUIContent minContent, GUIContent maxContent, Utility.IntervalInt interval, int min, int max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			Utility.IntervalInt newInterval = new(interval);
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			int orgIntentLevel = EditorGUI.indentLevel;

			IntervalFieldLabel(label, labelStyle, flexibleSpace, 140f);

			int newIntervalMin = Slider(minContent, interval.Min, min, newInterval.Max, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Min != newIntervalMin)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Min = newIntervalMin;
			}

			GUILayout.Space(15f);

			int newIntervalMax = Slider(maxContent, interval.Max, newInterval.Min, max, recordObjectUndo, undoName, setDirty, options);

			if (newInterval.Max != newIntervalMax)
			{
				newInterval.OverrideBorders = interval.OverrideBorders;
				newInterval.ClampToZero = interval.ClampToZero;
				newInterval.Max = newIntervalMax;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;
			EditorGUI.indentLevel = orgIntentLevel;

			EditorGUILayout.EndHorizontal();

			return newInterval;
		}
		public static Utility.IntervalInt IntervalSlider(GUIContent label, GUIStyle labelStyle, string minContent, string maxContent, Utility.IntervalInt interval, int min, int max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, new GUIContent(minContent), new GUIContent(maxContent), interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}
		public static Utility.IntervalInt IntervalSlider(string label, GUIStyle labelStyle, string minContent, string maxContent, Utility.IntervalInt interval, int min, int max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool flexibleSpace = false, bool setDirty = true, params GUILayoutOption[] options)
		{
			return IntervalSlider(new GUIContent(label), labelStyle, minContent, maxContent, interval, min, max, recordObjectUndo, undoName, flexibleSpace, setDirty, options);
		}

		// Number Field (label, value, unit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static float NumberField(GUIContent label, float value, Utility.Units unit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit);
			fieldStyle ??= EditorStyles.numberField;

			Utility.UnitType unitType = UnitTypeFromUnit(unit);
			float valueMultiplier = Utility.UnitMultiplier(unit, unitType);
			bool dividerUnit = unit == Utility.Units.FuelConsumption && unitType != Utility.UnitType.Metric;

			if (dividerUnit)
				value = valueMultiplier / value;
			else
				value *= valueMultiplier;

			float newValue = EditorGUILayout.FloatField(label, rounded ? math.round(value) : value, fieldStyle, options);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(Utility.Unit(unit, unitType), GUILayoutUtility.GetLastRect());

			if (dividerUnit)
				newValue = valueMultiplier / newValue;
			else
				newValue /= valueMultiplier;

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(string label, float value, Utility.Units unit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, unit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, unit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static float NumberField(Rect rect, GUIContent label, float value, Utility.Units unit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			label = GUIContentWithMeasureTooltip(label, unit);
			fieldStyle ??= EditorStyles.numberField;

			Utility.UnitType unitType = UnitTypeFromUnit(unit);
			float valueMultiplier = Utility.UnitMultiplier(unit, unitType);
			bool dividerUnit = unit == Utility.Units.FuelConsumption && unitType != Utility.UnitType.Metric;

			if (dividerUnit)
				value = valueMultiplier / value;
			else
				value *= valueMultiplier;

			float newValue = EditorGUI.FloatField(rect, label, rounded ? math.round(value) : value, fieldStyle);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(Utility.Unit(unit, unitType), GUILayoutUtility.GetLastRect());

			if (dividerUnit)
				newValue = valueMultiplier / newValue;
			else
				newValue /= valueMultiplier;

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(Rect rect, string label, float value, Utility.Units unit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, unit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle);
		}
		// Number Field (label, value, unit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static float NumberField(GUIContent label, float value, Utility.Units unit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit);
			fieldStyle ??= EditorStyles.numberField;

			Utility.UnitType unitType = UnitTypeFromUnit(unit);
			float valueMultiplier = Utility.UnitMultiplier(unit, unitType);
			bool dividerUnit = unit == Utility.Units.FuelConsumption && unitType != Utility.UnitType.Metric;

			if (dividerUnit)
				value = valueMultiplier / value;
			else
				value *= valueMultiplier;

			float newValue = EditorGUILayout.FloatField(label, Utility.Round(value, decimals), fieldStyle, options);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(Utility.Unit(unit, unitType), GUILayoutUtility.GetLastRect());

			if (dividerUnit)
				newValue = valueMultiplier / newValue;
			else
				newValue /= valueMultiplier;

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(string label, float value, Utility.Units unit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, unit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, unit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static float NumberField(Rect rect, GUIContent label, float value, Utility.Units unit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			label = GUIContentWithMeasureTooltip(label, unit);
			fieldStyle ??= EditorStyles.numberField;

			Utility.UnitType unitType = UnitTypeFromUnit(unit);
			float valueMultiplier = Utility.UnitMultiplier(unit, unitType);
			bool dividerUnit = unit == Utility.Units.FuelConsumption && unitType != Utility.UnitType.Metric;

			if (dividerUnit)
				value = valueMultiplier / value;
			else
				value *= valueMultiplier;

			float newValue = EditorGUI.FloatField(rect, label, Utility.Round(value, decimals), fieldStyle);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(Utility.Unit(unit, unitType), GUILayoutUtility.GetLastRect());

			if (dividerUnit)
				newValue = valueMultiplier / newValue;
			else
				newValue /= valueMultiplier;

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(Rect rect, string label, float value, Utility.Units unit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, unit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle);
		}
		// Number Field (label, value, unit, fullUnit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static float NumberField(GUIContent label, float value, string unit, string fullUnit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit, fullUnit);
			fieldStyle ??= EditorStyles.numberField;

			float newValue = EditorGUILayout.FloatField(label, rounded ? math.round(value) : value, fieldStyle, options);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(unit, GUILayoutUtility.GetLastRect());

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(string label, float value, string unit, string fullUnit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, unit, fullUnit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, unit, fullUnit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static float NumberField(Rect rect, GUIContent label, float value, string unit, string fullUnit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			label = GUIContentWithMeasureTooltip(label, unit, fullUnit);
			fieldStyle ??= EditorStyles.numberField;

			float newValue = EditorGUI.FloatField(rect, label, rounded ? math.round(value) : value, fieldStyle);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(unit, GUILayoutUtility.GetLastRect());

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(Rect rect, string label, float value, string unit, string fullUnit, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, unit, fullUnit, rounded, recordObjectUndo, undoName, setDirty, fieldStyle);
		}
		// Number Field (label, value, unit, fullUnit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static float NumberField(GUIContent label, float value, string unit, string fullUnit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit, fullUnit);
			fieldStyle ??= EditorStyles.numberField;

			float newValue = EditorGUILayout.FloatField(label, Utility.Round(value, decimals), fieldStyle, options);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(unit, GUILayoutUtility.GetLastRect());

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(string label, float value, string unit, string fullUnit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, unit, fullUnit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, unit, fullUnit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static float NumberField(Rect rect, GUIContent label, float value, string unit, string fullUnit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			label = GUIContentWithMeasureTooltip(label, unit, fullUnit);
			fieldStyle ??= EditorStyles.numberField;

			float newValue = EditorGUI.FloatField(rect, label, Utility.Round(value, decimals), fieldStyle);

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(unit, GUILayoutUtility.GetLastRect());

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(Rect rect, string label, float value, string unit, string fullUnit, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, unit, fullUnit, decimals, recordObjectUndo, undoName, setDirty, fieldStyle);
		}
		// Number Field (label, value, rounded, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static float NumberField(GUIContent label, float value, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return FloatFieldRecord(label, value, EditorGUILayout.FloatField(label, rounded ? math.round(value) : value, fieldStyle ?? EditorStyles.numberField, options), recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(string label, float value, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, rounded, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, rounded, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static float NumberField(Rect rect, GUIContent label, float value, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return FloatFieldRecord(label, value, EditorGUI.FloatField(rect, label, rounded ? math.round(value) : value, fieldStyle ?? EditorStyles.numberField), recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(Rect rect, string label, float value, bool rounded = false, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, rounded, recordObjectUndo, undoName, setDirty, fieldStyle);
		}
		// Number Field (label, value, decimals, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static float NumberField(GUIContent label, float value, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return FloatFieldRecord(label, value, EditorGUILayout.FloatField(label, Utility.Round(value, decimals), fieldStyle ?? EditorStyles.numberField, options), recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(string label, float value, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, decimals, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, decimals, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static float NumberField(Rect rect, GUIContent label, float value, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return FloatFieldRecord(label, value, EditorGUI.FloatField(rect, label, Utility.Round(value, decimals), fieldStyle ?? EditorStyles.numberField), recordObjectUndo, undoName, setDirty);
		}
		public static float NumberField(Rect rect, string label, float value, uint decimals, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, decimals, recordObjectUndo, undoName, setDirty, fieldStyle);
		}

		// Number Field (label, value, unit, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static int NumberField(GUIContent label, int value, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return IntFieldRecord(label, value, (int)NumberField(label, value, unit, true, recordObjectUndo, undoName, setDirty, fieldStyle, options), recordObjectUndo, undoName, setDirty);
		}
		public static int NumberField(string label, int value, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, unit, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, unit, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static int NumberField(Rect rect, GUIContent label, int value, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return IntFieldRecord(label, value, (int)NumberField(rect, label, value, unit, true, recordObjectUndo, undoName, setDirty, fieldStyle), recordObjectUndo, undoName, setDirty);
		}
		public static int NumberField(Rect rect, string label, int value, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, unit, recordObjectUndo, undoName, setDirty, fieldStyle);
		}
		// Number Field (label, value, unit, fullUnit, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static int NumberField(GUIContent label, int value, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return IntFieldRecord(label, value, (int)NumberField(label, value, unit, fullUnit, true, recordObjectUndo, undoName, setDirty, fieldStyle, options), recordObjectUndo, undoName, setDirty);
		}
		public static int NumberField(string label, int value, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, unit, fullUnit, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, unit, fullUnit, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static int NumberField(Rect rect, GUIContent label, int value, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return IntFieldRecord(label, value, (int)NumberField(rect, label, value, unit, fullUnit, true, recordObjectUndo, undoName, setDirty, fieldStyle), recordObjectUndo, undoName, setDirty);
		}
		public static int NumberField(Rect rect, string label, int value, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, unit, fullUnit, recordObjectUndo, undoName, setDirty, fieldStyle);
		}
		// Number Field (label, value, recordObjectUndo, undoName, setDirty, fieldStyle, options)
		public static int NumberField(GUIContent label, int value, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return IntFieldRecord(label, value, (int)NumberField(label, value, true, recordObjectUndo, undoName, setDirty, fieldStyle, options), recordObjectUndo, undoName, setDirty);
		}
		public static int NumberField(string label, int value, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null, params GUILayoutOption[] options)
		{
			return NumberField(new GUIContent(label), value, recordObjectUndo, undoName, setDirty, fieldStyle, options);
		}
		// Number Field (rect, label, value, recordObjectUndo, undoName, setDirty, fieldStyle)
		public static int NumberField(Rect rect, GUIContent label, int value, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return IntFieldRecord(label, value, (int)NumberField(rect, label, value, true, recordObjectUndo, undoName, setDirty, fieldStyle), recordObjectUndo, undoName, setDirty);
		}
		public static int NumberField(Rect rect, string label, int value, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, GUIStyle fieldStyle = null)
		{
			return NumberField(rect, new GUIContent(label), value, recordObjectUndo, undoName, setDirty, fieldStyle);
		}

		// Slider (label, value, min, max, unit, recordObjectUndo, undoName, setDirty, options)
		public static float Slider(GUIContent label, float value, float min, float max, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit);

			Utility.UnitType unitType = UnitTypeFromUnit(unit);
			float valueMultiplier = Utility.UnitMultiplier(unit, unitType);
			bool dividerUnit = unit == Utility.Units.FuelConsumption && unitType != Utility.UnitType.Metric;

			if (dividerUnit)
				value = valueMultiplier / value;
			else
				value *= valueMultiplier;

			EditorGUIUtility.fieldWidth += sliderWidthAddition;

			float newValue = EditorGUILayout.Slider(label, value, min, max, options);

			EditorGUIUtility.fieldWidth -= sliderWidthAddition;

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(Utility.Unit(unit, unitType), GUILayoutUtility.GetLastRect());

			if (dividerUnit)
				newValue = valueMultiplier / newValue;
			else
				newValue /= valueMultiplier;

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float Slider(string label, float value, float min, float max, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			return Slider(new GUIContent(label), value, min, max, unit, recordObjectUndo, undoName, setDirty, options);
		}
		// Slider (label, value, min, max, unit, fullUnit, recordObjectUndo, undoName, setDirty, options)
		public static float Slider(GUIContent label, float value, float min, float max, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit, fullUnit);

			EditorGUIUtility.fieldWidth += sliderWidthAddition;

			float newValue = EditorGUILayout.Slider(label, value, min, max, options);

			EditorGUIUtility.fieldWidth -= sliderWidthAddition;

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(unit, GUILayoutUtility.GetLastRect());

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float Slider(string label, float value, float min, float max, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			return Slider(new GUIContent(label), value, min, max, unit, fullUnit, recordObjectUndo, undoName, setDirty, options);
		}
		// Slider (label, value, min, max, recordObjectUndo, undoName, setDirty, options)
		public static float Slider(GUIContent label, float value, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			EditorGUIUtility.fieldWidth += sliderWidthAddition;

			float newValue = EditorGUILayout.Slider(label, value, min, max, options);

			EditorGUIUtility.fieldWidth -= sliderWidthAddition;

			return FloatFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static float Slider(string label, float value, float min, float max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			return Slider(new GUIContent(label), value, min, max, recordObjectUndo, undoName, setDirty, options);
		}

		// Slider (label, value, min, max, unit, recordObjectUndo, undoName, setDirty, options)
		public static int Slider(GUIContent label, int value, int min, int max, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit);

			Utility.UnitType unitType = UnitTypeFromUnit(unit);
			float valueMultiplier = Utility.UnitMultiplier(unit, unitType);
			bool dividerUnit = unit == Utility.Units.FuelConsumption && unitType != Utility.UnitType.Metric;
			float floatValue = value;

			if (dividerUnit)
				floatValue = valueMultiplier / floatValue;
			else
				floatValue *= valueMultiplier;

			EditorGUIUtility.fieldWidth += sliderWidthAddition;

			float newValue = EditorGUILayout.IntSlider(label, (int)floatValue, min, max, options);

			EditorGUIUtility.fieldWidth -= sliderWidthAddition;

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(Utility.Unit(unit, unitType), GUILayoutUtility.GetLastRect());

			if (dividerUnit)
				newValue = valueMultiplier / newValue;
			else
				newValue /= valueMultiplier;

			return IntFieldRecord(label, value, (int)newValue, recordObjectUndo, undoName, setDirty);
		}
		public static int Slider(string label, int value, int min, int max, Utility.Units unit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			return Slider(new GUIContent(label), value, min, max, unit, recordObjectUndo, undoName, setDirty, options);
		}
		// Slider (label, value, min, max, unit, fullUnit, recordObjectUndo, undoName, setDirty, options)
		public static int Slider(GUIContent label, int value, int min, int max, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			label = GUIContentWithMeasureTooltip(label, unit, fullUnit);

			EditorGUIUtility.fieldWidth += sliderWidthAddition;

			int newValue = EditorGUILayout.IntSlider(label, value, min, max, options);

			EditorGUIUtility.fieldWidth -= sliderWidthAddition;

			if (math.abs(newValue) != math.INFINITY)
				DrawUnitText(unit, GUILayoutUtility.GetLastRect());

			return IntFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static int Slider(string label, int value, int min, int max, string unit, string fullUnit, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			return Slider(new GUIContent(label), value, min, max, unit, fullUnit, recordObjectUndo, undoName, setDirty, options);
		}
		// Slider (label, value, min, max, recordObjectUndo, undoName, setDirty, options)
		public static int Slider(GUIContent label, int value, int min, int max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			EditorGUIUtility.fieldWidth += sliderWidthAddition;

			int newValue = EditorGUILayout.IntSlider(label, value, min, max, options);

			EditorGUIUtility.fieldWidth -= sliderWidthAddition;

			return IntFieldRecord(label, value, newValue, recordObjectUndo, undoName, setDirty);
		}
		public static int Slider(string label, int value, int min, int max, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true, params GUILayoutOption[] options)
		{
			return Slider(new GUIContent(label), value, min, max, recordObjectUndo, undoName, setDirty, options);
		}

		public static void ReorderableList(string label, SerializedProperty property, ref ReorderableList list, bool displayHeaderLabel = true, GUIStyle labelStyle = null)
		{
			ReorderableList(new GUIContent(label), property, ref list, displayHeaderLabel, labelStyle);
		}
		public static void ReorderableList(GUIContent label, SerializedProperty property, ref ReorderableList list, bool displayHeaderLabel = true, GUIStyle labelStyle = null)
		{
			void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				SerializedProperty elementProperty = property.GetArrayElementAtIndex(index);

				EditorGUI.indentLevel++;

				EditorGUI.PropertyField(rect, elementProperty, true);

				EditorGUI.indentLevel--;
			}

			list.drawElementCallback = DrawListElement;

			if (displayHeaderLabel)
				EditorGUILayout.LabelField(label, labelStyle ?? EditorStyles.boldLabel);

			list.DoLayoutList();
		}

		internal static void SelectObject(GameObject gameObject)
		{
			Selection.activeGameObject = gameObject;

			FocusInspectorWindow();
		}
		internal static void SelectObject(Object @object)
		{
			Selection.activeObject = @object;

			FocusInspectorWindow();
		}
		internal static void FocusSceneView()
		{
			FocusWindowUsingTypeName("UnityEditor.SceneView");
		}
		internal static void FocusInspectorWindow()
		{
			FocusWindowUsingTypeName("UnityEditor.InspectorWindow");
		}

		private static void FocusWindowUsingTypeName(string typeName)
		{
			Type windowType = typeof(UnityEditor.Editor).Assembly.GetType(typeName);

			EditorWindow focusedWindow = EditorWindow.focusedWindow;

			EditorWindow.FocusWindowIfItsOpen(windowType);

			if (focusedWindow != EditorWindow.focusedWindow)
				EditorWindow.GetWindow(windowType);
		}
		private static float FloatFieldRecord(GUIContent label, float value, float newValue, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true)
		{
			if (recordObjectUndo && (math.abs(value) != math.INFINITY && math.abs(newValue) != math.INFINITY && !Mathf.Approximately(value, newValue) || (math.abs(value) == math.INFINITY || math.abs(newValue) == math.INFINITY) && value != newValue))
			{
				RecordNumber(label, recordObjectUndo, undoName);

				if (setDirty)
					EditorUtility.SetDirty(recordObjectUndo);
			}

			return newValue;
		}
		private static int IntFieldRecord(GUIContent label, int value, int newValue, Object recordObjectUndo = null, string undoName = Utility.emptyString, bool setDirty = true)
		{
			if (recordObjectUndo && value != newValue)
			{
				RecordNumber(label, recordObjectUndo, undoName);

				if (setDirty)
					EditorUtility.SetDirty(recordObjectUndo);
			}

			return newValue;
		}
		private static void IntervalFieldLabel(GUIContent label, Utility.Units unit, GUIStyle labelStyle, bool flexibleSpace, float labelWidth = 75f)
		{
			IntervalFieldLabel(GUIContentWithMeasureTooltip(label, unit), labelStyle, flexibleSpace, labelWidth);
		}
		private static void IntervalFieldLabel(GUIContent label, string unit, string fullUnit, GUIStyle labelStyle, bool flexibleSpace, float labelWidth = 75f)
		{
			IntervalFieldLabel(GUIContentWithMeasureTooltip(label, unit, fullUnit), labelStyle, flexibleSpace, labelWidth);
		}
		private static void IntervalFieldLabel(GUIContent label, GUIStyle labelStyle, bool flexibleSpace, float labelWidth = 75f)
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUIUtility.labelWidth = labelWidth;

			if (labelStyle != null)
				EditorGUILayout.LabelField(label, labelStyle);
			else
				EditorGUILayout.LabelField(label);

			EditorGUIUtility.labelWidth = 30f;

			if (flexibleSpace)
				GUILayout.FlexibleSpace();

			EditorGUI.indentLevel = default;
		}
		private static void RecordNumber(GUIContent label, Object recordObjectUndo, string undoName)
		{
			RecordObject(label, recordObjectUndo, undoName, "Change", "Number");
		}
		private static void RecordToggle(GUIContent label, Object recordObjectUndo, string undoName)
		{
			RecordObject(label, recordObjectUndo, undoName, "Switch", "Toggle");
		}
		private static void RecordEnum(GUIContent label, Object recordObjectUndo, string undoName)
		{
			RecordObject(label, recordObjectUndo, undoName, "Change", "Enum");
		}
		private static void RecordObject(GUIContent label, Object recordObjectUndo, string undoName, string action, string parameterType)
		{
			Undo.RegisterCompleteObjectUndo(recordObjectUndo, undoName.IsNullOrEmpty() ? $"{action} {(label.text.IsNullOrEmpty() ? parameterType : label.text)}" : undoName);
		}
		private static GUIContent GUIContentWithMeasureTooltip(GUIContent content, Utility.Units unit)
		{
			if (!content.tooltip.IsNullOrEmpty())
				content.tooltip += $". {MeasuredTooltip(unit)}";

			return content;
		}
		private static GUIContent GUIContentWithMeasureTooltip(GUIContent content, string unit, string fullUnit)
		{
			if (content.tooltip.IsNullOrEmpty())
				content.tooltip = MeasuredTooltip(unit, fullUnit);
			else if (!unit.IsNullOrEmpty() && !fullUnit.IsNullOrEmpty())
				content.tooltip += $". {MeasuredTooltip(unit, fullUnit)}";

			return content;
		}
		private static void DrawUnitText(string unit, Rect fieldRect)
		{
			if (unit.IsNullOrEmpty())
				return;

			GUIContent unitText = new(unit);
			float unitTextWidth = NumberFieldUnitText.CalcSize(unitText).x;
			Rect unitTextRect = fieldRect;

			unitTextRect.x += unitTextRect.width - 5f - unitTextWidth - EditorGUI.indentLevel * 15f;
			unitTextRect.width = unitTextWidth;

			EditorGUI.LabelField(unitTextRect, unitText, NumberFieldUnitText);
		}
		private static string MeasuredTooltip(Utility.Units unit)
		{
			Utility.UnitType unitType = UnitTypeFromUnit(unit);

			return $"Measured in {Utility.FullUnit(unit, unitType)} ({Utility.Unit(unit, unitType)})";
		}
		private static string MeasuredTooltip(string unit, string fullUnit)
		{
			if (unit.IsNullOrEmpty() || fullUnit.IsNullOrEmpty())
				return string.Empty;

			return $"Measured in {fullUnit} ({unit})";
		}
		private static Utility.UnitType UnitTypeFromUnit(Utility.Units unit)
		{
			return unit switch
			{
				Utility.Units.Torque => Settings.editorTorqueUnit,
				Utility.Units.Power => Settings.editorPowerUnit,
				_ => Settings.editorValuesUnit
			};
		}

		#endregion
	}
}
