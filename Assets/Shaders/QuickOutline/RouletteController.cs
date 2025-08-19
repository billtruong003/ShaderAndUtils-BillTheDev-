using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BillTheDev.QuickOutline;
using Sirenix.OdinInspector;

public class RouletteController : MonoBehaviour
{
    [Title("Roulette Targets")]
    [Required, SceneObjectsOnly]
    [SerializeField] private List<GameObject> rouletteObjects;

    [Title("Configuration")]
    [Required, AssetsOnly]
    [SerializeField] private OutlineConfiguration outlineConfiguration;

    [SerializeField, Range(0.1f, 5f)]
    private float switchInterval = 0.75f;

    [Title("Shader Dependencies")]
    [Required, AssetsOnly]
    [SerializeField, Tooltip("Assign the Outline Mask shader asset here.")]
    private Shader outlineMaskShader;

    [Required, AssetsOnly]
    [SerializeField, Tooltip("Assign the Outline Fill shader asset here.")]
    private Shader outlineFillShader;

    private readonly List<BillTheDev.QuickOutline.Outline> cachedOutlines = new List<BillTheDev.QuickOutline.Outline>();
    private Coroutine rouletteCoroutine;
    private bool isInitialized = false;

    private void OnEnable()
    {
        Initialize();
        if (isInitialized)
        {
            rouletteCoroutine = StartCoroutine(RouletteLoop());
        }
    }

    private void OnDisable()
    {
        if (rouletteCoroutine != null)
        {
            StopCoroutine(rouletteCoroutine);
            rouletteCoroutine = null;
        }
        CleanupOutlines();
    }

    private void Initialize()
    {
        if (isInitialized) return;
        if (rouletteObjects == null || rouletteObjects.Count == 0)
        {
            Debug.LogWarning("Roulette object list is empty. Halting initialization.", this);
            return;
        }

        cachedOutlines.Clear();
        foreach (var targetObject in rouletteObjects)
        {
            if (targetObject == null)
            {
                Debug.LogWarning("A null object was found in the roulette list. It will be skipped.", this);
                continue;
            }

            var outline = targetObject.AddComponent<BillTheDev.QuickOutline.Outline>();
            outline.Configure(outlineMaskShader, outlineFillShader, outlineConfiguration);
            outline.enabled = false;
            cachedOutlines.Add(outline);
        }

        if (cachedOutlines.Count > 0)
        {
            isInitialized = true;
        }
    }

    private void CleanupOutlines()
    {
        foreach (var outline in cachedOutlines)
        {
            if (outline != null)
            {
                Destroy(outline);
            }
        }
        cachedOutlines.Clear();
        isInitialized = false;
    }

    private IEnumerator RouletteLoop()
    {
        if (cachedOutlines.Count == 0)
        {
            yield break;
        }

        int currentIndex = -1;
        int previousIndex = -1;

        while (true)
        {
            previousIndex = currentIndex;
            currentIndex = (currentIndex + 1) % cachedOutlines.Count;

            if (previousIndex >= 0)
            {
                cachedOutlines[previousIndex].enabled = false;
            }

            cachedOutlines[currentIndex].enabled = true;

            yield return new WaitForSeconds(switchInterval);
        }
    }
}