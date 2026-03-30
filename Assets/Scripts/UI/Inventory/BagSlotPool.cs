using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包格子对象池。预热一批 BagSlotView，借出时从队列取，归还时放回队列，避免频繁 Instantiate/Destroy。
/// </summary>
public class BagSlotPool : MonoBehaviour
{
    [SerializeField] private BagSlotView slotPrefab;

    [Tooltip("池中隐藏格子的挂载节点，建议放在 Canvas 下一个不可见的空节点")]
    [SerializeField] private Transform poolRoot;

    [Tooltip("预热数量")]
    [SerializeField] private int preWarmCount = 36;

    /// <summary> 预热数量，供外部计算最小格子数使用。 </summary>
    public int PreWarmCount => preWarmCount;

    private readonly Queue<BagSlotView> _idle = new Queue<BagSlotView>();
    private readonly List<BagSlotView> _active = new List<BagSlotView>();

    private void Awake()
    {
        PreWarm();
    }

    /// <summary>
    /// 预热：提前创建指定数量的格子并放入空闲队列。
    /// </summary>
    private void PreWarm()
    {
        for (int i = 0; i < preWarmCount; i++)
        {
            BagSlotView slot = CreateSlot();
            slot.gameObject.SetActive(false);
            _idle.Enqueue(slot);
        }
    }

    /// <summary>
    /// 从池中借出一个格子，挂到指定父节点下并激活。
    /// 若空闲队列为空则动态扩容创建新格子。
    /// </summary>
    /// <param name="parent">格子要挂载的父节点（ScrollRect 的 Content）</param>
    public BagSlotView Get(Transform parent)
    {
        BagSlotView slot = _idle.Count > 0 ? _idle.Dequeue() : CreateSlot();

        slot.transform.SetParent(parent, false);
        slot.gameObject.SetActive(true);
        _active.Add(slot);
        return slot;
    }

    /// <summary>
    /// 归还单个格子：清空数据，移回池根节点，放入空闲队列。
    /// </summary>
    public void Return(BagSlotView slot)
    {
        if (slot == null) return;

        slot.Release();
        slot.transform.SetParent(poolRoot, false);
        slot.gameObject.SetActive(false);

        _active.Remove(slot);
        _idle.Enqueue(slot);
    }

    /// <summary>
    /// 归还所有当前使用中的格子。切换分类或关闭背包时调用。
    /// </summary>
    public void ReturnAll()
    {
        // 倒序遍历避免 Remove 时索引错位
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Return(_active[i]);
        }
    }

    private BagSlotView CreateSlot()
    {
        return Instantiate(slotPrefab, poolRoot);
    }
}
