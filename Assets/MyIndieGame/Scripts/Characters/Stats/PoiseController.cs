using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PoiseController : MonoBehaviour
{
    [SerializeField] private float maxPoise = 100f;
    [SerializeField] private float poiseRegenRate = 20f;
    [SerializeField] private float poiseRegenDelay = 3f;
    [SerializeField] private float staggerDuration = 1.5f;

    public UnityEvent OnStaggered;
    public UnityEvent OnStaggerEnd;

    private float currentPoise;
    private float lastHitTime;

    private void Start()
    {
        currentPoise = maxPoise;
    }

    private void Update()
    {
        if (Time.time > lastHitTime + poiseRegenDelay && currentPoise < maxPoise)
        {
            currentPoise = Mathf.MoveTowards(currentPoise, maxPoise, poiseRegenRate * Time.deltaTime);
        }
    }

    public void TakePoiseDamage(float damage)
    {
        if (currentPoise <= 0) return; // Đã bị stagger, không nhận thêm sát thương poise

        lastHitTime = Time.time;
        currentPoise -= damage;

        if (currentPoise <= 0)
        {
            currentPoise = 0;
            OnStaggered.Invoke();
            StartCoroutine(StaggerRoutine());
        }
    }

    private IEnumerator StaggerRoutine()
    {
        yield return new WaitForSeconds(staggerDuration);
        currentPoise = maxPoise; // Hồi đầy poise sau khi hết stagger
        OnStaggerEnd.Invoke();
    }
}