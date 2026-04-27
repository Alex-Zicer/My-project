using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家动画特效控制器。
/// 负责攻击特效的资源查找、缓存与生成，以及玩家子对象二段跳特效的播放。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public class PlayerEffectSpawner : MonoBehaviour
{
    [Header("二段跳特效")]
    [Tooltip("二段跳特效对象，建议作为玩家子对象挂在这里")]
    [SerializeField] private GameObject doubleJumpEffectObject;
    [Tooltip("二段跳特效 Animator；为空时会从二段跳特效对象上自动查找")]
    [SerializeField] private Animator doubleJumpEffectAnimator;
    [Tooltip("二段跳特效要播放的状态名；留空时默认播放第 0 层默认状态")]
    [SerializeField] private string doubleJumpEffectStateName;

    private readonly Dictionary<string, GameObject> _effectCache = new Dictionary<string, GameObject>();

    private PlayerController _owner;

    /// <summary>
    /// 缓存宿主控制器和常用组件引用。
    /// </summary>
    private void Awake()
    {
        _owner = GetComponent<PlayerController>();

        if (doubleJumpEffectAnimator == null && doubleJumpEffectObject != null)
        {
            doubleJumpEffectAnimator = doubleJumpEffectObject.GetComponent<Animator>();
        }
    }

    /// <summary>
    /// 停止当前播放的二段跳特效，避免重复启动协程。
    /// </summary>
    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 供攻击动画事件调用，按 PlayerEffects 文件夹下的资源名生成攻击特效。
    /// 第二段攻击会根据当前动作状态自动切到 SlashAltEffect。
    /// </summary>
    /// <param name="effectName">Assets/Resources/PlayerEffects/ 下的预制体名称，不带扩展名。</param>
    public void SpawnAttackEffect(string effectName)
    {
        if (string.IsNullOrWhiteSpace(effectName))
        {
            Debug.LogWarning("[PlayerEffectSpawner] 攻击特效名称为空，无法生成特效。", this);
            return;
        }

        effectName = ResolveAttackEffectName(effectName);

        string effectResourcePath = $"PlayerEffects/{effectName}";
        GameObject effectPrefab = LoadEffectPrefab(effectResourcePath);
        if (effectPrefab == null)
        {
            Debug.LogWarning(
                $"[PlayerEffectSpawner] 未找到攻击特效预制体：Assets/Resources/{effectResourcePath}。请将预制体放到该目录下。",
                this);
            return;
        }

        GameObject effectInstance = Instantiate(effectPrefab, transform.position, transform.rotation);

        if (!effectInstance.TryGetComponent<PlayerAttackEffect>(out PlayerAttackEffect attackEffect))
        {
            Debug.LogWarning(
                $"[PlayerEffectSpawner] 攻击特效 {effectPrefab.name} 缺少 PlayerAttackEffect 脚本，无法执行伤害判定。",
                effectInstance);
            return;
        }

        // 由特效脚本自行决定偏移、命中窗口和销毁时机，Spawner 只负责生成与归属绑定。
        attackEffect.Initialize(_owner, transform);
    }

    /// <summary>
    /// 播放玩家子对象上的二段跳特效。
    /// 适合翅膀、爆气等跟随玩家本体的纯视觉特效。
    /// 动画播放完毕后会自动隐藏该特效对象。
    /// </summary>
    public void PlayDoubleJumpEffect()
    {
        if (doubleJumpEffectObject == null)
        {
            return;
        }

        doubleJumpEffectObject.SetActive(true);

        if (doubleJumpEffectAnimator == null)
        {
            doubleJumpEffectAnimator = doubleJumpEffectObject.GetComponent<Animator>();
        }

        if (doubleJumpEffectAnimator == null)
        {
            return;
        }

        doubleJumpEffectAnimator.Rebind();
        doubleJumpEffectAnimator.Update(0f);

        if (string.IsNullOrWhiteSpace(doubleJumpEffectStateName))
        {
            doubleJumpEffectAnimator.Play(0, 0, 0f);
        }
        else
        {
            doubleJumpEffectAnimator.Play(doubleJumpEffectStateName, 0, 0f);
        }

        StopCoroutine(HideDoubleJumpEffectAfterAnimation());
        StartCoroutine(HideDoubleJumpEffectAfterAnimation());
    }

    /// <summary>
    /// 等待二段跳特效动画播放完毕，然后隐藏该对象。
    /// </summary>
    private IEnumerator HideDoubleJumpEffectAfterAnimation()
    {
        if (doubleJumpEffectAnimator == null)
        {
            yield break;
        }

        AnimatorStateInfo stateInfo = doubleJumpEffectAnimator.GetCurrentAnimatorStateInfo(0);
        float clipLength = stateInfo.length;
        yield return new WaitForSeconds(clipLength);

        if (doubleJumpEffectObject != null && doubleJumpEffectObject.activeSelf)
        {
            doubleJumpEffectObject.SetActive(false);
        }
    }

    /// <summary>
    /// 按路径读取并缓存特效预制体，避免重复 Resources.Load。
    /// </summary>
    private GameObject LoadEffectPrefab(string effectResourcePath)
    {
        if (_effectCache.TryGetValue(effectResourcePath, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

        GameObject effectPrefab = Resources.Load<GameObject>(effectResourcePath);
        if (effectPrefab != null)
        {
            _effectCache[effectResourcePath] = effectPrefab;
        }

        return effectPrefab;
    }

    /// <summary>
    /// 根据当前动作种类决定动画事件应生成的攻击特效。
    /// 第二段攻击暂时复用 Slash 动画事件，但改用 SlashAltEffect。
    /// </summary>
    private string ResolveAttackEffectName(string defaultEffectName)
    {
        if (_owner == null || _owner.StateMachine == null || _owner.StateMachine.CurrentStateType != PlayerStateType.Action)
        {
            return defaultEffectName;
        }

        PlayerActionState actionState = _owner.StateMachine.GetState<PlayerActionState>();
        if (actionState == null)
        {
            return defaultEffectName;
        }

        return actionState.CurrentActionKind == PlayerActionKind.SlashAlt ? "SlashAltEffect" : defaultEffectName;
    }
}