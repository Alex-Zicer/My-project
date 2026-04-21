# 对话系统（Demo 主线版）

## 1. 当前定位

当前项目里的对话系统已经不是“功能不够”的状态，而是已经具备了比较完整的工程结构。

现在更适合把它分成两层来理解：

1. Demo 主线
   聚焦最容易讲清楚、最适合求职展示的能力。
2. 扩展层
   保留更完整的项目级能力，供以后继续做独立游戏时使用。

这意味着当前阶段不是继续补功能，而是从已实现系统里主动收缩出一条更轻、更清晰的展示路径。

---

## 2. Demo 主线只讲什么

当前最推荐的展示主线是：

```text
玩家交互
-> DialogueDemoTrigger
-> DialogueService
-> JsonDialogueProvider
-> DialogueGraph
-> DialoguePageController
-> 分支选择
-> 对话结束
```

这条主线只强调 5 个点：

1. 对话图
2. JSON 数据驱动
3. 分支跳转逻辑
4. UI 展示
5. 完整播放流程

这也是当前求职 demo 最应该讲的内容，因为它：

1. 容易在几分钟内讲清楚。
2. 运行效果直观，面试官一眼就能理解。
3. 能同时体现数据驱动、交互逻辑和 UI 实现能力。
4. 不会把展示拖进过重的系统设计细节里。

---

## 3. Demo 主线涉及的核心脚本

```text
Demo Mainline
├─ DialogueDemoTrigger.cs
│  轻量触发入口，只负责玩家交互后启动一段对话
├─ DialogueReference.cs
│  描述对话数据来源，当前 demo 推荐只用 Json
├─ JsonDialogueProvider.cs
│  从 StreamingAssets 读取 JSON 并转换成运行时对话图
├─ DialogueGraph.cs
│  定义节点、选项和跳转结构
├─ DialogueService.cs
│  负责开始对话、打字机播放、节点推进、分支跳转、结束对话
├─ DialoguePageController.cs
│  负责说话人、正文、选项按钮和键盘/手柄操作
└─ sample_dialogue.json
   作为最直观的 demo 内容数据
```

这几个脚本足够组成一个完整、干净、可录视频的展示闭环。

---

## 4. 当前推荐的 demo 配置方式

### 4.1 数据来源

当前 demo 推荐只用 JSON。

原因不是 SO 或其他方式没价值，而是 JSON 更容易直接体现“数据驱动”的展示价值。

推荐方式：

1. 在 `StreamingAssets/Dialogue/` 下放一份对话 JSON。
2. JSON 中只保留：
   - `dialogueId`
   - `startNodeId`
   - `nodes`
   - `choiceText`
   - `nextNodeId`
3. 用一段 2 到 4 个节点的分支对话作为 demo 内容。

### 4.2 场景入口

当前 demo 推荐优先使用 `DialogueDemoTrigger`。

它是专门给展示主线准备的轻量触发脚本，只做这些事：

1. 玩家进入交互范围。
2. 显示提示。
3. 按 `UI.Submit` 触发对话。
4. 直接把 `DialogueReference` 交给 `DialogueService`。

它不会把路由、剧情状态、Profile 规则带进主演示流程里。

### 4.3 UI 层

当前 demo 保留并强调：

1. 说话人名称显示
2. 正文显示
3. 打字机效果
4. 选项按钮生成
5. 键盘 / 手柄切换选项
6. 无鼠标完成整轮对话

这部分直接决定你的 demo 看起来像不像一个完整作品，而不是一组底层脚本。

---

## 5. 已实现但降级为扩展层的能力

下面这些模块都保留，但当前不放进 demo 主叙事：

```text
Extension Layer
├─ DialogueTrigger.cs
│  支持更完整的路由路径，但不作为当前 demo 主入口
├─ DialogueRouterService.cs
│  用于 NPC/Profile 规则路由
├─ DialogueGameStateService.cs
│  用于全局剧情状态读写
├─ DialogueProgressSaveable.cs
│  用于首次/重复对话进度持久化
├─ DialogueGameStateSaveable.cs
│  用于剧情状态持久化
├─ NpcDialogueProfileSO / NpcDialogueRule
│  用于复杂 NPC 对话配置和规则分发
└─ So / Csv / Custom 数据源能力
   保留扩展性，但当前 demo 不展开讲
```

这些模块不是没价值，而是：

1. 它们更偏完整项目框架层。
2. 解释成本明显高于演示收益。
3. 更适合作为“如果继续做独立游戏，我已经预留了这些能力”的补充说明。

最合适的处理方式不是删除，而是后置。

---

## 6. 当前 demo 中应弱化的内容

当前 demo 不建议继续强调：

1. 复杂 NPC/Profile 路由
2. 首次 / 重复对话规则本身
3. 剧情状态写回链路
4. 全局剧情状态系统
5. 过于框架化的 provider / store / route 术语
6. 多数据源并列展示

这些内容都可以保留在项目里，也可以在 README 后段或面试追问时再提，但不应该抢主线。

---

## 7. 推荐的求职 demo 演示顺序

最推荐的演示流程是：

1. 先展示一份 JSON 对话文件。
2. 说明节点、选项、跳转关系。
3. 进入场景，与 NPC 交互。
4. 播放正文，展示打字机效果。
5. 出现选项并完成一次分支选择。
6. 进入不同结果节点。
7. 结束对话。
8. 最后补一句：项目中还保留了更完整的 Profile 路由、剧情状态和持久化能力，但当前 demo 主线只展示最核心的 JSON 分支对话闭环。

这样最容易把重点放在：

1. 这是数据驱动的。
2. 这是可交互的。
3. 这是可复用的。
4. 这是有实际 UI 完成度的。

---

## 8. 当前推荐的展示说法

如果你要在面试里用一句话介绍当前对话系统，推荐的说法是：

“我把对话系统做成了一个以 JSON 驱动内容、以对话图组织结构、支持分支跳转并配有完整 UI 播放流程的模块；同时在项目内部保留了更完整的路由、状态和持久化扩展能力。”

这句话比“我做了一个完整剧情框架”更适合作为求职 demo 的开场。

---

## 9. 何时再讲扩展层

如果面试官继续深问，再展开这些内容：

1. `DialogueRouterService` 如何做条件路由。
2. `DialogueGameStateService` 如何参与状态判断和写回。
3. 首次 / 重复对话如何切换入口节点。
4. 对话状态和进度如何进入存档系统。
5. 为什么项目中保留了 SO / CSV / Custom 的多数据源能力。

这样高级层会变成加分项，而不会变成第一轮理解门槛。

---

## 10. 一句话结论

当前这套对话系统最合适的展示方式，不是继续强调它已经具备的完整框架能力，而是把它收缩成“JSON 数据驱动 + 对话图 + 分支跳转 + UI 播放闭环”的 demo 主线，并把更复杂的路由、状态和持久化能力保留为项目内部的扩展层。
