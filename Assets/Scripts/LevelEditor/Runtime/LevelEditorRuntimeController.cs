using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Entry point for the runtime level editor. Bootstraps a Canvas and
/// EventSystem if the scene doesn't already have one, owns the current
/// in-memory LevelAsset, and wires the toolbar (New / Save / Load /
/// + Screen / Delete Screen) to the ScreenCanvasView.
///
/// Attach this to a single empty GameObject in the Level Editor scene -
/// everything else is built at runtime, no prefab required.
/// </summary>
public class LevelEditorRuntimeController : MonoBehaviour
{
    private const float ToolbarHeight = 44f;

    [Tooltip("Scene that boots gameplay from PlayTestSession's saved level - " +
             "must match its name in Build Settings.")]
    [SerializeField] private string _PlayTestSceneName = "PlayTest";

    private LevelAsset _Level;
    private ScreenCanvasView _CanvasView;
    private TMP_InputField _NameField;
    private RectTransform _LoadListContent;
    private GameObject _LoadPanel;
    private GameObject _PalettePanel;
    private GameObject _EntityPalettePanel;
    private TMP_Text _StatusLabel;

    private void Awake()
    {
        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();

        // ScreenCanvasView is created first (and so sits behind, in sibling
        // order) so the toolbar and its Load dropdown - built after, below -
        // render on top of it instead of being hidden underneath.
        _CanvasView = ScreenCanvasView.Create(canvas.transform);
        var canvasRect = (RectTransform)_CanvasView.transform;
        canvasRect.offsetMax = new Vector2(0, -ToolbarHeight);

        BuildToolbar(canvas.transform);

        // Coming back from a Play Test rather than a fresh launch of this
        // scene - restore the level that was being edited instead of
        // starting blank. Consumed once here; PlayTestSession.IsPlaytesting
        // is what gated the playtest scene's level source and its "back to
        // editor" hotkey, so it must go false as soon as we're back.
        if (PlayTestSession.IsPlaytesting)
        {
            PlayTestSession.IsPlaytesting = false;
            LoadLevel(PlayTestSession.LevelSlotName);
            _NameField.text = PlayTestSession.ReturnDisplayName;
        }
        else
        {
            NewLevel();
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        // activeInputHandler is "Both" for this project, so the legacy
        // StandaloneInputModule (no InputActionAsset wiring required) works.
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas EnsureCanvas()
    {
        var existing = FindAnyObjectByType<Canvas>();
        if (existing != null)
            return existing;

        var go = new GameObject("LevelEditorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        return canvas;
    }

    // ------------------------------------------------------------------
    // Toolbar
    // ------------------------------------------------------------------

    private void BuildToolbar(Transform parent)
    {
        var barGO = new GameObject("Toolbar", typeof(RectTransform), typeof(Image));
        barGO.transform.SetParent(parent, false);

        var barRect = (RectTransform)barGO.transform;
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0, 1);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0, ToolbarHeight);
        barGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var layout = barGO.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        _NameField = CreateInputField(barRect, "Level Name", 180);
        _NameField.text = "New Level";

        CreateButton(barRect, "New", NewLevel, 60);
        CreateButton(barRect, "Save", SaveLevel, 60);
        CreateButton(barRect, "Load", ToggleLoadPanel, 60);
        CreateButton(barRect, "+ Screen", AddScreen, 80);
        CreateButton(barRect, "Delete Screen", DeleteScreen, 110);
        CreateButton(barRect, "Screens Mode", () => SetMode(ScreenCanvasView.InteractionMode.Screens), 110);
        CreateButton(barRect, "Paint Mode", () => SetMode(ScreenCanvasView.InteractionMode.Paint), 100);
        CreateButton(barRect, "Entities Mode", () => SetMode(ScreenCanvasView.InteractionMode.Entities), 120);
        CreateButton(barRect, "Delete Entity", DeleteEntity, 110);
        CreateButton(barRect, "Toggle Snap", ToggleSnapToGrid, 100);
        CreateButton(barRect, "Play Test", PlayTestLevel, 90);

        _LoadPanel = CreateLoadPanel(parent);
        _PalettePanel = CreatePalettePanel(parent);
        _EntityPalettePanel = CreateEntityPalettePanel(parent);
        CreateStatusPanel(parent);
        UpdateStatusLabel();
    }

    // Always visible regardless of mode (unlike the two palette panels) -
    // snap state in particular needs to be readable while placing entities.
    // Anchored bottom-left: top-left is the Load dropdown's spot and
    // top-right is the two palette panels', so bottom-left is the one
    // corner nothing else ever occupies.
    private void CreateStatusPanel(Transform parent)
    {
        var go = new GameObject(
            "StatusPanel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(8, 8);
        rect.sizeDelta = new Vector2(260, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var statusGO = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(go.transform, false);
        statusGO.AddComponent<LayoutElement>().preferredHeight = 72f;
        _StatusLabel = statusGO.GetComponent<TextMeshProUGUI>();
        _StatusLabel.fontSize = 12;
        _StatusLabel.color = new Color(1f, 1f, 1f, 0.8f);
        _StatusLabel.enableWordWrapping = true;
    }

    private static TMP_InputField CreateInputField(Transform parent, string placeholder, float width)
    {
        var go = new GameObject("NameField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredWidth = width;
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // TMP_InputField expects textViewport to be a distinct child (with its
        // own RectMask2D), not the input field's own rect - matching Unity's
        // own TMP_InputField prefab structure here rather than the shortcut
        // used before, which could make typed input behave unreliably.
        var viewportGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(go.transform, false);
        var viewportRect = (RectTransform)viewportGO.transform;
        StretchFull(viewportRect, 6);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(viewportGO.transform, false);
        StretchFull((RectTransform)textGO.transform, 0);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.fontSize = 16;
        text.color = Color.white;
        text.enableWordWrapping = false;

        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(viewportGO.transform, false);
        StretchFull((RectTransform)placeholderGO.transform, 0);
        var placeholderText = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 16;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(1f, 1f, 1f, 0.4f);

        var field = go.GetComponent<TMP_InputField>();
        field.textViewport = viewportRect;
        field.textComponent = text;
        field.placeholder = placeholderText;

        return field;
    }

    private static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float width)
    {
        var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        // Without an explicit preferredHeight, a VerticalLayoutGroup with
        // childForceExpandHeight = false (the Load list) collapses this
        // button to zero height - visible label, but nothing clickable.
        // The toolbar's HorizontalLayoutGroup masks the same gap by force-
        // expanding height, which is why only the Load list showed this.
        layoutElement.preferredHeight = 28f;

        go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)textGO.transform, 4);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void CreateLabel(Transform parent, string label)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14;
        text.color = new Color(1f, 1f, 1f, 0.6f);
    }

    private static void StretchFull(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private GameObject CreateLoadPanel(Transform parent)
    {
        var go = new GameObject(
            "LoadPanel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(8, -ToolbarHeight - 4);
        rect.sizeDelta = new Vector2(220, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 4;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _LoadListContent = rect;
        go.SetActive(false);
        return go;
    }

    private void ToggleLoadPanel()
    {
        bool show = !_LoadPanel.activeSelf;
        _LoadPanel.SetActive(show);
        if (show)
            RefreshLoadList();
    }

    private void RefreshLoadList()
    {
        for (int i = _LoadListContent.childCount - 1; i >= 0; i--)
            Destroy(_LoadListContent.GetChild(i).gameObject);

        // The Play Test scratch slot is an implementation detail, not a
        // level the user saved - never list it.
        var levels = LevelIO.ListLevels().Where(name => name != PlayTestSession.LevelSlotName).ToList();
        Debug.Log($"[LevelEditor] Found {levels.Count} saved level(s) under {Application.persistentDataPath}/Levels");
        if (levels.Count == 0)
        {
            CreateLabel(_LoadListContent, "No saved levels.");
            return;
        }

        foreach (var name in levels)
        {
            string captured = name;
            CreateButton(_LoadListContent, captured, () => LoadLevel(captured), 200);
        }
    }

    private GameObject CreatePalettePanel(Transform parent)
    {
        var go = new GameObject(
            "PalettePanel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-8, -ToolbarHeight - 4);
        rect.sizeDelta = new Vector2(240, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 4;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layerRowGO = new GameObject("LayerRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        layerRowGO.transform.SetParent(go.transform, false);
        layerRowGO.AddComponent<LayoutElement>().preferredHeight = 28f;
        var layerRowLayout = layerRowGO.GetComponent<HorizontalLayoutGroup>();
        layerRowLayout.spacing = 4;
        layerRowLayout.childForceExpandWidth = true;
        layerRowLayout.childForceExpandHeight = true;
        CreateButton(layerRowGO.transform, "Background", () => SetActiveLayer(ScreenCanvasView.TileLayerKind.Background), 110);
        CreateButton(layerRowGO.transform, "Foreground", () => SetActiveLayer(ScreenCanvasView.TileLayerKind.Foreground), 110);

        CreateButton(go.transform, "Eraser", () => SetActiveTile(""), 200);

        foreach (var tileDef in TileCatalog.All)
        {
            string tileId = tileDef.Id;
            CreateTileButton(go.transform, tileDef, () => SetActiveTile(tileId));
        }

        if (TileCatalog.All.Count == 0)
            CreateLabel(go.transform, "No tiles found. Add a Tileset asset under Assets/Resources.");

        go.SetActive(false);
        return go;
    }

    private static void CreateTileButton(Transform parent, TileDef tileDef, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(tileDef.Id + " Tile Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 36f;
        go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = (RectTransform)iconGO.transform;
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(4, 0);
        iconRect.sizeDelta = new Vector2(28, -6);
        var icon = iconGO.GetComponent<Image>();
        icon.sprite = tileDef.Sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var textRect = (RectTransform)textGO.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(38, 2);
        textRect.offsetMax = new Vector2(-4, -2);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = string.IsNullOrEmpty(tileDef.DisplayName) ? tileDef.Id : tileDef.DisplayName;
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
    }

    private GameObject CreateEntityPalettePanel(Transform parent)
    {
        var go = new GameObject(
            "EntityPalettePanel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-8, -ToolbarHeight - 4);
        rect.sizeDelta = new Vector2(240, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 4;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var def in EntityCatalog.All)
        {
            string typeId = def.TypeId;
            CreateEntityButton(go.transform, def, () => SetActiveEntityType(typeId));
        }

        if (EntityCatalog.All.Count == 0)
            CreateLabel(go.transform, "No entity types found. Add an Entity Definition asset under Assets/Resources.");

        go.SetActive(false);
        return go;
    }

    private static void CreateEntityButton(Transform parent, EntityDefinition def, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(def.TypeId + " Entity Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 36f;
        go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = (RectTransform)iconGO.transform;
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(4, 0);
        iconRect.sizeDelta = new Vector2(28, -6);
        var icon = iconGO.GetComponent<Image>();
        icon.sprite = def.Icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var textRect = (RectTransform)textGO.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(38, 2);
        textRect.offsetMax = new Vector2(-4, -2);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = string.IsNullOrEmpty(def.DisplayName) ? def.TypeId : def.DisplayName;
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
    }

    // ------------------------------------------------------------------
    // Actions
    // ------------------------------------------------------------------

    private void NewLevel()
    {
        _Level = new LevelAsset();
        _CanvasView.SetLevel(_Level);
    }

    private void SaveLevel()
    {
        if (_Level == null)
        {
            Debug.LogWarning("[LevelEditor] No level loaded, nothing to save.");
            return;
        }

        if (!AllScreensHaveSpawnPoint(out int missingScreenId))
        {
            Debug.LogError(
                $"[LevelEditor] Save blocked: Screen {missingScreenId} has no spawn point entity. " +
                "Place one (an entity type with IsSpawnPoint checked) before saving.");
            return;
        }

        string name = string.IsNullOrWhiteSpace(_NameField.text) ? "New Level" : _NameField.text.Trim();
        Debug.Log($"[LevelEditor] Saving as '{name}' ({_Level.Screens.Count} screen(s))...");
        LevelIO.Save(_Level, name);
    }

    /// <summary>
    /// Saves the in-memory level to the Play Test scratch slot and hands off
    /// to the PlayTest scene, which boots real gameplay from it via
    /// LevelInstantiator - no manual scene duplication or prefab wiring per
    /// playtest. Press the in-game "back to editor" key (F1 by default) to
    /// return here with this same level still loaded.
    /// </summary>
    private void PlayTestLevel()
    {
        if (_Level == null || _Level.Screens.Count == 0)
        {
            Debug.LogWarning("[LevelEditor] Nothing to playtest - add at least one screen first.");
            return;
        }

        if (!AllScreensHaveSpawnPoint(out int missingScreenId))
        {
            Debug.LogError(
                $"[LevelEditor] Play Test blocked: Screen {missingScreenId} has no spawn point entity. " +
                "Place one (an entity type with IsSpawnPoint checked) before playtesting.");
            return;
        }

        LevelIO.Save(_Level, PlayTestSession.LevelSlotName);
        PlayTestSession.IsPlaytesting = true;
        PlayTestSession.ReturnDisplayName = _NameField.text;

        Debug.Log($"[LevelEditor] Launching Play Test ('{_PlayTestSceneName}')...");
        SceneManager.LoadScene(_PlayTestSceneName);
    }

    /// <summary>True if every screen has at least one entity whose type is marked IsSpawnPoint.</summary>
    private bool AllScreensHaveSpawnPoint(out int missingScreenId)
    {
        foreach (var screen in _Level.Screens)
        {
            bool hasSpawnPoint = _Level.Entities.Any(e =>
                e.ScreenId == screen.Id
                && EntityCatalog.Lookup.TryGetValue(e.TypeId, out var def)
                && def.IsSpawnPoint);

            if (!hasSpawnPoint)
            {
                missingScreenId = screen.Id;
                return false;
            }
        }

        missingScreenId = -1;
        return true;
    }

    private void LoadLevel(string name)
    {
        Debug.Log($"[LevelEditor] Loading '{name}'...");
        var loaded = LevelIO.Load(name);
        if (loaded == null)
        {
            Debug.LogWarning($"[LevelEditor] LevelIO.Load('{name}') returned null.");
            return;
        }

        Debug.Log($"[LevelEditor] Loaded '{name}' ({loaded.Screens.Count} screen(s)).");
        _Level = loaded;
        _NameField.text = name;
        _CanvasView.SetLevel(_Level);
        _LoadPanel.SetActive(false);
    }

    private void AddScreen() => _CanvasView.AddScreen();
    private void DeleteScreen() => _CanvasView.DeleteSelectedScreen();

    private void SetMode(ScreenCanvasView.InteractionMode mode)
    {
        _CanvasView.SetMode(mode);
        _PalettePanel.SetActive(mode == ScreenCanvasView.InteractionMode.Paint);
        _EntityPalettePanel.SetActive(mode == ScreenCanvasView.InteractionMode.Entities);
        UpdateStatusLabel();
    }

    private void SetActiveLayer(ScreenCanvasView.TileLayerKind layer)
    {
        _CanvasView.SetActiveLayer(layer);
        UpdateStatusLabel();
    }

    private void SetActiveTile(string tileId)
    {
        _CanvasView.SetActiveTile(tileId);
        UpdateStatusLabel();
    }

    private void SetActiveEntityType(string typeId)
    {
        _CanvasView.SetActiveEntityType(typeId);
        UpdateStatusLabel();
    }

    private void DeleteEntity() => _CanvasView.DeleteSelectedEntity();

    private void ToggleSnapToGrid()
    {
        _CanvasView.SetSnapToGridEnabled(!_CanvasView.SnapToGridEnabled);
        UpdateStatusLabel();
    }

    private void UpdateStatusLabel()
    {
        if (_StatusLabel == null)
            return;

        string tile = string.IsNullOrEmpty(_CanvasView.ActiveTileId) ? "(eraser)" : _CanvasView.ActiveTileId;
        string entityType = string.IsNullOrEmpty(_CanvasView.ActiveEntityTypeId) ? "(none)" : _CanvasView.ActiveEntityTypeId;
        string snap = _CanvasView.SnapToGridEnabled ? "ON (hold Ctrl to disable)" : "OFF (hold Ctrl to enable)";
        _StatusLabel.text = $"Mode: {_CanvasView.Mode}\nLayer: {_CanvasView.ActiveLayer}\nTile: {tile}\nEntity: {entityType}\nSnap: {snap}";
    }
}
