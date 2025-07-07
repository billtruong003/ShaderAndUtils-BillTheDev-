using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AfterImage : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField]
    bool RandomColor = false;
    [SerializeField]
    float delay = 0.1f;
    [SerializeField]
    float afterImageDuration = 1f;
    [SerializeField]
    string fadeProperty = "_Fade";
    [SerializeField]
    string colorProperty = "_Color";
    [SerializeField]
    string rimColorProperty = "_RimColor"; // New: Property for Rim Color
    [SerializeField]
    GameObject presetObj;
    [SerializeField]
    int poolSize = 100;

    float calculatedFadeSpeed;

    Vector3 lastPosition;

    SkinnedMeshRenderer[] skinRenderers;
    MeshRenderer[] meshRenderers;
    MeshFilter[] meshFilters;

    List<GameObject> objectPool;
    MeshFilter[] poolMeshFilters;
    Renderer[] poolRenderers;
    MaterialPropertyBlock propBlock;

    float timer;
    float[] currentFadeAmounts;

    Matrix4x4 matrix;
    CombineInstance[] combineInstances;

    void Start()
    {
        if (afterImageDuration <= 0)
        {
            Debug.LogWarning("AfterImageDuration must be greater than 0. Setting to 1s.", this);
            afterImageDuration = 1f;
        }
        calculatedFadeSpeed = 1f / afterImageDuration;

        SetUpRenderers();
        InitializePool();
        lastPosition = transform.position;
    }

    void SetUpRenderers()
    {
        skinRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        meshFilters = new MeshFilter[meshRenderers.Length];
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshFilters[i] = meshRenderers[i].GetComponent<MeshFilter>();
        }

        combineInstances = new CombineInstance[skinRenderers.Length + meshRenderers.Length];
    }

    void InitializePool()
    {
        propBlock = new MaterialPropertyBlock();

        objectPool = new List<GameObject>(poolSize);
        poolMeshFilters = new MeshFilter[poolSize];
        poolRenderers = new Renderer[poolSize];
        currentFadeAmounts = new float[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(presetObj, Vector3.zero, Quaternion.identity, parent);

            poolMeshFilters[i] = obj.GetComponent<MeshFilter>();
            if (poolMeshFilters[i] == null)
            {
                Debug.LogError("AfterImage: presetObj must have a MeshFilter component!", presetObj);
                Destroy(this);
                return;
            }

            poolRenderers[i] = obj.GetComponent<Renderer>();
            if (poolRenderers[i] == null)
            {
                Debug.LogError("AfterImage: presetObj must have a Renderer component!", presetObj);
                Destroy(this);
                return;
            }

            poolMeshFilters[i].mesh = new Mesh();

            obj.SetActive(false);
            objectPool.Add(obj);
            currentFadeAmounts[i] = 0f;
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;

        float currentMovementMagnitude = (transform.position - lastPosition).magnitude;
        bool isMoving = currentMovementMagnitude > 0.01f;

        if (timer <= 0 && isMoving)
        {
            timer = delay;
            CreateAfterImage();
        }

        lastPosition = transform.position;

        for (int i = 0; i < poolSize; i++)
        {
            if (objectPool[i].activeInHierarchy)
            {
                currentFadeAmounts[i] += Time.deltaTime * calculatedFadeSpeed;

                poolRenderers[i].GetPropertyBlock(propBlock);
                propBlock.SetFloat(fadeProperty, currentFadeAmounts[i]);
                poolRenderers[i].SetPropertyBlock(propBlock);

                if (currentFadeAmounts[i] <= 0f)
                {
                    objectPool[i].SetActive(false);
                    if (poolMeshFilters[i].mesh != null)
                    {
                        poolMeshFilters[i].mesh.Clear();
                    }
                }
            }
        }
    }

    public (GameObject, int) GetPooledObject()
    {
        for (int i = 0; i < objectPool.Count; i++)
        {
            if (!objectPool[i].activeInHierarchy)
            {
                return (objectPool[i], i);
            }
        }
        Debug.LogWarning("AfterImage: Object pool exhausted! Consider increasing poolSize.");
        return (null, -1);
    }

    void CreateAfterImage()
    {
        (GameObject afterImageObj, int poolIndex) = GetPooledObject();

        if (afterImageObj == null)
        {
            return;
        }

        poolMeshFilters[poolIndex].mesh.Clear();

        matrix = transform.worldToLocalMatrix;

        for (int i = 0; i < skinRenderers.Length; i++)
        {
            Mesh bakedMesh = new Mesh();
            skinRenderers[i].BakeMesh(bakedMesh);
            combineInstances[i].mesh = bakedMesh;
            combineInstances[i].transform = matrix * skinRenderers[i].localToWorldMatrix;
        }

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            int combineIndex = skinRenderers.Length + i;
            combineInstances[combineIndex].mesh = meshFilters[i].sharedMesh;
            combineInstances[combineIndex].transform = matrix * meshRenderers[i].transform.localToWorldMatrix;
        }

        poolMeshFilters[poolIndex].mesh.CombineMeshes(combineInstances, false, true);

        poolRenderers[poolIndex].GetPropertyBlock(propBlock);
        currentFadeAmounts[poolIndex] = 1f;
        propBlock.SetFloat(fadeProperty, currentFadeAmounts[poolIndex]);

        if (RandomColor)
        {
            Color randomBaseColor = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f); // Base color (high saturation, high value)
            propBlock.SetColor(colorProperty, randomBaseColor);

            // For Rim Color, you might want a brighter, more saturated version or a slightly different hue
            // Example 1: Brighter version of base color
            Color randomRimColor = randomBaseColor * 1.5f; // Make it brighter
            randomRimColor.a = 1f; // Ensure full alpha for rim
            propBlock.SetColor(rimColorProperty, randomRimColor);

            // Example 2 (Alternative): Completely new random color for rim, maybe more neon-like
            // Color randomRimColor = Random.ColorHSV(0f, 1f, 0.9f, 1f, 1f, 1f); // Very high saturation, full value
            // propBlock.SetColor(rimColorProperty, randomRimColor);
        }
        poolRenderers[poolIndex].SetPropertyBlock(propBlock);

        afterImageObj.transform.position = transform.position;
        afterImageObj.transform.rotation = transform.rotation;
        afterImageObj.SetActive(true);

        for (int i = 0; i < skinRenderers.Length; i++)
        {
            Destroy(combineInstances[i].mesh);
            combineInstances[i].mesh = null;
        }
    }
}