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
// At runtime: wobbles up and down forever, and launches the player away
// from itself (a knockback pad, not a solid bounce pad like Spring) the
// moment the player touches it.

speed = expose("Speed", 3.0)
launchSpeed = expose("LaunchSpeed", 15.0)

start = function()
    setSprite "Textures/Spring/spring-1"
    setCollider 1, 1, 1   // pure trigger, 1x1 - see onTriggerEnter
    globals.origin = getPosition
end function

t = 0

update = function()
    globals.t += deltaTime
    setPosition globals.origin[0], globals.origin[1] + sin(globals.t * 5)
end function

onTriggerEnter = function(other)
    if not other.isPlayer then return
    setColor 1, 0, 0, 1

    // Direction from this entity to the player, normalized - getPosition
    // is local (relative to this entity's screen), not directly comparable
    // to getPlayerPosition's world space, so getWorldPosition is the one to
    // use here.
    entityPos = getPosition
    playerPos = getPlayerPosition
    dx = playerPos[0] - entityPos[0]
    dy = playerPos[1] - entityPos[1]
    length = sqrt(dx * dx + dy * dy)
    if length > 0 then
        dx = dx / length
        dy = dy / length
    end if

    setPlayerSpeed dx * launchSpeed, dy * launchSpeed
end function

onTriggerExit = function(other)
    if other.isPlayer then
        setColor 1, 1, 1, 1
    end if
end function
