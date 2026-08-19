using System.Collections.Generic;
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
    // The name this level is actually saved under on disk, if any - null for
    // a never-saved level. Distinct from _NameField.text, which is just
    // whatever's currently typed (may not match any saved file yet). Lets
    // RenameLevel know which old file to delete.
    private string _LoadedLevelName;
    private ScreenCanvasView _CanvasView;
    private TMP_InputField _NameField;
    private RectTransform _LoadListContent;
    private GameObject _LoadPanel;
    private GameObject _PalettePanel;
    private Image _BackgroundLayerButtonImage;
    private Image _ForegroundLayerButtonImage;
    private GameObject _EntityPalettePanel;
    private TMP_Text _StatusLabel;
    private GameObject _EntityInspectorPanel;
    private RectTransform _EntityInspectorContent;
    private readonly ScriptPropertySchemaCollector _ScriptPropertySource = new();
    // Sentinel (not a real selection state) so the first Update() always
    // builds the panel's initial content, even though SelectedEntityId also
    // starts at -1.
    private int _InspectedEntityId = -2;

    // New Script Entity dialog - creates real runtime content (see
    // RuntimeEntityIO), no Editor/AssetDatabase dependency, so this works
    // the same in a standalone build as it does here.
    private GameObject _NewScriptEntityPanel;
    private TMP_InputField _NewEntityDisplayNameField;
    private TMP_InputField _NewEntityCategoryField;
    private TMP_InputField _NewEntityIconPathField;
    private Toggle _NewEntityIsSpawnPointToggle;

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
            // LoadLevel just set these from the playtest scratch slot -
            // restore what they actually were before Play Test was pressed.
            _NameField.text = PlayTestSession.ReturnDisplayName;
            _LoadedLevelName = PlayTestSession.ReturnLoadedLevelName;
        }
        else
        {
            NewLevel();
        }
    }

    /// <summary>
    /// Polls for a selection change rather than reacting to an event -
    /// EntityMarkerView mutates ScreenCanvasView.SelectedEntityId directly
    /// from pointer handlers (click, drag) with no notification hook, so
    /// this is the cheapest way to notice without adding one just for this.
    /// Also handles keyboard shortcuts every frame - see HandleKeybinds.
    /// </summary>
    private void Update()
    {
        if (_CanvasView.SelectedEntityId != _InspectedEntityId)
            RefreshEntityInspector();

        HandleKeybinds();
    }

    /// <summary>
    /// One keybind per toolbar action (shown in each button's own label) -
    /// skipped entirely while a text field has focus, so typing a level/tile
    /// name or a property value never gets hijacked by e.g. "g".
    /// </summary>
    private void HandleKeybinds()
    {
        if (IsTypingInField())
            return;

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrl && Input.GetKeyDown(KeyCode.N)) NewLevel();
        else if (ctrl && Input.GetKeyDown(KeyCode.S)) SaveLevel();
        else if (ctrl && Input.GetKeyDown(KeyCode.R)) RenameLevel();
        else if (ctrl && Input.GetKeyDown(KeyCode.L)) ToggleLoadPanel();
        else if (Input.GetKeyDown(KeyCode.F5)) PlayTestLevel();
        else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) AddScreen();
        else if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(ScreenCanvasView.InteractionMode.Screens);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(ScreenCanvasView.InteractionMode.Paint);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(ScreenCanvasView.InteractionMode.Entities);
        else if (Input.GetKeyDown(KeyCode.G)) ToggleSnapToGrid();
        else if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) DeleteSelected();
    }

    /// <summary>True while a TMP_InputField (Level Name, a property row's
    /// field, ...) has input focus - lets HandleKeybinds step aside rather
    /// than hijack ordinary typing.</summary>
    private static bool IsTypingInField()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.GetComponent<TMP_InputField>() != null;
    }

    /// <summary>Context-sensitive Delete/Backspace - the selected entity in
    /// Entities mode, otherwise the selected screen. Replaces the old
    /// dedicated "Delete Entity" toolbar button (removed - this keybind is
    /// its only way to delete an entity now); Delete Screen keeps its own
    /// button too, since screen deletion is rarer and more consequential.</summary>
    private void DeleteSelected()
    {
        if (_CanvasView.Mode == ScreenCanvasView.InteractionMode.Entities)
            DeleteEntity();
        else
            DeleteScreen();
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

        // Every button's own keybind is shown right in its label rather than
        // as a separate legend - HandleKeybinds is the single source of
        // truth for what each key actually does, this is just documentation.
        // No "Delete Entity" button anymore - Delete/Backspace (context-
        // sensitive, see DeleteSelected) is its only way to fire now.
        CreateButton(barRect, "New (^N)", NewLevel, 80);
        CreateButton(barRect, "Save (^S)", SaveLevel, 80);
        CreateButton(barRect, "Rename (^R)", RenameLevel, 100);
        CreateButton(barRect, "Load (^L)", ToggleLoadPanel, 80);
        CreateButton(barRect, "+ Screen (+)", AddScreen, 100);
        CreateButton(barRect, "Delete Screen (Del)", DeleteScreen, 150);
        CreateButton(barRect, "Screens (1)", () => SetMode(ScreenCanvasView.InteractionMode.Screens), 100);
        CreateButton(barRect, "Paint (2)", () => SetMode(ScreenCanvasView.InteractionMode.Paint), 90);
        CreateButton(barRect, "Entities (3)", () => SetMode(ScreenCanvasView.InteractionMode.Entities), 100);
        CreateButton(barRect, "Snap (G)", ToggleSnapToGrid, 90);
        CreateButton(barRect, "Play Test (F5)", PlayTestLevel, 120);

        _LoadPanel = CreateLoadPanel(parent);
        _PalettePanel = CreatePalettePanel(parent);
        _EntityPalettePanel = CreateEntityPalettePanel(parent);
        _EntityInspectorPanel = CreateEntityInspectorPanel(parent);
        CreateStatusPanel(parent);
        _NewScriptEntityPanel = CreateNewScriptEntityPanel(parent);
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
        rect.sizeDelta = new Vector2(500, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var statusGO = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(go.transform, false);
        statusGO.AddComponent<LayoutElement>().preferredHeight = 140f;
        _StatusLabel = statusGO.GetComponent<TextMeshProUGUI>();
        _StatusLabel.fontSize = 20;
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

    /// <summary>Returns the button's own background Image, so a caller that
    /// wants to highlight it later (e.g. whichever tile layer is active)
    /// can hang onto a reference instead of rebuilding the button.</summary>
    private static Image CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float width, float height = 28f, int fontSize = 14)
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
        layoutElement.preferredHeight = height;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)textGO.transform, 4);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return image;
    }

    private static void CreateLabel(Transform parent, string label, int fontSize = 14)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.color = new Color(1f, 1f, 1f, 0.6f);
        text.enableWordWrapping = true;
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
        rect.sizeDelta = new Vector2(440, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6;
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
            CreateLabel(_LoadListContent, "No saved levels.", 18);
            return;
        }

        foreach (var name in levels)
        {
            string captured = name;
            CreateButton(_LoadListContent, captured, () => LoadLevel(captured), 400, 44f, 18);
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
        rect.sizeDelta = new Vector2(480, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layerRowGO = new GameObject("LayerRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        layerRowGO.transform.SetParent(go.transform, false);
        layerRowGO.AddComponent<LayoutElement>().preferredHeight = 48f;
        var layerRowLayout = layerRowGO.GetComponent<HorizontalLayoutGroup>();
        layerRowLayout.spacing = 6;
        layerRowLayout.childForceExpandWidth = true;
        layerRowLayout.childForceExpandHeight = true;
        _BackgroundLayerButtonImage = CreateButton(layerRowGO.transform, "Background", () => SetActiveLayer(ScreenCanvasView.TileLayerKind.Background), 220, 48f, 18);
        _ForegroundLayerButtonImage = CreateButton(layerRowGO.transform, "Foreground", () => SetActiveLayer(ScreenCanvasView.TileLayerKind.Foreground), 220, 48f, 18);
        UpdateActiveLayerButtonColors();

        CreateButton(go.transform, "Eraser", () => SetActiveTile(""), 440, 48f, 18);

        foreach (var tileDef in TileCatalog.All)
        {
            string tileId = tileDef.Id;
            CreateTileButton(go.transform, tileDef, () => SetActiveTile(tileId));
        }

        if (TileCatalog.All.Count == 0)
            CreateLabel(go.transform, "No tiles found. Add a Tileset asset under Assets/Resources.", 18);

        go.SetActive(false);
        return go;
    }

    private static void CreateTileButton(Transform parent, TileDef tileDef, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(tileDef.Id + " Tile Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 68f;
        go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = (RectTransform)iconGO.transform;
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(6, 0);
        iconRect.sizeDelta = new Vector2(56, -10);
        var icon = iconGO.GetComponent<Image>();
        icon.sprite = tileDef.Sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var textRect = (RectTransform)textGO.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(70, 2);
        textRect.offsetMax = new Vector2(-4, -2);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = string.IsNullOrEmpty(tileDef.DisplayName) ? tileDef.Id : tileDef.DisplayName;
        text.fontSize = 18;
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
        rect.sizeDelta = new Vector2(480, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateButton(go.transform, "+ New Script Entity...", OpenNewScriptEntityDialog, 440, 48f, 18);

        foreach (var def in EntityCatalog.All)
        {
            string typeId = def.TypeId;
            CreateEntityButton(go.transform, def, () => SetActiveEntityType(typeId));
        }

        if (EntityCatalog.All.Count == 0)
            CreateLabel(go.transform, "No entity types found. Add an Entity Definition asset under Assets/Resources.", 18);

        go.SetActive(false);
        return go;
    }

    private static void CreateEntityButton(Transform parent, EntityDefinition def, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(def.TypeId + " Entity Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 68f;
        go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = (RectTransform)iconGO.transform;
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(6, 0);
        iconRect.sizeDelta = new Vector2(56, -10);
        var icon = iconGO.GetComponent<Image>();
        icon.sprite = def.Icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var textRect = (RectTransform)textGO.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(70, 2);
        textRect.offsetMax = new Vector2(-4, -2);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = string.IsNullOrEmpty(def.DisplayName) ? def.TypeId : def.DisplayName;
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
    }

    // ------------------------------------------------------------------
    // New Script Entity dialog - creates a real RuntimeEntityIO entity
    // (a starter .ms file + a JSON record, both plain files under
    // Application.persistentDataPath), so unlike the AssetDatabase-based
    // version this used to be, it works the same in a standalone build as
    // it does here. No Type Id field or script picker - see OpenDialog/
    // CreateNewScriptEntity for why.
    // ------------------------------------------------------------------

    private void OpenNewScriptEntityDialog()
    {
        _NewEntityDisplayNameField.text = "";
        _NewEntityCategoryField.text = "script";
        _NewEntityIconPathField.text = "";
        _NewEntityIsSpawnPointToggle.isOn = false;
        _NewScriptEntityPanel.SetActive(true);
    }

    private GameObject CreateNewScriptEntityPanel(Transform parent)
    {
        var go = new GameObject(
            "NewScriptEntityPanel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);

        // Centered - this is a modal-ish dialog, not a corner panel like
        // everything else, since it needs room for several fields at once.
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(640, 0);
        go.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.14f, 0.99f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateInspectorHeader(go.transform, "New Script Entity");

        _NewEntityDisplayNameField = CreateLabeledField(go.transform, "Display Name");
        _NewEntityCategoryField = CreateLabeledField(go.transform, "Category");
        _NewEntityIconPathField = CreateLabeledField(go.transform, "Icon (Resources path to a Sprite, optional)");

        var spawnRowGO = new GameObject("SpawnRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        spawnRowGO.transform.SetParent(go.transform, false);
        spawnRowGO.AddComponent<LayoutElement>().preferredHeight = 36f;
        var spawnRowLayout = spawnRowGO.GetComponent<HorizontalLayoutGroup>();
        spawnRowLayout.spacing = 8;
        spawnRowLayout.childAlignment = TextAnchor.MiddleLeft;
        _NewEntityIsSpawnPointToggle = CreateCompactToggle(spawnRowGO.transform, false, 28f);
        CreateLabel(spawnRowGO.transform, "Is Spawn Point", 18);

        CreateLabel(go.transform, "Creates a starter MiniScript behaviour script.", 14);

        var buttonRowGO = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttonRowGO.transform.SetParent(go.transform, false);
        buttonRowGO.AddComponent<LayoutElement>().preferredHeight = 48f;
        var buttonRowLayout = buttonRowGO.GetComponent<HorizontalLayoutGroup>();
        buttonRowLayout.spacing = 8;
        CreateButton(buttonRowGO.transform, "Create", CreateNewScriptEntity, 200, 48f, 18);
        CreateButton(buttonRowGO.transform, "Cancel", () => _NewScriptEntityPanel.SetActive(false), 200, 48f, 18);

        go.SetActive(false);
        return go;
    }

    private static TMP_InputField CreateLabeledField(Transform parent, string label)
    {
        CreateLabel(parent, label, 16);
        return CreateCompactInputField(parent, 600, "", 18);
    }

    /// <summary>
    /// Creates a new runtime script entity (RuntimeEntityIO - a starter .ms
    /// file plus a JSON record under persistentDataPath, no Unity asset
    /// involved) from Display Name/Category/Icon/Is Spawn Point, adds it to
    /// EntityCatalog immediately so it shows up in the palette without a
    /// reload, and opens the new script in whatever app the OS has
    /// associated with .ms files.
    /// </summary>
    private void CreateNewScriptEntity()
    {
        string displayName = _NewEntityDisplayNameField.text.Trim();
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogError("[LevelEditor] New Script Entity blocked: Display Name is required.");
            return;
        }

        string typeId = MakeUniqueTypeId(SlugifyTypeId(displayName));
        string category = _NewEntityCategoryField.text.Trim();
        string iconPath = _NewEntityIconPathField.text.Trim();
        bool isSpawnPoint = _NewEntityIsSpawnPointToggle.isOn;

        var def = RuntimeEntityIO.Create(typeId, displayName, category, isSpawnPoint, iconPath, out string scriptPath);
        EntityCatalog.AddRuntimeDefinition(def);
        RebuildEntityPalettePanel();
        _NewScriptEntityPanel.SetActive(false);

        OpenWithDefaultApp(scriptPath);
    }

    /// <summary>Lowercases, strips anything that isn't a letter/digit, and
    /// collapses runs of stripped characters into single underscores - "Fire
    /// Trap!" becomes "fire_trap". Falls back to "entity" if that leaves
    /// nothing usable.</summary>
    private static string SlugifyTypeId(string displayName)
    {
        var builder = new System.Text.StringBuilder();
        foreach (char c in displayName.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                builder.Append(c);
            else if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                builder.Append('_');
        }

        string slug = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(slug) ? "entity" : slug;
    }

    /// <summary>Appends "_2", "_3", ... until the id no longer collides with
    /// an existing entity type.</summary>
    private static string MakeUniqueTypeId(string baseId)
    {
        if (!EntityCatalog.Lookup.ContainsKey(baseId))
            return baseId;

        int suffix = 2;
        string candidate;
        do
        {
            candidate = $"{baseId}_{suffix}";
            suffix++;
        } while (EntityCatalog.Lookup.ContainsKey(candidate));

        return candidate;
    }

    /// <summary>
    /// Launches the OS's default handler for the given file (e.g. the
    /// user's usual text/code editor for a .ms file) - UseShellExecute is
    /// what makes Process.Start defer to file-type associations instead of
    /// trying to execute the file directly. Not guaranteed: a machine with
    /// no .ms association at all may show Windows' own "how do you want to
    /// open this" picker instead, or in a locked-down environment fail
    /// outright - both just logged, since the entity was already created
    /// successfully either way; this is a convenience on top of that, not
    /// a requirement for it.
    /// </summary>
    private static void OpenWithDefaultApp(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[LevelEditor] Could not automatically open '{path}': {exception}");
        }
    }

    /// <summary>Rebuilds the entity palette panel from scratch so a newly
    /// created entity type (or any other EntityCatalog change) shows up
    /// without needing to reopen the level editor.</summary>
    private void RebuildEntityPalettePanel()
    {
        bool wasActive = _EntityPalettePanel.activeSelf;
        Transform parent = _EntityPalettePanel.transform.parent;
        Destroy(_EntityPalettePanel);
        _EntityPalettePanel = CreateEntityPalettePanel(parent);
        _EntityPalettePanel.SetActive(wasActive);
    }

    // ------------------------------------------------------------------
    // Entity inspector (component property overrides)
    // ------------------------------------------------------------------

    // Bottom-right - the one corner nothing else occupies (Load is top-left,
    // the tile/entity palettes are top-right, Status is bottom-left).
    private GameObject CreateEntityInspectorPanel(Transform parent)
    {
        var go = new GameObject(
            "EntityInspectorPanel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-8, 8);
        rect.sizeDelta = new Vector2(600, 0);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.97f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _EntityInspectorContent = rect;
        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// Rebuilds the inspector's contents for whatever ScreenCanvasView.SelectedEntityId
    /// currently is. The property schema is never authored on the
    /// EntityDefinition itself (see its own comment) - for a NativePrefab
    /// entity it comes from whichever INativePrefabAdapter targets a
    /// component on its Prefab (NativePrefabAdapterRegistry); for a
    /// ScriptBehavior entity it comes from running its Script once to
    /// collect its expose(...) calls (RenderScriptProperties). Called on
    /// every selection change and after every edit/reset, since edits can
    /// change which fields are highlighted as overridden.
    /// </summary>
    private void RefreshEntityInspector()
    {
        _InspectedEntityId = _CanvasView.SelectedEntityId;

        for (int i = _EntityInspectorContent.childCount - 1; i >= 0; i--)
            Destroy(_EntityInspectorContent.GetChild(i).gameObject);

        if (_InspectedEntityId < 0)
        {
            CreateLabel(_EntityInspectorContent, "No entity selected.", 18);
            return;
        }

        var instance = _Level.Entities.Find(e => e.Id == _InspectedEntityId);
        if (instance == null)
        {
            CreateLabel(_EntityInspectorContent, "Selected entity no longer exists.", 18);
            return;
        }

        if (!EntityCatalog.Lookup.TryGetValue(instance.TypeId, out var def))
        {
            CreateLabel(_EntityInspectorContent, $"Unknown entity type '{instance.TypeId}'.", 18);
            return;
        }

        CreateInspectorHeader(_EntityInspectorContent, string.IsNullOrEmpty(def.DisplayName) ? def.TypeId : def.DisplayName);

        if (instance.ComponentOverrides.Count > 0)
        {
            CreateButton(_EntityInspectorContent, "Reset All Overrides", () =>
            {
                instance.ComponentOverrides.Clear();
                RefreshEntityInspector();
            }, 560, 44f, 18);
        }

        if (def.Backing == EntityBackingKind.ScriptBehavior)
        {
            RenderScriptProperties(instance, def);
            return;
        }

        if (def.Prefab == null)
        {
            CreateLabel(_EntityInspectorContent, "This entity type has no Prefab assigned.", 18);
            return;
        }

        if (!NativePrefabAdapterRegistry.TryGetForPrefab(def.Prefab, out var adapter) || adapter.Schema.Count == 0)
        {
            CreateLabel(_EntityInspectorContent, "No adapter registered for this prefab - no editable properties.", 18);
            return;
        }

        CreateInspectorHeader(_EntityInspectorContent, adapter.AdapterId, subHeader: true);
        foreach (var propDef in adapter.Schema)
        {
            PropertyValue value = EntityPropertyResolver.GetEffectiveValue(instance, def.Prefab, adapter, propDef);
            CreatePropertyRow(_EntityInspectorContent, instance, adapter.AdapterId, propDef, value);
        }
    }

    /// <summary>
    /// Runs the script once (ScriptPropertySchemaCollector - a throwaway
    /// Interpreter, not the real gameplay one) purely to collect its
    /// expose(key, defaultValue) calls, then renders the same kind of rows
    /// as a NativePrefab's adapter Schema, keyed under the shared
    /// ScriptPropertyResolver.AdapterId instead of a per-prefab adapter id.
    /// </summary>
    private void RenderScriptProperties(EntityInstance instance, EntityDefinition def)
    {
        if (def.Script == null)
        {
            CreateLabel(_EntityInspectorContent, "This entity type has no Script assigned.", 18);
            return;
        }

        var schema = _ScriptPropertySource.GetExposedProperties(def);
        if (schema.Count == 0)
        {
            CreateLabel(_EntityInspectorContent, "This script exposes no properties (no expose(...) calls found).", 18);
            return;
        }

        CreateInspectorHeader(_EntityInspectorContent, "Script", subHeader: true);
        foreach (var propDef in schema)
        {
            PropertyValue value = ScriptPropertyResolver.GetEffectiveValue(instance, propDef);
            CreatePropertyRow(_EntityInspectorContent, instance, ScriptPropertyResolver.AdapterId, propDef, value);
        }
    }

    private static void CreateInspectorHeader(Transform parent, string text, bool subHeader = false)
    {
        var go = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = subHeader ? 20 : 26;
        label.fontStyle = FontStyles.Bold;
        label.color = subHeader ? new Color(1f, 1f, 1f, 0.6f) : Color.white;
    }

    /// <summary>
    /// One editable row for a single PropertyDef: a label (amber when an
    /// override exists), a type-appropriate control pre-filled with the
    /// given effective value (already resolved by the caller - a
    /// NativePrefab's adapter.Read or a ScriptBehavior's PropertyDef.
    /// DefaultValue, override applied either way), and a reset button that
    /// clears just this property's override. Edits commit on end-edit/
    /// value-changed via SetOverride, then rebuild the whole panel so the
    /// highlight and (for numeric fields) any range clamping are reflected
    /// immediately. adapterId is just the ComponentOverrides key to write
    /// under - see EntityPropertyResolver/ScriptPropertyResolver.
    /// </summary>
    private void CreatePropertyRow(Transform parent, EntityInstance instance, string adapterId, PropertyDef propDef, PropertyValue value)
    {
        bool isOverridden = instance.ComponentOverrides.TryGetValue(adapterId, out var existingOverrides)
            && existingOverrides.ContainsKey(propDef.Key);

        string labelText = string.IsNullOrEmpty(propDef.Label) ? propDef.Key : propDef.Label;
        if (propDef.HasRange)
            labelText += $" ({propDef.MinValue:0.##}-{propDef.MaxValue:0.##})";

        var rowGO = new GameObject(propDef.Key + " Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGO.transform.SetParent(parent, false);
        rowGO.AddComponent<LayoutElement>().preferredHeight = 44f;
        var rowLayout = rowGO.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(rowGO.transform, false);
        labelGO.AddComponent<LayoutElement>().preferredWidth = 176f;
        var labelComp = labelGO.GetComponent<TextMeshProUGUI>();
        labelComp.text = labelText;
        labelComp.fontSize = 16;
        labelComp.enableWordWrapping = true;
        labelComp.color = isOverridden ? new Color(1f, 0.8f, 0.35f) : new Color(1f, 1f, 1f, 0.75f);

        void Commit(PropertyValue newValue)
        {
            SetOverride(instance, adapterId, propDef, newValue);
            RefreshEntityInspector();
        }

        switch (propDef.Type)
        {
            case PropertyType.Float:
            {
                var field = CreateCompactInputField(rowGO.transform, 130, value.FloatValue.ToString("0.###"), 16);
                field.contentType = TMP_InputField.ContentType.DecimalNumber;
                field.onEndEdit.AddListener(text =>
                {
                    float f = float.TryParse(text, out float parsed) ? parsed : value.FloatValue;
                    if (propDef.HasRange)
                        f = Mathf.Clamp(f, propDef.MinValue, propDef.MaxValue);
                    Commit(PropertyValue.FromFloat(f));
                });
                break;
            }
            case PropertyType.Int:
            {
                var field = CreateCompactInputField(rowGO.transform, 130, value.IntValue.ToString(), 16);
                field.contentType = TMP_InputField.ContentType.IntegerNumber;
                field.onEndEdit.AddListener(text =>
                {
                    int i = int.TryParse(text, out int parsed) ? parsed : value.IntValue;
                    if (propDef.HasRange)
                        i = Mathf.Clamp(i, (int)propDef.MinValue, (int)propDef.MaxValue);
                    Commit(PropertyValue.FromInt(i));
                });
                break;
            }
            case PropertyType.Bool:
            {
                var toggle = CreateCompactToggle(rowGO.transform, value.BoolValue, 36f);
                toggle.onValueChanged.AddListener(b => Commit(PropertyValue.FromBool(b)));
                break;
            }
            case PropertyType.String:
            {
                var field = CreateCompactInputField(rowGO.transform, 260, value.StringValue ?? "", 16);
                field.contentType = TMP_InputField.ContentType.Standard;
                field.onEndEdit.AddListener(text => Commit(PropertyValue.FromString(text)));
                break;
            }
            case PropertyType.Vector2:
            {
                var xField = CreateCompactInputField(rowGO.transform, 100, value.Vector2Value.x.ToString("0.###"), 16);
                xField.contentType = TMP_InputField.ContentType.DecimalNumber;
                var yField = CreateCompactInputField(rowGO.transform, 100, value.Vector2Value.y.ToString("0.###"), 16);
                yField.contentType = TMP_InputField.ContentType.DecimalNumber;

                void CommitVector(string _)
                {
                    float x = float.TryParse(xField.text, out float xv) ? xv : value.Vector2Value.x;
                    float y = float.TryParse(yField.text, out float yv) ? yv : value.Vector2Value.y;
                    Commit(PropertyValue.FromVector2(new Vector2(x, y)));
                }
                xField.onEndEdit.AddListener(CommitVector);
                yField.onEndEdit.AddListener(CommitVector);
                break;
            }
            case PropertyType.Color:
            {
                var swatchGO = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                swatchGO.transform.SetParent(rowGO.transform, false);
                swatchGO.AddComponent<LayoutElement>().preferredWidth = 36f;
                swatchGO.GetComponent<Image>().color = value.ColorValue;

                var field = CreateCompactInputField(rowGO.transform, 160, "#" + ColorUtility.ToHtmlStringRGBA(value.ColorValue), 16);
                field.contentType = TMP_InputField.ContentType.Standard;
                field.onEndEdit.AddListener(text =>
                {
                    if (ColorUtility.TryParseHtmlString(text, out Color parsed))
                        Commit(PropertyValue.FromColor(parsed));
                    else
                        RefreshEntityInspector(); // invalid hex - just revert the display
                });
                break;
            }
        }

        var resetGO = new GameObject("Reset", typeof(RectTransform), typeof(Image), typeof(Button));
        resetGO.transform.SetParent(rowGO.transform, false);
        resetGO.AddComponent<LayoutElement>().preferredWidth = 36f;
        resetGO.GetComponent<Image>().color = isOverridden ? new Color(0.5f, 0.25f, 0.25f, 1f) : new Color(0.22f, 0.22f, 0.22f, 1f);
        var resetButton = resetGO.GetComponent<Button>();
        resetButton.interactable = isOverridden;
        resetButton.onClick.AddListener(() =>
        {
            ClearOverride(instance, adapterId, propDef);
            RefreshEntityInspector();
        });

        var resetTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        resetTextGO.transform.SetParent(resetGO.transform, false);
        StretchFull((RectTransform)resetTextGO.transform, 0);
        var resetText = resetTextGO.GetComponent<TextMeshProUGUI>();
        resetText.text = "x";
        resetText.fontSize = 16;
        resetText.alignment = TextAlignmentOptions.Center;
        resetText.color = Color.white;
        resetText.raycastTarget = false;
    }

    private static void SetOverride(EntityInstance instance, string adapterId, PropertyDef propDef, PropertyValue value)
    {
        if (!instance.ComponentOverrides.TryGetValue(adapterId, out var overrides))
        {
            overrides = new Dictionary<string, PropertyValue>();
            instance.ComponentOverrides[adapterId] = overrides;
        }

        overrides[propDef.Key] = value;
    }

    private static void ClearOverride(EntityInstance instance, string adapterId, PropertyDef propDef)
    {
        if (!instance.ComponentOverrides.TryGetValue(adapterId, out var overrides))
            return;

        overrides.Remove(propDef.Key);
        if (overrides.Count == 0)
            instance.ComponentOverrides.Remove(adapterId);
    }

    /// <summary>Minimal TMP_InputField for inline inspector rows - same
    /// viewport/mask structure CreateInputField uses (TMP_InputField needs
    /// textViewport to be a distinct masked child), just without a
    /// placeholder since these are always pre-filled with a real value.</summary>
    private static TMP_InputField CreateCompactInputField(Transform parent, float width, string initialText, int fontSize = 12)
    {
        var go = new GameObject("Field", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredWidth = width;
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var viewportGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(go.transform, false);
        var viewportRect = (RectTransform)viewportGO.transform;
        StretchFull(viewportRect, 4);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(viewportGO.transform, false);
        StretchFull((RectTransform)textGO.transform, 0);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.enableWordWrapping = false;

        var field = go.GetComponent<TMP_InputField>();
        field.textViewport = viewportRect;
        field.textComponent = text;
        field.text = initialText;

        return field;
    }

    private static Toggle CreateCompactToggle(Transform parent, bool initial, float size = 20f)
    {
        var go = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredWidth = size;
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGO.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)checkGO.transform, size * 0.15f);
        var checkImage = checkGO.GetComponent<Image>();
        checkImage.color = new Color(0.4f, 0.85f, 0.4f, 1f);

        var toggle = go.GetComponent<Toggle>();
        toggle.graphic = checkImage;
        toggle.targetGraphic = go.GetComponent<Image>();
        toggle.isOn = initial;

        return toggle;
    }

    // ------------------------------------------------------------------
    // Actions
    // ------------------------------------------------------------------

    private void NewLevel()
    {
        _Level = new LevelAsset();
        _LoadedLevelName = null;
        _CanvasView.SetLevel(_Level);
    }

    private void SaveLevel() => SaveInternal();

    /// <summary>
    /// Saves under the Level Name field's current text. Shared by Save and
    /// Rename - the only difference is Rename additionally deletes whatever
    /// this level was previously saved as (see RenameLevel), so plain Save
    /// still works as "Save As" for anyone who wants that (type a new name,
    /// hit Save, and the old file is left alone as a separate level).
    /// Returns the name saved under, or null if saving was blocked/skipped.
    /// </summary>
    private string SaveInternal()
    {
        if (_Level == null)
        {
            Debug.LogWarning("[LevelEditor] No level loaded, nothing to save.");
            return null;
        }

        if (!AllScreensHaveSpawnPoint(out int missingScreenId))
        {
            Debug.LogError(
                $"[LevelEditor] Save blocked: Screen {missingScreenId} has no spawn point entity. " +
                "Place one (an entity type with IsSpawnPoint checked) before saving.");
            return null;
        }

        string name = string.IsNullOrWhiteSpace(_NameField.text) ? "New Level" : _NameField.text.Trim();
        Debug.Log($"[LevelEditor] Saving as '{name}' ({_Level.Screens.Count} screen(s))...");
        LevelIO.Save(_Level, name);
        _LoadedLevelName = name;
        return name;
    }

    /// <summary>
    /// Renames the current level: saves under the Level Name field's
    /// current text, and - if this level was already saved under a
    /// different name - deletes that old file, so the rename doesn't leave
    /// an orphaned duplicate behind the way plain Save alone would.
    /// </summary>
    private void RenameLevel()
    {
        string oldName = _LoadedLevelName;
        string newName = SaveInternal();
        if (newName == null)
            return;

        if (!string.IsNullOrEmpty(oldName) && oldName != newName)
        {
            LevelIO.Delete(oldName);
            Debug.Log($"[LevelEditor] Renamed '{oldName}' to '{newName}'.");
        }
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
        PlayTestSession.ReturnLoadedLevelName = _LoadedLevelName;

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
        _LoadedLevelName = name;
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
        _EntityInspectorPanel.SetActive(mode == ScreenCanvasView.InteractionMode.Entities);
        UpdateStatusLabel();
    }

    private void SetActiveLayer(ScreenCanvasView.TileLayerKind layer)
    {
        _CanvasView.SetActiveLayer(layer);
        UpdateActiveLayerButtonColors();
        UpdateStatusLabel();
    }

    private static readonly Color ActiveLayerColor = new(0.25f, 0.65f, 0.3f, 1f);
    private static readonly Color InactiveLayerColor = new(0.3f, 0.3f, 0.3f, 1f);

    private void UpdateActiveLayerButtonColors()
    {
        bool background = _CanvasView.ActiveLayer == ScreenCanvasView.TileLayerKind.Background;
        _BackgroundLayerButtonImage.color = background ? ActiveLayerColor : InactiveLayerColor;
        _ForegroundLayerButtonImage.color = background ? InactiveLayerColor : ActiveLayerColor;
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
