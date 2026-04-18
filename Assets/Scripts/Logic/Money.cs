using UnityEngine;

/// <summary>
/// 金钱数据组件：负责维护当前钱数并广播变更事件。
/// </summary>
public class Money : MonoBehaviour
{
    [Header("金钱设置")]
    [SerializeField] private int currentMoney; // 当前钱数。
    [SerializeField] private int minMoney = 0; // 钱数下限。

    public int CurrentMoney => currentMoney; // 对外只读当前钱数。

    public System.Action<int, int> OnMoneyChanged; // 金钱变化事件：(当前值, 变化量delta)。

    /// <summary>
    /// 增加金钱并广播变化事件。
    /// </summary>
    /// <param name="amount">增加数量</param>
    public void AddMoney(int amount)
    {
        // 保护：无效增加值直接忽略。
        if (amount <= 0)
        {
            return;
        }

        int oldMoney = currentMoney;
        currentMoney += amount;
        BroadcastMoneyChanged(oldMoney);
    }

    /// <summary>
    /// 消费金钱（只改数值，不负责动画）。
    /// </summary>
    /// <param name="amount">消费数量</param>
    /// <returns>扣款成功返回 true，余额不足返回 false</returns>
    public bool SpendMoney(int amount)
    {
        // 保护：无效消费值视为失败。
        if (amount <= 0)
        {
            return false;
        }

        if (currentMoney < amount)
        {
            return false;
        }

        int oldMoney = currentMoney;
        currentMoney = Mathf.Max(currentMoney - amount, minMoney);
        BroadcastMoneyChanged(oldMoney);
        return true;
    }

    /// <summary>
    /// 直接设置当前金钱并广播变化事件。
    /// </summary>
    /// <param name="value">目标钱数</param>
    public void SetMoney(int value)
    {
        int oldMoney = currentMoney;
        currentMoney = Mathf.Max(value, minMoney);
        BroadcastMoneyChanged(oldMoney);
    }

    /// <summary>
    /// 判断当前是否有足够金钱进行消费。
    /// </summary>
    /// <param name="cost">消费金额</param>
    /// <returns>足够返回 true</returns>
    public bool CanAfford(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        return currentMoney >= cost;
    }

    /// <summary>
    /// 对外广播金钱变化（delta 为新旧差值）。
    /// </summary>
    /// <param name="oldMoney">变化前钱数</param>
    private void BroadcastMoneyChanged(int oldMoney)
    {
        int delta = currentMoney - oldMoney;
        if (delta == 0)
        {
            return;
        }

        OnMoneyChanged?.Invoke(currentMoney, delta);
    }
}