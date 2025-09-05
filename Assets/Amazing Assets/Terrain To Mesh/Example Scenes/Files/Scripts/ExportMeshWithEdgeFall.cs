// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportMeshWithEdgeFall : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;

        [Space(10)]
        public TerrainToMeshEdgeFall edgeFall = new TerrainToMeshEdgeFall(0, true);
        public Texture2D edgeFallTexture;


        void Start()
        {            
            if (terrainData == null)
                return;


            GameObject go = new GameObject("Terrain Mesh");


            //Exporting mesh from TerrainData with edge fall///////////////////////////////////////////////////////////////////////////////////////////////////

            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical, edgeFall);

            go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;



            //Creating materials////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Material meshMaterial = new Material(TerrainToMeshUtilities.GetDefaultShader());      //Material for the main mesh 

            Material edgeFallMaterial = new Material(TerrainToMeshUtilities.GetDefaultShader());  //Material for the edge fall (saved in sub-mesh)


            string mainTexProprtyName = TerrainToMeshUtilities.GetDefaultShaderProperty(TerrainToMeshEnum.ShaderProperty.DiffuseMap);
            edgeFallMaterial.SetTexture(mainTexProprtyName, edgeFallTexture);     //Assign edge fall texture to the _MainTex property


            go.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { meshMaterial, edgeFallMaterial };
        }
    }
}
