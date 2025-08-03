using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    public static class InitializeURPSupportForBakeAO
    {
        [BakeAOURPSupportVersion]
        public const string URPVersion = "17.0.3";

        [BakeAOURPSupportFolderGUID]
        public const string URPSupportFolderGUID = "0d16390e7097ab94bb2336b46e2a1e0c";
        
        [InitializeOnLoadMethod]
        private static void InitializeURPSetup()
        {
            InitializeShaders("Procedural Pixels/Bake AO - URP/Simple Lit", "Universal Render Pipeline/Simple Lit");
            InitializeShaders("Procedural Pixels/Bake AO - URP/Lit", "Universal Render Pipeline/Lit");
            InitializeShaders("Procedural Pixels/Bake AO - URP/Unlit", "Universal Render Pipeline/Unlit");
            InitializeShaders("Procedural Pixels/Bake AO - URP/Complex Lit", "Universal Render Pipeline/Complex Lit");
        }

        private static void InitializeShaders(string bakeAOSupportedShaderName, string originalShaderName)
        {
            var bakeAOSupportedShader = Shader.Find(bakeAOSupportedShaderName);
            var originalShader = Shader.Find(originalShaderName);

            if (bakeAOSupportedShader == null || originalShader == null)
                return;

            BakeAOSettings.Instance.MarkShaderAsSupported(bakeAOSupportedShader);
            BakeAOSettings.Instance.AddShaderRemap(originalShader, bakeAOSupportedShader);
        }
    } 
}
