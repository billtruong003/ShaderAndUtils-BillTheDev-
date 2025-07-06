using UnityEngine;
using System.Collections.Generic;

public class FloatingTextPool : MonoBehaviour
{
    public static FloatingTextPool Instance { get; private set; }

    [SerializeField] private FloatingText floatingTextPrefab;
    [SerializeField] private int initialPoolSize = 20;

    private Queue<FloatingText> pool = new Queue<FloatingText>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewTextObject();
        }
    }

    public void ShowText(string text, Vector3 position, Color color)
    {
        if (pool.Count == 0) CreateNewTextObject();
        FloatingText textToShow = pool.Dequeue();
        textToShow.transform.position = position;
        textToShow.transform.rotation = Camera.main.transform.rotation;
        textToShow.gameObject.SetActive(true);
        textToShow.Show(text, color, ReturnToPool);
    }

    private void CreateNewTextObject()
    {
        FloatingText newText = Instantiate(floatingTextPrefab, transform);
        newText.gameObject.SetActive(false);
        pool.Enqueue(newText);
    }

    private void ReturnToPool(FloatingText textObject)
    {
        textObject.gameObject.SetActive(false);
        pool.Enqueue(textObject);
    }
}