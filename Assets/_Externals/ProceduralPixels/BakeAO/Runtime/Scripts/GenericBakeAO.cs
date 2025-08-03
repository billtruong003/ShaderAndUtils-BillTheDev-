/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using UnityEngine;

namespace ProceduralPixels.BakeAO
{
    [HelpURL("https://proceduralpixels.com/BakeAO/Documentation/BakeAOComponent")]
    [ExecuteAlways, DisallowMultipleComponent]
    public abstract class GenericBakeAO : MonoBehaviour
    {
        [Tooltip("Ambient occlusion texture that should be applied to the model.")] [SerializeField] protected Texture2D ambientOcclusionTexture;
        [Tooltip("Occlusion Strength parameter controls the ambient occlusion application strength. Default value is 1.0.")] [SerializeField] [Range(0.0f, 2.0f)] protected float occlusionStrength = 1.0f;
        [Tooltip("UV set of a model that will be used to sample ambient occlusion texture. It should be a UV Set that the texture was baked for.")] [SerializeField] protected UVChannel occlusionUVSet = UVChannel.UV0;
        [Tooltip("Decides if the ambient occlusion texture should be applied also into a diffuse texture. By default AO texture is applied only to the ambient lighting.")] [SerializeField] protected bool applyOcclusionIntoDiffuse = false;

        public Texture2D AmbientOcclusionTexture
        {
            get => ambientOcclusionTexture;
            set
            {
                ambientOcclusionTexture = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        public float OcclusionStrength
        {
            get => occlusionStrength;
            set
            {
                occlusionStrength = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        public UVChannel OcclusionUVSet
        {
            get => occlusionUVSet;
            set
            {
                occlusionUVSet = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        public bool ApplyOcclusionIntoDiffuse
        {
            get => applyOcclusionIntoDiffuse;
            set
            {
                applyOcclusionIntoDiffuse = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        [HideInInspector] public Renderer rendererRef;

        public static readonly int occlusionMapStandardID = Shader.PropertyToID("_OcclusionMap");
        public static readonly int occlusionStrengthStandardID = Shader.PropertyToID("_OcclusionStrength");
        public static readonly int occlusionUVSetStandardID = Shader.PropertyToID("_AOTextureUV");
        public static readonly int applyOcclusionToDiffuseStandardID = Shader.PropertyToID("_MultiplyAlbedoAndOcclusion");

        protected MaterialPropertyBlock propertyBlock;

        protected void Awake()
        {
            if (rendererRef == null)
                TryGetComponent(out rendererRef);
        }

        protected void OnEnable()
        {
            UpdateAmbientOcclusionProperties();
        }

        public void UpdateAmbientOcclusionProperties()
        {
            if (!enabled)
                return;

            if (rendererRef == null || ambientOcclusionTexture == null)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            propertyBlock.Clear();

            SetupPropertyBlock(propertyBlock);

            rendererRef.SetPropertyBlock(propertyBlock);
        }

        protected abstract void SetupPropertyBlock(MaterialPropertyBlock propertyBlock);

        public void Refresh()
        {
            OnDisable();
            if (isActiveAndEnabled)
                OnEnable();
        }

        protected void OnValidate()
        {
            if (rendererRef == null)
                TryGetComponent(out rendererRef);

            if (ambientOcclusionTexture == null)
                OnDisable();

            UpdateAmbientOcclusionProperties();
        }

        protected void OnDisable()
        {
            if (rendererRef == null)
                return;

            if (propertyBlock == null)
                return;

            propertyBlock.Clear();
            rendererRef.SetPropertyBlock(null);
        }

#if UNITY_EDITOR
        // Used in editor scripts to check if the texture is broken, which can happen when user modifies/deletes the texture from the assets.
        public bool HasInvalidTexture()
        {
            if (propertyBlock != null)
            {
                var aoTexture = propertyBlock.GetTexture(occlusionMapStandardID);
                return aoTexture == null;
            }

            return false;
        }
#endif
    }
}
