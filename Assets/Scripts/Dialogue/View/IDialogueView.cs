using System;
using System.Collections.Generic;
using UnityEngine;

// 瀵硅瘽灞曠ず鎺ュ彛锛堣繍琛屽眰涓?UI 灞傝В鑰﹁竟鐣岋級锛?// DialogueService 鍙緷璧栬鎺ュ彛锛屼笉鐩存帴渚濊禆 TMP銆丅utton銆佸叿浣撻〉闈㈠疄鐜般€?
public interface IDialogueView
{
    // 鐢ㄦ埛璇锋眰鈥滅户缁笅涓€姝モ€濓紙鎸夐敭鐐瑰嚮绛夛級鏃惰Е鍙戙€?
event Action OnNextRequested;
    // 鐢ㄦ埛閫夋嫨鍒嗘敮閫夐」鏃惰Е鍙戯紝鍙傛暟涓洪€夐」绱㈠紩銆?
event Action<int> OnChoiceSelected;

    // 鎵撳紑瀵硅瘽鐣岄潰銆?
/// <summary>
/// Open。
/// </summary>
void Open();
    // 鍏抽棴瀵硅瘽鐣岄潰銆?
/// <summary>
/// Close。
/// </summary>
void Close();
    // 璁剧疆璇磋瘽浜轰俊鎭€?
/// <summary>
/// SetSpeaker。
/// </summary>
/// <param name="name">参数。</param>
/// <param name="portrait">参数。</param>
void SetSpeaker(string name, Sprite portrait);
    // 鍒锋柊姝ｆ枃鏂囨湰锛沬sTyping 琛ㄧず鏄惁澶勪簬鎵撳瓧鏈鸿繃绋嬨€?
/// <summary>
/// SetContent。
/// </summary>
/// <param name="text">参数。</param>
/// <param name="isTyping">参数。</param>
void SetContent(string text, bool isTyping);
    // 鏄剧ず鍙€夊垎鏀垪琛ㄣ€?
/// <summary>
/// ShowChoices。
/// </summary>
/// <param name="choices">参数。</param>
void ShowChoices(IReadOnlyList<DialogueChoiceViewModel> choices);
    // 娓呯┖褰撳墠鍒嗘敮鎸夐挳銆?
/// <summary>
/// ClearChoices。
/// </summary>
void ClearChoices();
}
