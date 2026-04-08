using System;
using Newtonsoft.Json;

/// <summary>
/// 基于 Newtonsoft.Json 的存档序列化器实现。
/// </summary>
public class JsonNetSaveSerializer : ISaveSerializer
{
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.None
    };

    /// <summary>
    /// 序列化存档对象。
    /// </summary>
    /// <param name="saveData">存档对象。</param>
    /// <returns>JSON 文本。</returns>
    public string Serialize(SaveData saveData)
    {
        if (saveData == null)
        {
            throw new ArgumentNullException(nameof(saveData));
        }

        return JsonConvert.SerializeObject(saveData, SerializerSettings);
    }

    /// <summary>
    /// 反序列化 JSON 文本为存档对象。
    /// </summary>
    /// <param name="content">JSON 文本。</param>
    /// <returns>存档对象。</returns>
    public SaveData Deserialize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("反序列化失败：content 为空。", nameof(content));
        }

        SaveData saveData = JsonConvert.DeserializeObject<SaveData>(content, SerializerSettings);
        if (saveData == null)
        {
            throw new InvalidOperationException("反序列化失败：得到空 SaveData。");
        }

        if (saveData.objectsData == null)
        {
            saveData.objectsData = new System.Collections.Generic.Dictionary<string, object>();
        }

        return saveData;
    }
}
