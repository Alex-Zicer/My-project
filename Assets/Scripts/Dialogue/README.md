# 对话系统实现说明

## 1. 本次实现了什么

当前版本已经把路线图里对话系统的 3 个目标补成了可运行闭环。

### 1.1 对话状态持久化

已实现：

1. `DialogueGameStateService` 的剧情布尔状态可以存档和读档。
2. `DialogueRouterService` 使用的首次/重复对话进度可以存档和读档。
3. 两个服务都会在游戏启动早期自动创建，并自动注册到 `SaveManager`。
4. 读档时不会因为“服务尚未实例化”而丢失对话状态恢复。

### 1.2 对话主流程闭环

当前系统已经支持：

1. 同一个 NPC 根据 `NpcDialogueProfileSO` 区分首次对话和重复对话。
2. 对话结束后执行 `onCompleted` 状态写回。
3. 对话结束后记录当前规则已经播放过首次或重复分支。
4. 再次交互时自动切换到正确的分支入口。
5. 存档后退出，再读档，仍能保持正确的对话阶段。

### 1.3 对话输入与导航基础

已实现：

1. NPC 交互和对话继续统一走 `UI.Submit` 输入。
2. 键盘 `E` 可触发交互和继续对话。
3. 手柄提交键可触发交互和继续对话。
4. `UI.Navigate` 支持 `WASD`、方向键、手柄摇杆、手柄方向键。
5. 选项出现后自动选中第一项。
6. 键盘/手柄可切换选项并提交，不依赖鼠标。
7. 动态生成的选项按钮自动建立上下导航关系。

---

## 2. 对话系统树状结构

```text
Dialogue System
├─ Trigger
│  └─ DialogueTrigger.cs
│     ├─ Update()
│     │  监听 UI.Submit；玩家在交互范围内时启动对话
│     ├─ TryStartByProfile()
│     │  向 DialogueRouterService 请求本次应播放的对话
│     ├─ RefreshHintText()
│     │  刷新交互提示文案
│     └─ OnTriggerEnter2D()/OnTriggerExit2D()
│        维护玩家是否处于交互范围内
│
├─ Routing
│  ├─ DialogueRouterService.cs
│  │  ├─ TryResolve()
│  │  │  根据 NPC、Profile、剧情状态，解析本次应播放的对话
│  │  ├─ NotifyDialogueCompleted()
│  │  │  在对话结束后写回进度和剧情状态
│  │  ├─ GetOrCreateMemoryProgressStore()
│  │  │  提供可持久化的进度仓库给存档组件使用
│  │  ├─ TryResolveFromRule()
│  │  │  解析规则命中时的首次/重复入口节点
│  │  └─ TryResolveDefault()
│  │     规则未命中时回退到默认对话
│  │
│  ├─ DialogueGameStateService.cs
│  │  ├─ TryGetBool()/HasKey()
│  │  │  供规则条件读取剧情状态
│  │  ├─ SetBool()/Remove()
│  │  │  供对话完成后写回剧情状态
│  │  ├─ GetAllBoolStates()
│  │  │  导出剧情状态快照用于存档
│  │  └─ ReplaceAllBoolStates()
│  │     读档时整表恢复剧情状态
│  │
│  └─ DialogueGameStateSaveable.cs
│     ├─ CaptureState()
│     │  把剧情布尔表转换成可序列化快照
│     └─ RestoreState()
│        把读档结果恢复回 DialogueGameStateService
│
├─ Progress
│  ├─ DialogueMemoryProgressStore.cs
│  │  ├─ HasPlayedFirst()/HasPlayedRepeat()
│  │  │  判断某条规则的首次/重复分支是否已播放
│  │  ├─ MarkPlayedFirst()/MarkPlayedRepeat()
│  │  │  记录分支播放进度
│  │  ├─ GetFirstPlayedKeys()/GetRepeatPlayedKeys()
│  │  │  导出进度快照用于存档
│  │  └─ ReplaceAll()
│  │     读档时整表恢复播放记录
│  │
│  └─ DialogueProgressSaveable.cs
│     ├─ CaptureState()
│     │  保存首次/重复进度集合
│     └─ RestoreState()
│        读档时恢复首次/重复进度集合
│
├─ Runtime
│  └─ DialogueService.cs
│     ├─ StartDialogue(reference, routeResult)
│     │  加载对话图，打开 UI，锁定玩家输入，并进入起始节点
│     ├─ EnterNodeById()
│     │  切换到指定节点并刷新说话人/正文
│     ├─ BeginTyping()/TypeLineRoutine()
│     │  打字机逐字显示对话正文
│     ├─ HandleNextRequested()
│     │  处理继续输入：快进或推进到下个节点
│     ├─ HandleChoiceSelected()
│     │  处理选项确认并切换到目标节点
│     └─ EndDialogue()
│        关闭页面、恢复输入、通知 Router 写回状态和进度
│
├─ View
│  ├─ DialoguePageController.cs
│  │  ├─ Update()
│  │  │  读取 UI.Submit / UI.Navigate，处理继续和选项导航
│  │  ├─ ShowChoices()
│  │  │  动态创建选项按钮，默认选中第一项
│  │  ├─ HandleChoiceNavigation()
│  │  │  用键盘/手柄切换选项焦点
│  │  ├─ SubmitCurrentChoice()
│  │  │  提交当前焦点选项
│  │  ├─ EnableUiInput()/DisableUiInput()
│  │  │  在打开/关闭对话页时启用/停用 UI ActionMap
│  │  └─ ConfigureChoiceNavigation()
│  │     给动态按钮建立上下导航关系
│  │
│  └─ DialogueChoiceButtonView.cs
│     ├─ Setup()
│     │  初始化按钮文本和点击回调
│     ├─ SetNavigation()
│     │  设置上下选择关系
│     ├─ Select()
│     │  选中当前按钮
│     └─ Submit()
│        主动触发当前按钮的确认逻辑
│
├─ Data
│  ├─ DialogueDataSO.cs
│  │  ScriptableObject 形式的对话图数据
│  ├─ DialogueReference.cs
│  │  描述对话来源、首次节点、重复节点
│  └─ DialogueProviderRegistry.cs
│     按来源类型加载 SO / JSON / CSV 对话图
│
└─ Profile
   ├─ NpcDialogueProfileSO.cs
   │  NPC 的规则集合和默认对话配置
   ├─ NpcDialogueRule.cs
   │  单条对话规则：优先级、条件、引用、完成后写回
   ├─ DialogueCondition.cs
   │  规则命中条件
   └─ DialogueStateMutation.cs
      对话完成后的剧情状态变更
```

