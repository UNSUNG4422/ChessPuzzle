using System.Collections;
using UnityEngine;

public class TitleJitter : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float minInterval = 2.0f;
    [SerializeField] private float maxInterval = 6.0f;

    [Header("Jitter")]
    [SerializeField] private int jitterCount = 4;
    [SerializeField] private float jitterDuration = 0.035f;
    [SerializeField] private float positionAmount = 4f;
    [SerializeField] private float rotationAmount = 1.5f;

    private RectTransform rectTransform;
    private Vector3 originalAnchoredPosition;
    private Quaternion originalRotation;
    private Coroutine routine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;
    }

    private void OnEnable()
    {
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;
        routine = StartCoroutine(JitterLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localRotation = originalRotation;
    }

    private IEnumerator JitterLoop()
    {
        while (true)
        {
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);

            yield return JitterOnce();
        }
    }

    private IEnumerator JitterOnce()
    {
        for (int i = 0; i < jitterCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * positionAmount;
            float angle = Random.Range(-rotationAmount, rotationAmount);

            rectTransform.anchoredPosition = originalAnchoredPosition + new Vector3(offset.x, offset.y, 0f);
            rectTransform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);

            yield return new WaitForSeconds(jitterDuration);
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localRotation = originalRotation;
    }
}