using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器：负责场景内可存档对象注册、统一存档与读档。
/// </summary>
public sealed class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 1; // 当前存档格式版本号。
    private const int MinSlotIndex = 0; // 合法槽位最小值。
    private const int NoActiveSlotIndex = -1; // 当前未绑定任何活动槽位。
    private const string SaveDirectoryName = "Saves"; // 存档目录名。
    private const string SaveFileNamePattern = "save_{0}.json"; // 存档文件命名格式。

    private static SaveManager _instance; // 单例实例。

    [SerializeField] private bool _dontDestroyOnLoad = true; // 是否跨场景常驻。
    [SerializeField] private bool _enableVerboseLog = true; // 是否输出详细日志。

    private readonly Dictionary<string, ISaveable> _saveableObjects = new Dictionary<string, ISaveable>(); // 当前场景已注册对象。
    private ISaveSerializer _serializer; // 序列化器扩展点。
    private ISaveFileHandler _fileHandler; // 文件处理扩展点。
    private int _currentSlotIndex = NoActiveSlotIndex; // 当前正在使用的槽位。

    /// <summary>
    /// 存档完成事件（参数：槽位编号）。
    /// </summary>
    public event Action<int> OnSaveCompleted;

    /// <summary>
    /// 读档完成事件（参数：槽位编号）。
    /// </summary>
    public event Action<int> OnLoadCompleted;

    /// <summary>
    /// 存档失败事件（参数：槽位编号、错误信息）。
    /// </summary>
    public event Action<int, string> OnSaveFailed;

    /// <summary>
    /// 读档失败事件（参数：槽位编号、错误信息）。
    /// </summary>
    public event Action<int, string> OnLoadFailed;

    /// <summary>
    /// 当前是否已存在单例实例。
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// 当前活动槽位编号；未绑定时返回 -1。
    /// </summary>
    public int CurrentSlotIndex => _currentSlotIndex;

    /// <summary>
    /// 当前是否已经绑定活动槽位。
    /// </summary>
    public bool HasCurrentSlot => _currentSlotIndex >= MinSlotIndex;

    /// <summary>
    /// 存档管理器单例入口（若场景中不存在则自动创建）。
    /// </summary>
    public static SaveManager Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

#if UNITY_2023_1_OR_NEWER
            _instance = FindFirstObjectByType<SaveManager>();
#else
            _instance = FindObjectOfType<SaveManager>();
