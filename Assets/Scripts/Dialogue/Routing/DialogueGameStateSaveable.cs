using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 对话剧情状态存档组件：负责保存与恢复剧情布尔状态表。
/// </summary>
public class DialogueGameStateSaveable : MonoBehaviour, ISaveable
{
    private const string SaveId = "dialogue_game_state_store";

    [SerializeField] private DialogueGameStateService _gameStateService;

    [Serializable]
    private class DialogueGameStateEntry
    {
        public string key;
        public bool value;
    }

    [Serializable]
    private class DialogueGameStateSnapshot
    {
        public List<DialogueGameStateEntry> entries = new List<DialogueGameStateEntry>();
    }

    /// <summary>
    /// 获取固定存档 ID，保证跨运行实例稳定匹配。
    /// </summary>
    public string GetUniqueId()
    {
        return SaveId;
    }

    /// <summary>
    /// Unity 生命周期：注册到存档系统。
    /// </summary>
    private void Awake()
    {
        if (_gameStateService == null)
        {
            _gameStateService = GetComponent<DialogueGameStateService>();
        }

        SaveManager.Instance.Register(SaveId, this);
    }

    /// <summary>
    /// Unity 生命周期：组件销毁时从存档系统注销。
    /// </summary>
    private void OnDestroy()
    {
        if (!SaveManager.HasInstance)
        {
            return;
        }

        SaveManager.Instance.Unregister(SaveId);
    }

    /// <summary>
    /// 捕获当前剧情状态快照。
    /// </summary>
    public object CaptureState()
    {
        DialogueGameStateService gameStateService = GetGameStateService();
        DialogueGameStateSnapshot snapshot = new DialogueGameStateSnapshot();
        if (gameStateService == null)
        {
            Debug.LogWarning("[DialogueGameStateSaveable] CaptureState 失败：状态服务引用为空。");
            return snapshot;
        }

        Dictionary<string, bool> states = gameStateService.GetAllBoolStates();
        foreach (KeyValuePair<string, bool> pair in states)
        {
            snapshot.entries.Add(new DialogueGameStateEntry
            {
                key = pair.Key,
                value = pair.Value
            });
        }

        return snapshot;
    }

    /// <summary>
    /// 恢复剧情状态快照。
    /// </summary>
    public void RestoreState(object state)
    {
        DialogueGameStateSnapshot snapshot = ConvertState<DialogueGameStateSnapshot>(state);
        if (snapshot == null)
        {
            Debug.LogWarning("[DialogueGameStateSaveable] RestoreState 失败：状态数据为空或格式不正确。");
            return;
        }

        DialogueGameStateService gameStateService = GetGameStateService();
        if (gameStateService == null)
        {
            Debug.LogWarning("[DialogueGameStateSaveable] RestoreState 失败：状态服务引用为空。");
            return;
        }

        Dictionary<string, bool> restoredStates = new Dictionary<string, bool>();
        List<DialogueGameStateEntry> entries = snapshot.entries ?? new List<DialogueGameStateEntry>();
        for (int i = 0; i < entries.Count; i++)
        {
            DialogueGameStateEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            restoredStates[entry.key] = entry.value;
        }

        gameStateService.ReplaceAllBoolStates(restoredStates);
    }

    /// <summary>
    /// 获取剧情状态服务实例。
    /// </summary>
    private DialogueGameStateService GetGameStateService()
    {
        if (_gameStateService == null)
        {
            _gameStateService = GetComponent<DialogueGameStateService>();
        }

        return _gameStateService;
    }

    /// <summary>
    /// 将 object 状态安全转换为目标类型。
    /// </summary>
    private static T ConvertState<T>(object state) where T : class
    {
        if (state == null)
        {
            return null;
        }

        if (state is T typed)
        {
            return typed;
        }

        if (state is JObject jObject)
        {
            return jObject.ToObject<T>();
        }

        if (state is JToken jToken)
        {
            return jToken.ToObject<T>();
        }

        if (state is string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        string fallbackJson = JsonConvert.SerializeObject(state);
        return JsonConvert.DeserializeObject<T>(fallbackJson);
    }
}