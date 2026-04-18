using UnityEngine;

/// <summary>
/// 场景中的可拾取金币组件。
/// 挂在金币掉落物上，当玩家进入触发器时增加金钱并销毁该金币对象。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MoneyPickup : MonoBehaviour
{
    [Header("金币拾取配置")]
    [Tooltip("玩家拾取后增加的金钱数量")]
    [SerializeField] private int amount = 1; // 本次拾取增加的金钱。

    [Tooltip("允许触发拾取的对象标签")]
    [SerializeField] private string playerTag = "Player"; // 可触发拾取的标签。

    private bool _isCollected; // 防止同一金币被重复拾取。

    /// <summary>
    /// Unity 物理回调：当 Collider2D 进入本触发器时执行拾取逻辑。
    /// </summary>
    /// <param name="other">进入触发器的碰撞体</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 已被拾取后直接忽略后续触发，防止多次加钱。
        if (_isCollected)
        {
            return;
        }

        // 仅允许玩家触发拾取。
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // 拾取数量必须大于 0。
        if (amount <= 0)
        {
            Debug.LogWarning($"金币对象 {name} 的 amount 配置无效，无法拾取。", this);
            return;
        }

        // 兼容玩家碰撞体在子节点的情况，向父级查找 Money 组件。
        Money money = other.GetComponentInParent<Money>();
        if (money == null)
        {
            Debug.LogWarning("玩家对象未找到 Money 组件，无法拾取金币。", this);
            return;
        }

        _isCollected = true;
        money.AddMoney(amount);
        Destroy(gameObject);
    }

    /// <summary>
    /// 在编辑器重置时自动把本对象 Collider2D 设为 Trigger。
    /// </summary>
    private void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            collider2D.isTrigger = true;
        }
    }

}