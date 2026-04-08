/// <summary>
/// 存档序列化器接口：用于扩展不同序列化方案（JSON/二进制/加密等）。
/// </summary>
public interface ISaveSerializer
{
    /// <summary>
    /// 将存档对象序列化为字符串。
    /// </summary>
    /// <param name="saveData">存档对象。</param>
    /// <returns>序列化后的文本。</returns>
    string Serialize(SaveData saveData);

    /// <summary>
    /// 将字符串反序列化为存档对象。
    /// </summary>
    /// <param name="content">存档文本。</param>
    /// <returns>反序列化后的存档对象。</returns>
    SaveData Deserialize(string content);
}
