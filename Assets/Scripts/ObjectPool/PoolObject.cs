using System;
using System.Collections;
using UnityEngine;

public class PoolObject : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private int poolId = 0;
    [SerializeField] private int arrayIndex = 0;
    [SerializeField] private ObjectPooling objectPooling;
    public void SetObjectPool(ObjectPooling value) => objectPooling = value;
    public int ArrayIndex
    {
        get => arrayIndex;
        set
        {
            if (arrayIndex == 0 && value != 0)
            {
                arrayIndex = value;
            }
            else
            {
                Debug.LogError("Not allowed to change ArrayIndex.");
            }
        }
    }
    public int PoolId
    {
        get => poolId;
        set
        {
            if (poolId == 0 && value != 0)
            {
                poolId = value;
            }
            else
            {
                Debug.LogError("Not allowed to change PoolId.");
            }
        }
    }
    private void OnEnable()
    {
        StartCoroutine(LifeTimeCoroutine());
    }

    private IEnumerator LifeTimeCoroutine()
    {
        yield return new WaitForSeconds(lifeTime);
        if (objectPooling != null)
        {
            objectPooling.ReturnToPool(this);
        }
        else
        {
            Debug.LogError("ObjectPooling reference is null.");
        }
    }


}
