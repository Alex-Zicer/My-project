using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// UI 总控单例。统一管理主菜单页、游戏内页与 HUD，并在场景加载时动态收集页面、初始化对应 UI。
/// </summary>
public class UIManager : MonoBehaviour
{
    private const int MinSlotIndex = 0; // 合法存档槽位下限。
    private const int NoPendingLoadSlot = -1; // 无待处理读档请求标记。
    private const string GamePlaySceneName = "GamePlay"; // 目标玩法场景名。
    public static UIManager Instance { get; private set; }

    [Header("核心子管理器")]
    [SerializeField] private MainMenuPageManager mainMenuPageManager;
    [SerializeField] private InGamePageManager inGamePageManager;
    [SerializeField] private HUDManager hudManager;

    [Header("Cursor")]
    [SerializeField] private Texture2D _cursorTexture;
    [SerializeField] private Vector2 _cursorHotSpot = Vector2.zero;
    [SerializeField] private bool _lockCursorDuringGameplay = true;

    public MainMenuPageManager MainMenuPage => mainMenuPageManager;
    public InGamePageManager InGamePage => inGamePageManager;
    public HUDManager HUD => hudManager;

    public EventSystem eventSystem;
    /// <summary> 当前是否为游戏内场景（用于 Esc 只在实际游戏中触发暂停）。 </summary>
    private bool isInGameScene;
    // 场景切换完成后待执行的读档槽位。
    private int _pendingLoadSlot = NoPendingLoadSlot;

    /// <summary>
    /// 初始化 HUD：从当前场景找到 HUDManager 并显示。进入游戏场景后由 SceneLoader 或 RefreshManagersByScene 调用。
    /// </summary>
    public void InitHUD()
    {
        ResolveHUDFromCurrentScene();
        if (hudManager != null) hudManager.SetHUDActive(true);
    }

    /// <summary>
    /// 加载游戏玩法场景。主菜单“开始游戏”等按钮可调用。
    /// </summary>
    public void LoadGamePlay()
    {
        SceneLoader.Instance.LoadScene("GamePlay");
    }

    /// <summary>
    /// 刷新当前上下文对应的光标状态。
    /// 主菜单场景始终显示光标；游戏内仅在打开页面时显示，其余时间隐藏。
    /// </summary>
    public void RefreshCursorState()
    {
        ApplyCustomCursor();

        if (!isInGameScene)
        {
            ShowCursorForUI();
            return;
        }

        bool shouldShowCursor = inGamePageManager != null && !string.IsNullOrEmpty(inGamePageManager.CurrentPageKey);
        if (shouldShowCursor)
        {
            ShowCursorForUI();
            return;
        }

        HideCursorForGameplay();
    }

