#region Namespaces

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;
using Utilities;
using Utilities.Inputs.Mobile;
using MVC.Core;
using MVC.Utilities;

#endregion

namespace MVC.UI
{
	[AddComponentMenu("Multiversal Vehicle Controller/Core/UI Controller", 20)]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(200)]
	[RequireComponent(typeof(VehicleManager))]
	public class VehicleUIController : ToolkitBehaviour
	{
		#region Modules

		[Serializable]
		public class SpeedMeter
		{
			#region Enumerators

			public enum Type { Digital, Radial }

			#endregion

			#region Modules

			[Serializable]
			public class NeedleModule
			{
				#region Variables

				public Type type;
				public Image Needle
				{
					get
					{
						if (!group || needle && !needle.transform.IsChildOf(group))
							return null;

						return needle;
					}
					set
					{
						if (!group || value && !value.transform.IsChildOf(group))
							return;
						
						needle = value;
					}
				}
				public float Fill
				{
					get
					{
						return fill;
					}
					set
					{
						fill = Utility.Clamp01(value);
					}
				}
				public float Offset
				{
					get
					{
						return offset;
					}
					set
					{
						offset = Utility.Clamp01(value);
					}
				}
				public bool IsValid
				{
					get
					{
						return group && (!Needle || Needle && Needle.transform.IsChildOf(group));
					}
				}
				public bool InvertRotation
				{
					get
					{
						if (type != Type.Radial)
							return false;

						return invertRotation;
					}
					set
					{
						if (type != Type.Radial)
							return;

						invertRotation = value;
					}
				}

				[SerializeField]
				private Image needle;
				[SerializeField]
				private float fill = 1f;
				[SerializeField]
				private float offset;
				[SerializeField]
				private bool invertRotation;
				[SerializeField]
				private RectTransform group;

				#endregion

				#region Constructors

				public NeedleModule (RectTransform group)
				{
					this.group = group;
				}

				#endregion

				#region Operators

				public static implicit operator bool(NeedleModule module) => module != null;

				#endregion
			}
			[Serializable]
			public class SpriteModule
			{
				#region Enumerators

				public enum SpriteType { Image, Text }

				#endregion

				#region Variables

				public SpriteType type;
				public Image Image
				{
					get
					{
						if (!group || type != SpriteType.Image || image && !image.transform.IsChildOf(group))
							return null;

						return image;
					}
					set
					{
						if (!group || type != SpriteType.Image || value && !value.transform.IsChildOf(group))
							return;

						image = value;
					}
				}
				public TextMeshProUGUI Text
				{
					get
					{
						if (!group || type != SpriteType.Text || text && !text.transform.IsChildOf(group))
							return null;

						return text;
					}
					set
					{
						if (!group || type != SpriteType.Text || value && !value.transform.IsChildOf(group))
							return;

						text = value;
					}
				}
				public Color NormalColor
				{
					get
					{
						if (!Image && !Text)
							return Color.clear;

						return normalColor;
					}
					set
					{
						if (!Image && !Text)
							return;

						value.a = 1f;
						normalColor = value;

						if (!Application.isPlaying)
						{
							if (Image)
								Image.color = normalColor;
							else
								Text.color = normalColor;
						}
					}
				}
				public Color ActiveColor
				{
					get
					{
						if (!Image && !Text)
							return Color.clear;

						return activeColor;
					}
					set
					{
						if (!Image && !Text)
							return;

						value.a = 1f;
						activeColor = value;
					}
				}
				public Color DisabledColor
				{
					get
					{
						if (!Image && !Text)
							return Color.clear;

						return disabledColor;
					}
					set
					{
						if (!Image && !Text)
							return;

						value.a = 1f;
						disabledColor = value;
					}
				}
				public bool IsValid => group && (!Image && !Text || Image && Image.transform.IsChildOf(group) || Text && Text.transform.IsChildOf(group));

				[SerializeField]
				private Image image;
				[SerializeField]
				private TextMeshProUGUI text;
				[SerializeField]
				private RectTransform group;
				[SerializeField]
				private Color normalColor = Color.white;
				[SerializeField]
				private Color activeColor = Color.red;
				[SerializeField]
				private Color disabledColor = Color.grey;

				#endregion

				#region Constructors

				public SpriteModule(RectTransform group)
				{
					this.group = group;
				}

				#endregion

				#region Operators

				public static implicit operator bool(SpriteModule module) => module != null;

				#endregion
			}
			[Serializable]
			public class TextModule
			{
				#region Variables

				public TextMeshProUGUI Text
				{
					get
					{
						if (!group || text && !text.transform.IsChildOf(group))
							return null;

						return text;
					}
					set
					{
						if (!group || value && !value.transform.IsChildOf(group))
							return;

						text = value;
					}
				}
				public string placeholder;
				public bool IsValid
				{
					get
					{
						return group && (!Text || Text && Text.transform.IsChildOf(group));
					}
				}

				[SerializeField]
				private TextMeshProUGUI text;
				[SerializeField]
				private RectTransform group;

				#endregion

				#region Constructors

				public TextModule(RectTransform group, string placeholder)
				{
					this.group = group;
					this.placeholder = placeholder;
				}

				#endregion

				#region Operators

				public static implicit operator bool(TextModule module) => module != null;

				#endregion
			}

			#endregion

			#region Variables

			public RectTransform group;
			public NeedleModule RPMNeedle
			{
				get
				{
					if (!group)
						return null;

					if (!m_RPMNeedle || !m_RPMNeedle.IsValid)
						m_RPMNeedle = new(group);

					return m_RPMNeedle;
				}
				set
				{
					if (!group || !value.IsValid || value.Needle && !value.Needle.transform.IsChildOf(group))
						return;

					m_RPMNeedle = value;
				}
			}
			public NeedleModule RPMOverRevNeedle
			{
				get
				{
					if (!group)
						return null;

					if (!m_RPMOverRevNeedle || !m_RPMOverRevNeedle.IsValid)
						m_RPMOverRevNeedle = new(group);

					return m_RPMOverRevNeedle;
				}
				set
				{
					if (!group || !value.IsValid || value.Needle && !value.Needle.transform.IsChildOf(group))
						return;

					m_RPMOverRevNeedle = value;
				}
			}
			public NeedleModule NOSNeedle
			{
				get
				{
					if (!group)
						return null;

					if (!m_NOSNeedle || !m_NOSNeedle.IsValid)
						m_NOSNeedle = new(group);

					return m_NOSNeedle;
				}
				set
				{
					if (!group || !value.IsValid || value.Needle && !value.Needle.transform.IsChildOf(group))
						return;

					m_NOSNeedle = value;
				}
			}
			public NeedleModule SpeedNeedle
			{
				get
				{
					if (!group)
						return null;

					if (!speedNeedle || !speedNeedle.IsValid)
						speedNeedle = new(group);

					return speedNeedle;
				}
				set
				{
					if (!group || !value.IsValid || value.Needle && !value.Needle.transform.IsChildOf(group))
						return;

					speedNeedle = value;
				}
			}
			public NeedleModule BoostNeedle
			{
				get
				{
					if (!group)
						return null;

					if (!boostNeedle || !boostNeedle.IsValid)
						boostNeedle = new(group);

					return boostNeedle;
				}
				set
				{
					if (!group || !value.IsValid || value.Needle && !value.Needle.transform.IsChildOf(group))
						return;

					boostNeedle = value;
				}
			}
			public SpriteModule ABSSprite
			{
				get
				{
					if (!group)
						return null;

					if (!m_ABSSprite || !m_ABSSprite.IsValid)
						m_ABSSprite = new(group);

					return m_ABSSprite;
				}
				set
				{
					if (!group || !value.IsValid || value.Image && !value.Image.transform.IsChildOf(group) || value.Text && !value.Text.transform.IsChildOf(group))
						return;

					m_ABSSprite = value;
				}
			}
			public SpriteModule ESPSprite
			{
				get
				{
					if (!group)
						return null;

					if (!m_ESPSprite || !m_ESPSprite.IsValid)
						m_ESPSprite = new(group);

					return m_ESPSprite;
				}
				set
				{
					if (!group || !value.IsValid || value.Image && !value.Image.transform.IsChildOf(group) || value.Text && !value.Text.transform.IsChildOf(group))
						return;

					m_ESPSprite = value;
				}
			}
			public SpriteModule TCSSprite
			{
				get
				{
					if (!group)
						return null;

					if (!m_TCSSprite || !m_TCSSprite.IsValid)
						m_TCSSprite = new(group);

					return m_TCSSprite;
				}
				set
				{
					if (!group || !value.IsValid || value.Image && !value.Image.transform.IsChildOf(group) || value.Text && !value.Text.transform.IsChildOf(group))
						return;

					m_TCSSprite = value;
				}
			}
			public SpriteModule HandbrakeSprite
			{
				get
				{
					if (!group)
						return null;

					if (!handbrakeSprite || !handbrakeSprite.IsValid)
						handbrakeSprite = new(group);

					return handbrakeSprite;
				}
				set
				{
					if (!group || !value.IsValid || value.Image && !value.Image.transform.IsChildOf(group) || value.Text && !value.Text.transform.IsChildOf(group))
						return;

					handbrakeSprite = value;
				}
			}
			public SpriteModule LeftSideSignalLightSprite
			{
				get
				{
					if (!group)
						return null;

					if (!leftIndicatorSprite || !leftIndicatorSprite.IsValid)
						leftIndicatorSprite = new(group);

					return leftIndicatorSprite;
				}
				set
				{
					if (!group || !value.IsValid || value.Image && !value.Image.transform.IsChildOf(group) || value.Text && !value.Text.transform.IsChildOf(group))
						return;

					leftIndicatorSprite = value;
				}
			}
			public SpriteModule RightSideSignalLightSprite
			{
				get
				{
					if (!group)
						return null;

					if (!rightIndicatorSprite || !rightIndicatorSprite.IsValid)
						rightIndicatorSprite = new(group);

					return rightIndicatorSprite;
				}
				set
				{
					if (!group || !value.IsValid || value.Image && !value.Image.transform.IsChildOf(group) || value.Text && !value.Text.transform.IsChildOf(group))
						return;

					rightIndicatorSprite = value;
				}
			}
			public TextModule RPMText
			{
				get
				{
					if (!group)
						return null;

					if (!m_RPMText || !m_RPMText.IsValid)
						m_RPMText = new(group, "0000");

					return m_RPMText;
				}
				set
				{
					if (!group || !value.Text.transform.IsChildOf(group))
						return;

					m_RPMText = value;
				}
			}
			public TextModule SpeedText
			{
				get
				{
					if (!group)
						return null;

					if (!speedText || !speedText.IsValid)
						speedText = new(group, "000");

					return speedText;
				}
				set
				{
					if (!group || !value.Text.transform.IsChildOf(group))
						return;

					speedText = value;
				}
			}
			public TextModule GearText
			{
				get
				{
					if (!group)
						return null;

					if (!gearText || !gearText.IsValid)
						gearText = new(group, "");

					return gearText;
				}
				set
				{
					if (!group || !value.Text.transform.IsChildOf(group))
						return;

					gearText = value;
				}
			}

			[SerializeField]
			private NeedleModule m_RPMNeedle;
			[SerializeField]
			private NeedleModule m_RPMOverRevNeedle;
			[SerializeField]
			private NeedleModule m_NOSNeedle;
			[SerializeField]
			private NeedleModule speedNeedle;
			[SerializeField]
			private NeedleModule boostNeedle;
			[SerializeField]
			private SpriteModule m_ABSSprite;
			[SerializeField]
			private SpriteModule m_ESPSprite;
			[SerializeField]
			private SpriteModule m_TCSSprite;
			[SerializeField]
			private SpriteModule handbrakeSprite;
			[SerializeField]
			private SpriteModule leftIndicatorSprite;
			[SerializeField]
			private SpriteModule rightIndicatorSprite;
			[SerializeField]
			private TextModule speedText;
			[SerializeField]
			private TextModule m_RPMText;
			[SerializeField]
			private TextModule gearText;
			private float fillFactor;
			private int index = -1;

			#endregion

			#region Methods

			public void Update()
			{
				if (HasInternalErrors || !UIController)
					return;

				if (!group || UIController.SpeedMeters == null || UIController.SpeedMeters.Length < 1)
					return;

				if (index < 0)
				{
					index = UIController.SpeedMeters.ToList().IndexOf(this);

					if (index < 0)
						return;
				}

				if (UIController.ActiveSpeedMeter != index && UIController.SpeedMeters.Length > 1)
				{
					group.gameObject.SetActive(false);

					return;
				}
				else if (!group.gameObject.activeSelf)
					group.gameObject.SetActive(true);

				if (RPMNeedle.Needle)
				{
					fillFactor = VehicleInstance ? VehicleInstance.Stats.engineRPM / VehicleInstance.Engine.MaximumRPM : 0f;
					fillFactor = RPMNeedle.Offset + (RPMNeedle.InvertRotation ? -1f : 1f) * fillFactor * RPMNeedle.Fill;

					if (RPMNeedle.type == Type.Digital)
						RPMNeedle.Needle.fillAmount = Utility.Clamp01(fillFactor);
					else
						RPMNeedle.Needle.rectTransform.localEulerAngles = 360f * fillFactor * Vector3.forward;
				}

				if (RPMOverRevNeedle.Needle)
				{
					fillFactor = VehicleInstance && VehicleInstance.Stats.isEngineRunning ? 1f - (VehicleInstance.Engine.RedlineRPM / VehicleInstance.Engine.MaximumRPM) : 0f;
					fillFactor = RPMOverRevNeedle.Offset + (RPMOverRevNeedle.InvertRotation ? -1f : 1f) * fillFactor * RPMOverRevNeedle.Fill;

					if (RPMOverRevNeedle.type == Type.Digital)
						RPMOverRevNeedle.Needle.fillAmount = Utility.Lerp(RPMOverRevNeedle.Needle.fillAmount, fillFactor, Time.deltaTime * 3f);
					else
						RPMNeedle.Needle.rectTransform.localEulerAngles = Vector3.forward * Mathf.LerpAngle(RPMNeedle.Needle.rectTransform.localEulerAngles.z, 360f * fillFactor, Time.deltaTime * 3f);
				}

				if (RPMText.Text)
					RPMText.Text.text = $"{math.round(VehicleInstance ? VehicleInstance.Stats.engineRPM : 0f).ToString(RPMText.placeholder)} RPM";

				if (NOSNeedle.Needle)
				{
					fillFactor = VehicleInstance ? VehicleInstance.Stats.NOS / VehicleInstance.Behaviour.NOSBottlesCount / VehicleInstance.Behaviour.NOSCapacity : 0f;
					fillFactor = NOSNeedle.Offset + (NOSNeedle.InvertRotation ? -1f : 1f) * fillFactor * NOSNeedle.Fill;

					if (NOSNeedle.type == Type.Digital)
						NOSNeedle.Needle.fillAmount = Utility.Clamp01(fillFactor);
					else
						NOSNeedle.Needle.rectTransform.localEulerAngles = 360f * fillFactor * Vector3.forward;
				}

				if (SpeedNeedle.Needle)
				{
					fillFactor = VehicleInstance ? VehicleInstance.Stats.currentSpeed / VehicleInstance.TopSpeed : 0f;
					fillFactor = SpeedNeedle.Offset + (SpeedNeedle.InvertRotation ? -1f : 1f) * fillFactor * SpeedNeedle.Fill;

					if (SpeedNeedle.type == Type.Digital)
						SpeedNeedle.Needle.fillAmount = Utility.Clamp01(fillFactor);
					else
						SpeedNeedle.Needle.rectTransform.localEulerAngles = 360f * fillFactor * Vector3.forward;
				}

				if (SpeedText.Text)
					SpeedText.Text.text = $"{math.round(VehicleInstance ? VehicleInstance.Stats.currentSpeed * Utility.UnitMultiplier(Utility.Units.Speed, Settings.valuesUnit) : 0f).ToString(SpeedText.placeholder)} {Utility.Unit(Utility.Units.Speed, Settings.valuesUnit)}";

				if (BoostNeedle.Needle)
				{
					fillFactor = VehicleInstance ? VehicleInstance.Stats.engineBoost : 0f;
					fillFactor = BoostNeedle.Offset + (BoostNeedle.InvertRotation ? -1f : 1f) * fillFactor * BoostNeedle.Fill;

					if (BoostNeedle.type == Type.Digital)
						BoostNeedle.Needle.fillAmount = Utility.Clamp01(fillFactor);
					else
						BoostNeedle.Needle.rectTransform.localEulerAngles = 360f * fillFactor * Vector3.forward;
				}

				if (GearText.Text)
					GearText.Text.text = VehicleInstance ? VehicleInstance.CurrentGearToString : "N";

				if (ABSSprite.Image || ABSSprite.Text)
					switch (ABSSprite.type)
					{
						case SpriteModule.SpriteType.Image:
							ABSSprite.Image.color = VehicleInstance.Stability.useABS ? VehicleInstance.Stats.isABSActive ? ABSSprite.ActiveColor : ABSSprite.NormalColor : ABSSprite.DisabledColor;

							break;

						case SpriteModule.SpriteType.Text:
							ABSSprite.Text.color = VehicleInstance.Stability.useABS ? VehicleInstance.Stats.isABSActive ? ABSSprite.ActiveColor : ABSSprite.NormalColor : ABSSprite.DisabledColor;

							break;
					}

				if (ESPSprite.Image || ESPSprite.Text)
					switch (ESPSprite.type)
					{
						case SpriteModule.SpriteType.Image:
							ESPSprite.Image.color = VehicleInstance.Stability.useESP ? VehicleInstance.Stats.isESPActive ? ESPSprite.ActiveColor : ESPSprite.NormalColor : ESPSprite.DisabledColor;

							break;

						case SpriteModule.SpriteType.Text:
							ESPSprite.Text.color = VehicleInstance.Stability.useESP ? VehicleInstance.Stats.isESPActive ? ESPSprite.ActiveColor : ESPSprite.NormalColor : ESPSprite.DisabledColor;

							break;
					}

				if (TCSSprite.Image || TCSSprite.Text)
					switch (TCSSprite.type)
					{
						case SpriteModule.SpriteType.Image:
							TCSSprite.Image.color = VehicleInstance.Stability.useTCS ? VehicleInstance.Stats.isTCSActive || VehicleInstance.Stats.isLaunchControlActive ? TCSSprite.ActiveColor : TCSSprite.NormalColor : TCSSprite.DisabledColor;

							break;

						case SpriteModule.SpriteType.Text:
							TCSSprite.Text.color = VehicleInstance.Stability.useTCS ? VehicleInstance.Stats.isTCSActive || VehicleInstance.Stats.isLaunchControlActive ? TCSSprite.ActiveColor : TCSSprite.NormalColor : TCSSprite.DisabledColor;

							break;
					}

				if (HandbrakeSprite.Image || HandbrakeSprite.Text)
					switch (HandbrakeSprite.type)
					{
						case SpriteModule.SpriteType.Image:
							HandbrakeSprite.Image.color = VehicleInstance.Inputs.Handbrake > 0f ? HandbrakeSprite.ActiveColor : HandbrakeSprite.NormalColor;

							break;

						case SpriteModule.SpriteType.Text:
							HandbrakeSprite.Text.color = VehicleInstance.Inputs.Handbrake > 0f ? HandbrakeSprite.ActiveColor : HandbrakeSprite.NormalColor;

							break;
					}

				if (LeftSideSignalLightSprite.Image || LeftSideSignalLightSprite.Text)
					switch (LeftSideSignalLightSprite.type)
					{
						case SpriteModule.SpriteType.Image:
							LeftSideSignalLightSprite.Image.color = VehicleInstance.Stats.isSignalLeftLightsOn ? LeftSideSignalLightSprite.ActiveColor : LeftSideSignalLightSprite.NormalColor;

							break;

						case SpriteModule.SpriteType.Text:
							LeftSideSignalLightSprite.Text.color = VehicleInstance.Stats.isSignalLeftLightsOn ? LeftSideSignalLightSprite.ActiveColor : LeftSideSignalLightSprite.NormalColor;

							break;
					}

				if (RightSideSignalLightSprite.Image || RightSideSignalLightSprite.Text)
					switch (RightSideSignalLightSprite.type)
					{
						case SpriteModule.SpriteType.Image:
							RightSideSignalLightSprite.Image.color = VehicleInstance.Stats.isSignalRightLightsOn ? RightSideSignalLightSprite.ActiveColor : RightSideSignalLightSprite.NormalColor;

							break;

						case SpriteModule.SpriteType.Text:
							RightSideSignalLightSprite.Text.color = VehicleInstance.Stats.isSignalRightLightsOn ? RightSideSignalLightSprite.ActiveColor : RightSideSignalLightSprite.NormalColor;

							break;
					}
			}

			#endregion
		}
		[Serializable]
		public class MobileInputPreset
		{
			#region Enumerators

			public enum InputType { Touch, Sensor }
			public enum JoystickAxis { None, Vertical, Horizontal }
			public enum AxisPolarity { Positive, Negative }
			public enum SensorType { None, Accelerometer, Gyroscope, GravitySensor, AttitudeSensor, LinearAccelerationSensor }
			public enum SensorAxis { None, X, Y, Z }

			#endregion

			#region Modules

			[Serializable]
			public class MobileInput
			{
				#region Variables

				#region Editor Variables

				[NonSerialized]
				public bool editorFoldout;

				#endregion

				#region Global Variables

				public InputType Type
				{
					get
					{
						return type;
					}
				}
				public SensorType SensorType
				{
					get
					{
						if (Type != InputType.Sensor)
							return SensorType.None;

						return sensorType;
					}
					set
					{
						if (Type != InputType.Sensor)
							return;

						sensorType = value;
					}
				}
				public Joystick Source
				{
					get
					{
						if (Type != InputType.Touch)
							return null;

						return source;
					}
				}
				public JoystickAxis JoystickAxis
				{
					get
					{
						if (!Source || Source.type == JoystickType.Button)
							return JoystickAxis.None;

						return joystickAxis;
					}
					set
					{
						if (!Source || Source.type == JoystickType.Button)
							return;

						joystickAxis = value;
					}
				}
				public SensorAxis SensorAxis
				{
					get
					{
						if (Type != InputType.Sensor)
							return SensorAxis.None;

						return sensorAxis;
					}
					set
					{
						if (Type != InputType.Sensor)
							return;

						sensorAxis = value;
					}
				}
				public AxisPolarity AxisPolarityTarget
				{
					get
					{
						if (Type == InputType.Touch && (!Source || Source.type == JoystickType.Button))
							return AxisPolarity.Positive;

						return axisPolarityTarget;
					}
					set
					{
						if (Type == InputType.Touch && (!Source || Source.type == JoystickType.Button))
							return;

						axisPolarityTarget = value;
					}
				}
				public bool IsValid
				{
					get
					{
						return Type == InputType.Touch && Source || Type == InputType.Sensor;
					}
				}
				public float Value
				{
					get
					{
						switch (Type)
						{
							case InputType.Sensor:
								return SensorType switch
								{
									SensorType.Accelerometer => 0f,
									SensorType.Gyroscope => 0f,
									SensorType.GravitySensor => 0f,
									SensorType.AttitudeSensor => 0f,
									SensorType.LinearAccelerationSensor => GetSensorValue(Vector3.zero, SensorAxis, AxisPolarityTarget),
									_ => 0f,
								};

							default:
								if (!Source)
									return 0f;

								return Source.type switch
								{
									JoystickType.Handle => JoystickAxis switch
									{
										JoystickAxis.Vertical => Utility.Clamp01((AxisPolarityTarget == AxisPolarity.Positive ? 1f : -1f) * Source.Vertical),
										JoystickAxis.Horizontal => Utility.Clamp01((AxisPolarityTarget == AxisPolarity.Positive ? 1f : -1f) * Source.Horizontal),
										_ => 0f,
									},
									_ => Source.PressValue,
								};
						}
					}
				}

				[SerializeField]
				private InputType type;
				[SerializeField]
				private SensorType sensorType;
				[SerializeField]
				private Joystick source;
				[SerializeField]
				private JoystickAxis joystickAxis;
				[SerializeField]
				private SensorAxis sensorAxis;
				[SerializeField]
				private AxisPolarity axisPolarityTarget;

				#endregion

				#endregion

				#region Constructors

				public MobileInput(InputType type, Joystick source)
				{
					this.type = type;
					this.source = type == InputType.Touch ? source : null;
				}

				#endregion

				#region Operators

				public static implicit operator bool(MobileInput input) => input != null;

				#endregion
			}

			#endregion

			#region Variables

			public RectTransform group;
			public MobileInput SteeringWheelLeft
			{
				get
				{
					if (!steeringWheelLeft)
						steeringWheelLeft = new(InputType.Touch, null);

					return steeringWheelLeft;
				}
				set
				{
					steeringWheelLeft = value;
				}
			}
			public MobileInput SteeringWheelRight
			{
				get
				{
					if (!steeringWheelRight)
						steeringWheelRight = new(InputType.Touch, null);

					return steeringWheelRight;
				}
				set
				{
					steeringWheelRight = value;
				}
			}
			public MobileInput FuelPedal
			{
				get
				{
					if (!fuelPedal)
						fuelPedal = new(InputType.Touch, null);

					return fuelPedal;
				}
				set
				{
					fuelPedal = value;
				}
			}
			public MobileInput BrakePedal
			{
				get
				{
					if (!brakePedal)
						brakePedal = new(InputType.Touch, null);

					return brakePedal;
				}
				set
				{
					brakePedal = value;
				}
			}
			public MobileInput ClutchPedal
			{
				get
				{
					if (!clutchPedal)
						clutchPedal = new(InputType.Touch, null);

					return clutchPedal;
				}
				set
				{
					clutchPedal = value;
				}
			}
			public MobileInput Handbrake
			{
				get
				{
					if (!handbrake)
						handbrake = new(InputType.Touch, null);

					return handbrake;
				}
				set
				{
					handbrake = value;
				}
			}
			public MobileInput GearShiftUp
			{
				get
				{
					if (!gearShiftUp || gearShiftUp.Type == InputType.Touch && gearShiftUp.Source && gearShiftUp.Source.type != JoystickType.Button)
						gearShiftUp = new(InputType.Touch, null);

					return gearShiftUp;
				}
				set
				{
					gearShiftUp = value;
				}
			}
			public MobileInput GearShiftDown
			{
				get
				{
					if (!gearShiftDown || gearShiftDown.Type == InputType.Touch && gearShiftDown.Source && gearShiftDown.Source.type != JoystickType.Button)
						gearShiftDown = new(InputType.Touch, null);

					return gearShiftDown;
				}
				set
				{
					gearShiftDown = value;
				}
			}
			public MobileInput EngineStartSwitch
			{
				get
				{
					if (!engineStartSwitch || engineStartSwitch.Type == InputType.Touch && engineStartSwitch.Source && engineStartSwitch.Source.type != JoystickType.Button)
						engineStartSwitch = new(InputType.Touch, null);

					return engineStartSwitch;
				}
				set
				{
					engineStartSwitch = value;
				}
			}
			public MobileInput NOS
			{
				get
				{
					if (!nos || nos.Type == InputType.Touch && nos.Source && nos.Source.type != JoystickType.Button)
						nos = new(InputType.Touch, null);

					return nos;
				}
				set
				{
					nos = value;
				}
			}
			public MobileInput LaunchControlSwitch
			{
				get
				{
					if (!launchControlSwitch || launchControlSwitch.Type == InputType.Touch && launchControlSwitch.Source && launchControlSwitch.Source.type != JoystickType.Button)
						launchControlSwitch = new(InputType.Touch, null);

					return launchControlSwitch;
				}
				set
				{
					launchControlSwitch = value;
				}
			}
			/*public MobileInput Horn
			{
				get
				{
					if (!horn || horn.Type == InputType.Joystick && horn.Source && horn.Source.Type != JoystickType.Button)
						horn = new(InputType.Touch, null);

					return horn;
				}
				set
				{
					horn = value;
				}
			}*/
			public MobileInput Reset
			{
				get
				{
					if (!reset || reset.Type == InputType.Touch && reset.Source && reset.Source.type != JoystickType.Button)
						reset = new(InputType.Touch, null);

					return reset;
				}
				set
				{
					reset = value;
				}
			}
			public MobileInput ChangeCamera
			{
				get
				{
					if (!changeCamera || changeCamera.Type == InputType.Touch && changeCamera.Source && changeCamera.Source.type != JoystickType.Button)
						changeCamera = new(InputType.Touch, null);

					return changeCamera;
				}
				set
				{
					changeCamera = value;
				}
			}
			public MobileInput LightSwitch
			{
				get
				{
					if (!lightSwitch || lightSwitch.Type == InputType.Touch && lightSwitch.Source && lightSwitch.Source.type != JoystickType.Button)
						lightSwitch = new(InputType.Touch, null);

					return lightSwitch;
				}
				set
				{
					lightSwitch = value;
				}
			}
			public MobileInput HighBeamLightSwitch
			{
				get
				{
					if (!highBeamLightSwitch || highBeamLightSwitch.Type == InputType.Touch && highBeamLightSwitch.Source && highBeamLightSwitch.Source.type != JoystickType.Button)
						highBeamLightSwitch = new(InputType.Touch, null);

					return highBeamLightSwitch;
				}
				set
				{
					highBeamLightSwitch = value;
				}
			}
			public MobileInput InteriorLightSwitch
			{
				get
				{
					if (!interiorLightSwitch || interiorLightSwitch.Type == InputType.Touch && interiorLightSwitch.Source && interiorLightSwitch.Source.type != JoystickType.Button)
						interiorLightSwitch = new(InputType.Touch, null);

					return interiorLightSwitch;
				}
				set
				{
					interiorLightSwitch = value;
				}
			}
			public MobileInput LeftSideSignalSwitch
			{
				get
				{
					if (!leftSideSignalSwitch || leftSideSignalSwitch.Type == InputType.Touch && leftSideSignalSwitch.Source && leftSideSignalSwitch.Source.type != JoystickType.Button)
						leftSideSignalSwitch = new(InputType.Touch, null);

					return leftSideSignalSwitch;
				}
				set
				{
					leftSideSignalSwitch = value;
				}
			}
			public MobileInput RightSideSignalSwitch
			{
				get
				{
					if (!rightSideSignalSwitch || rightSideSignalSwitch.Type == InputType.Touch && rightSideSignalSwitch.Source && rightSideSignalSwitch.Source.type != JoystickType.Button)
						rightSideSignalSwitch = new(InputType.Touch, null);

					return rightSideSignalSwitch;
				}
				set
				{
					rightSideSignalSwitch = value;
				}
			}
			public MobileInput HazardLightsSwitch
			{
				get
				{
					if (!hazardLightsSwitch || hazardLightsSwitch.Type == InputType.Touch && hazardLightsSwitch.Source && hazardLightsSwitch.Source.type != JoystickType.Button)
						hazardLightsSwitch = new(InputType.Touch, null);

					return hazardLightsSwitch;
				}
				set
				{
					hazardLightsSwitch = value;
				}
			}
			public MobileInput TrailerLinkSwitch
			{
				get
				{
					if (!trailerLinkSwitch || trailerLinkSwitch.Type == InputType.Touch && trailerLinkSwitch.Source && trailerLinkSwitch.Source.type != JoystickType.Button)
						trailerLinkSwitch = new(InputType.Touch, null);

					return trailerLinkSwitch;
				}
				set
				{
					trailerLinkSwitch = value;
				}
			}
			public bool AnyInputInUse
			{
				get
				{
					return SteeringWheelLeft.Value != 0f || SteeringWheelRight.Value != 0f || FuelPedal.Value != 0f || BrakePedal.Value != 0f || ClutchPedal.Value != 0f || Handbrake.Value != 0f || GearShiftUp.Value != 0f || GearShiftDown.Value != 0f || EngineStartSwitch.Value != 0f || NOS.Value != 0f || LaunchControlSwitch.Value != 0f || /* Horn.Value != 0f ||*/ Reset.Value != 0f || ChangeCamera.Value != 0f || LightSwitch.Value != 0f || HighBeamLightSwitch.Value != 0f || LeftSideSignalSwitch.Value != 0f || RightSideSignalSwitch.Value != 0f || HazardLightsSwitch.Value != 0f || TrailerLinkSwitch.Value != 0f || InteriorLightSwitch.Value != 0f;
				}
			}
			public bool AnyInputWasPressed
			{
				get
				{
					return SteeringWheelLeft.Source && SteeringWheelLeft.Source.WasPressed || SteeringWheelRight.Source && SteeringWheelRight.Source.WasPressed || FuelPedal.Source && FuelPedal.Source.WasPressed || BrakePedal.Source && BrakePedal.Source.WasPressed || ClutchPedal.Source && ClutchPedal.Source.WasPressed || Handbrake.Source && Handbrake.Source.WasPressed || GearShiftUp.Source && GearShiftUp.Source.WasPressed || GearShiftDown.Source && GearShiftDown.Source.WasPressed || EngineStartSwitch.Source && EngineStartSwitch.Source.WasPressed || NOS.Source && NOS.Source.WasPressed || LaunchControlSwitch.Source && LaunchControlSwitch.Source.WasPressed || /* Horn.Source && Horn.Source.WasPressed ||*/ Reset.Source && Reset.Source.WasPressed || ChangeCamera.Source && ChangeCamera.Source.WasPressed || LightSwitch.Source && LightSwitch.Source.WasPressed || HighBeamLightSwitch.Source && HighBeamLightSwitch.Source.WasPressed || InteriorLightSwitch.Source && InteriorLightSwitch.Source.WasPressed || LeftSideSignalSwitch.Source && LeftSideSignalSwitch.Source.WasPressed || RightSideSignalSwitch.Source && RightSideSignalSwitch.Source.WasPressed || HazardLightsSwitch.Source && HazardLightsSwitch.Source.WasPressed || TrailerLinkSwitch.Source && TrailerLinkSwitch.Source.WasPressed;
				}
			}
			public bool AnyInputWasReleased
			{
				get
				{
					return SteeringWheelLeft.Source.WasReleased || SteeringWheelRight.Source && SteeringWheelRight.Source.WasReleased || FuelPedal.Source && FuelPedal.Source.WasReleased || BrakePedal.Source && BrakePedal.Source.WasReleased || ClutchPedal.Source && ClutchPedal.Source.WasReleased || Handbrake.Source && Handbrake.Source.WasReleased || GearShiftUp.Source && GearShiftUp.Source.WasReleased || GearShiftDown.Source && GearShiftDown.Source.WasReleased || EngineStartSwitch.Source && EngineStartSwitch.Source.WasReleased || NOS.Source && NOS.Source.WasReleased || LaunchControlSwitch.Source && LaunchControlSwitch.Source.WasReleased || /* Horn.Source && Horn.Source.WasReleased ||*/ Reset.Source && Reset.Source.WasReleased || ChangeCamera.Source && ChangeCamera.Source.WasReleased || LightSwitch.Source && LightSwitch.Source.WasReleased || HighBeamLightSwitch.Source && HighBeamLightSwitch.Source.WasReleased || InteriorLightSwitch.Source && InteriorLightSwitch.Source.WasReleased || LeftSideSignalSwitch.Source && LeftSideSignalSwitch.Source.WasReleased || RightSideSignalSwitch.Source && RightSideSignalSwitch.Source.WasReleased || HazardLightsSwitch.Source && HazardLightsSwitch.Source.WasReleased || TrailerLinkSwitch.Source && TrailerLinkSwitch.Source.WasReleased;
				}
			}

			[SerializeField]
			private MobileInput steeringWheelLeft;
			[SerializeField]
			private MobileInput steeringWheelRight;
			[SerializeField]
			private MobileInput fuelPedal;
			[SerializeField]
			private MobileInput brakePedal;
			[SerializeField]
			private MobileInput clutchPedal;
			[SerializeField]
			private MobileInput handbrake;
			[SerializeField]
			private MobileInput gearShiftUp;
			[SerializeField]
			private MobileInput gearShiftDown;
			[SerializeField]
			private MobileInput engineStartSwitch;
			[SerializeField]
			private MobileInput nos;
			[SerializeField]
			private MobileInput launchControlSwitch;
			/*[SerializeField]
			private MobileInput horn;*/
			[SerializeField]
			private MobileInput reset;
			[SerializeField]
			private MobileInput changeCamera;
			[SerializeField]
			private MobileInput lightSwitch;
			[SerializeField]
			private MobileInput highBeamLightSwitch;
			[SerializeField]
			private MobileInput interiorLightSwitch;
			[SerializeField]
			private MobileInput leftSideSignalSwitch;
			[SerializeField]
			private MobileInput rightSideSignalSwitch;
			[SerializeField]
			private MobileInput hazardLightsSwitch;
			[SerializeField]
			private MobileInput trailerLinkSwitch;

			private int index = -1;

			#endregion

			#region Methods

			#region Update

			public void Update()
			{
				if (HasInternalErrors || !Settings.useMobileInputs)
					return;

				if (!group || UIController.MobilePresets.Length < 1)
					return;

				if (index < 0)
				{
					index = UIController.MobilePresets.ToList().IndexOf(this);

					if (index < 0)
						return;
				}

				if (UIController.ActiveMobilePreset != index && UIController.MobilePresets.Length > 1)
				{
					group.gameObject.SetActive(false);

					return;
				}
				else if (!group.gameObject.activeSelf)
					group.gameObject.SetActive(true);
			}

			#endregion

			#region Utilities

			private static float GetSensorValue(Vector3 value, SensorAxis axis, AxisPolarity polarity)
			{
				return axis switch
				{
					SensorAxis.X => GetAxisClampedValue(value.x, polarity == AxisPolarity.Negative),
					SensorAxis.Y => GetAxisClampedValue(value.y, polarity == AxisPolarity.Negative),
					SensorAxis.Z => GetAxisClampedValue(value.z, polarity == AxisPolarity.Negative),
					_ => 0f,
				};
			}
			private static float GetAxisClampedValue(float value, bool invert)
			{
				return Utility.Clamp01((invert ? -1f : 1f) * value);
			}

			#endregion

			#endregion

			#region Operators

			public static implicit operator bool(MobileInputPreset preset) => preset != null;

			#endregion
		}

		#endregion

		#region Variables

		#region Static Variables

		internal static VehicleUIController Instance => instance;

		private static Vehicle VehicleInstance => Manager.PlayerVehicle;
		private static VehicleUIController instance;

		#endregion

		#region Global Variables

		public SpeedMeter[] SpeedMeters
		{
			get
			{
				speedMeters ??= new SpeedMeter[] { };

				return speedMeters;
			}
			set
			{
				speedMeters = value;
			}
		}
		public MobileInputPreset[] MobilePresets
		{
			get
			{
				mobilePresets ??= new MobileInputPreset[] { };

				return mobilePresets;
			}
			set
			{
				mobilePresets = value;
			}
		}
		public int ActiveSpeedMeter
		{
			get
			{
				if (SpeedMeters.Length < 1)
					return 0;

				return math.clamp(activeSpeedMeter, 0, SpeedMeters.Length);
			}
			set
			{
				if (SpeedMeters.Length < 1)
					return;

				activeSpeedMeter = math.clamp(value, 0, SpeedMeters.Length);
			}
		}
		public int ActiveMobilePreset
		{
			get
			{
				if (MobilePresets.Length < 1)
					return 0;

				return math.clamp(activeMobilePreset, 0, MobilePresets.Length);
			}
			set
			{
				if (MobilePresets.Length < 1)
					return;

				activeMobilePreset = math.clamp(value, 0, MobilePresets.Length);
			}
		}

		[SerializeField]
		private SpeedMeter[] speedMeters;
		[SerializeField]
		private MobileInputPreset[] mobilePresets;
		[SerializeField]
		private int activeSpeedMeter;
		[SerializeField]
		private int activeMobilePreset;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static VehicleUIController GetOrCreateInstance()
		{
			if (HasInternalErrors)
				return null;

			GameObject controllerGameObject = GetOrCreateGameController();

			instance = controllerGameObject.GetComponent<VehicleUIController>();

			if (!instance)
				instance = controllerGameObject.AddComponent<VehicleUIController>();

			return instance;
		}

		#endregion

		#region Global Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || !IsSetupDone || instance && instance != this)
				return;

			instance = this;

			if (!Manager)
				VehicleManager.GetOrCreateInstance();

			Awaken = true;
		}

		private void Awake()
		{
			if (Awaken)
				return;

			Restart();
		}
		private void Initialize()
		{
			for (int i = 0; i < speedMeters.Length; i++)
				if (speedMeters[i].RPMOverRevNeedle && speedMeters[i].RPMOverRevNeedle.Needle)
					speedMeters[i].RPMOverRevNeedle.Needle.fillAmount = 0f;
		}

		#endregion

		#region Update

		private void Update()
		{
			if (!Awaken || !VehicleInstance || !VehicleInstance.Engine)
				return;

			for (int i = 0; i < SpeedMeters.Length; i++)
				SpeedMeters[i].Update();

			for (int i = 0; i < MobilePresets.Length; i++)
				MobilePresets[i].Update();
		}

		#endregion

		#region Enable, Disable & Destroy

		private void OnEnable()
		{
			Awake();
		}
		private void OnDisable()
		{
			if (instance == this)
				instance = null;
		}
		private void OnDestroy()
		{
			OnDisable();
		}

		#endregion

		#endregion

		#endregion
	}
}
