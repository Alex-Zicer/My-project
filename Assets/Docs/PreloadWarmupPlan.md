# 预加载与预热规范（项目版）

## 1. 目标

- 核心目标：进入场景后 `0~2s` 内不出现“首次触发卡顿”。
- 流程目标：`FadeOut -> 异步加载 -> 场景激活 -> 黑屏预热完成 -> FadeIn`。
- 约束：`FadeIn` 的时机由“预热完成”决定，而不是“预热开始”。

---

## 2. 分层策略（必须先分层，再实现）

### 2.1 Required（必须预热完成）

- 音频：场景首帧/首秒一定会响的 BGM、脚步、攻击、受击。
- UI：开场立刻可见的主 HUD 树、关键字体材质、首屏按钮状态。
- 池对象：开场必定会展示的对象池（如背包格子池、伤害数字池）。
- 视觉：开场必定触发的粒子/特效、相机链路关键组件。

### 2.2 Optional（可延后）

- 低频特效、低频音效。
- 非首屏页面（设置页、二级弹窗）。
- 远距离或较晚才出现的敌人预制体。

---

## 3. 当前项目建议清单

## 3.1 音频（已部分完成）

- 已接入：
  - `AudioWarmupTaskSO` + `SceneWarmupProfileSO` + `SceneLoader` 黑屏预热阶段。
  - `GameplayAudioWarmup` 中包含：
    - `PlayerRun`
    - `PlayerAttack`
    - `SwordmanAttack`
    - `GamePlayBGM`
- 关键规则：
  - 预热等待 `AudioClip.loadState == Loaded` 后才算完成。
  - 场景默认 BGM 在 `SceneLoader.IsLoading == false` 后再播放（避免抢跑）。

## 3.2 背包对象池（建议纳入 Required）

- 相关脚本：
  - `BagSlotPool`：`Awake` 中预热 `preWarmCount`。
  - `BagPageController`：开关页面时 `Get/ReturnAll`。
- 风险：
  - 如果背包页首次打开时才实例化整个页面，仍可能首次卡顿。
- 建议：
  - 在 Gameplay 预热阶段主动触发“背包页面对象树初始化 + 池预热”。
  - 保证 `BagSlotPool.PreWarm()` 在黑屏阶段已经执行。

## 3.3 UI 系统（建议拆 Required/Optional）

- Required：
  - HUD 根节点、血条、必要字体资源。
- Optional：
  - 设置页、详细面板、次级按钮组。
- 风险点：
  - 场景切换后大量 `FindObjectsByType` 可能集中在同一帧。
- 建议：
  - 必要 UI 在黑屏预热时完成一次初始化。
  - 非必要 UI 延后到 FadeIn 后分帧处理。

## 3.4 动画与特效

- Required：
  - 主角 `Idle->Run` 首次状态切换相关动画资源。
  - 开场必定出现的受击/攻击特效预制体。
- Optional：
  - 稀有敌人的特效与动画图集。
- 建议：
  - 为“首帧必用特效”建立 VFX 预热任务（实例化一次后回收）。

---

## 4. 任务系统扩展建议（基于现有架构）

- 保留现有抽象：`SceneWarmupTaskSO`。
- 新增任务类型（按需逐步加）：
  - `PoolWarmupTaskSO`：对象池预创建（背包格子、伤害数字等）。
  - `UiWarmupTaskSO`：首屏 UI 初始化/字体与材质预触发。
  - `PrefabWarmupTaskSO`：关键预制体实例化-回收。
  - `ShaderWarmupTaskSO`（可后置）：关键材质变体预热。

---

## 5. 实施顺序（推荐）

1. 音频（已完成）：保持稳定，不再频繁改动。
2. 对象池：先做 `PoolWarmupTaskSO`，把背包格子池纳入 Gameplay Required。
3. UI：把首屏 UI 初始化放入预热，非首屏页面延迟。
4. 特效/预制体：针对首帧会触发的内容补预热任务。
5. 最后再做 Shader/更细粒度优化。

---

## 6. 验收标准

- 从主菜单进入 Gameplay：
  - 开局 `0~2s` 无明显单帧卡顿。
  - BGM 正常播放，无首播卡顿。
  - 立即移动、立即攻击、首次打开背包，无新增明显卡顿。
- Profiler：
  - 启动尖峰不再集中在单一首次加载调用。
  - 卡顿帧 CPU 峰值显著降低且更平滑。

