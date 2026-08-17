using System.Collections.Generic;

/// <summary>
/// Seam for MiniScript-authored (EntityBackingKind.ScriptBehavior) entities
/// to declare which variables the runtime editor's inspector should expose -
/// the ScriptBehavior counterpart to INativePrefabAdapter.Schema. Solved the
/// problem this originally called out (the level editor needs a script's
/// exposed-property schema at authoring time, before any MiniScript VM is
/// running to ask) not by parsing a separate declaration convention, but by
/// actually running the script once in a throwaway Interpreter and letting
/// it call the real "expose" intrinsic - see ScriptPropertySchemaCollector,
/// the one implementation of this interface, and TailboundIntrinsics.
///
/// A ScriptBehavior entity can also expose native Unity component properties
/// alongside its own script variables (e.g. its SpriteRenderer's Color) via
/// the existing INativeComponentBinder/NativeComponentBinderRegistry -
/// ScriptEntityRunner.ApplySpriteRendererProperty is the first example.
/// </summary>
public interface IScriptPropertySource
{
    List<PropertyDef> GetExposedProperties(EntityDefinition definition);
}
