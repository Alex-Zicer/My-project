using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
// 2D dialogue trigger for player interaction and optional profile routing.
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Config (Routing)")]
    // Whether profile routing is enabled.
    [SerializeField] private bool useProfileRouting = true;

    // NPC id used by router/progress store.
    [SerializeField] private string npcId = "npc_001";

    // Dialogue profile for routing mode.
    [SerializeField] private NpcDialogueProfileSO dialogueProfile;

    [Header("Dialogue Config (Legacy)")]
    // Direct dialogue reference when routing is disabled.
    [SerializeField] private DialogueReference dialogueReference = new DialogueReference();

    // One-shot flag in legacy mode.
    [SerializeField] private bool oneShot;

    [Header("Interaction")]
    // Interaction key.
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    // Interaction anchor transform.
    [SerializeField] private Transform interactOrigin;

    // Trigger collider zone.
    [SerializeField] private BoxCollider2D triggerZone;

    [Header("Hint (Optional)")]
    // Root object for interaction hint.
    [SerializeField] private GameObject interactHintRoot;

    // Whether hint follows interaction anchor.
    [SerializeField] private bool followInteractOrigin = true;

    // Hint position offset.
    [SerializeField] private Vector3 hintOffset = new Vector3(0f, 2f, 0f);

    // Hint text format. {0} is key name.
    [SerializeField] private string hintFormat = "[{0}] Interact";

    // Hint TMP text.
    [SerializeField] private TMP_Text hintText;

    // Legacy one-shot runtime flag.
    private bool _hasTriggered;

    // Whether player is inside trigger range.
    private bool _playerInRange;

    // Current hint visibility cache.
    private bool _hintVisible;

    /// <summary>
    /// Initializes trigger references and hint state.
    /// </summary>
    private void Awake()
    {
        if (interactOrigin == null)
        {
            interactOrigin = transform;
        }

        if (triggerZone == null)
        {
            triggerZone = GetComponent<BoxCollider2D>();
        }

        if (triggerZone != null)
        {
            triggerZone.isTrigger = true;
        }

        RefreshHintText();
        SetHintVisible(false, true);
    }

    /// <summary>
    /// Resets hint state on enable.
    /// </summary>
    private void OnEnable()
    {
        SetHintVisible(false, true);
    }

    /// <summary>
    /// Clears range state and hint on disable.
    /// </summary>
    private void OnDisable()
    {
        _playerInRange = false;
        SetHintVisible(false, true);
    }

    /// <summary>
    /// Handles interaction input and dialogue start.
    /// </summary>
    private void Update()
    {
        bool isDialogueRunning = DialogueService.HasInstance && DialogueService.Instance.IsRunning;
        bool useRouting = IsUsingProfileRouting();

        if (!useRouting && oneShot && _hasTriggered)
        {
            SetHintVisible(false);
            return;
        }

        if (!_playerInRange)
        {
            SetHintVisible(false);
            return;
        }

        if (isDialogueRunning)
        {
            SetHintVisible(false);
            return;
        }

        SetHintVisible(true);
        if (_hintVisible)
        {
            RefreshHintTransform();
        }

        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        bool started = useRouting
            ? TryStartByProfile()
            : DialogueService.Instance.StartDialogue(dialogueReference);

        if (started && !useRouting && oneShot)
        {
            _hasTriggered = true;
        }

        if (started)
        {
            SetHintVisible(false);
        }
    }

    /// <summary>
    /// Checks whether profile routing can be used.
    /// </summary>
    /// <returns>True when routing config is valid.</returns>
    private bool IsUsingProfileRouting()
    {
        if (!useProfileRouting)
        {
            return false;
        }

        if (dialogueProfile == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(npcId);
    }

    /// <summary>
    /// Resolves route by profile and starts dialogue.
    /// </summary>
    /// <returns>True when dialogue starts successfully.</returns>
    private bool TryStartByProfile()
    {
        if (!DialogueRouterService.Instance.TryResolve(npcId, dialogueProfile, out DialogueRouteResult route, out string error))
        {
            Debug.LogWarning($"[DialogueTrigger] Failed to resolve NPC '{npcId}': {error}");
            return false;
        }

        return DialogueService.Instance.StartDialogue(route.DialogueReference, route);
    }

    /// <summary>
    /// Marks player as in range.
    /// </summary>
    /// <param name="other">Trigger collider.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = true;
        }
    }

    /// <summary>
    /// Marks player as out of range.
    /// </summary>
    /// <param name="other">Trigger collider.</param>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = false;
            SetHintVisible(false);
        }
    }

    /// <summary>
    /// Sets hint visibility with cached state.
    /// </summary>
    /// <param name="visible">Target visibility.</param>
    /// <param name="force">Whether to force update.</param>
    private void SetHintVisible(bool visible, bool force = false)
    {
        if (interactHintRoot == null)
        {
            return;
        }

        if (!force && _hintVisible == visible)
        {
            return;
        }

        _hintVisible = visible;
        interactHintRoot.SetActive(visible);

        if (visible)
        {
            RefreshHintTransform();
        }
    }

    /// <summary>
    /// Updates hint world position.
    /// </summary>
    private void RefreshHintTransform()
    {
        if (interactHintRoot == null || !followInteractOrigin)
        {
            return;
        }

        interactHintRoot.transform.position = interactOrigin.position + hintOffset;
    }

    /// <summary>
    /// Refreshes hint label text.
    /// </summary>
    private void RefreshHintText()
    {
        if (hintText == null)
        {
            return;
        }

        string keyName = interactKey.ToString().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(hintFormat))
        {
            hintText.text = keyName;
            return;
        }

        hintText.text = hintFormat.Contains("{0}")
            ? hintFormat.Replace("{0}", keyName)
            : hintFormat + " " + keyName;
    }

    /// <summary>
    /// Keeps serialized references valid in editor.
    /// </summary>
    private void OnValidate()
    {
        if (interactOrigin == null)
        {
            interactOrigin = transform;
        }

        if (triggerZone == null)
        {
            triggerZone = GetComponent<BoxCollider2D>();
        }

        if (triggerZone != null)
        {
            triggerZone.isTrigger = true;
        }

        if (string.IsNullOrWhiteSpace(npcId))
        {
            npcId = gameObject != null ? gameObject.name : "npc_001";
        }

        RefreshHintText();
    }

    /// <summary>
    /// Draws trigger bounds for scene debugging.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D zone = triggerZone != null ? triggerZone : GetComponent<BoxCollider2D>();
        if (zone == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = zone.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(zone.offset, zone.size);
        Gizmos.matrix = old;
    }
}
