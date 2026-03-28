using System.Collections.Generic;
using UnityEngine;

// 榛樿鍓ф儏鐘舵€佹湇鍔★細鎻愪緵鏉′欢璇诲彇鍜岀姸鎬佸啓鍥炵殑缁熶竴鍏ュ彛銆?
public class DialogueGameStateService : MonoBehaviour, IDialogueGameStateReader, IDialogueGameStateWriter
{
    // _instance 字段。
    private static DialogueGameStateService _instance;

    // 甯冨皵鐘舵€佷粨搴撱€?
    private readonly Dictionary<string, bool> _boolStates = new Dictionary<string, bool>();
    // 鏁村瀷鐘舵€佷粨搴撱€?
    private readonly Dictionary<string, int> _intStates = new Dictionary<string, int>();
    // 瀛楃涓茬姸鎬佷粨搴撱€?
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

    // 寤惰繜鍒涘缓鐘舵€佹湇鍔″疄渚嬶紝閬垮厤鍦烘櫙鎵嬪姩鎽嗘斁渚濊禆銆?
    /// <summary>
    /// CreateInstance。
    /// </summary>
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueGameStateService");
        _instance = go.AddComponent<DialogueGameStateService>();
        DontDestroyOnLoad(go);
    }

    // 纭繚鍦烘櫙涓彧鏈変竴涓姸鎬佹湇鍔″疄渚嬨€?
    /// <summary>
    /// Awake。
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

    // 瀵硅薄閿€姣佹椂娓呯悊闈欐€佸疄渚嬪紩鐢ㄣ€?
    /// <summary>
    /// OnDestroy。
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // 鍒ゆ柇 key 鏄惁瀛樺湪浜庝换鎰忕被鍨嬬殑鐘舵€佷粨搴撲腑銆?
    /// <summary>
    /// HasKey。
    /// </summary>
    /// <param name="key">参数。</param>
    public bool HasKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return _boolStates.ContainsKey(key) || _intStates.ContainsKey(key) || _stringStates.ContainsKey(key);
    }

    // 璇诲彇甯冨皵鐘舵€侊紱涓嶅瓨鍦ㄦ椂杩斿洖 false銆?
    /// <summary>
    /// TryGetBool。
    /// </summary>
    /// <param name="key">参数。</param>
    /// <param name="value">参数。</param>
    public bool TryGetBool(string key, out bool value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = false;
            return false;
        }
        return _boolStates.TryGetValue(key, out value);
    }

    // 璇诲彇鏁村瀷鐘舵€侊紱涓嶅瓨鍦ㄦ椂杩斿洖 false銆?
    /// <summary>
    /// TryGetInt。
    /// </summary>
    /// <param name="key">参数。</param>
    /// <param name="value">参数。</param>
    public bool TryGetInt(string key, out int value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = 0;
            return false;
        }
        return _intStates.TryGetValue(key, out value);
    }

    // 璇诲彇瀛楃涓茬姸鎬侊紱涓嶅瓨鍦ㄦ椂杩斿洖 false銆?
    /// <summary>
    /// TryGetString。
    /// </summary>
    /// <param name="key">参数。</param>
    /// <param name="value">参数。</param>
    public bool TryGetString(string key, out string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = string.Empty;
            return false;
        }
        return _stringStates.TryGetValue(key, out value);
    }

    // 鍐欏叆甯冨皵鐘舵€侊紝骞剁Щ闄ゅ悓 key 鐨勫叾浠栫被鍨嬪€笺€?
    /// <summary>
    /// SetBool。
    /// </summary>
    /// <param name="key">参数。</param>
    /// <param name="value">参数。</param>
    public void SetBool(string key, bool value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _boolStates[key] = value;
        _intStates.Remove(key);
        _stringStates.Remove(key);
    }

    // 鍐欏叆鏁村瀷鐘舵€侊紝骞剁Щ闄ゅ悓 key 鐨勫叾浠栫被鍨嬪€笺€?
    /// <summary>
    /// SetInt。
    /// </summary>
    /// <param name="key">参数。</param>
    /// <param name="value">参数。</param>
    public void SetInt(string key, int value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _intStates[key] = value;
        _boolStates.Remove(key);
        _stringStates.Remove(key);
    }

    // 鍐欏叆瀛楃涓茬姸鎬侊紝骞剁Щ闄ゅ悓 key 鐨勫叾浠栫被鍨嬪€笺€?
    /// <summary>
    /// SetString。
    /// </summary>
    /// <param name="key">参数。</param>
    /// <param name="value">参数。</param>
    public void SetString(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _stringStates[key] = value ?? string.Empty;
        _boolStates.Remove(key);
        _intStates.Remove(key);
    }

    // 鍒犻櫎鎸囧畾 key 鐨勫叏閮ㄧ被鍨嬬姸鎬佸€笺€?
    /// <summary>
    /// Remove。
    /// </summary>
    /// <param name="key">参数。</param>
    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _boolStates.Remove(key);
        _intStates.Remove(key);
        _stringStates.Remove(key);
    }
}
