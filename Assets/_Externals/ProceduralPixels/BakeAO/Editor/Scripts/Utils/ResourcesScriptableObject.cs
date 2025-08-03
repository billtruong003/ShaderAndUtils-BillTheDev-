/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{

    /// <summary>
    /// Class that stores the references to assets that are used from the code. It's singleton. It automatically searches for the fields or properties that don't have assigned object. In this case it will invoke <see cref="RecreateAssets"/> method. Mark all fields that are assigned from code by using <see cref="SubassetResourceAttribute"/>. All fields that needs to be assigned from inspector mark with <see cref="ExternalResourceAttribute"/>.
    /// </summary>
    internal abstract class ResourcesScriptableObject<T> : SingletonScriptableObject<T> where T : SingletonScriptableObject<T>
    {
        protected void RemoveSubassets()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));
            for (int i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (AssetDatabase.IsSubAsset(asset) && asset != this)
                    AssetDatabase.RemoveObjectFromAsset(asset);
            }
        }

        protected void RecreateSubasset<TAsset>(Func<TAsset> getter, Action<TAsset> setter, Func<TAsset> creator) where TAsset : UnityEngine.Object
        {
            var subasset = getter();

            if (subasset != null)
            {
                AssetDatabase.RemoveObjectFromAsset(subasset);
                setter(null);
            }

            CreateSubassetIfNotExist(getter, setter, creator);
        }

        /// <summary>
        /// Method creates the asset using the creator method if the getter returns null. Created asset will be added as a subasset.
        /// </summary>
        protected void CreateSubassetIfNotExist<TAsset>(Func<TAsset> getter, Action<TAsset> setter, Func<TAsset> creator) where TAsset : UnityEngine.Object
        {
            if (getter() == null)
            {
                try
                {
                    var asset = creator();

                    if (asset == null)
                        throw new Exception("Asset was not created by the creator method.");

                    setter(asset);

                    AssetDatabase.GetAssetPath(this);
                    AssetDatabase.AddObjectToAsset(asset, this);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error when trying to create asset. \n{e.Message}\n{e.StackTrace}", this);
                }
            }
        }

        /// <summary>
        /// Method that sets the reference to the asset.
        protected void SetReferenceIfNotExist<TAsset>(Func<TAsset> refFieldGetter, Action<TAsset> refFieldSetter, Func<TAsset> provider) where TAsset : UnityEngine.Object
        {
            if (refFieldGetter() == null)
            {
                try
                {
                    var asset = provider();

                    if (asset == null)
                        throw new Exception("Asset reference was not provided.");

                    refFieldSetter(asset);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error when trying to create asset. \n{e.Message}\n{e.StackTrace}", this);
                }
            }
        }

        /// <summary>
        /// Recreates the assets that needs to be created from code. Use <see cref="CreateSubassetIfNotExist{TAsset}(Func{TAsset}, Action{TAsset}, Func{TAsset})"/> method to create the assets.
        /// </summary>
        protected abstract void RecreateAssets();

        /// <summary>
        /// Removes all subassets created from code and executes <see cref="RecreateAssets"/> to restore all the data.
        /// </summary>
        protected void RecreateAllAssets()
        {
            foreach (var field in AllSubassetResourceFields)
            {
                var propertyValue = field.GetValue(this);
                if (propertyValue == null)
                    continue;

                if (propertyValue is UnityEngine.Object unityObject)
                {
                    if (unityObject != null)
                    {
#if UNITY_EDITOR
                        AssetDatabase.RemoveObjectFromAsset(unityObject);
#endif
                        field.SetValue(this, null);
                    }
                }
            }

            RecreateAssets();
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
#endif
            RemoveAllNotReferencedSubassets();
        }

        private void RemoveAllNotReferencedSubassets()
        {
            HashSet<UnityEngine.Object> referencedObjects = new HashSet<UnityEngine.Object>();

            foreach (var field in AllSubassetResourceFields)
            {
                var propertyValue = field.GetValue(this);
                if (propertyValue == null)
                    continue;

                if (propertyValue is UnityEngine.Object unityObject)
                    if (unityObject != null)
                        referencedObjects.Add(unityObject);
            }

            var subassets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(this)).ToList();
            for (int i = subassets.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object subasset = subassets[i];
                if (subasset != this && !referencedObjects.Contains(subasset))
                    AssetDatabase.RemoveObjectFromAsset(subasset);
            }
        }

        private IEnumerable<FieldInfo> AllSubassetResourceFields => GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(prop => prop.IsDefined(typeof(SubassetResourceAttribute), false));

        private IEnumerable<FieldInfo> AllAutoReferenceResourceFields => GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(prop => prop.IsDefined(typeof(AutoReferenceResourceAttribute), false));

        private IEnumerable<FieldInfo> AllExternalSubassetResourceFields => GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(prop => prop.IsDefined(typeof(ExternalResourceAttribute), false));

        private IEnumerable<FieldInfo> AllNeededFields => AllSubassetResourceFields.Concat(AllAutoReferenceResourceFields).Concat(AllExternalSubassetResourceFields);

        private void OnValidate()
        {
            bool anyAssetMissing = false;

            foreach (var field in AllSubassetResourceFields)
            {
                var fieldValue = field.GetValue(this);
                if (fieldValue == null)
                {
                    anyAssetMissing = true;
                    break;
                }

                if (fieldValue is UnityEngine.Object unityObject)
                {
                    if (unityObject == null)
                    {
                        anyAssetMissing = true;
                        break;
                    }
                }
            }

            foreach (var field in AllAutoReferenceResourceFields)
            {
                var fieldValue = field.GetValue(this);
                if (fieldValue == null)
                {
                    anyAssetMissing = true;
                    break;
                }

                if (fieldValue is UnityEngine.Object unityObject)
                {
                    if (unityObject == null)
                    {
                        anyAssetMissing = true;
                        break;
                    }
                }
            }

            if (anyAssetMissing)
            {
                RecreateAssets();
#if UNITY_EDITOR
                AssetDatabase.SaveAssets();
#endif
            }

            foreach (var field in AllNeededFields)
            {
                var fieldValue = field.GetValue(this);
                if (fieldValue == null)
                {
                    if (field.IsDefined(typeof(SubassetResourceAttribute)))
                        Debug.LogError($"Asset in the field {field.Name} is not properly created from the script.", this);

                    if (field.IsDefined(typeof(AutoReferenceResourceAttribute)))
                        Debug.LogError($"Reference in the field {field.Name} is not properly set from the script.", this);

                    if (field.IsDefined(typeof(ExternalResourceAttribute)))
                        Debug.LogError($"Reference in the field {field.Name} is not set. Please set the field using the inspector", this);
                    continue;
                }

                if (fieldValue is UnityEngine.Object unityObject)
                {
                    if (unityObject == null)
                    {
                        anyAssetMissing = true;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Notifies that the object field is supplied by the code in <see cref="ResourcesScriptableObject{T}.RecreateAssets"/> method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class SubassetResourceAttribute : Attribute { }

    /// <summary>
    /// Notifies that the object field is supplied by the code in <see cref="ResourcesScriptableObject{T}.RecreateAssets"/> method, but is not serialized as an subasset.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class AutoReferenceResourceAttribute : Attribute { }

    /// <summary>
    /// Notifies that the object field is supplied by the reference in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class ExternalResourceAttribute : Attribute { }
    public sealed class CreateIfNotExistAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CreateInFolderWithAttribute : System.Attribute
    {
        public Type targetType;
        public CreateInFolderWithAttribute(Type targetType)
        {
            this.targetType = targetType;
        }
    }

    public abstract class SingletonScriptableObject<SingletonType> : ScriptableObject where SingletonType : SingletonScriptableObject<SingletonType>
    {
        private static SingletonType instance;
        public static SingletonType Instance
        {
            get
            {
                if (instance == null)
                    instance = GetInstance();

                return instance;
            }
        }

        protected virtual void OnEnable()
        {
            if (instance != null)
                if (instance != this)
                    Debug.LogError($"There is more than one instance of {typeof(SingletonType).FullName}");

            instance = (SingletonType)this;
        }

        protected virtual void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        public static SingletonType GetInstance()
        {
            var obj = AssetDatabase.FindAssets($"t:{typeof(SingletonType).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(SingletonType)))
                .FirstOrDefault();

            if (obj == null)
            {
                bool createIfNotExist = typeof(SingletonType).IsDefined(typeof(CreateIfNotExistAttribute), true);

                if (createIfNotExist)
                {
                    CreateInstance();
                    return instance;
                }
                else
                {
                    //Debug.LogError($"Tried to create {typeof(SingletonType).Name} singleton but not found any asset");
                    return null;
                }

            }
            else if (obj is SingletonType assetInstance)
                return assetInstance;
            else
                throw new Exception($"Not known error when trying to find singleton object for {typeof(SingletonType).Name}");
        }

        protected static void CreateInstance()
        {
            if (instance != null)
                throw new Exception("Instance already exist");

            string prefferedFolderPath = null;

            if (typeof(SingletonType).IsDefined(typeof(CreateInFolderWithAttribute)))
            {
                CreateInFolderWithAttribute attribute = typeof(SingletonType).GetCustomAttribute(typeof(CreateInFolderWithAttribute)) as CreateInFolderWithAttribute;

                var foundAssets = AssetDatabase.FindAssets($"t:{attribute.targetType.Name}");
                if (foundAssets.Length > 0)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                    var folderPath = PathUtils.GetContainingFolderPath(assetPath); 
                    if (AssetDatabase.IsValidFolder(folderPath))
                        prefferedFolderPath = folderPath;
                } 
            } 

            if (prefferedFolderPath == null)
                prefferedFolderPath = "Assets";

            SingletonType newInstance = (SingletonType)CreateInstance(typeof(SingletonType));
            AssetDatabase.CreateAsset(newInstance, Path.Combine(prefferedFolderPath, typeof(SingletonType).Name + ".asset"));
            instance = newInstance;
        }
    }
}