using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    #region ObjectToPool Class
    [Serializable]
    public class ObjectToPool
    {
        public Transform parent;
        public GameObject Prefab;
        public int InitialSize;
        public bool Expandable;

        public ObjectToPool(GameObject prefab, int initialSize, bool expandable)
        {
            Prefab = prefab;
            InitialSize = initialSize;
            Expandable = expandable;
        }

        private PoolObject[] initArrPoolObject;
        private PoolObject tempPoolObject;
        public PoolObject[] Init(ObjectPooling objectPooling)
        {
            initArrPoolObject = new PoolObject[InitialSize];
            for (int i = 0; i < InitialSize; i++)
            {
                GameObject obj = Instantiate(Prefab, parent);
                tempPoolObject = obj.GetComponent<PoolObject>();
                tempPoolObject.SetObjectPool(objectPooling);
                initArrPoolObject[i] = tempPoolObject;
                obj.SetActive(false);
            }

            return initArrPoolObject;
        }
    }
    #endregion
    [SerializeField] private ObjectToPool[] objectsToPool;
    [SerializeField] private List<PoolObject[]> pooledObjectArrays = new List<PoolObject[]>();
    [SerializeField] private List<PoolObject> activeObject = new List<PoolObject>();


    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GetRandomPooledObject();
        }
    }

    private void GetRandomPooledObject()
    {
        if (pooledObjectArrays.Count == 0)
        {
            Debug.LogError("No pooled objects available.");
            return;
        }

        int randomPoolId = UnityEngine.Random.Range(0, pooledObjectArrays.Count);
        PoolObject[] selectedPool = pooledObjectArrays[randomPoolId];
        PoolObject poolObject = selectedPool[UnityEngine.Random.Range(0, selectedPool.Length)];
        if (poolObject == null || poolObject.gameObject.activeSelf)
        {
            Debug.LogError("No available PoolObject found in the selected pool.");
            return;
        }
        poolObject.gameObject.SetActive(true);
        poolObject.ArrayIndex = Array.IndexOf(selectedPool, poolObject);
        RandomPoseAndRotateFor(poolObject);
        pooledObjectArrays[randomPoolId][poolObject.ArrayIndex] = null;
        activeObject.Add(poolObject);
        Debug.Log($"Activated PoolObject: {poolObject.gameObject.name} from PoolId: {randomPoolId}");

    }

    private void RandomPoseAndRotateFor(PoolObject poolObject)
    {
        if (poolObject == null)
        {
            Debug.LogError("PoolObject is null.");
            return;
        }

        poolObject.transform.localPosition = UnityEngine.Random.insideUnitSphere * 5f;
        poolObject.transform.localRotation = UnityEngine.Random.rotation;
        poolObject.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.5f, 1.5f);
    }
    private void Initialize()
    {
        foreach (var objectToPool in objectsToPool)
        {
            PoolObject[] arrPool = new PoolObject[objectToPool.InitialSize];
            if (objectToPool == null || objectToPool.Prefab == null)
            {
                Debug.LogError("ObjectToPool or Prefab is not set.");
                continue;
            }
            arrPool = objectToPool.Init(this);
            foreach (var poolObject in arrPool)
            {
                if (poolObject == null)
                {
                    Debug.LogError("PoolObject is null after initialization.");
                    continue;
                }
                poolObject.PoolId = pooledObjectArrays.Count;
            }
            pooledObjectArrays.Add(arrPool);
        }
        foreach (var arr in pooledObjectArrays)
        {
            foreach (var poolObject in arr)
            {
                if (poolObject != null)
                {
                    Debug.Log($"Initialized PoolObject: {poolObject.gameObject.name}");
                }
            }
        }
    }

    public void ReturnToPool(PoolObject poolObject)
    {
        if (poolObject == null)
        {
            Debug.LogError("Attempted to return a null PoolObject to the pool.");
            return;
        }

        poolObject.gameObject.SetActive(false);
        activeObject.Remove(poolObject);
        pooledObjectArrays[poolObject.PoolId][poolObject.ArrayIndex] = poolObject;
    }
}