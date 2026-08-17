using System.Collections.Generic;

/// <summary>
/// Future seam for MiniScript-authored (EntityBackingKind.ScriptBehavior)
/// entities to declare which variables the runtime editor's inspector
/// should expose - not implemented, since MiniScript itself doesn't exist
/// yet. This is the ScriptBehavior counterpart to
/// INativePrefabAdapter.Schema, but note the harder problem it'll need to
/// solve that prefab adapters don't have: the level editor needs a script's
/// exposed-property schema at authoring time, before any MiniScript VM is
/// running to ask - so whatever implements this will likely need to read it
/// out of the script source itself (e.g. a lightweight parse of an
/// "expose" declaration convention), not query a live script instance.
///
/// A MiniScript entity may also want to expose native Unity component
/// properties alongside its own script variables (e.g. its SpriteRenderer's
/// Color) - INativeComponentBinder/NativeComponentBinderRegistry already
/// exist for that and don't need to change; a script-backed EntityDefinition
/// would just reference the relevant binder ids too.
/// </summary>
public interface IScriptPropertySource
{
    List<PropertyDef> GetExposedProperties(EntityDefinition definition);
}