#endif
            if (_instance != null)
            {
                return _instance;
            }

            GameObject managerObject = new GameObject("SaveManager");
            _instance = managerObject.AddComponent<SaveManager>();
            return _instance;
        }
    }

    // 存档根目录完整路径。
    private string SaveRootPath => Path.Combine(Application.persistentDataPath, SaveDirectoryName);

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitializeDependenciesIfNeeded();
        EnsureSaveDirectory();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 注入自定义序列化器（扩展点：可替换为加密序列化流程）。
    /// </summary>
    /// <param name="serializer">自定义序列化器。</param>
    public void SetSerializer(ISaveSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// 注入自定义文件处理器（扩展点：可替换为压缩/加密/云端读写）。
    /// </summary>
    /// <param name="fileHandler">自定义文件处理器。</param>
    public void SetFileHandler(ISaveFileHandler fileHandler)
    {
        _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
    }

    /// <summary>
    /// 设置当前活动槽位，供“开始新游戏/读档后继续保存”复用。
    /// </summary>
    /// <param name="slotIndex">要绑定的槽位编号。</param>
    public void SetCurrentSlot(int slotIndex)
    {
        if (!IsSlotIndexValid(slotIndex, nameof(SetCurrentSlot)))
        {
            return;
        }

        _currentSlotIndex = slotIndex;
    }

    /// <summary>
    /// 注册可存档对象。
    /// </summary>
    /// <param name="id">对象唯一 ID。</param>
    /// <param name="obj">可存档对象。</param>
    public void Register(string id, ISaveable obj)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("[SaveManager] Register 失败：ID 为空。");
            return;
        }

        if (obj == null)
        {
            Debug.LogWarning($"[SaveManager] Register 失败：对象为空，ID={id}。");
            return;
        }

        if (_saveableObjects.ContainsKey(id))
        {
            Debug.LogWarning($"[SaveManager] 检测到重复 ID，后注册对象将覆盖旧对象：{id}");
        }

        _saveableObjects[id] = obj;
    }

    /// <summary>
    /// 注销可存档对象。
    /// </summary>
    /// <param name="id">对象唯一 ID。</param>
    public void Unregister(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _saveableObjects.Remove(id);
    }

    /// <summary>
    /// 异步保存到指定槽位。
    /// </summary>
    /// <param name="slotIndex">存档槽位编号（从 0 开始）。</param>
    public async Task Save(int slotIndex)
    {
        if (!IsSlotIndexValid(slotIndex, nameof(Save)))
        {
            return;
        }

        InitializeDependenciesIfNeeded();
        EnsureSaveDirectory();

        try
        {
            SaveData saveData = BuildSaveData();
            string saveContent = _serializer.Serialize(saveData);
            string filePath = GetSaveFilePath(slotIndex);

            await _fileHandler.WriteAllTextAsync(filePath, saveContent);

            if (_enableVerboseLog)
            {
                Debug.Log($"[SaveManager] 存档成功，槽位={slotIndex}，路径={filePath}");
            }

            _currentSlotIndex = slotIndex;
            OnSaveCompleted?.Invoke(slotIndex);
        }
        catch (Exception exception)
        {
            string errorMessage = $"存档失败：{exception.Message}";
            Debug.LogError($"[SaveManager] {errorMessage}");
            OnSaveFailed?.Invoke(slotIndex, errorMessage);
        }
    }

    /// <summary>
    /// 异步从指定槽位读取并恢复。
    /// </summary>
    /// <param name="slotIndex">存档槽位编号（从 0 开始）。</param>
    public async Task Load(int slotIndex)
    {
        if (!IsSlotIndexValid(slotIndex, nameof(Load)))
        {
            return;
        }

        InitializeDependenciesIfNeeded();
        string filePath = GetSaveFilePath(slotIndex);

        try
        {
            bool hasSaveFile = await _fileHandler.ExistsAsync(filePath);
            if (!hasSaveFile)
            {
                string notFoundMessage = $"读档失败：槽位 {slotIndex} 文件不存在。";
                Debug.LogWarning($"[SaveManager] {notFoundMessage}");
                OnLoadFailed?.Invoke(slotIndex, notFoundMessage);
                return;
            }

            string saveContent = await _fileHandler.ReadAllTextAsync(filePath);
            SaveData saveData = _serializer.Deserialize(saveContent);

            if (saveData.version != CurrentSaveVersion)
            {
                Debug.LogWarning($"[SaveManager] 存档版本不匹配：文件版本={saveData.version}，当前版本={CurrentSaveVersion}。");
            }

            RestoreAllObjects(saveData);

            if (_enableVerboseLog)
            {
                Debug.Log($"[SaveManager] 读档成功，槽位={slotIndex}，场景={saveData.sceneName}，时间={saveData.timestamp}");
            }

            _currentSlotIndex = slotIndex;
            OnLoadCompleted?.Invoke(slotIndex);
        }
        catch (Exception exception)
        {
            string errorMessage = $"读档失败：{exception.Message}";
            Debug.LogError($"[SaveManager] {errorMessage}");
            OnLoadFailed?.Invoke(slotIndex, errorMessage);
        }
    }

    /// <summary>
    /// 异步删除指定槽位文件。
    /// </summary>
    /// <param name="slotIndex">存档槽位编号（从 0 开始）。</param>
    public async Task Delete(int slotIndex)
    {
        if (!IsSlotIndexValid(slotIndex, nameof(Delete)))
        {
            return;
        }

        InitializeDependenciesIfNeeded();

        string filePath = GetSaveFilePath(slotIndex);
        await _fileHandler.DeleteAsync(filePath);

        if (_enableVerboseLog)
        {
            Debug.Log($"[SaveManager] 已删除存档，槽位={slotIndex}，路径={filePath}");
        }
    }

    /// <summary>
    /// 异步检查指定槽位是否存在存档文件。
    /// </summary>
    /// <param name="slotIndex">存档槽位编号（从 0 开始）。</param>
    /// <returns>存在返回 true，否则 false。</returns>
    public Task<bool> HasSave(int slotIndex)
    {
        if (!IsSlotIndexValid(slotIndex, nameof(HasSave)))
        {
            return Task.FromResult(false);
        }

        InitializeDependenciesIfNeeded();
        string filePath = GetSaveFilePath(slotIndex);
        return _fileHandler.ExistsAsync(filePath);
    }

    /// <summary>
    /// 获取指定槽位存档文件路径。
    /// </summary>
    /// <param name="slotIndex">存档槽位编号。</param>
    /// <returns>存档文件完整路径。</returns>
    public string GetSaveFilePath(int slotIndex)
    {
        string fileName = string.Format(SaveFileNamePattern, slotIndex);
        return Path.Combine(SaveRootPath, fileName);
    }

    /// <summary>
    /// 组装当前场景所有可存档对象的数据快照。
    /// </summary>
    /// <returns>完整 SaveData 对象。</returns>
    private SaveData BuildSaveData()
    {
        SaveData saveData = new SaveData
        {
            version = CurrentSaveVersion,
            timestamp = DateTime.UtcNow.ToString("O"),
            sceneName = SceneManager.GetActiveScene().name,
            objectsData = new Dictionary<string, object>()
        };

        List<KeyValuePair<string, ISaveable>> objectSnapshot = new List<KeyValuePair<string, ISaveable>>(_saveableObjects);
        for (int index = 0; index < objectSnapshot.Count; index++)
        {
            KeyValuePair<string, ISaveable> pair = objectSnapshot[index];
            if (pair.Value == null)
            {
                continue;
            }

            try
            {
                object state = pair.Value.CaptureState();
                saveData.objectsData[pair.Key] = state;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveManager] 捕获对象状态失败，ID={pair.Key}，错误={exception.Message}");
            }
        }

        return saveData;
    }

    /// <summary>
    /// 使用存档数据恢复所有已注册对象状态。
    /// </summary>
    /// <param name="saveData">存档对象。</param>
    private void RestoreAllObjects(SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[SaveManager] RestoreAllObjects 失败：saveData 为空。");
            return;
        }

        if (saveData.objectsData == null)
        {
            Debug.LogWarning("[SaveManager] RestoreAllObjects 提示：objectsData 为空。");
            return;
        }

        List<KeyValuePair<string, ISaveable>> objectSnapshot = new List<KeyValuePair<string, ISaveable>>(_saveableObjects);
        for (int index = 0; index < objectSnapshot.Count; index++)
        {
            KeyValuePair<string, ISaveable> pair = objectSnapshot[index];
            if (pair.Value == null)
            {
                continue;
            }

            if (!saveData.objectsData.TryGetValue(pair.Key, out object state))
            {
                continue;
            }

            try
            {
                pair.Value.RestoreState(state);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveManager] 恢复对象状态失败，ID={pair.Key}，错误={exception.Message}");
            }
        }
    }

    /// <summary>
    /// 初始化默认扩展点实现。
    /// </summary>
    private void InitializeDependenciesIfNeeded()
    {
        if (_serializer == null)
        {
            _serializer = new JsonNetSaveSerializer();
        }

        if (_fileHandler == null)
        {
            _fileHandler = new AsyncSaveFileHandler();
        }
    }

    /// <summary>
    /// 确保存档目录存在。
    /// </summary>
    private void EnsureSaveDirectory()
    {
        Directory.CreateDirectory(SaveRootPath);
    }

    /// <summary>
    /// 校验槽位编号合法性。
    /// </summary>
    /// <param name="slotIndex">槽位编号。</param>
    /// <param name="callerName">调用方方法名。</param>
    /// <returns>合法返回 true，不合法返回 false。</returns>
    private bool IsSlotIndexValid(int slotIndex, string callerName)
    {
        if (slotIndex >= MinSlotIndex)
        {
            return true;
        }

        Debug.LogWarning($"[SaveManager] {callerName} 失败：slotIndex 不能小于 {MinSlotIndex}。");
        return false;
    }
}