    /// <summary>
    /// 显示光标并解除锁定，用于主菜单、暂停、背包等 UI 页面。
    /// </summary>
    public void ShowCursorForUI()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// 隐藏光标；若开启了玩法锁定，则同时锁定鼠标。
    /// </summary>
    public void HideCursorForGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = _lockCursorDuringGameplay ? CursorLockMode.Locked : CursorLockMode.None;
    }

    /// <summary>
    /// 从指定槽位开始新游戏：记录当前槽位、清空常驻运行时状态，再进入 GamePlay。
    /// </summary>
    /// <param name="slotIndex">开始新游戏使用的槽位编号。</param>
    public void StartNewGameFromSlot(int slotIndex)
    {
        int safeSlotIndex = Mathf.Max(slotIndex, MinSlotIndex);
        _pendingLoadSlot = NoPendingLoadSlot;
        SaveManager.Instance.SetCurrentSlot(safeSlotIndex);
        ResetPersistentRuntimeStateForNewGame();
        LoadGamePlay();
    }

    /// <summary>
    /// 先切换到 GamePlay，再在场景就绪后执行读档。
    /// </summary>
    /// <param name="slotIndex">读档槽位编号。</param>
    public void LoadGamePlayFromSave(int slotIndex)
    {
        int safeSlotIndex = Mathf.Max(slotIndex, MinSlotIndex);
        _pendingLoadSlot = safeSlotIndex;
        SaveManager.Instance.SetCurrentSlot(safeSlotIndex);
        LoadGamePlay();
    }

    /// <summary>
    /// 查找场景中的 EventSystem，供 UIPage 设置默认选中项等使用。
    /// </summary>
    private void SetUpEventSystem()
    {
        var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allEventSystems == null || allEventSystems.Length == 0)
        {
            eventSystem = null;
            Debug.LogWarning("缺少事件系统！");
            return;
        }

        // 选择一个“当前要用”的 EventSystem（优先用激活的；否则用第一个）
        EventSystem chosen = null;
        foreach (var es in allEventSystems)
        {
            if (es != null && es.isActiveAndEnabled)
            {
                chosen = es;
                break;
            }
        }
        if (chosen == null) chosen = allEventSystems[0];
        eventSystem = chosen;

        // 如果同时存在多个“激活的 EventSystem”，会在它们 OnEnable 时打出警告。
        // 这里把多余的禁用掉，并打印来源，方便定位到底哪个对象/哪个场景带了它。
        int activeCount = 0;
        foreach (var es in allEventSystems)
        {
            if (es != null && es.isActiveAndEnabled) activeCount++;
        }

        if (activeCount > 1)
        {
            var sb = new StringBuilder();
            sb.AppendLine("检测到多个激活的 EventSystem，将禁用多余的实例：");

            bool keptOne = false;
            foreach (var es in allEventSystems)
            {
                if (es == null) continue;

                string sceneName = es.gameObject.scene.name;
                sb.AppendLine($"- {(es.isActiveAndEnabled ? "[Active]" : "[Inactive]")} {es.name} (scene={sceneName})");

                if (!es.isActiveAndEnabled) continue;

                if (!keptOne)
                {
                    keptOne = true;
                    continue;
                }

                es.enabled = false;
            }

            Debug.LogWarning(sb.ToString());
        }
    }

    /// <summary>
    /// 单例初始化：重复则销毁自身；否则设为 Instance 并跨场景保留，然后确保核心子管理器存在。
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 子对象挂在已常驻根节点下时无需再次调用，避免 Unity 警告。
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        ApplyCustomCursor();
        EnsureSubManagers();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 确保主菜单与游戏内两个页面管理器都存在且已配置：
    /// 若未赋值则从本物体获取或自动添加，并调用 Configure 设定分类与默认页。
    /// 子页面管理器改为每组页面自管理，不再由 UIManager 统一维护。
    /// </summary>
    private void EnsureSubManagers()
    {
        // MainMenuPageManager 是主菜单场景内对象，不要挂在常驻 UIManager 上自动创建。
        // 它会在每次场景加载后由 RefreshManagersByScene 按 scene 解析并绑定。
        // InGamePageManager 是“场景内对象”（如 GamePlay 场景），不要在这里全局查找/创建。
        // 它会在每次场景加载后由 RefreshManagersByScene 按 scene 解析并绑定。
    }

    /// <summary>
    /// 场景加载完成后，根据新场景刷新所有管理器的页面与 HUD，并按主菜单/游戏内分支初始化。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshManagersByScene(scene);
        TryExecutePendingLoad(scene);
    }

    /// <summary>
    /// 若存在待处理读档请求，则在目标场景加载后执行。
    /// </summary>
    /// <param name="scene">刚加载完成的场景。</param>
    private async void TryExecutePendingLoad(Scene scene)
    {
        if (_pendingLoadSlot < MinSlotIndex)
        {
            return;
        }

        if (scene.name != GamePlaySceneName)
        {
            return;
        }

        int slotToLoad = _pendingLoadSlot;
        _pendingLoadSlot = NoPendingLoadSlot;

        // 等一帧，确保场景对象完成 Awake/注册后再恢复状态。
        await Task.Yield();
        await SaveManager.Instance.Load(slotToLoad);
    }

    /// <summary>
    /// 开始新游戏前，清空会跨场景保留的运行时状态，避免把旧局数据带入新局。
    /// </summary>
    private void ResetPersistentRuntimeStateForNewGame()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.ReplaceAllItems(new List<InventoryItem>());
        }

        if (DialogueRouterService.HasInstance)
        {
            DialogueRouterService.Instance.GetOrCreateMemoryProgressStore().ResetAll();
        }

        if (DialogueGameStateService.HasInstance)
        {
            DialogueGameStateService.Instance.ReplaceAllBoolStates(new Dictionary<string, bool>());
        }
    }

    /// <summary>
    /// 收集指定场景中所有的 UIPage（含未激活的），用于分发给各页面管理器。
    /// </summary>
    /// <param name="scene">要收集的场景</param>
    /// <returns>该场景内的 UIPage 列表</returns>
    private List<UIPage> CollectScenePages(Scene scene)
    {
        var allPages = FindObjectsByType<UIPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var scenePages = new List<UIPage>(allPages.Length);

        foreach (var page in allPages)
        {
            if (page != null && page.gameObject.scene == scene)
            {
                scenePages.Add(page);
            }
        }

        return scenePages;
    }

    /// <summary>
    /// 从当前激活场景中查找 HUDManager，并赋值给 hudManager。切换场景后需调用以指向新场景的 HUD。
    /// </summary>
    private void ResolveHUDFromCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        var hudList = FindObjectsByType<HUDManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        hudManager = null;
        foreach (var hud in hudList)
        {
            if (hud != null && hud.gameObject.scene == currentScene)
            {
                hudManager = hud;
                break;
            }
        }
    }

    /// <summary>
    /// 从指定场景中查找 InGamePageManager，并赋值给 inGamePageManager。
    /// UIManager 自身常驻（DontDestroyOnLoad），因此必须在场景切换后重新解析引用。
    /// </summary>
    private void ResolveInGamePageManagerFromScene(Scene scene)
    {
        var mgrs = FindObjectsByType<InGamePageManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        inGamePageManager = null;
        foreach (var mgr in mgrs)
        {
            if (mgr != null && mgr.gameObject.scene == scene)
            {
                inGamePageManager = mgr;
                break;
            }
        }

        if (inGamePageManager == null)
        {
            Debug.LogWarning($"场景 {scene.name} 未找到 InGamePageManager：请在该场景中手动添加一个。");
        }
        else
        {
            inGamePageManager.Configure(UIPageCategory.InGame, "");
        }
    }

    /// <summary>
    /// 从指定场景中查找 MainMenuPageManager，并赋值给 mainMenuPageManager。
    /// UIManager 自身常驻（DontDestroyOnLoad），因此必须在场景切换后重新解析引用。
    /// </summary>
    private void ResolveMainMenuPageManagerFromScene(Scene scene)
    {
        var mgrs = FindObjectsByType<MainMenuPageManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        mainMenuPageManager = null;
        foreach (var mgr in mgrs)
        {
            if (mgr != null && mgr.gameObject.scene == scene)
            {
                mainMenuPageManager = mgr;
                break;
            }
        }

        if (mainMenuPageManager != null)
        {
            mainMenuPageManager.Configure(UIPageCategory.MainMenu, "MainMenuPage");
        }
    }

    /// <summary>
    /// 根据当前场景刷新 UI：重设事件系统与 HUD 引用，收集本场景 UIPage 并分发给两个页面管理器，
    /// 再根据是否为“主菜单”场景决定初始化主菜单还是游戏内 UI，并控制 HUD 显隐与时间尺度。
    /// </summary>
    /// <param name="scene">刚加载的场景</param>
    private void RefreshManagersByScene(Scene scene)
    {
        SetUpEventSystem();
        ResolveHUDFromCurrentScene();
        ResolveMainMenuPageManagerFromScene(scene);
        ResolveInGamePageManagerFromScene(scene);

        List<UIPage> scenePages = CollectScenePages(scene);
        if (mainMenuPageManager != null) mainMenuPageManager.RegisterPages(scenePages);
        if (inGamePageManager != null) inGamePageManager.RegisterPages(scenePages);

        bool isMainMenuScene = scene.name.ToLower().Contains("menu");
        isInGameScene = !isMainMenuScene;

        if (isMainMenuScene)
        {
            if (mainMenuPageManager == null)
            {
                Debug.LogWarning($"场景 {scene.name} 未找到 MainMenuPageManager：请在该场景中手动添加一个。");
            }
            else
            {
                mainMenuPageManager.Initialize();
            }
            if (inGamePageManager != null) inGamePageManager.CloseAll();
            if (hudManager != null) hudManager.SetHUDActive(false);
            Time.timeScale = 1;
            RefreshCursorState();
            return;
        }

        if (mainMenuPageManager != null) mainMenuPageManager.CloseAll();
        if (inGamePageManager != null) inGamePageManager.Initialize();
        if (hudManager != null) hudManager.SetHUDActive(true);
        Time.timeScale = 1;
        RefreshCursorState();
    }

    /// <summary>
    /// 首次进入时按当前场景做一次完整的 UI 刷新与初始化。
    /// </summary>
    private void Start()
    {
        RefreshManagersByScene(SceneManager.GetActiveScene());
    }

    /// <summary>
    /// 仅在游戏内场景时，Esc 触发暂停/恢复。
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isInGameScene && inGamePageManager != null)
            {
                inGamePageManager.TogglePause();
            }
        }
    }

    /// <summary>
    /// 应用自定义光标图片。未配置贴图时沿用系统默认光标。
    /// </summary>
    private void ApplyCustomCursor()
    {
        Cursor.SetCursor(_cursorTexture, _cursorHotSpot, CursorMode.Auto);
    }
}
