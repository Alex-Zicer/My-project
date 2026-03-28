using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 鍗曚釜閫夐」鎸夐挳瑙嗗浘锛?// 璐熻矗鎶娾€滈€夐」鏂囨湰 + 绱㈠紩鈥濈粦瀹氬埌 UI Button 鐐瑰嚮浜嬩欢銆?
public class DialogueChoiceButtonView : MonoBehaviour
{
    // 瀹為檯鍝嶅簲鐐瑰嚮鐨勬寜閽粍浠躲€?
    [SerializeField] private Button button;
    // 鏄剧ず閫夐」鍐呭鐨勬枃鏈粍浠躲€?
    [SerializeField] private TextMeshProUGUI label;

    // 褰撳墠鎸夐挳瀵瑰簲鐨勯€夐」绱㈠紩銆?
    private int _choiceIndex;
    // 鐐瑰嚮鍚庡洖璋冪粰涓婂眰锛堥€氬父鏄?DialoguePageController锛夈€?
    private Action<int> _clickHandler;

    /// <summary>
    /// Reset。
    /// </summary>
    private void Reset()
    {
        // 鍦ㄧ紪杈戝櫒閲嶇疆鏃惰嚜鍔ㄥ皾璇曟姄鍙栧悓鐗╀綋缁勪欢锛屽噺灏戞墜宸ョ粦瀹氥€?
if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Awake。
    /// </summary>
    private void Awake()
    {
        // 杩愯鏃跺厹搴曪細濡傛灉鏈湪 Inspector 缁戝畾锛岃嚜鍔ㄦ煡鎵俱€?
if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
    }

    // 鍒濆鍖栨寜閽樉绀轰笌鐐瑰嚮琛屼负銆?
    /// <summary>
    /// Setup。
    /// </summary>
    /// <param name="choiceIndex">参数。</param>
    /// <param name="text">参数。</param>
    /// <param name="clickHandler">参数。</param>
    public void Setup(int choiceIndex, string text, Action<int> clickHandler)
    {
        _choiceIndex = choiceIndex;
        _clickHandler = clickHandler;

        if (label != null) label.text = text ?? string.Empty;
        if (button != null)
        {
            // 鍏堟竻鍐嶇粦锛岄伩鍏嶅鐢ㄦ寜閽椂閲嶅璁㈤槄銆?
button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    /// <summary>
    /// OnClick。
    /// </summary>
    private void OnClick()
    {
        // 灏嗙储寮曞洖浼犵粰涓婂眰锛屽叿浣撹烦杞€昏緫鐢辫繍琛屽眰鍐冲畾銆?
_clickHandler?.Invoke(_choiceIndex);
    }
}
