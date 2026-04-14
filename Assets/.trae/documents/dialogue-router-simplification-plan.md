# 路由层简化方案

## 当前复杂度分析

路由层当前包含以下复杂特性：

| 特性                               | 复杂度 | 说明               |
| -------------------------------- | --- | ---------------- |
| 规则优先级排序                          | 中   | 需要排序算法，同优先级保持原顺序 |
| 条件组合模式 (All/Any)                 | 中   | 两种逻辑组合方式         |
| 多值类型条件 (Bool/Int/String)         | 高   | 每种类型有不同的比较逻辑     |
| 多比较模式 (Equals/Greater/Exists...) | 高   | 每种类型支持多种比较       |
| 首次/重复对话分流                        | 高   | 同一规则需要配置两套对话引用   |
| 首次/重复策略 (Once/Repeatable)        | 高   | 控制对话是否可重复播放      |
| 状态写回机制                           | 中   | 对话结束后修改游戏状态      |
| 进度记录存储                           | 低   | 记录是否播放过          |

***

## 关键问题：对话内部的部分重复

### 场景描述

```
首次对话：A → B → C → D → E（全部播放）
重复对话：D → E（只播放后面部分）
```

### 解决方案对比

#### 方案 1：拆分对话图（简化方案可行）

```
对话图1（首次）：A → B → C → D → E
对话图2（重复）：D → E

规则配置：
- 规则1：条件 met=false → 对话图1 → 写回 met=true
- 规则2：条件 met=true → 对话图2
```

**优点**：简单直接，对话图独立
**缺点**：D → E 部分需要维护两份（重复）

***

#### 方案 2：同一对话图 + 不同起始节点（推荐）

```
对话图：A → B → C → D → E
        ↑           ↑
      start1      start2

规则配置：
- 规则1：条件 met=false → 对话图(startNodeId=A) → 写回 met=true
- 规则2：条件 met=true → 对话图(startNodeId=D)
```

**实现方式**：扩展 DialogueReference 支持指定起始节点

```csharp
public class DialogueReference
{
    public DialogueSourceType sourceType;
    public DialogueDataSO primarySO;
    public string keyOrPath;
    public DialogueDataSO fallbackSO;
    
    // 新增：指定起始节点（为空时使用对话图默认起始节点）
    public string startNodeId;
}
```

**优点**：

* 对话图只维护一份

* 灵活指定从任意节点开始

* 不需要修改路由层核心逻辑

**缺点**：

* 需要修改 DialogueReference 和 DialogueService

***

#### 方案 3：保留首次/重复分流（当前系统）

```
规则配置：
- firstDialogueReference → 对话图(startNodeId=A)
- repeatDialogueReference → 对话图(startNodeId=D)
```

**优点**：配置集中在一规则内
**缺点**：规则结构复杂

***

## 推荐方案：方案 2（扩展 DialogueReference）

### 核心改动

只需在 `DialogueReference` 中添加一个 `startNodeId` 字段：

```csharp
[Serializable]
public class DialogueReference
{
    public DialogueSourceType sourceType = DialogueSourceType.So;
    public DialogueDataSO primarySO;
    public string keyOrPath;
    public DialogueDataSO fallbackSO;
    
    // 新增：指定起始节点（为空时使用对话图默认起始节点）
    public string startNodeId;
}
```

### DialogueService 配合修改

```csharp
public bool StartDialogue(DialogueReference reference, DialogueRouteResult routeResult)
{
    // ... 加载对话图 ...
    
    // 使用引用中指定的起始节点，或对话图默认起始节点
    string startNodeId = string.IsNullOrWhiteSpace(reference.startNodeId) 
        ? _graph.StartNodeId 
        : reference.startNodeId;
    
    EnterNodeById(startNodeId);
}
```

### 配置示例

```
对话图：blacksmith_dialogue
├── 节点 A: "欢迎光临！"（首次问候）
├── 节点 B: "我是铁匠..."
├── 节点 C: "有什么需要帮忙的？"
├── 节点 D: "今天想买点什么？"（日常问候）
└── 节点 E: "看看我的货物吧。"

默认起始节点：A

规则配置：
┌─────────────────────────────────────────────────────────┐
│ 规则1：首次见面                                          │
│ 条件: met_blacksmith = false                            │
│ dialogueReference:                                       │
│   - primarySO: blacksmith_dialogue                      │
│   - startNodeId: A（或留空使用默认）                      │
│ onCompleted: [ { key: "met_blacksmith", value: true } ] │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ 规则2：老顾客                                            │
│ 条件: met_blacksmith = true                              │
│ dialogueReference:                                       │
│   - primarySO: blacksmith_dialogue                      │
│   - startNodeId: D（跳过 A-C，直接从日常问候开始）         │
└─────────────────────────────────────────────────────────┘
```

