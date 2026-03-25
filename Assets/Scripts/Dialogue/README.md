# 对话系统（版本 1）

## 场景配置
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

## 触发器配置
1. 在对话对象上挂载 `DialogueTrigger`。
2. 配置 `DialogueReference`：
   - SO 模式：`sourceType = So`，并设置 `primarySO`
   - JSON 模式：`sourceType = Json`，并设置 `keyOrPath`  
     相对路径会基于 `StreamingAssets` 解析，例如 `Dialogue/sample_dialogue.json`
3. 设置交互参数：
   - `interactRange`（交互距离）
   - `interactKey`（默认 `E`）

## 说明
- 运行时只消费 `DialogueGraph`，与具体数据来源无关。
- `CsvDialogueProvider` 目前是预留扩展点，需要按项目需求补充解析逻辑。
- 对话进行中会禁用玩家输入，并屏蔽暂停/背包热键；对话结束后自动恢复。
