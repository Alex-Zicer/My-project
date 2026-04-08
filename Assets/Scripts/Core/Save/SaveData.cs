using System;
using System.Collections.Generic;

/// <summary>
/// 存档数据总模型。
/// </summary>
[Serializable]
public class SaveData
{
    // 存档格式版本号。
    public int version;

    // 存档时间戳（UTC ISO 8601）。
    public string timestamp;

    // 保存时所在场景名。
    public string sceneName;

    // 场景内对象状态字典（key: uniqueId, value: state）。
    public Dictionary<string, object> objectsData = new Dictionary<string, object>();
}
