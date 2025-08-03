/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal struct SerializableMeshContext
    {
        public string meshGUID;
        public long meshFileID;
        public UVChannel uv;
        public Matrix4x4 objectToWorld;
        public float uvToWorldRatio;

        public MeshContextUseFlags useFlags;
        public Mesh mesh;

        public SerializableMeshContext(MeshContext meshContext) : this(meshContext.mesh, meshContext.uv, meshContext.objectToWorld, meshContext.uvToWorldRatio)
        {
            useFlags |= meshContext.useFlags;
        }

        public SerializableMeshContext(Mesh mesh, UVChannel uv, Matrix4x4 objectToWorld, float uvToWorldRatio)
        {
            this.uv = uv;
            this.objectToWorld = objectToWorld;
            this.mesh = mesh;
            this.useFlags = MeshContextUseFlags.Default;
            this.useFlags |= (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out meshGUID, out meshFileID)) ? MeshContextUseFlags.IsTemporary : MeshContextUseFlags.Default;
            this.uvToWorldRatio = uvToWorldRatio;
        }

        public Mesh GetMesh()
        {
            AssetDatabaseUtils.TryGetObjectFromGUIDAndLocalFileIdentifier(meshGUID, meshFileID, out Mesh mesh);
            return mesh;
        }

        public MeshContext GetMeshContext()
        {
            MeshContext context = new MeshContext();
            context.uv = uv;
            context.objectToWorld = objectToWorld;
            context.useFlags = useFlags;
            context.uvToWorldRatio = uvToWorldRatio;

            if (AssetDatabaseUtils.TryGetObjectFromGUIDAndLocalFileIdentifier(meshGUID, meshFileID, out Mesh mesh))
                context.mesh = mesh;

            return context;
        }

        public void SerializeTemporaryMesh()
        {
            if (useFlags.HasFlag(MeshContextUseFlags.IsTemporary))
            {
                Mesh meshToSerialize;
                //if (useFlags.HasFlag(MeshContextUseFlags.IsNotUsedInAnyAsset))
                //    meshToSerialize = mesh;
                //else
                    meshToSerialize = BakeAOUtils.CloneMesh(mesh);

                BakeAOTemporaryData.SerializeAndAddTemporaryMesh(meshToSerialize);
                this.mesh = meshToSerialize;
                this.useFlags |= (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out meshGUID, out meshFileID)) ? MeshContextUseFlags.IsTemporary : MeshContextUseFlags.Default;
            }
        }
    }
}