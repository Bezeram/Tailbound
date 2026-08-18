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

speed = expose("Speed", 2.0)
minBrightness = expose("MinBrightness", 0.2)
maxBrightness = expose("MaxBrightness", 1.0)
spritePath = expose("SpritePath", "")

if spritePath != "" then
    setSprite spritePath
end if

SetPosition = function(position)
    setPosition position[0], position[1]
end function

x = 10
y = 5
r = 5
position = [x, y]
radius = r
t = 0
while true
    brightness = minBrightness + (maxBrightness - minBrightness) * (0.5 + 0.5 * sin(t * speed))
    setColor brightness, 1, brightness, 1
    
    radius = sin(t) * r
    position[1] = y + sin(t * speed) * radius
    position[0] = x + cos(t * speed) * radius
    SetPosition position

    wait 0.05
    t += 0.05
end while
