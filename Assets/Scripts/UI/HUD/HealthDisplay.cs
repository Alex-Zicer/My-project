using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 分段血条显示控制器：监听生命值变化与 HUD 显隐事件，驱动每个血格播放对应动画。
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private Health playerHealth;    // 玩家生命组件。
    [SerializeField] private HUDManager hudManager;  // HUD 总管理器。

    [Header("血格设置")]
    [SerializeField] private Transform maskRoot;                    // 血格父节点。
    [SerializeField] private HealthMaskCell maskCellPrefab;         // 单个血格预制体。
    [SerializeField] private int initialMaskCount = 5;              // 初始血格数量（1 格 = 1 血）。
    [SerializeField] private bool forceInitialHealthAtStart = true; // 开局是否强制重置为初始血量。

    private readonly List<HealthMaskCell> _cells = new List<HealthMaskCell>(); // 已创建血格缓存。
    private float _lastCurrentHealth; // 上一次生命值（用于比较受伤/回血）。
    private float _lastMaxHealth;     // 上一次最大生命值（用于检测 MaxUp）。

    private void Awake()
    {
        // 先补齐引用，再初始化数据与显示，避免空引用和初帧状态错误。
        ResolveReferences();
        InitializeHealthValue();
        InitializeMasks();
        ForceSyncCellState(playerHealth != null ? playerHealth.currentHealth : 0f);

        if (playerHealth != null)
        {
            _lastCurrentHealth = playerHealth.currentHealth;
            _lastMaxHealth = playerHealth.maxHealth;
        }
    }

    private void OnEnable()
    {
        // 订阅生命值变化：驱动血格状态动画。
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }

        // 订阅 HUD 显隐：HUD 显示时统一触发 Appear。
        if (hudManager != null)
        {
            hudManager.OnHUDActiveChanged += HandleHUDActiveChanged;
        }
    }

    private void Start()
    {
        // 若 HUD 当前已显示，组件启用后需要立刻同步一次 Appear。
        if (hudManager != null && hudManager.IsHUDActive)
        {
            PlayAppearForAllMasks();
        }
    }

    private void OnDisable()
    {
        // 及时退订，避免对象销毁后回调残留。
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        if (hudManager != null)
        {
            hudManager.OnHUDActiveChanged -= HandleHUDActiveChanged;
        }
    }

    /// <summary>
    /// 处理生命值变化：扣血触发 Break，回血触发 Refill，上限增加触发 MaxUp。
    /// </summary>
    /// <param name="current">当前生命值</param>
    /// <param name="max">最大生命值</param>
    private void HandleHealthChanged(float current, float max)
    {
        // 最大生命值变化会影响血格数量（向下取整，1 格 = 1 血）。
        int targetCount = Mathf.Max(0, Mathf.FloorToInt(max));
        EnsureMaskCount(targetCount, max > _lastMaxHealth);

        // 每个血格根据 old/new 血量自行决定 Break 或 Refill。
        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].ApplyHealthChange(_lastCurrentHealth, current);
        }

        // 更新快照，供下一次变化比较使用。
        _lastCurrentHealth = current;
        _lastMaxHealth = max;
    }

    /// <summary>
    /// 接收 HUD 显隐事件，显示时统一触发 Appear。
    /// </summary>
    /// <param name="isActive">HUD 是否显示</param>
    private void HandleHUDActiveChanged(bool isActive)
    {
        if (!isActive)
        {
            return;
        }

        PlayAppearForAllMasks();
    }

    /// <summary>
    /// 初始化生命值：1 格 = 1 血，默认初始 5 格 5 血。
    /// </summary>
    private void InitializeHealthValue()
    {
        if (!forceInitialHealthAtStart || playerHealth == null)
        {
            return;
        }

        playerHealth.SetMaxHealth(initialMaskCount, true);
    }

    /// <summary>
    /// 初始化血格列表。
    /// </summary>
    private void InitializeMasks()
    {
        _cells.Clear();

        int maskCount = playerHealth != null
            ? Mathf.Max(0, Mathf.FloorToInt(playerHealth.maxHealth))
            : Mathf.Max(0, initialMaskCount);

        EnsureMaskCount(maskCount, false);
    }

    /// <summary>
    /// 根据目标数量补齐血格。上限增加时对新格播放 MaxUp。
    /// </summary>
    /// <param name="targetCount">目标格子数</param>
    /// <param name="playMaxUpOnNewMask">新格是否播放 MaxUp</param>
    private void EnsureMaskCount(int targetCount, bool playMaxUpOnNewMask)
    {
        if (maskRoot == null || maskCellPrefab == null)
        {
            return;
        }

        while (_cells.Count < targetCount)
        {
            // 仅在需要时动态创建新血格；新增上限时可播放 MaxUp。
            HealthMaskCell cell = Instantiate(maskCellPrefab, maskRoot);
            cell.transform.SetAsLastSibling();
            cell.SetCellIndex(_cells.Count);
            cell.SetFilledInstant(false);

            if (playMaxUpOnNewMask)
            {
                cell.PlayMaxUp();
            }

            _cells.Add(cell);
        }
    }

    /// <summary>
    /// 强制把所有血格同步到当前满/空状态（初始化使用）。
    /// </summary>
    /// <param name="currentHealth">当前生命值</param>
    private void ForceSyncCellState(float currentHealth)
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            // 规则：第 i 格在 currentHealth >= i + 1 时视为满格。
            bool isFilled = currentHealth >= i + 1;
            _cells[i].SetFilledInstant(isFilled);
        }
    }

    /// <summary>
    /// 统一播放所有血格的 Appear 动画。
    /// </summary>
    private void PlayAppearForAllMasks()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].PlayAppear();
        }
    }

    /// <summary>
    /// 自动补齐运行时引用，减少场景手动绑定成本。
    /// </summary>
    private void ResolveReferences()
    {
        // 未手动指定时，默认用当前对象作为血格容器。
        if (maskRoot == null)
        {
            maskRoot = transform;
        }

        // 未绑定玩家生命组件时，按 Player 标签自动查找。
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        // 未绑定 HUDManager 时，从场景中自动查找（包含未激活对象）。
        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
        }
    }
}