***

## 最终简化方案

### 保留的功能

| 功能         | 说明                                   |
| ---------- | ------------------------------------ |
| 规则优先级排序    | 按优先级匹配规则                             |
| 条件系统（简化）   | 只支持 Bool + Exists/NotExists          |
| 状态写回（简化）   | 只支持 Bool                             |
| 默认对话兜底     | 无规则命中时使用                             |
| **起始节点指定** | **新增：DialogueReference.startNodeId** |

### 移除的功能

| 功能            | 替代方案                                |
| ------------- | ----------------------------------- |
| 首次/重复对话分流     | 通过条件 + 多规则实现                        |
| 首次/重复策略       | 通过条件控制                              |
| 进度记录存储        | 通过状态写回实现                            |
| Int/String 条件 | 只保留 Bool                            |
| 复杂比较模式        | 只保留 Exists/NotExists/IsTrue/IsFalse |

***

## 需要修改的文件

### 1. DialogueReference.cs（新增字段）

```csharp
[Serializable]
public class DialogueReference
{
    public DialogueSourceType sourceType = DialogueSourceType.So;
    public DialogueDataSO primarySO;
    public string keyOrPath;
    public DialogueDataSO fallbackSO;
    
    // 新增：指定起始节点
    public string startNodeId;
    
    public static DialogueReference FromSo(DialogueDataSO so, string startNodeId = null)
    {
        return new DialogueReference
        {
            sourceType = DialogueSourceType.So,
            primarySO = so,
            startNodeId = startNodeId
        };
    }
}
```

### 2. DialogueService.cs（支持自定义起始节点）

```csharp
public bool StartDialogue(DialogueReference reference, DialogueRouteResult routeResult)
{
    // ... 加载对话图 ...
    
    // 使用引用中指定的起始节点
    string startNodeId = string.IsNullOrWhiteSpace(reference?.startNodeId) 
        ? _graph.StartNodeId 
        : reference.startNodeId;
    
    EnterNodeById(startNodeId);
}
```

### 3. NpcDialogueRule.cs（简化）

```csharp
[Serializable]
public class NpcDialogueRule
{
    public string ruleId = "rule";
    public bool enabled = true;
    public int priority;
    
    public DialogueConditionMode conditionMode = DialogueConditionMode.All;
    public List<DialogueCondition> conditions = new List<DialogueCondition>();
    
    // 简化为单一对话引用
    public DialogueReference dialogueReference = new DialogueReference();
    
    // 简化为单一写回列表
    public List<DialogueStateMutation> onCompleted = new List<DialogueStateMutation>();
    
    public bool IsMatch(IDialogueGameStateReader stateReader) { ... }
}
```

### 4. DialogueCondition.cs（简化）

```csharp
[Serializable]
public class DialogueCondition
{
    public bool enabled = true;
    public string key;
    public DialogueConditionComparison comparison = DialogueConditionComparison.IsTrue;
    public bool expectedValue = true;
    
    // 移除：intValue, stringValue, valueType
}
```

### 5. DialogueStateMutation.cs（简化）

```csharp
[Serializable]
public class DialogueStateMutation
{
    public string key;
    public bool value;
    
    // 移除：intValue, stringValue, valueType
}
```

### 6. DialogueRouterService.cs（简化）

* 移除首次/重复分流逻辑

* 移除进度记录相关代码

* 简化 TryResolveFromRule

### 7. 可移除的文件

* `IDialogueProgressStore.cs`

* `DialogueMemoryProgressStore.cs`

* `DialogueRepeatPolicy.cs`

* `DialogueConditionValueType.cs`（简化后不再需要）

***

## 实施步骤

1. **扩展 DialogueReference**：添加 startNodeId 字段
2. **修改 DialogueService**：支持自定义起始节点
3. **简化 DialogueCondition**：移除 Int/String 支持
4. **简化 DialogueConditionComparison**：只保留 Bool 相关比较
5. **简化 DialogueStateMutation**：只支持 Bool
6. **简化 NpcDialogueRule**：合并首次/重复为单一引用
7. **简化 NpcDialogueProfileSO**：移除 defaultRepeatPolicy
8. **简化 DialogueRoutePhase**：只保留 Default
9. **简化 DialogueRouterService**：移除首次/重复分流和进度记录
10. **移除废弃文件**：IDialogueProgressStore、DialogueMemoryProgressStore、DialogueRepeatPolicy
11. **测试验证**

***

## 请确认

这个方案通过扩展 `DialogueReference.startNodeId` 来实现对话内部的部分重复，同时简化了路由层的其他复杂特性。

请告诉我：

1. 这个方案是否满足你的需求？
2. 是否有其他场景需要考虑？

