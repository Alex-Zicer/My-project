using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{    
    /// <summary>
    /// 接收伤害
    /// </summary>
    /// <param name="rawDamage">原始伤害</param>
    public void TakeDamage(float rawDamage);
}
