// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using System.Linq;
using System.Collections.Generic;

using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportGrass : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;

        void Start()
        {
            if (terrainData == null)
                return;


            GameObject go = new GameObject("Grass");
            go.transform.position = this.transform.position;


            //Exporting mesh from TerrainData////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical);

            go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;
                        


            //Creating default material////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            go.AddComponent<MeshRenderer>().sharedMaterial = new Material(TerrainToMeshUtilities.GetDefaultShader());




            //////////////////////////////////////////////////////////////////////////////////
            //                                                                              //
            //                                                                              //
            //      Check manual file for more info and examples about grass exporting      //
            //                                                                              //
            //                                                                              //
            //////////////////////////////////////////////////////////////////////////////////

            //Exporting grass from TerrainData////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            TerrainToMeshPrototype[] grassPrototypes = terrainData.TerrainToMesh().ExportPrototypes(TerrainToMeshEnum.Prototype.Grass);

            //Combining 100% of grassPrototypes into meshes with each one with 65K vertices
            Dictionary<int, Mesh[]> grassMeshes = TerrainToMeshUtilities.ConvertPrototypesToGrassMeshes(grassPrototypes,
                                                                                                        TerrainToMeshEnum.GrassCombineType.ByTexture,
                                                                                                        TerrainToMeshEnum.GrassMeshTexcoord2.None,
                                                                                                        true,                                          //Bake health and dry colors
                                                                                                        null,                                          //Atlas UV rects
                                                                                                        100,                                           //Export percentage
                                                                                                        65000,                                         //Vertex count per mesh
                                                                                                        3,                                             //Grass quad mesh sides count
                                                                                                        0.5f);                                         //Grass quad mesh distortion


            //Creating materials for each grass layer
            Dictionary<int, Material> grassMaterials = new Dictionary<int, Material>();
            foreach (var item in grassMeshes)
            {
                Texture2D grassTexture = terrainData.detailPrototypes[item.Key].prototypeTexture;

                Material layerMaterial = new Material(TerrainToMeshUtilities.GetDefaultShader());

                //Setting up material to use grass texture for '_MainTex' and enabling Alpha Cutout
                TerrainToMeshUtilities.SetupDefaultMaterial(layerMaterial, grassTexture, true, null, null, null);

                layerMaterial.name = grassTexture.name;


                grassMaterials.Add(item.Key, layerMaterial);
            }

            //Instantiating gameObject with grass meshes
            foreach (var item in grassMeshes)
            {
                Mesh[] meshes = item.Value;
                for (int i = 0; i < meshes.Length; i++)
                {
                    GameObject grassGO = new GameObject($"Grass (Layer : {item.Key}) {grassMaterials[item.Key].name}");

                    //Updating childs position
                    grassGO.transform.position = go.transform.position;


                    grassGO.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                    grassGO.AddComponent<MeshRenderer>().sharedMaterial = grassMaterials[item.Key];

                    grassGO.transform.SetParent(go.transform);
                }
            }
        }
    }
}
