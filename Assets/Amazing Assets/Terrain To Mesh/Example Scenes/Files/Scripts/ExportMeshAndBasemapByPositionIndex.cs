// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportMeshAndBasemapByPositionIndex : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;


        [Space(10)]
        public bool exportHoles = false;
        public int mapsResolution = 512;


        [Space(10)]
        [Range(1, 8)]
        public int splitCountHorizontal = 4;
        [Range(1, 8)] 
        public int splitCountVertical = 4;

        [ContextMenu("Run")]
        void Start()
        {
            if (terrainData == null)
                return;

            for (int h = 0; h < splitCountHorizontal; h++)
            {
                for (int v = 0; v < splitCountVertical; v++)
                {
                    //Creating gameObject 
                    GameObject go = new GameObject($"{terrainData.name} [{h}, {v}]");


                    //Exporting mesh from TerrainData by position//////////////////////////////////////////////////////////////////////////////////////////////////////

                    Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical, splitCountHorizontal, splitCountVertical, h, v, true);

                    go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;

                    //Exporting basemap textures by position///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    Texture2D diffuseTexture = terrainData.TerrainToMesh().ExportBasemapDiffuseTexture(mapsResolution, exportHoles, splitCountHorizontal, splitCountVertical, h, v);  //Diffuse textures's alpha channel contains holesmap cutout, if 'exportHoles' is enabled
                    Texture2D normalTexture = terrainData.TerrainToMesh().ExportBasemapNormalTexture(mapsResolution, false, splitCountHorizontal, splitCountVertical, h, v);

                    Texture2D maskTexture = terrainData.TerrainToMesh().ExportBasemapMaskTexture(mapsResolution, splitCountHorizontal, splitCountVertical, h, v);                     //Contains metallic(R), occlusion(G) and smoothness(A)
                    Texture2D occlusionTexture = terrainData.TerrainToMesh().ExportBasemapOcclusionTexture(mapsResolution, splitCountHorizontal, splitCountVertical, h, v);



                    //Creating material and assign exported basemaps///////////////////////////////////////////////////////////////////////////////////////////////////

                    Material material = new Material(TerrainToMeshUtilities.GetDefaultShader());
                    material.name = go.name;

                    TerrainToMeshUtilities.SetupDefaultMaterial(material, diffuseTexture, exportHoles, normalTexture, maskTexture, occlusionTexture);


                    go.AddComponent<MeshRenderer>().sharedMaterial = material;
                }
            }
        }
    }
}
