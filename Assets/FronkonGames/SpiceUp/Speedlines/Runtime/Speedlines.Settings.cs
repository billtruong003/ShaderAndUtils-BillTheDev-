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
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FronkonGames.SpiceUp.Speedlines
{
  ///------------------------------------------------------------------------------------------------------------------
  /// <summary> Settings. </summary>
  /// <remarks> Only available for Universal Render Pipeline. </remarks>
  ///------------------------------------------------------------------------------------------------------------------
  public sealed partial class Speedlines
  {
    /// <summary> Settings. </summary>
    [Serializable]
    public sealed class Settings
    {
      public Settings() => ResetDefaultValues();

#region Common settings.
      /// <summary> Controls the intensity of the effect [0, 1]. Default 1. </summary>
      /// <remarks> An effect with Intensity equal to 0 will not be executed. </remarks>
      public float intensity = 1.0f;
#endregion

#region Speedlines settings.
      /// <summary> Effect strength [0, 1]. Default 0. </summary>
      public float strength = 0.0f;

      /// <summary> Effect radius [0, 2]. Default 1.25. </summary>
      public float radius = 1.25f;

      /// <summary> Length of the lines [0, 5]. Default 2. </summary>
      public float length = 2.0f;

      /// <summary> Speed in the movement of the lines [0, 10]. Default 2. </summary>
      public float speed = 2.0f;

      /// <summary> Edge of the lines [0, 50]. Default 10. </summary>
      public float frequency = 10.0f;

      /// <summary> Smoothness of the lines [0, 1]. Default 0.1. </summary>
      public float softness = 0.1f;

      /// <summary> Geometry of the shape of the lines [0, 1]. Default 0.1. </summary>
      public float noise = 0.1f;

      /// <summary> Respect or not the aspect ratio. Default false. </summary>
      public bool aspect = false;

      /// <summary> Color blend operation. Default Solid. </summary>
      public ColorBlends colorBlend = ColorBlends.Solid;

      /// <summary> Color brightness [0, 10]. Default 1. </summary>
      public float colorBrightness = 1.0f;

      /// <summary> Color gradient offset [0, 1]. Default 0.5. </summary>
      public float colorOffset = 0.5f;

      /// <summary> Color gradient definition [0, 10]. Default 1. </summary>
      public float colorDefinition = 1.0f;

      /// <summary> Start color of the lines. Use the alpha channel to define its transparency. </summary>
      public Color colorBorder = Color.white;

      /// <summary> End color of the lines. Use the alpha channel to define its transparency. </summary>
      public Color colorCenter = Color.white;
#endregion

#region Color settings.
      /// <summary> Brightness [-1.0, 1.0]. Default 0. </summary>
      public float brightness = 0.0f;

      /// <summary> Contrast [0.0, 10.0]. Default 1. </summary>
      public float contrast = 1.0f;

      /// <summary>Gamma [0.1, 10.0]. Default 1. </summary>
      public float gamma = 1.0f;

      /// <summary> The color wheel [0.0, 1.0]. Default 0. </summary>
      public float hue = 0.0f;

      /// <summary> Intensity of a colors [0.0, 2.0]. Default 1. </summary>
      public float saturation = 1.0f;
#endregion

#region Advanced settings.
      /// <summary> Does it affect the Scene View? </summary>
      public bool affectSceneView = false;

      /// <summary> Filter mode. Default Bilinear. </summary>
      public FilterMode filterMode = FilterMode.Bilinear;

      /// <summary> Render pass injection. Default BeforeRenderingPostProcessing. </summary>
      public RenderPassEvent whenToInsert = RenderPassEvent.BeforeRenderingPostProcessing;

      /// <summary> Enable render pass profiling. </summary>
      public bool enableProfiling = false;
#endregion

      /// <summary> Reset to default values. </summary>
      public void ResetDefaultValues()
      {
        intensity = 1.0f;

        strength = 0.0f;
        radius = 1.25f;
        length = 2.0f;
        speed = 2.0f;
        frequency = 10.0f;
        softness = 0.1f;
        noise = 0.1f;
        aspect = false;
        colorBlend = ColorBlends.Solid;
        colorBrightness = 1.0f;
        colorOffset = 0.5f;
        colorDefinition = 1.0f;
        colorBorder = colorCenter = Color.white;

        brightness = 0.0f;
        contrast = 1.0f;
        gamma = 1.0f;
        hue = 0.0f;
        saturation = 1.0f;

        affectSceneView = false;
        filterMode = FilterMode.Bilinear;
        whenToInsert = RenderPassEvent.BeforeRenderingPostProcessing;
        enableProfiling = false;
      }
    }
  }
}
