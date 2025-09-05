// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportMeshAndSplatmap : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;

        [Space(10)]
        public bool exportHoles = false;
        public bool createFallbackTextures;

        void Start()
        {
            if (terrainData == null)
                return;


            GameObject go = new GameObject("Terrain Mesh");


            //Exporting mesh with edge fall/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical);

            go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;



            //Exporting Splatmap material from TerrainData/////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Material splatmapMaterial = terrainData.TerrainToMesh().ExportSplatmapMaterial(exportHoles);

            go.AddComponent<MeshRenderer>().sharedMaterial = splatmapMaterial;



            //Creating fallback material textures

            if (createFallbackTextures)
            {
                Texture2D fallbackDiffuse = terrainData.TerrainToMesh().ExportBasemapDiffuseTexture(1024, exportHoles, false);
                Texture2D fallbackNormal = terrainData.TerrainToMesh().ExportBasemapNormalTexture(1024, false);

                //Setting up material to use diffuse and normal maps
                TerrainToMeshUtilities.SetupDefaultMaterial(splatmapMaterial, fallbackDiffuse, exportHoles, fallbackNormal, null, null);
            }
        }
    }
}
