using UnityEngine;

/// <summary>
/// 玩家掉入空挡后扣血并传回出生点。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class FallRespawnZone : MonoBehaviour
{
    [SerializeField] private Transform _respawnPoint;
    [SerializeField] private float _fallDamage = 1f;

    /// <summary>
    /// 配置掉落区的重生点与伤害值。
    /// </summary>
    /// <param name="respawnPoint">玩家回传位置。</param>
    /// <param name="fallDamage">每次掉落扣除的最终血量。</param>
    public void Configure(Transform respawnPoint, float fallDamage)
    {
        _respawnPoint = respawnPoint;
        _fallDamage = Mathf.Max(0f, fallDamage);
    }

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        if (player == null || !player.CompareTag("Player"))
        {
            return;
        }

        ApplyFallDamage(player);
        RespawnPlayer(player);
    }

    private void ApplyFallDamage(PlayerController player)
    {
        if (player == null || _fallDamage <= 0f)
        {
            return;
        }

        player.TakeDamage(_fallDamage);
    }

    private void RespawnPlayer(PlayerController player)
    {
        if (player == null || _respawnPoint == null)
        {
            return;
        }

        Rigidbody2D rb = player.Rb != null ? player.Rb : player.GetComponent<Rigidbody2D>();
        Vector2 respawnPosition = _respawnPoint.position;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.position = respawnPosition;
            return;
        }

        player.transform.position = respawnPosition;
    }

    private void EnsureTriggerCollider()
    {
        BoxCollider2D collider2D = GetComponent<BoxCollider2D>();
        if (collider2D != null)
        {
            collider2D.isTrigger = true;
        }
    }
}
