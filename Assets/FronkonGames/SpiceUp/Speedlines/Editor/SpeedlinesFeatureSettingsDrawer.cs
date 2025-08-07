////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Martin Bustos @FronkonGames <fronkongames@gmail.com>. All rights reserved.
//
// THIS FILE CAN NOT BE HOSTED IN PUBLIC REPOSITORIES.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;
using static FronkonGames.SpiceUp.Speedlines.Inspector;

namespace FronkonGames.SpiceUp.Speedlines.Editor
{
  /// <summary> Spice up Speed inspector. </summary>
  [CustomPropertyDrawer(typeof(Speedlines.Settings))]
  public class SpeedlinesFeatureSettingsDrawer : Drawer
  {
    private Speedlines.Settings settings;

    protected override void ResetValues() => settings?.ResetDefaultValues();

    protected override void InspectorGUI()
    {
      settings ??= GetSettings<Speedlines.Settings>();

      /////////////////////////////////////////////////
      // Common.
      /////////////////////////////////////////////////
      settings.intensity = Slider("Intensity", "Controls the intensity of the effect [0, 1]. Default 1.", settings.intensity, 0.0f, 1.0f, 1.0f);

      /////////////////////////////////////////////////
      // Speed.
      /////////////////////////////////////////////////
      Separator();

      settings.strength = Slider("Strength", "Effect strength [0, 1]. Default 0.", settings.strength, 0.0f, 1.0f, 0.0f);
      settings.radius = Slider("Radius", "Effect radius [0, 2]. Default 1.25.", settings.radius, 0.0f, 2.0f, 1.25f);
      settings.length = Slider("Length", "Length of the lines [0, 5]. Default 2.", settings.length, 0.0f, 5.0f, 2.0f);
      settings.speed = Slider("Speed", "Speed in the movement of the lines [0, 10]. Default 2.", settings.speed, 0.0f, 10.0f, 2.0f);
      settings.frequency = Slider("Frequency", "Edge of the lines [0, 50]. Default 10.", settings.frequency, 0.0f, 50.0f, 10.0f);
      settings.softness = Slider("Softness", "Smoothness of the lines [0, 1]. Default 0.1.", settings.softness, 0.0f, 1.0f, 0.1f);
      settings.noise = Slider("Noise", "Geometry of the shape of the lines [0, 1]. Default 0.1.", settings.noise, 0.0f, 1.0f, 0.1f);

      settings.colorBlend = (ColorBlends)EnumPopup("Color blend", "Color blend operation. Default Solid.", settings.colorBlend, ColorBlends.Solid);
      IndentLevel++;
      settings.colorBrightness = Slider("Brightness", "Color brightness [0, 10]. Default 1.", settings.colorBrightness, 0.0f, 10.0f, 1.0f);
      settings.colorOffset = Slider("Offset", "Color gradient offset [0, 1]. Default 0.5.", settings.colorOffset, 0.0f, 1.0f, 0.5f);
      settings.colorDefinition = Slider("Definition", "Color gradient definition [0, 10]. Default 1.", settings.colorDefinition, 0.0f, 10.0f, 1.0f);
      settings.colorBorder = ColorField("Border", "Start color of the lines. Use the alpha channel to define its transparency.", settings.colorBorder, Color.white);
      settings.colorCenter = ColorField("Center", "End color of the lines. Use the alpha channel to define its transparency.", settings.colorCenter, Color.white);
      IndentLevel--;

      settings.aspect = Toggle("Aspect ratio", "Respect or not the aspect ratio. Default false.", settings.aspect, false);

      /////////////////////////////////////////////////
      // Color.
      /////////////////////////////////////////////////
      Separator();

      if (Foldout("Color") == true)
      {
        IndentLevel++;

        settings.brightness = Slider("Brightness", "Brightness [-1.0, 1.0]. Default 0.", settings.brightness, -1.0f, 1.0f, 0.0f);
        settings.contrast = Slider("Contrast", "Contrast [0.0, 10.0]. Default 1.", settings.contrast, 0.0f, 10.0f, 1.0f);
        settings.gamma = Slider("Gamma", "Gamma [0.1, 10.0]. Default 1.", settings.gamma, 0.01f, 10.0f, 1.0f);
        settings.hue = Slider("Hue", "The color wheel [0.0, 1.0]. Default 0.", settings.hue, 0.0f, 1.0f, 0.0f);
        settings.saturation = Slider("Saturation", "Intensity of a colors [0.0, 2.0]. Default 1.", settings.saturation, 0.0f, 2.0f, 1.0f);

        IndentLevel--;
      }

      /////////////////////////////////////////////////
      // Advanced.
      /////////////////////////////////////////////////
      Separator();

      if (Foldout("Advanced") == true)
      {
        IndentLevel++;

        settings.affectSceneView = Toggle("Affect the Scene View?", "Does it affect the Scene View?", settings.affectSceneView);
        settings.filterMode = (FilterMode)EnumPopup("Filter mode", "Filter mode. Default Bilinear.", settings.filterMode, FilterMode.Bilinear);
        settings.whenToInsert = (UnityEngine.Rendering.Universal.RenderPassEvent)EnumPopup("RenderPass event",
          "Render pass injection. Default BeforeRenderingPostProcessing.",
          settings.whenToInsert,
          UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingPostProcessing);
        settings.enableProfiling = Toggle("Enable profiling", "Enable render pass profiling", settings.enableProfiling);

        IndentLevel--;
      }
    }
  }
}
