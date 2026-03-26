// 路由阶段：描述本次命中的对话属于首次、重复或默认兜底。
public enum DialogueRoutePhase
{
    // 命中规则的首次对话。
    First,
    // 命中规则后的重复对话。
    Repeat,
    // 默认对话（无规则命中时）。
    Default
}
