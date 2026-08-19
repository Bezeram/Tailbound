// Blinker - reference example for a ScriptBehavior EntityDefinition.
//
// Named .txt, not .ms - Unity only auto-imports a specific extension
// whitelist (.txt/.json/.xml/.bytes/.html/.csv/.yaml/.fnt/...) as a
// TextAsset; anything else (including .ms) imports as a generic
// DefaultAsset that a TextAsset-typed field (EntityDefinition.Script)
// can't accept. The content below is still plain MiniScript source - only
// the extension differs from MiniScriptHelloWorld.ms.
//
// Every expose(key, defaultValue) call below shows up as an editable
// property on this entity type's instances in the runtime level editor's
// inspector (Entities Mode, select an instance of this type). Per-instance
// overrides set there win over the defaults given here.
//
// At runtime, pulses a SpriteRenderer's brightness up and down forever.

speed = expose("Speed", 3.0)

start = function()
    setSprite "Textures/Spring/spring-1"
    setCollider 1, 1, 1   // trigger, 1x1
    globals.origin = getPosition
end function

t = 0

update = function()
    globals.t += deltaTime
    setPosition globals.origin[0], globals.origin[1] + sin(globals.t * 5)
end function

onTriggerEnter = function(other)
    if other.isPlayer then
        setColor 1, 0, 0, 1
    end if
end function

onTriggerExit = function(other)
    if other.isPlayer then
        setColor 1, 1, 1, 1
    end if
end function
