using System;
using UnityEngine;

/// <summary>
/// 可存档组件基类：自动完成注册与注销。
/// </summary>
public abstract class SaveableBehaviour : MonoBehaviour, ISaveable
{
    // 对象唯一 ID（建议在 Inspector 固定，不要随意变更）。
    [SerializeField] private string _uniqueId;

    /// <summary>
    /// 获取对象唯一 ID。
    /// </summary>
    /// <returns>对象唯一 ID。</returns>
    public string GetUniqueId() => _uniqueId;

    /// <summary>
    /// Unity 生命周期：自动注册到 SaveManager。
    /// </summary>
    protected virtual void Awake()
    {
        EnsureUniqueIdIfNeeded();
        SaveManager.Instance.Register(_uniqueId, this);
    }

    /// <summary>
    /// Unity 生命周期：对象销毁时自动从 SaveManager 注销。
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (!SaveManager.HasInstance)
        {
            return;
        }

        SaveManager.Instance.Unregister(_uniqueId);
    }

    /// <summary>
    /// 捕获当前组件状态。
    /// </summary>
    /// <returns>可序列化状态对象。</returns>
    public abstract object CaptureState();

    /// <summary>
    /// 恢复当前组件状态。
    /// </summary>
    /// <param name="state">状态对象。</param>
    public abstract void RestoreState(object state);

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下保证唯一 ID 非空。
    /// </summary>
    protected virtual void OnValidate()
    {
        EnsureUniqueIdIfNeeded();
    }
#endif

    /// <summary>
    /// 若 uniqueId 为空则自动生成。
    /// </summary>
    private void EnsureUniqueIdIfNeeded()
    {
        if (!string.IsNullOrWhiteSpace(_uniqueId))
        {
            return;
        }

        _uniqueId = Guid.NewGuid().ToString("N");
    }
}
