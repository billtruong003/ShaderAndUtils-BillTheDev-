// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using System.Collections.Generic;

using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportTrees : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;

        void Start()
        {
            if (terrainData == null)
                return;


            GameObject go = new GameObject("Trees");
            go.transform.position = this.transform.position;


            //Exporting mesh from TerrainData////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical);

            go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;



            //Creating default material////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            go.AddComponent<MeshRenderer>().sharedMaterial = new Material(TerrainToMeshUtilities.GetDefaultShader());




            /////////////////////////////////////////////////////////////////////////////////
            //                                                                             //
            //                                                                             //
            //      Check manual file for more info and examples about tree exporting      //
            //                                                                             //
            //                                                                             //
            /////////////////////////////////////////////////////////////////////////////////

            //Exporting trees//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            TerrainToMeshPrototype[] treePrototypes = terrainData.TerrainToMesh().ExportPrototypes(TerrainToMeshEnum.Prototype.Tree, vertexCountHorizontal, vertexCountVertical, 1, 1);


            bool createTreesManually = false;
            if (createTreesManually)
                CreateTreeGameObjectsManually(treePrototypes, go);     //Manually create tree gameObjects
            else
                CreateTreeGameObjects(treePrototypes, go);             //Use TerrainToMeshUtilities method for automatically creating tree gameObjects
        }

        void CreateTreeGameObjects(TerrainToMeshPrototype[] treePrototypes, GameObject parentGO)
        {
            Dictionary<int, GameObject> treeGO = TerrainToMeshUtilities.ConvertPrototypesToTreeGameObjects(treePrototypes, null, 100);
            foreach (var item in treeGO)
            {
                //Adjusting objects position
                item.Value.transform.position = item.Value.transform.position + parentGO.transform.position;


                item.Value.transform.SetParent(parentGO.transform);
            }

        }

        void CreateTreeGameObjectsManually(TerrainToMeshPrototype[] treePrototypes, GameObject parentGO)
        {
            for (int i = 0; i < treePrototypes.Length; i++)
            {
                //Instantiate tree prefab
                GameObject tree = Instantiate(treePrototypes[i].treePrefab);

                //Set position
                tree.transform.position = treePrototypes[i].position;

                //Random random rotation
                tree.transform.rotation = Quaternion.Euler(0, Random.value * 360, 0);

                //Scale
                tree.transform.localScale = treePrototypes[i].scale;



                //Add parent
                tree.transform.SetParent(parentGO.transform, false);
            }
        }
    }
}
