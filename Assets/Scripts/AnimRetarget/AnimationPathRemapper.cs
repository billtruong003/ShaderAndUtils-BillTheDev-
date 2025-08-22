#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public static class AnimationPathRemapper
{
    public static AnimationClip CreateRemappedClip(GameObject root, AnimationClip sourceClip)
    {
        AnimationClip newClip = new AnimationClip();
        newClip.name = $"{sourceClip.name}_Remapped";

        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(sourceClip);
        Dictionary<string, Transform> foundTransformsCache = new Dictionary<string, Transform>();

        foreach (var binding in curveBindings)
        {
            string boneName = GetLeafNameFromPath(binding.path);
            if (string.IsNullOrEmpty(boneName)) continue;

            Transform targetTransform = FindCachedOrNewTransform(root.transform, boneName, foundTransformsCache);

            if (targetTransform != null)
            {
                string newPath = BuildPathFromRootToChild(root.transform, targetTransform);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);

                EditorCurveBinding newBinding = binding;
                newBinding.path = newPath;

                AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
            }
            else
            {
                Debug.LogWarning($"Could not find a matching transform for bone '{boneName}' from original path '{binding.path}' in the new hierarchy.");
            }
        }

        // Copy settings from the old clip to the new one (e.g., loop time)
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

        return newClip;
    }

    private static string GetLeafNameFromPath(string path)
    {
        int lastSeparator = path.LastIndexOf('/');
        if (lastSeparator == -1)
        {
            return path;
        }
        return path.Substring(lastSeparator + 1);
    }

    private static Transform FindCachedOrNewTransform(Transform root, string transformName, Dictionary<string, Transform> cache)
    {
        if (cache.TryGetValue(transformName, out Transform cachedTransform))
        {
            return cachedTransform;
        }

        Transform foundTransform = FindTransformRecursively(root, transformName);
        if (foundTransform != null)
        {
            cache[transformName] = foundTransform;
        }
        return foundTransform;
    }

    private static Transform FindTransformRecursively(Transform parent, string nameToFind)
    {
        if (parent.name == nameToFind)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform result = FindTransformRecursively(child, nameToFind);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static string BuildPathFromRootToChild(Transform root, Transform child)
    {
        if (child == root)
        {
            return "";
        }

        StringBuilder pathBuilder = new StringBuilder();
        Transform current = child;

        while (current != null && current != root)
        {
            if (pathBuilder.Length > 0)
            {
                pathBuilder.Insert(0, "/");
            }
            pathBuilder.Insert(0, current.name);
            current = current.parent;
        }

        return pathBuilder.ToString();
    }
}
#endif