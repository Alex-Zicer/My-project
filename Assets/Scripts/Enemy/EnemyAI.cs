using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour, IDamageable
{
    public EnemyBaseData enemyBaseData;
    private Health health;

    void Start()
    {
        health = GetComponent<Health>();
        health.maxHealth = enemyBaseData.maxHealth;//初始化最大生命值
        health.currentHealth = health.maxHealth;//初始化生命值
    }

    public void TakeDamage(float rawDamage)
    {
        //计算最终受到的伤害
        float finalDamage = Mathf.Max(rawDamage - enemyBaseData.defence);
        health.UpdateHealth(finalDamage);
    }
}