---

## 3. 本次新建脚本说明

### 3.1 `DialogueProgressSaveable.cs`

作用：

1. 把 `DialogueRouterService` 当前使用的 `DialogueMemoryProgressStore` 接入存档系统。
2. 保存首次分支和重复分支的播放记录。
3. 读档后把这些记录恢复回路由服务。

关键函数：

1. `GetUniqueId()`
   返回固定存档 ID，保证跨运行实例稳定匹配。
2. `CaptureState()`
   导出 `firstPlayedKeys` 和 `repeatPlayedKeys`。
3. `RestoreState(object state)`
   从存档快照恢复播放进度。

### 3.2 `DialogueGameStateSaveable.cs`

作用：

1. 把 `DialogueGameStateService` 的剧情布尔状态接入存档系统。
2. 保存剧情状态表。
3. 读档后恢复剧情状态表。

关键函数：

1. `GetUniqueId()`
   返回固定存档 ID，保证跨运行实例稳定匹配。
2. `CaptureState()`
   导出当前所有剧情状态键值对。
3. `RestoreState(object state)`
   把读档内容恢复回 `DialogueGameStateService`。

---

## 4. 详细运行流程

### 4.1 启动阶段

1. 游戏启动时，`DialogueGameStateService` 和 `DialogueRouterService` 会通过 `RuntimeInitializeOnLoadMethod` 提前创建。
2. 两个服务对象会自动挂上对应的 Saveable 组件。
3. Saveable 组件会使用固定存档 ID 向 `SaveManager` 注册。
4. 这样当玩家执行读档时，对话系统相关对象已经存在，可以立即恢复状态。

### 4.2 读档阶段

1. `SaveManager.Load(slot)` 读取 JSON 存档。
2. `DialogueGameStateSaveable.RestoreState()` 恢复剧情状态表。
3. `DialogueProgressSaveable.RestoreState()` 恢复每个 NPC 规则的首次/重复播放记录。
4. 读档完成后，路由服务马上可以根据恢复后的状态决定该播放哪段对话。

