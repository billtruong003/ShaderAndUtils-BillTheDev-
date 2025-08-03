/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal static class AssetDatabaseUtils
    {
        public static bool TryGetObjectFromGUIDAndLocalFileIdentifier(string guid, long localFileIdentifier, out Object obj)
        {
            obj = null;
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            List<Object> allObjectsAtPath = new List<Object>();
            allObjectsAtPath.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath));
            allObjectsAtPath.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath));

            foreach (var loadedObj in allObjectsAtPath)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(loadedObj, out string otherGUID, out long otherLocalID))
                {
                    if (otherGUID.Equals(guid) && otherLocalID.Equals(localFileIdentifier))
                    {
                        obj = loadedObj;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetObjectFromGUIDAndLocalFileIdentifier<T>(string guid, long localFileIdentifier, out T obj) where T : Object
        {
            obj = null;
            bool isObjectFound = TryGetObjectFromGUIDAndLocalFileIdentifier(guid, localFileIdentifier, out Object o);
            if (!isObjectFound)
                return false;

            if (o is T correctTypeObj)
            {
                obj = correctTypeObj;
                return true;
            }
            else
                return false;
        }
    }
}