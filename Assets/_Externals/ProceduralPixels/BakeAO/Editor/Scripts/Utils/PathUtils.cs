/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using System.IO;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal static class PathUtils
    {
        internal static string GetContainingFolderPath(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            string folderPath = filePath.Substring(0, filePath.Length - fileInfo.Name.Length).Trim(new char[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar });
            return folderPath;
        }

        internal static void EnsureDirectoryExists(string directoryPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
        }

        internal static bool TryGetProjectPathFromAbsolutePath(string absolutePath, out string projectPath)
        {
            absolutePath = absolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string projectDirectory = Directory.GetCurrentDirectory().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (!absolutePath.Contains(projectDirectory))
            {
                projectPath = null;
                return false;
            }
            else
            {
                projectPath = absolutePath.Remove(absolutePath.IndexOf(projectDirectory), projectDirectory.Length + 1);
                return true;
            }
        }

        internal static string EnsureContainsExtension(string str, string extension)
        {
            if (!str.Substring(str.Length - extension.Length, extension.Length).Equals(extension, StringComparison.OrdinalIgnoreCase))
                return str + extension;

            return str;
        }

        internal static void EnumerateFilesRecursively(string directoryPath, Action<string> filePathAction)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            var files = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);

            foreach(var file in files)
                filePathAction?.Invoke(Path.Combine(directoryPath, file.Name));

            var directories = directoryInfo.GetDirectories();
            foreach (var directory in directories)
                EnumerateFilesRecursively(Path.Combine(directoryPath, directory.Name), filePathAction);
        }
    }
}