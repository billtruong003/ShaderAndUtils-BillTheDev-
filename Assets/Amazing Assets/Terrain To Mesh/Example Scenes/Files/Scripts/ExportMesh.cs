// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportMesh : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;


        void Start()
        {
            if (terrainData == null)
                return;


            GameObject go = new GameObject("Terrain Mesh");


            //Exporting mesh from TerrainData//////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical);

            go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;



            //Assigning default material///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Shader shader = TerrainToMeshUtilities.GetDefaultShader();  //Returns shader based on the used render pipeline: Standard, URP/Lit or HDRP/Lit

            Material material = new Material(shader);

            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
