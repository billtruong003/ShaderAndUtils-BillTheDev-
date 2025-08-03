/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    /// <summary>
    /// Unsafe access to internal texture data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe struct UnsafeTexture2D<T> where T : unmanaged
    {
        public T* ptr;
        public Vector2Int resolution;
        public int length => resolution.x * resolution.y;

        public T this[int index]
        {
            get => ptr[index];
            set => ptr[index] = value;
        }

        public UnsafeTexture2D(Texture2D texture)
        {
            NativeArray<T> data = texture.GetRawTextureData<T>();
            ptr = (T*)data.GetUnsafePtr();
            resolution = new Vector2Int(texture.width, texture.height);
        }

        public static UnsafeTexture2D<T> Uninitialized => new UnsafeTexture2D<T>()
        {
            ptr = null,
            resolution = Vector2Int.zero
        };

        public T Load(int x, int y)
        {
            return ptr[y * resolution.x + x];
        }

        public T Load(Vector2Int coord)
        {
            return ptr[coord.y * resolution.x + coord.x];
        }
    }

}