// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportMeshAndBasemap : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;

        [Space(10)]
        public bool exportHoles = false;
        public int mapsResolution = 512;


        void Start()
        {
            if (terrainData == null)
                return;


            GameObject go = new GameObject("Terrain Mesh");


            //Exporting mesh from TerrainData////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical);

            go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;



            //Exporting basemap textures////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Texture2D diffuseTexture = terrainData.TerrainToMesh().ExportBasemapDiffuseTexture(mapsResolution, exportHoles);    //Diffuse textures's alpha channel contains holesmap cutout, if 'exportHoles' is enabled
            Texture2D normalTexture = terrainData.TerrainToMesh().ExportBasemapNormalTexture(mapsResolution, false);

            Texture2D maskTexture = terrainData.TerrainToMesh().ExportBasemapMaskTexture(mapsResolution);                       //Contains metallic(R), occlusion(G) and smoothness(A)
            Texture2D occlusionTexture = terrainData.TerrainToMesh().ExportBasemapOcclusionTexture(mapsResolution);



            //Creating default material and assign exported basemaps/////////////////////////////////////////////////////////////////////////////////////////////////

            Material material = new Material(TerrainToMeshUtilities.GetDefaultShader());

            TerrainToMeshUtilities.SetupDefaultMaterial(material, diffuseTexture, exportHoles, normalTexture, maskTexture, occlusionTexture);


            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
