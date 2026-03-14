using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("����ֵ����")]
    public float maxHealth;
    public float currentHealth;
    
    public  System.Action<float, float> OnHealthChanged;

    /// <summary>
    /// ����Ѫ��
    /// </summary>
    /// <param name="amount">�ܵ����˺�</param>
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

        //�������Ҿ͸���Ѫ��
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
    /// Ѫ��Ϊ0ʱ����������
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
