using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局游戏管理器（基础壳体）。
/// 当前职责较轻：仅保证自身跨场景常驻。
/// 后续可在此扩展全局状态、流程控制、全局事件分发等能力。
/// </summary>
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // 让 GameManager 在切换场景后继续保留，避免重复初始化全局系统。
        DontDestroyOnLoad(gameObject);
    }

    // 生命周期预留：供后续扩展初始化流程。
    private void Start()
    {
    }

    // 生命周期预留：供后续扩展全局逐帧逻辑。
    private void Update()
    {
    }
}
