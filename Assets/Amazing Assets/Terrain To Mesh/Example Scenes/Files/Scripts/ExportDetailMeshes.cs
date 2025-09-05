// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using System.Collections.Generic;

using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Examples
{
    public class ExportDetailMeshes : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;

        void Start()
        {
            if (terrainData == null)
                return;


            GameObject go = new GameObject("Detail Meshes");
            go.transform.position = this.transform.position;


            //Exporting mesh from terrain////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical);

            go.AddComponent<MeshFilter>().sharedMesh = terrainMesh;



            //Creating default material////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            go.AddComponent<MeshRenderer>().sharedMaterial = new Material(TerrainToMeshUtilities.GetDefaultShader());




            ////////////////////////////////////////////////////////////////////////////////////////
            //                                                                                    //
            //                                                                                    //
            //      Check manual file for more info and examples about detail mesh exporting      //
            //                                                                                    //
            //                                                                                    //
            ////////////////////////////////////////////////////////////////////////////////////////

            //Exporting detail meshes from the TerrainData
            TerrainToMeshPrototype[] detailMeshPrototypes = terrainData.TerrainToMesh().ExportPrototypes(TerrainToMeshEnum.Prototype.DetailMesh);


            //Instantiating 100% of detail mesh prefabs as gameObjects
            Dictionary<int, GameObject> detailMeshGOs = TerrainToMeshUtilities.ConvertPrototypesToDetailMeshGameObjects(detailMeshPrototypes, null, 100);


            //Attaching instantiated detail mesh gameObjects to the terrainMesh
            foreach (var item in detailMeshGOs)
            {
                //Updating childs posiiton
                item.Value.transform.position = item.Value.transform.position + go.transform.position;

                item.Value.transform.SetParent(go.transform);
            }

        }
    }
}
