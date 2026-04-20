using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 对话进度存档组件：负责保存与恢复首次/重复对话分支的播放记录。
/// </summary>
public class DialogueProgressSaveable : MonoBehaviour, ISaveable
{
    private const string SaveId = "dialogue_progress_store";

    [SerializeField] private DialogueRouterService _routerService;

    [Serializable]
    private class DialogueProgressState
    {
        public List<string> firstPlayedKeys = new List<string>();
        public List<string> repeatPlayedKeys = new List<string>();
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
        if (_routerService == null)
        {
            _routerService = GetComponent<DialogueRouterService>();
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
    /// 捕获当前对话进度快照。
    /// </summary>
    public object CaptureState()
    {
        DialogueMemoryProgressStore progressStore = GetProgressStore();
        DialogueProgressState state = new DialogueProgressState();
        if (progressStore == null)
        {
            Debug.LogWarning("[DialogueProgressSaveable] CaptureState 失败：找不到 DialogueMemoryProgressStore。");
            return state;
        }

        state.firstPlayedKeys = progressStore.GetFirstPlayedKeys();
        state.repeatPlayedKeys = progressStore.GetRepeatPlayedKeys();
        return state;
    }

    /// <summary>
    /// 恢复对话进度快照。
    /// </summary>
    public void RestoreState(object state)
    {
        DialogueProgressState progressState = ConvertState<DialogueProgressState>(state);
        if (progressState == null)
        {
            Debug.LogWarning("[DialogueProgressSaveable] RestoreState 失败：状态数据为空或格式不正确。");
            return;
        }

        DialogueMemoryProgressStore progressStore = GetProgressStore();
        if (progressStore == null)
        {
            Debug.LogWarning("[DialogueProgressSaveable] RestoreState 失败：找不到 DialogueMemoryProgressStore。");
            return;
        }

        progressStore.ReplaceAll(progressState.firstPlayedKeys, progressState.repeatPlayedKeys);
    }

    /// <summary>
    /// 获取当前路由服务使用的内存进度仓库。
    /// </summary>
    private DialogueMemoryProgressStore GetProgressStore()
    {
        if (_routerService == null)
        {
            _routerService = GetComponent<DialogueRouterService>();
        }

        return _routerService != null ? _routerService.GetOrCreateMemoryProgressStore() : null;
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