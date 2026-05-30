using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central owner of all popups. Attach to the PopupCanvas GameObject. Looks up
/// prefabs from a <see cref="PopupsConfig"/> by <see cref="PopupType"/>, lazily
/// instantiates them under the canvas, caches them for reuse, and shows them one at
/// a time through a FIFO queue.
///
/// Queue rules (only one popup is ever visible, transitions never overlap):
/// - <see cref="Show"/> with nothing open → opens immediately.
/// - <see cref="Show"/> while a popup is open and idle → that popup is closed
///   (preempted) and the requested one opens after the close animation.
/// - <see cref="Show"/> while a transition is in progress → the request waits in the
///   queue and opens after the current popup is dismissed.
///
/// Persists across scenes (DontDestroyOnLoad) so Settings/Shop/etc. are available
/// on both the Home and Game scenes, matching SFXManager/ThemeManager.
/// </summary>
public class PopupCanvasManager : MonoBehaviour
{
    public static PopupCanvasManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private PopupsConfig config;

    [Tooltip("Parent for instantiated popups. Defaults to this transform (the canvas).")]
    [SerializeField] private Transform popupParent;

    private readonly Dictionary<PopupType, Popup> _cache = new Dictionary<PopupType, Popup>();
    private readonly List<PopupType> _queue = new List<PopupType>();

    private PopupType? _current; // the popup currently shown (null if none)
    private bool _busy;          // true while an open/close animation is running

    /// <summary>True while a popup is animating open or closed.</summary>
    public bool IsBusy => _busy;

    /// <summary>The popup currently shown, or null if none.</summary>
    public PopupType? CurrentPopup => _current;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (popupParent == null)
            popupParent = transform;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Requests a popup. See the class summary for queue/preempt behaviour.
    /// Returns the (possibly not-yet-visible) instance for configuration, or null on
    /// lookup failure.</summary>
    public Popup Show(PopupType type)
    {
        Popup popup = GetOrCreate(type);
        if (popup == null)
            return null;

        // Already showing this exact popup and idle → no-op.
        if (!_busy && _current.HasValue && _current.Value == type)
            return popup;

        Enqueue(type);

        if (!_busy)
        {
            if (_current.HasValue)
                CloseCurrent();   // preempt the idle popup; the queue pumps on close
            else
                PumpQueue();      // nothing open → open the next queued popup now
        }
        return popup;
    }

    /// <summary>Typed convenience for <see cref="Show(PopupType)"/>, e.g.
    /// Show&lt;ShopPanel&gt;(PopupType.ShopPanel) to configure the controller.</summary>
    public T Show<T>(PopupType type) where T : Popup
    {
        return Show(type) as T;
    }

    /// <summary>Closes <paramref name="type"/> if it is the popup currently shown,
    /// otherwise removes it from the pending queue.</summary>
    public void Hide(PopupType type)
    {
        if (_current.HasValue && _current.Value == type)
            CloseCurrent();
        else
            _queue.Remove(type);
    }

    /// <summary>Closes the popup currently shown (the next queued popup opens after).</summary>
    public void CloseCurrent()
    {
        if (_busy || !_current.HasValue)
            return;

        if (!_cache.TryGetValue(_current.Value, out Popup popup) || popup == null)
        {
            _current = null;
            PumpQueue();
            return;
        }

        _busy = true;
        popup.Close(() =>
        {
            _current = null;
            _busy = false;
            PumpQueue();
        });
    }

    /// <summary>Drops all pending (not-yet-shown) requests. Does not affect the open popup.</summary>
    public void ClearQueue() => _queue.Clear();

    /// <summary>True if <paramref name="type"/> is the popup currently shown.</summary>
    public bool IsOpen(PopupType type) => _current.HasValue && _current.Value == type;

    private void Enqueue(PopupType type)
    {
        if (_current.HasValue && _current.Value == type) return; // already showing
        if (_queue.Contains(type)) return;                       // already pending
        _queue.Add(type);
    }

    private void PumpQueue()
    {
        if (_busy || _current.HasValue || _queue.Count == 0)
            return;

        PopupType next = _queue[0];
        _queue.RemoveAt(0);

        Popup popup = GetOrCreate(next);
        if (popup == null)
        {
            PumpQueue(); // skip a bad entry and try the next one
            return;
        }

        _busy = true;
        popup.transform.SetAsLastSibling();
        popup.Open(() =>
        {
            _current = next;
            _busy = false;
            // Intentionally no PumpQueue here: the opened popup stays until it is
            // dismissed (RequestClose/Hide) or preempted by a new Show.
        });
    }

    private Popup GetOrCreate(PopupType type)
    {
        if (_cache.TryGetValue(type, out Popup existing) && existing != null)
            return existing;

        if (config == null)
        {
            Debug.LogWarning("[PopupCanvasManager] No PopupsConfig assigned.");
            return null;
        }

        if (!config.TryGetPrefab(type, out Popup prefab))
        {
            Debug.LogWarning($"[PopupCanvasManager] No prefab registered for popup type {type}.");
            return null;
        }

        Popup instance = Instantiate(prefab, popupParent);
        instance.SetType(type);
        instance.gameObject.SetActive(false);
        _cache[type] = instance;
        return instance;
    }
}
