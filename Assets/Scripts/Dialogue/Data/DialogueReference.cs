using System;
using UnityEngine;

// 对话来源类型：
// So/Json/Csv 表示内置支持的数据源，Custom 预留给项目自定义 Provider。
public enum DialogueSourceType
{
    So,
    Json,
    Csv,
    Custom
}

[Serializable]
public class DialogueReference
{
    // 当前引用使用的数据源类型。
    public DialogueSourceType sourceType = DialogueSourceType.So;

    // 主 SO 资源：当 sourceType=So 时由 SoDialogueProvider 使用。
    public DialogueDataSO primarySO;

    // 外部数据键/路径：
    // Json/Csv 模式下可填相对路径（相对 StreamingAssets）或绝对路径。
    public string keyOrPath;

    // 回退 SO：主来源加载失败时，注册表会尝试使用该资源兜底。
    public DialogueDataSO fallbackSO;

    // 便捷构造：快速从 SO 生成一个可直接运行的引用对象。
    public static DialogueReference FromSo(DialogueDataSO so)
    {
        return new DialogueReference
        {
            sourceType = DialogueSourceType.So,
            primarySO = so
        };
    }
}
