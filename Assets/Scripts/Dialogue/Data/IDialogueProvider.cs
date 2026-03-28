// 瀵硅瘽鏁版嵁鎻愪緵鑰呮帴鍙ｏ細
// 璐熻矗鎶婁换鎰忔潵婧愮殑鏁版嵁锛圫O/JSON/CSV/杩滅绛夛級杞崲涓虹粺涓€鐨?DialogueGraph銆?
public interface IDialogueProvider
{
    // 鍒ゆ柇褰撳墠 Provider 鏄惁鑳藉鐞嗚繖鏉″紩鐢ㄣ€?
/// <summary>
/// CanHandle。
/// </summary>
/// <param name="reference">参数。</param>
bool CanHandle(DialogueReference reference);

    // 灏濊瘯鍔犺浇骞惰浆鎹负杩愯鏃跺璇濆浘銆?    // 杩斿洖 false 鏃跺繀椤绘彁渚涘彲璇婚敊璇俊鎭紝渚夸簬鏃ュ織瀹氫綅涓庡洖閫€澶勭悊銆?
/// <summary>
/// TryLoad。
/// </summary>
/// <param name="reference">参数。</param>
/// <param name="graph">参数。</param>
/// <param name="error">参数。</param>
bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error);
}
