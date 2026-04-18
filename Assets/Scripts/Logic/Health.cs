using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth;       // 最大生命值。
    public float currentHealth;   // 当前生命值。

    public System.Action<float, float> OnHealthChanged; // 生命变化事件：current, max。

    /// <summary>
    /// 更新血量
    /// </summary>
    /// <param name="amount">受到的伤害</param>
    public void UpdateHealth(float amount)
    {
        // 保护：无效伤害直接忽略。
        if (amount <= 0f)
        {
            return;
        }

        // 受伤后保证血量不低于 0。
        if (currentHealth - amount < 0)
        {
            currentHealth = 0;
        }
        else
        {
            currentHealth -= amount;
        }

        // 如果是玩家就更新血条
        if (gameObject.CompareTag("Player"))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (currentHealth == 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 回复生命值并触发血量更新事件。
    /// </summary>
    /// <param name="amount">回复量</param>
    public void Heal(float amount)
    {
        // 保护：无效回血直接忽略。
        if (amount <= 0f)
        {
            return;
        }

        // 回血上限不超过最大生命值。
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (gameObject.CompareTag("Player"))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// 设置最大生命值；可选是否把当前生命值补满。
    /// </summary>
    /// <param name="newMaxHealth">新的最大生命值</param>
    /// <param name="fillCurrentToMax">是否同步补满当前生命值</param>
    public void SetMaxHealth(float newMaxHealth, bool fillCurrentToMax = false)
    {
        // 最大生命值不允许小于 0。
        maxHealth = Mathf.Max(newMaxHealth, 0f);

        if (fillCurrentToMax)
        {
            // 需要补满时，直接把当前生命同步到上限。
            currentHealth = maxHealth;
        }
        else
        {
            // 不补满时，把当前生命钳制在合法范围内。
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        if (gameObject.CompareTag("Player"))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// 血量为0时的死亡函数
    /// </summary>
    private void Die()
    {
        if (!gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
        else
        {
        }
    }
}
