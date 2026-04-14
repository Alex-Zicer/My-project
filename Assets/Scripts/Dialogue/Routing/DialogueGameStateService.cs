using System.Collections.Generic;
using UnityEngine;

public class DialogueGameStateService : MonoBehaviour, IDialogueGameStateReader, IDialogueGameStateWriter
{
    // 单例实例引用。
    private static DialogueGameStateService _instance;

    // 全局布尔状态表，键为状态名，值为状态值。
    private readonly Dictionary<string, bool> _boolStates = new Dictionary<string, bool>();

    // HasInstance 状态开关。
    public static bool HasInstance => _instance != null;

    public static DialogueGameStateService Instance
    {
        get
        {
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 创建单例实例并设置为跨场景持久对象。
    /// </summary>
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueGameStateService");
        _instance = go.AddComponent<DialogueGameStateService>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// 初始化组件并确保运行时状态有效。
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 清理实例引用与事件绑定，防止悬挂回调。
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 判断当前状态是否满足条件。
    /// </summary>
    public bool HasKey(string key)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(key)) return false;
        return _boolStates.ContainsKey(key);
    }

    /// <summary>
    /// 尝试读取指定状态键对应的布尔值。
    /// </summary>
    public bool TryGetBool(string key, out bool value)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(key))
        {
            value = false;
            return false;
        }
        return _boolStates.TryGetValue(key, out value);
    }

    /// <summary>
    /// 设置对应配置或运行时状态。
    /// </summary>
    public void SetBool(string key, bool value)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(key)) return;
        _boolStates[key] = value;
    }

    /// <summary>
    /// 移除指定状态键。
    /// </summary>
    public void Remove(string key)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(key)) return;
        _boolStates.Remove(key);
    }
}
