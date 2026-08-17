/// <summary>
/// Small static bridge carrying state across the scene load between the
/// Level Editor and the PlayTest scene - a MonoBehaviour field can't survive
/// that (both scenes create their own instances), so this is what lets
/// LevelInstantiator know it should build from the level the editor just
/// saved rather than its own Inspector-assigned level, and what lets the
/// editor scene know to restore the level being edited (instead of starting
/// a blank New Level) when the player backs out of the playtest.
/// </summary>
public static class PlayTestSession
{
    /// <summary>Scratch LevelIO slot the editor saves to and LevelInstantiator
    /// loads from during a playtest - never shown in the Load list.</summary>
    public const string LevelSlotName = "__playtest__";

    /// <summary>
    /// True from the moment the editor's Play Test button fires until the
    /// editor scene's Awake() consumes it back to false. Gates both
    /// LevelInstantiator's level source (this slot vs. its own _LevelName)
    /// and the in-game "back to editor" hotkey - false if the PlayTest scene
    /// was opened directly (e.g. hitting Play on it in the Editor), so
    /// nothing here fires unexpectedly outside the editor-driven flow.
    /// </summary>
    public static bool IsPlaytesting;

    /// <summary>The editor's Level Name field text at the moment Play Test
    /// was pressed, restored into that field on return.</summary>
    public static string ReturnDisplayName;
}
