using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField]
    private Health playerHealth;

    [Header("UI引用")]
    public Slider hpSlider;//实际血条
    public Image ghostFill;//缓冲血条

    [Header("平滑参数")]
    public float lerpSpeed = 0.05f;//缓冲速度
    public float waitBeforeDrop = 0.5f;//受伤之后先停顿多久才开始掉残影

    private Coroutine ghostCoroutine;

    private void OnEnable()
    {
        //订阅事件
        playerHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        //取消订阅事件，防止内存泄露
        playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void Start()
    {
        UpdateHealthBar(playerHealth.currentHealth, playerHealth.maxHealth);
    }

    /// <summary>
    /// 更新血条
    /// </summary>
    /// <param name="current">当前的血量</param>
    /// <param name="max">最大血量</param>
    private void UpdateHealthBar(float current, float max)
    {
        hpSlider.maxValue = max;
        float targetValue = current;

        if (targetValue >= hpSlider.value)
        {
            StopGhostCoroutine();
            hpSlider.value = targetValue;
            ghostFill.fillAmount = targetValue / max;
        }
        else
        {
            hpSlider.value = targetValue;

            StopGhostCoroutine();
            ghostCoroutine = StartCoroutine(SmoothDropGhost(targetValue / max));
        }
    }

    /// <summary>
    /// 血条减少后残影追赶血条的协程
    /// </summary>
    /// <param name="targetFillAmount">目标血条，也是实际血量</param>
    /// <returns></returns>
    private IEnumerator SmoothDropGhost(float targetFillAmount)
    {
        yield return new WaitForSeconds(waitBeforeDrop);

        while (ghostFill.fillAmount > targetFillAmount + 0.001f)
        {
            ghostFill.fillAmount = Mathf.MoveTowards(ghostFill.fillAmount, targetFillAmount, lerpSpeed * Time.deltaTime);
            yield return null;
        }

        //最后一点强制对齐
        ghostFill.fillAmount = targetFillAmount;
        ghostCoroutine = null;//清除协程
    }

    /// <summary>
    /// 停止协程并清空引用
    /// </summary>
    private void StopGhostCoroutine()
    {
        if (ghostCoroutine != null)
        {
            StopCoroutine(ghostCoroutine);
            ghostCoroutine = null;
        }
    }
}