### 4.3 玩家触发 NPC 对话

1. 玩家进入 `DialogueTrigger` 的 `BoxCollider2D` 范围。
2. `DialogueTrigger` 显示交互提示。
3. 玩家按下 `UI.Submit`。
4. `DialogueTrigger.Update()` 判断当前是否允许交互。
5. 若启用 `useProfileRouting`：
   - 调用 `DialogueRouterService.TryResolve()`
   - 根据 `npcId + profile + 当前剧情状态 + 当前进度` 解析本次对话
6. 若未启用 `useProfileRouting`：
   - 直接使用 `dialogueReference` 启动单段对话

### 4.4 路由解析阶段

1. `DialogueRouterService` 读取 `NpcDialogueProfileSO.rules`。
2. 按 `priority` 从高到低排序。
3. 逐条调用 `NpcDialogueRule.IsMatch()`。
4. 第一条命中的规则作为本次对话规则。
5. 若该规则首次未播放过：
   - 使用 `firstStartNodeId`
   - 路由阶段标记为 `First`
6. 若首次已播放过：
   - 使用 `repeatStartNodeId`
   - 路由阶段标记为 `Repeat`
7. 若无规则命中：
   - 回退到 `defaultDialogueReference`

### 4.5 对话运行阶段

1. `DialogueService.StartDialogue()` 加载 `DialogueGraph`。
2. 自动绑定 `IDialogueView`。
3. 禁用玩家输入。
4. 打开 `DialoguePage`。
5. 跳到路由结果指定的起始节点。
6. 开始打字机显示文本。

### 4.6 对话输入阶段

#### 无选项时

1. `DialoguePageController.Update()` 监听 `UI.Submit.WasPressedThisFrame()`。
2. 若当前还在打字：
   - `DialogueService` 收到继续请求后直接补全文字。
3. 若当前已打完字：
   - `DialogueService` 推进到下个节点。

#### 有选项时

1. `DialoguePageController.ShowChoices()` 动态创建按钮。
2. `ConfigureChoiceNavigation()` 建立上下导航。
3. 自动 `SelectChoice(0)`，默认选中第一项。
4. `UI.Navigate` 输入触发 `HandleChoiceNavigation()`。
5. 玩家切换到目标选项后按 `UI.Submit`。
6. `SubmitCurrentChoice()` 提交当前焦点按钮。
7. `DialogueService.HandleChoiceSelected()` 跳到对应节点。

### 4.7 对话结束阶段

1. `DialogueService.EndDialogue()` 停止打字协程。
2. 关闭对话页。
3. 恢复玩家输入。
4. 调用 `DialogueRouterService.NotifyDialogueCompleted()`。
5. `NotifyDialogueCompleted()` 做两件事：
   - 记录本次规则的首次/重复播放进度
   - 执行 `onCompleted` 中配置的剧情状态写回
6. 之后再次与 NPC 交互时，路由将基于新状态切换到正确分支。

### 4.8 存档阶段

1. `SaveManager.Save(slot)` 收集所有已注册 `ISaveable` 对象。
2. `DialogueGameStateSaveable.CaptureState()` 导出剧情布尔表。
3. `DialogueProgressSaveable.CaptureState()` 导出首次/重复播放记录。
4. 这些数据和其他系统数据一起写入同一个槽位文件。

---

## 5. 开发人员如何配置和使用

### 5.1 配置对话数据

你可以使用两种主流方式。

#### 方式 A：ScriptableObject

1. 新建 `DialogueDataSO`。
2. 填写：
   - `dialogueId`
   - `startNodeId`
   - `nodes`
3. 每个 `DialogueNodeData` 需要配置：
   - `nodeId`
   - `speakerName`
   - `content`
   - `nextNodeId` 或 `choices`
   - `isEndNode`

#### 方式 B：JSON

1. 把 JSON 对话文件放到 `StreamingAssets`。
2. 在 `DialogueReference` 中把 `sourceType` 设为 `Json`。
3. `keyOrPath` 填对应路径。

### 5.2 配置 NPC 路由

