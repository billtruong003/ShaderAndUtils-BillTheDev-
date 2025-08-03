/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/
﻿/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using System.Collections.Generic;

namespace ProceduralPixels.BakeAO.Editor
{
    public static class GlobalPool<T> where T : new()
    {
        private static Stack<T> pool = new Stack<T>();

        public static T Get()
        {
            if (pool == null)
                pool = new Stack<T>();

            lock (pool)
            {
                if (pool.Count == 0)
                    return new T();
                else
                    return pool.Pop();
            }
        }

        public static void Return(T item)
        {
            if (pool == null)
                throw new InvalidOperationException("Returning object to the pool that does not exist");

            lock (pool)
            {
                pool.Push(item);
            }
        }
    }
}
