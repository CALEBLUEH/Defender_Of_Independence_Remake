using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelThreeHealthDisplay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text valueText;
    [SerializeField, Min(1)] private int maximumHealth = 3;
    [SerializeField, Min(0f)] private float showDuration = 0.35f;

    private Coroutine fadeRoutine;

    public int CurrentHealth { get; private set; }
    public int MaximumHealth => maximumHealth;
    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.99f;

    private void Awake()
    {
        CurrentHealth = maximumHealth;
        Refresh();
        SetAlpha(0f);
    }

    private void OnDisable()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    public void Show()
    {
        if (!isActiveAndEnabled) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeToVisible());
    }

    public void ResetHealth()
    {
        CurrentHealth = maximumHealth;
        Refresh();
    }

    public bool LoseHealth(int amount = 1)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - Mathf.Max(0, amount), 0, maximumHealth);
        Refresh();
        return CurrentHealth <= 0;
    }

    private IEnumerator FadeToVisible()
    {
        float from = canvasGroup != null ? canvasGroup.alpha : 0f;
        if (showDuration <= 0f)
        {
            SetAlpha(1f);
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, 1f, Mathf.Clamp01(elapsed / showDuration)));
            yield return null;
        }
        SetAlpha(1f);
        fadeRoutine = null;
    }

    private void Refresh()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maximumHealth;
            healthSlider.SetValueWithoutNotify(CurrentHealth);
        }
        if (valueText != null) valueText.text = $"{CurrentHealth} / {maximumHealth}";
    }

    private void SetAlpha(float value)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = value;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maximumHealth = Mathf.Max(1, maximumHealth);
        showDuration = Mathf.Max(0f, showDuration);
    }
#endif
}
