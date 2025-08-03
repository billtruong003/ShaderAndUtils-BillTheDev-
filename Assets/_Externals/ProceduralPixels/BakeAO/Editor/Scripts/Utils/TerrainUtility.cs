/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralPixels.BakeAO.Editor
{
    public static class TerrainUtility
    {
        /// <summary>
        /// Returns null if terrain is not in bounds. 
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="boundsWS"></param>
        /// <returns></returns>
        public static Mesh GetTerrainMeshAtBounds(Terrain terrain, Bounds boundsWS)
        {
            TerrainData terrainData = terrain.terrainData;
            int heightmapResolution = terrainData.heightmapResolution;
            Matrix4x4 terrainToWorldSpace = Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, Vector3.one);
            Matrix4x4 worldToTerrainSpace = terrainToWorldSpace.inverse;

            Bounds boundsTS = BakeAOUtils.TransformBounds(boundsWS, worldToTerrainSpace);
            Bounds terrainBoundsTS = terrainData.bounds;

            if (!boundsTS.Intersects(terrainBoundsTS))
                return null;

            Vector3 minNormalized = Remap(boundsTS.min, terrainBoundsTS.min, terrainBoundsTS.max, Vector3.zero, Vector3.one);
            Vector3 maxNormalized = Remap(boundsTS.max, terrainBoundsTS.min, terrainBoundsTS.max, Vector3.zero, Vector3.one);

            Vector2 heightmapUVMin = new Vector2(minNormalized.x, minNormalized.z);
            Vector2 heightmapUVMax = new Vector2(maxNormalized.x, maxNormalized.z);

            Vector2 heightmapPixelCoordMin = heightmapUVMin * (terrainData.heightmapResolution - 1);
            Vector2 heightmapPixelCoordMax = heightmapUVMax * (terrainData.heightmapResolution - 1);

            Vector2Int heightmapPixelCoordMinInt = new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(heightmapPixelCoordMin.x), 0, terrainData.heightmapResolution),
                Mathf.Clamp(Mathf.FloorToInt(heightmapPixelCoordMin.y), 0, terrainData.heightmapResolution)
            );

            Vector2Int heightmapPixelCoordMaxInt = new Vector2Int(
                Mathf.Clamp(Mathf.CeilToInt(heightmapPixelCoordMax.x), 0, terrainData.heightmapResolution),
                Mathf.Clamp(Mathf.CeilToInt(heightmapPixelCoordMax.y), 0, terrainData.heightmapResolution)
            );

            Vector2Int vertexCount2D = heightmapPixelCoordMaxInt - heightmapPixelCoordMinInt + Vector2Int.one;
            int vertexCount = vertexCount2D.x * vertexCount2D.y;
            int indexCount = 2 * 3 * (vertexCount2D.x - 1) * (vertexCount2D.y - 1);

            NativeArray<Vector3> vertexBuffer = new NativeArray<Vector3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> indexBuffer = new NativeArray<int>(indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            float[,] zeroBasedHeights = terrainData.GetHeights(heightmapPixelCoordMinInt.x, heightmapPixelCoordMinInt.y, vertexCount2D.x, vertexCount2D.y);

            for (int y = heightmapPixelCoordMinInt.y; y <= heightmapPixelCoordMaxInt.y; y++)
            {
                for (int x = heightmapPixelCoordMinInt.x; x <= heightmapPixelCoordMaxInt.x; x++)
                {
                    int zeroBasedX = x - heightmapPixelCoordMinInt.x;
                    int zeroBasedY = y - heightmapPixelCoordMinInt.y;

                    int vertexIndex = zeroBasedY * (vertexCount2D.x) + zeroBasedX;
                    int indexIndex = (zeroBasedY * (vertexCount2D.x - 1) + zeroBasedX) * 6;

                    // Calculate vertex
                    float sampledHeight = zeroBasedHeights[zeroBasedY, zeroBasedX] * terrainData.heightmapScale.y;
                    Vector3 positionTS = PixelCoordToTerrainSpacePosition(x, y);
                    positionTS.y = sampledHeight;
                    Vector3 positionWS = terrainToWorldSpace.MultiplyPoint3x4(positionTS);

                    vertexBuffer[vertexIndex] = positionWS;

                    if (x != heightmapPixelCoordMaxInt.x && y != heightmapPixelCoordMaxInt.y)
                    {
                        // Calculate triangles
                        indexBuffer[indexIndex + 0] = vertexIndex;
                        indexBuffer[indexIndex + 1] = vertexIndex + 1 + vertexCount2D.x;
                        indexBuffer[indexIndex + 2] = vertexIndex + 1;

                        indexBuffer[indexIndex + 3] = vertexIndex;
                        indexBuffer[indexIndex + 4] = vertexIndex + vertexCount2D.x;
                        indexBuffer[indexIndex + 5] = vertexIndex + vertexCount2D.x + 1;
                    }
                }
            }

            //Debug.Assert(indexBuffer.All(i => i >= 0 && i < vertexCount), "Some indices are referring to non existing vertices");

            // Create mesh using low level API
            Mesh mesh = new Mesh();
            mesh.name = $"{terrain.name}_MESH";
            mesh.indexFormat = IndexFormat.UInt32;

            //mesh.SetVertexBufferParams(vertexCount, new VertexAttributeDescriptor[] { new VertexAttributeDescriptor(attribute: UnityEngine.Rendering.VertexAttribute.Position, format: VertexAttributeFormat.Float32, 3, 0) });
            //mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            //mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles), noChecks);
            //mesh.SetVertexBufferData(vertexBuffer, 0, 0, vertexCount, 0, noChecks);
            //mesh.SetIndexBufferData(indexBuffer, 0, 0, indexCount, noChecks);
            var meshBounds = BakeAOUtils.TransformBounds(terrainBoundsTS, terrainToWorldSpace);
            meshBounds.Expand(10000000.0f);
            //mesh.bounds = meshBounds;

            mesh.SetVertices(vertexBuffer.ToArray());
            mesh.SetTriangles(indexBuffer.ToArray(), 0);
            mesh.RecalculateNormals();
            //mesh.RecalculateTangents();
            mesh.bounds = meshBounds;

            return mesh;

            Vector3 PixelCoordToTerrainSpacePosition(int xCoord, int yCoord)
            {
                return Remap(new Vector3(xCoord, 0.0f, yCoord), Vector3.zero, Vector3.one * (heightmapResolution - 1), terrainBoundsTS.min, terrainBoundsTS.max);
            }
        }

        private static Vector3 Remap(Vector3 value, Vector3 originalRangeMin, Vector3 originalRangeMax, Vector3 outRangeMin, Vector3 outRangeMax)
        {
            Vector3 t = new Vector3(
                (value.x - originalRangeMin.x) / (originalRangeMax.x - originalRangeMin.x),
                (value.y - originalRangeMin.y) / (originalRangeMax.y - originalRangeMin.y),
                (value.z - originalRangeMin.z) / (originalRangeMax.z - originalRangeMin.z)
            );

            return new Vector3(
                Mathf.Lerp(outRangeMin.x, outRangeMax.x, t.x),
                Mathf.Lerp(outRangeMin.y, outRangeMax.y, t.y),
                Mathf.Lerp(outRangeMin.z, outRangeMax.z, t.z)
            );
        }
    }
}
