using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 默认异步文件处理器：负责本地文本读写。
/// </summary>
public class AsyncSaveFileHandler : ISaveFileHandler
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    /// <summary>
    /// 异步写入文本到文件。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="content">写入文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("WriteAllTextAsync 失败：filePath 为空。", nameof(filePath));
        }

        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string writeContent = content ?? string.Empty;
        await File.WriteAllTextAsync(filePath, writeContent, Utf8Encoding, cancellationToken);
    }

    /// <summary>
    /// 异步读取文件全部文本。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>文件文本。</returns>
    public async Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("ReadAllTextAsync 失败：filePath 为空。", nameof(filePath));
        }

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// 异步检查文件是否存在。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在返回 true，否则 false。</returns>
    public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool exists = !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// 异步删除文件（不存在时安全返回）。
    /// </summary>
    /// <param name="filePath">文件完整路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        bool exists = File.Exists(filePath);
        if (!exists)
        {
            return;
        }

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(filePath);
        }, cancellationToken);
    }
}
