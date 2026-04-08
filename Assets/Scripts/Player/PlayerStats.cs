using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 玩家存档组件：保存与恢复玩家位置和当前血量。
/// </summary>
public class PlayerStats : SaveableBehaviour
{
    private const float MinHealthValue = 0f; // 血量下限。

    // 玩家生命组件引用。
    [SerializeField] private Health _health;

    // 位置记录目标（默认使用当前 Transform）。
    [SerializeField] private Transform _targetTransform;

    [System.Serializable]
    private class PlayerState
    {
        // 玩家位置 X。
        public float posX;

        // 玩家位置 Y。
        public float posY;

        // 玩家位置 Z。
        public float posZ;

        // 玩家当前血量。
        public float currentHealth;
    }

    /// <summary>
    /// Unity 生命周期：缓存依赖后注册到 SaveManager。
    /// </summary>
    protected override void Awake()
    {
        if (_targetTransform == null)
        {
            _targetTransform = transform;
        }

        if (_health == null)
        {
            _health = GetComponent<Health>();
        }

        base.Awake();
    }

    /// <summary>
    /// 捕获玩家当前状态。
    /// </summary>
    /// <returns>玩家状态对象。</returns>
    public override object CaptureState()
    {
        Vector3 position = _targetTransform != null ? _targetTransform.position : Vector3.zero;
        float healthValue = _health != null ? _health.currentHealth : MinHealthValue;

        return new PlayerState
        {
            posX = position.x,
            posY = position.y,
            posZ = position.z,
            currentHealth = Mathf.Max(healthValue, MinHealthValue)
        };
    }

    /// <summary>
    /// 恢复玩家状态。
    /// </summary>
    /// <param name="state">玩家状态对象。</param>
    public override void RestoreState(object state)
    {
        PlayerState playerState = ConvertState<PlayerState>(state);
        if (playerState == null)
        {
            Debug.LogWarning("[PlayerStats] RestoreState 失败：状态数据为空或格式不正确。");
            return;
        }

        if (_targetTransform != null)
        {
            _targetTransform.position = new Vector3(playerState.posX, playerState.posY, playerState.posZ);
        }

        if (_health != null)
        {
            float maxHealth = Mathf.Max(_health.maxHealth, MinHealthValue);
            float restoredHealth = Mathf.Clamp(playerState.currentHealth, MinHealthValue, maxHealth);
            _health.currentHealth = restoredHealth;
            _health.OnHealthChanged?.Invoke(_health.currentHealth, _health.maxHealth);
        }
    }

    /// <summary>
    /// 将 object 状态安全转换为目标类型。
    /// </summary>
    /// <typeparam name="T">目标状态类型。</typeparam>
    /// <param name="state">原始状态对象。</param>
    /// <returns>转换后的状态对象。</returns>
    private static T ConvertState<T>(object state) where T : class
    {
        if (state == null)
        {
            return null;
        }

        if (state is T typed)
        {
            return typed;
        }

        if (state is JObject jObject)
        {
            return jObject.ToObject<T>();
        }

        if (state is JToken jToken)
        {
            return jToken.ToObject<T>();
        }

        if (state is string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        string fallbackJson = JsonConvert.SerializeObject(state);
        return JsonConvert.DeserializeObject<T>(fallbackJson);
    }
}