1. 新建 `NpcDialogueProfileSO`。
2. 设置 `profileId`。
3. 在 `rules` 中添加 `NpcDialogueRule`。
4. 每条规则建议填写唯一 `ruleId`。
5. 在 `conditions` 中填写命中条件。
6. 在 `dialogueReference` 中配置：
   - 对话来源
   - `firstStartNodeId`
   - `repeatStartNodeId`
7. 在 `onCompleted` 中填写对话结束后要写回的剧情状态。

### 5.3 配置首次/重复对话

最常见的做法是同一个对话图内做两个入口节点。

示例：

1. `firstStartNodeId = first_entry`
2. `repeatStartNodeId = repeat_entry`

这表示：

1. 第一次命中规则时，从 `first_entry` 进入。
2. 后续再次命中同一规则时，从 `repeat_entry` 进入。

### 5.4 配置对话 UI

1. 在 UI Canvas 下准备一个对话页对象。
2. 挂 `UIPage`：
   - `Category = InGame`
   - `PageKey = DialoguePage`
3. 挂 `DialoguePageController`。
4. 绑定：
   - `speakerText`
   - `contentText`
   - `portraitImage`
   - `choicesRoot`
   - `choiceButtonPrefab`
5. 确保场景里有 `EventSystem`。
6. 确保场景里有 `InGamePageManager` 且它管理到了 `DialoguePage`。

### 5.5 配置选项按钮预制体

1. 预制体上挂 `Button`。
2. 同物体或子物体上挂 `TextMeshProUGUI`。
3. 挂 `DialogueChoiceButtonView`。
4. 在脚本字段里绑定：
   - `button`
   - `label`

### 5.6 配置 NPC 触发器

1. 在 NPC 上挂 `DialogueTrigger`。
2. 绑定 `triggerZone` 或直接让脚本取同物体的 `BoxCollider2D`。
3. 打开 `useProfileRouting`。
4. 配置：
   - `npcId`
   - `dialogueProfile`
5. 如需交互提示，配置：
   - `interactHintRoot`
   - `hintText`
   - `hintFormat`
   - `interactInputLabel`

### 5.7 如何从其他系统驱动剧情条件

其他系统可以直接写入剧情状态，例如：

```csharp
DialogueGameStateService.Instance.SetBool("quest.main_001.started", true);
DialogueGameStateService.Instance.SetBool("npc.elder.reward_claimed", true);
```

对话路由会在下一次交互时读取这些状态。

### 5.8 如何验证配置是否正确

建议按下面顺序验收：

1. 进入场景，靠近 NPC，确认出现交互提示。
2. 按 `E` 或手柄提交键，确认进入首次对话。
3. 正常走完首次对话，确认结束后状态写回。
4. 再次交互，确认切换到重复对话入口。
5. 存档。
6. 退出并读档。
7. 再次交互，确认仍然是正确的重复对话或阶段性分支。
8. 打开有选项的节点，确认第一项默认选中。
9. 用 `W/S`、方向键、手柄摇杆切换选项。
10. 不用鼠标完成整轮对话。

---

## 6. 当前输入规则

### 6.1 已统一的输入

1. 交互 NPC：`UI.Submit`
2. 对话继续：`UI.Submit`
3. 选项确认：`UI.Submit`
4. 选项切换：`UI.Navigate`

### 6.2 当前绑定

1. `UI.Submit`
   - 键盘：`E`
   - 其他设备：沿用 Input System 的 Submit 绑定
2. `UI.Navigate`
   - 键盘：`WASD`、方向键
   - 手柄：摇杆、方向键

---

## 7. 设计注意事项

1. 对话状态持久化不是挂在场景对象上，而是挂在运行时单例服务上。
2. 存档组件使用固定 ID，而不是随机 GUID。
3. 这样做的原因是：对话服务是运行时动态创建的，如果使用随机 GUID，每次启动都会变，读档无法匹配原对象。
4. 当前版本仍保留 `DialogueTrigger` 的兼容模式，但推荐正式内容一律使用 `NpcDialogueProfileSO` 路由。
5. `CsvDialogueProvider` 仍然是预留扩展点，当前未实现。

---

## 8. 一句话理解当前系统

当前对话系统已经具备：

1. 基于 Profile 的条件路由
2. 首次/重复对话切换
3. 对话完成后的剧情状态写回
4. 对话状态和进度持久化
5. 键盘/手柄可完整操作的对话 UI

可以直接作为后续剧情、任务、NPC 互动的基础设施继续扩展。
