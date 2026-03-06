using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth;
    public float currentHealth;
    
    public  System.Action<float, float> OnHealthChanged;

    /// <summary>
    /// 更新血量
    /// </summary>
    /// <param name="amount">受到的伤害</param>
    public void UpdateHealth(float amount)
    {
        if (currentHealth - amount < 0)
        {
            currentHealth = 0;
        }
        else
        {
            currentHealth -= amount;
        }

        //如果是玩家就更新血条
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
