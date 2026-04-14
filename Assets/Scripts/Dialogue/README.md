# 对话系统（版本 2：数据路由）

## 1) 场景配置
1. 在游戏内 UI 画布中新增一个用于对话的 `UIPage`。
2. 设置该页面：
   - `UIPage.Category = InGame`
   - `UIPage.PageKey = DialoguePage`
3. 给该页面挂载 `DialoguePageController`，并绑定以下引用：
   - 说话人文本
   - 正文文本
   - 头像图片（可选）
   - 选项容器（根节点）
   - `DialogueChoiceButtonView` 预制体
4. 确保当前场景中存在 `InGamePageManager`。

## 2) 推荐模式：NpcDialogueProfile 路由
### 2.1 新建 Profile
1. 创建 `NpcDialogueProfileSO` 资源：
   - 菜单：`Data/Dialogue/NpcDialogueProfile`
2. 在 `rules` 中添加规则（按 `priority` 从高到低匹配）。

### 2.2 规则字段说明（NpcDialogueRule）
1. `ruleId`：规则 ID（建议唯一，便于排查）。
2. `conditions`：命中条件（支持 Bool/Int/String 比较）。
3. `firstDialogueReference`：首次命中时播放（配置方式与旧版一致：`sourceType=So/Json/Csv` + 对应字段）。
4. `repeatDialogueReference`：首次完成后重复播放（可选，配置方式同上）。
5. `firstRepeatPolicy`：未配置 `repeatDialogueReference` 时，首次对话是否可重复。
6. `repeatRepeatPolicy`：重复对话是否可重复。
7. `onFirstCompleted` / `onRepeatCompleted`：对话完成后写回状态（可选）。

### 2.3 Trigger 绑定
1. 在 NPC 上挂载 `DialogueTrigger`。
2. 开启 `useProfileRouting`。
3. 配置：
   - `npcId`（如 `npc_village_elder`）
   - `dialogueProfile`（上一步创建的 Profile）
4. 碰撞触发要求：
   - 同物体有 `BoxCollider2D` 且 `Is Trigger = true`
   - 玩家带 `Rigidbody2D`
   - 玩家碰撞体标签为 `Player`

## 3) 兼容模式（旧版单引用）
当未启用路由或未绑定 Profile 时，`DialogueTrigger` 会回退到：
1. `dialogueReference`（单段对话引用）
2. `oneShot`（仅该兼容模式生效）

## 4) 剧情状态读写
默认由 `DialogueGameStateService` 提供状态读写：
1. 条件读取：`IDialogueGameStateReader`
2. 完成回写：`IDialogueGameStateWriter`

可在其他系统中设置状态，例如：
1. `DialogueGameStateService.Instance.SetBool("quest.main_001.started", true);`
2. `DialogueGameStateService.Instance.SetBool("chapter_2_unlocked", true);`

## 5) 典型需求配置示例
“特殊阶段首播完整剧情，后续只重复关键提示”：
1. 规则条件：`quest.main_001.started == true`
2. `firstDialogueReference = Special_Full`（完整剧情）
3. `repeatDialogueReference = Special_KeyInfo`（关键提示）
4. `repeatRepeatPolicy = Repeatable`

“无特殊情况时重复日常对话”：
1. 低优先级规则或 `defaultDialogueReference`
2. 重复策略设为 `Repeatable`

## 6) 说明
1. 运行时仍只消费 `DialogueGraph`，与数据来源（SO/JSON/CSV）解耦。
2. `CsvDialogueProvider` 目前仍是预留扩展点。
3. 对话进行中会禁用玩家输入，并屏蔽暂停/背包热键；对话结束后自动恢复。
