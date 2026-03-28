using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 瀵硅瘽椤甸潰鎺у埗鍣紙IDialogueView 鐨勯粯璁ゅ疄鐜帮級锛?// 璐熻矗鎶?DialogueService 鐨勬寚浠ゆ槧灏勫埌鍏蜂綋 UI 缁勪欢锛屽苟鎶婄敤鎴疯緭鍏ュ洖浼犵粰 Service銆?
public class DialoguePageController : MonoBehaviour, IDialogueView
{
    [Header("鐣岄潰寮曠敤")]
    // 鍙€夛細濡傛灉椤圭洰浣跨敤 UIPage 绠＄悊锛岃繖閲岀粦瀹氶〉闈㈠璞°€?
    [SerializeField] private UIPage page;
    // 璇磋瘽浜哄悕鏂囨湰銆?
    [SerializeField] private TextMeshProUGUI speakerText;
    // 姝ｆ枃鏂囨湰銆?
    [SerializeField] private TextMeshProUGUI contentText;
    // 澶村儚鍥剧墖锛堝彲涓虹┖锛夈€?
    [SerializeField] private Image portraitImage;
    // 鍔ㄦ€侀€夐」鎸夐挳鎸傝浇鏍硅妭鐐广€?
    [SerializeField] private Transform choicesRoot;
    // 閫夐」鎸夐挳棰勫埗浣撱€?
    [SerializeField] private DialogueChoiceButtonView choiceButtonPrefab;

    [Header("杈撳叆璁剧疆")]
    // 鎺ㄨ繘瀵硅瘽鐨勪富鎸夐敭銆?
    [SerializeField] private KeyCode nextKey = KeyCode.E;
    // 鏄惁鍏佽绌烘牸閿綔涓烘帹杩涢敭銆?
    [SerializeField] private bool allowSpaceAsNext = true;
    // 鏄惁鍏佽鍥炶溅閿綔涓烘帹杩涢敭銆?
    [SerializeField] private bool allowReturnAsNext = true;

    // 鍚戣繍琛屽眰鍙戦€佲€滀笅涓€姝ヨ姹傗€濄€?
    public event Action OnNextRequested;
    // 鍚戣繍琛屽眰鍙戦€佲€滈€夐」琚€夋嫨鈥濄€?
    public event Action<int> OnChoiceSelected;

    // 褰撳墠瀹炰緥鍖栧嚭鐨勫叏閮ㄩ€夐」鎸夐挳锛岀敤浜庣粺涓€鍥炴敹銆?
    private readonly List<DialogueChoiceButtonView> _spawnedChoices = new List<DialogueChoiceButtonView>();
    // 椤甸潰鏄惁澶勪簬鎵撳紑鐘舵€併€?
    private bool _isOpen;
    // 鏄惁姝ｅ湪灞曠ず閫夐」锛堝睍绀洪€夐」鏃朵笉鍝嶅簲 Next 閿級銆?
    private bool _showingChoices;

    /// <summary>
    /// Reset。
    /// </summary>
    private void Reset()
    {
        // 缂栬緫鍣ㄤ笅鑷姩濉厖 page 寮曠敤銆?
if (page == null) page = GetComponent<UIPage>();
    }

    /// <summary>
    /// Awake。
    /// </summary>
    private void Awake()
    {
        // 杩愯鏃跺厹搴曡嚜鍔ㄨ幏鍙?page銆?
if (page == null) page = GetComponent<UIPage>();
    }

    /// <summary>
    /// OnEnable。
    /// </summary>
    private void OnEnable()
    {
        // 椤甸潰鍚敤鍗崇粦瀹氫负褰撳墠 View銆?
_isOpen = true;
        DialogueService.Instance.BindView(this);
    }

    /// <summary>
    /// OnDisable。
    /// </summary>
    private void OnDisable()
    {
        // 椤甸潰绂佺敤鏃舵竻鐞嗘湰鍦扮姸鎬侊紝闃叉鏃ч€夐」娈嬬暀銆?
_isOpen = false;
        _showingChoices = false;
        ClearChoices();

        if (DialogueService.HasInstance)
        {
            // 瑙ｇ粦鏃惰嫢瀵硅瘽鍦ㄨ繘琛屼腑锛孲ervice 浼氬畨鍏ㄧ粨鏉熸祦绋嬨€?
DialogueService.Instance.UnbindView(this);
        }
    }

