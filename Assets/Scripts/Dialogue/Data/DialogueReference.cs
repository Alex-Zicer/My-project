using System;
using UnityEngine;

// 瀵硅瘽鏉ユ簮绫诲瀷锛?// So/Json/Csv 琛ㄧず鍐呯疆鏀寔鐨勬暟鎹簮锛孋ustom 棰勭暀缁欓」鐩嚜瀹氫箟 Provider銆?
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
    // 褰撳墠寮曠敤浣跨敤鐨勬暟鎹簮绫诲瀷銆?
    public DialogueSourceType sourceType = DialogueSourceType.So;

    // 涓?SO 璧勬簮锛氬綋 sourceType=So 鏃剁敱 SoDialogueProvider 浣跨敤銆?
    public DialogueDataSO primarySO;

    // 澶栭儴鏁版嵁閿?璺緞锛?    // Json/Csv 妯″紡涓嬪彲濉浉瀵硅矾寰勶紙鐩稿 StreamingAssets锛夋垨缁濆璺緞銆?
    public string keyOrPath;

    // 鍥為€€ SO锛氫富鏉ユ簮鍔犺浇澶辫触鏃讹紝娉ㄥ唽琛ㄤ細灏濊瘯浣跨敤璇ヨ祫婧愬厹搴曘€?
    public DialogueDataSO fallbackSO;

    // 渚挎嵎鏋勯€狅細蹇€熶粠 SO 鐢熸垚涓€涓彲鐩存帴杩愯鐨勫紩鐢ㄥ璞°€?
    /// <summary>
    /// FromSo。
    /// </summary>
    /// <param name="so">参数。</param>
    public static DialogueReference FromSo(DialogueDataSO so)
    {
        return new DialogueReference
        {
            sourceType = DialogueSourceType.So,
            primarySO = so
        };
    }
}
