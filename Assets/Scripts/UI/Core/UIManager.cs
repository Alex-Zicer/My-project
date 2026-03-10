using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    //单例实例
    public static UIManager Instance { get; private set; }

    [SerializeField] private UIPageManager pageManager;
    [SerializeField] private HUDManager hudManager;

    public UIPageManager Page => pageManager;
    public HUDManager HUD => hudManager;

    public EventSystem eventSystem;

    /// <summary>
    /// 初始化HUD
    /// </summary>
    public void InitHUD()
    {
        if (hudManager != null) return;
        
        hudManager = GameObject.FindFirstObjectByType<HUDManager>();
        hudManager.SetHUDActive(true);
    }

    /// <summary>
    /// 加载GamePlay场景
    /// </summary>
    public void LoadGamePlay()
    {
        SceneLoader.Instance.LoadScene("GamePlay");
    }

    /// <summary>
    /// 初始化事件系统，确保UI管理器能够正确处理用户输入和交互事件。
    /// </summary>
    private void SetUpEventSystem()
    {
        eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.LogWarning("缺少事件系统！");
        }
    }

    /// <summary>
    /// 初始化UI管理器，确保只有一个实例存在，并且在场景中保持不被销毁。
    /// 这种单例模式的实现方式可以确保在整个游戏生命周期中，UI管理器始终可用，
    /// 并且不会因为场景切换而丢失。
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitSubManagers();
    }

    private void InitSubManagers()
    {
        if(pageManager != null) pageManager.Initialize();
        if(hudManager != null) hudManager.SetHUDActive(false);
    }

    /// <summary>
    /// 初始化UI管理器，设置事件系统和可交互UI元素的类型，以确保在游戏开始时，UI能够正确响应用户输入和交互事件。
    /// </summary>
    private void Start()
    {
        SetUpEventSystem();      
    }

    /// <summary>
    /// 检测是否输入Esc
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pageManager.TogglePause();
        }
    }

}