    /// <summary>
    /// Update。
    /// </summary>
    private void Update()
    {
        // 鏈墦寮€鎴栨鍦ㄩ€夐」闃舵鏃讹紝涓嶅鐞嗏€滀笅涓€姝モ€濇寜閿€?
if (!_isOpen || _showingChoices) return;

        bool pressed = Input.GetKeyDown(nextKey);
        if (allowSpaceAsNext) pressed |= Input.GetKeyDown(KeyCode.Space);
        if (allowReturnAsNext) pressed |= Input.GetKeyDown(KeyCode.Return);

        if (pressed)
        {
            OnNextRequested?.Invoke();
        }
    }

    /// <summary>
    /// Open。
    /// </summary>
    public void Open()
    {
        // 浼樺厛璧?UIPage 鐨勫紑椤甸€昏緫锛屾湭缁戝畾 page 鏃堕€€鍖栦负 SetActive銆?
if (page != null) page.Open();
        else gameObject.SetActive(true);
        _isOpen = true;
    }

    /// <summary>
    /// Close。
    /// </summary>
    public void Close()
    {
        // 鍏抽〉鍓嶅厛娓呯悊閫夐」瀹炰緥锛岄伩鍏嶄笅娆℃墦寮€鍑虹幇閲嶅銆?
ClearChoices();
        if (page != null) page.Close();
        else gameObject.SetActive(false);
        _isOpen = false;
        _showingChoices = false;
    }

    /// <summary>
    /// SetSpeaker。
    /// </summary>
    /// <param name="name">参数。</param>
    /// <param name="portrait">参数。</param>
    public void SetSpeaker(string name, Sprite portrait)
    {
        if (speakerText != null) speakerText.text = name ?? string.Empty;

        if (portraitImage != null)
        {
            // 鏃犲ご鍍忔椂闅愯棌鍥剧墖缁勪欢锛岄伩鍏嶆樉绀烘棫澶村儚銆?
portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }
    }

    /// <summary>
    /// SetContent。
    /// </summary>
    /// <param name="text">参数。</param>
    /// <param name="isTyping">参数。</param>
    public void SetContent(string text, bool isTyping)
    {
        // 褰撳墠瀹炵幇涓嶅尯鍒?isTyping 鏍峰紡锛涜嫢鍚庣画瑕佸仛闂儊鍏夋爣鍙敤璇ュ弬鏁版墿灞曘€?
if (contentText != null) contentText.text = text ?? string.Empty;
    }

    /// <summary>
    /// ShowChoices。
    /// </summary>
    /// <param name="choices">参数。</param>
    public void ShowChoices(IReadOnlyList<DialogueChoiceViewModel> choices)
    {
        ClearChoices();
        _showingChoices = choices != null && choices.Count > 0;

        // 缂哄皯蹇呰寮曠敤鏃剁洿鎺ヨ繑鍥烇紝閬垮厤绌哄紩鐢ㄥ紓甯搞€?
if (!_showingChoices || choicesRoot == null || choiceButtonPrefab == null)
        {
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoiceViewModel choice = choices[i];
            DialogueChoiceButtonView buttonView = Instantiate(choiceButtonPrefab, choicesRoot);
            buttonView.Setup(choice.Index, choice.Text, HandleChoiceClicked);
            _spawnedChoices.Add(buttonView);
        }
    }

    /// <summary>
    /// ClearChoices。
    /// </summary>
    public void ClearChoices()
    {
        // 閿€姣佹棫鎸夐挳瀹炰緥锛岀‘淇濇瘡娆″睍绀虹殑閫夐」涓庡綋鍓嶈妭鐐逛竴鑷淬€?
for (int i = 0; i < _spawnedChoices.Count; i++)
        {
            if (_spawnedChoices[i] != null)
            {
                Destroy(_spawnedChoices[i].gameObject);
            }
        }
        _spawnedChoices.Clear();
        _showingChoices = false;
    }

    /// <summary>
    /// HandleChoiceClicked。
    /// </summary>
    /// <param name="index">参数。</param>
    private void HandleChoiceClicked(int index)
    {
        // 灏嗛€夋嫨缁撴灉鍥炰紶缁欒繍琛屽眰澶勭悊璺宠浆銆?
OnChoiceSelected?.Invoke(index);
    }
}
