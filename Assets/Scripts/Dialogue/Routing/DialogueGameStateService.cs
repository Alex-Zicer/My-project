using System.Collections.Generic;
using UnityEngine;

// 默认剧情状态服务：提供条件读取和状态写回的统一入口。
public class DialogueGameStateService : MonoBehaviour, IDialogueGameStateReader, IDialogueGameStateWriter
{
    private static DialogueGameStateService _instance;

    // 布尔状态仓库。
    private readonly Dictionary<string, bool> _boolStates = new Dictionary<string, bool>();
    // 整型状态仓库。
    private readonly Dictionary<string, int> _intStates = new Dictionary<string, int>();
    // 字符串状态仓库。
    private readonly Dictionary<string, string> _stringStates = new Dictionary<string, string>();

    public static bool HasInstance => _instance != null;

    public static DialogueGameStateService Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    // 延迟创建状态服务实例，避免场景手动摆放依赖。
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueGameStateService");
        _instance = go.AddComponent<DialogueGameStateService>();
        DontDestroyOnLoad(go);
    }

    // 确保场景中只有一个状态服务实例。
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

    // 对象销毁时清理静态实例引用。
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // 判断 key 是否存在于任意类型的状态仓库中。
    public bool HasKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return _boolStates.ContainsKey(key) || _intStates.ContainsKey(key) || _stringStates.ContainsKey(key);
    }

    // 读取布尔状态；不存在时返回 false。
    public bool TryGetBool(string key, out bool value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = false;
            return false;
        }
        return _boolStates.TryGetValue(key, out value);
    }

    // 读取整型状态；不存在时返回 false。
    public bool TryGetInt(string key, out int value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = 0;
            return false;
        }
        return _intStates.TryGetValue(key, out value);
    }

    // 读取字符串状态；不存在时返回 false。
    public bool TryGetString(string key, out string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = string.Empty;
            return false;
        }
        return _stringStates.TryGetValue(key, out value);
    }

    // 写入布尔状态，并移除同 key 的其他类型值。
    public void SetBool(string key, bool value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _boolStates[key] = value;
        _intStates.Remove(key);
        _stringStates.Remove(key);
    }

    // 写入整型状态，并移除同 key 的其他类型值。
    public void SetInt(string key, int value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _intStates[key] = value;
        _boolStates.Remove(key);
        _stringStates.Remove(key);
    }

    // 写入字符串状态，并移除同 key 的其他类型值。
    public void SetString(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _stringStates[key] = value ?? string.Empty;
        _boolStates.Remove(key);
        _intStates.Remove(key);
    }

    // 删除指定 key 的全部类型状态值。
    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _boolStates.Remove(key);
        _intStates.Remove(key);
        _stringStates.Remove(key);
    }
}
