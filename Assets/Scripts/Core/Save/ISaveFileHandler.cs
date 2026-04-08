using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 存档文件处理接口：用于扩展不同文件存储方案（本地/加密/云端等）。
/// </summary>
public interface ISaveFileHandler
{
    /// <summary>
    /// 异步写入文本到文件。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="content">写入文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步读取文件全部文本。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>文件文本。</returns>
    Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步检查文件是否存在。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在返回 true，否则 false。</returns>
    Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步删除文件（不存在时安全返回）。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
}
