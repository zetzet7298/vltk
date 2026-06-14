# USS vs runtime C# — session evidence

**Date:** 2026-06-13
**Scope:** Moving a UI Toolkit HUD panel (`ChatBar` / `hud-chat-panel`) from
bottom-left (PC layout) to bottom-center of the screen, in the VLTK-mobile
Unity project.

## Timeline — three commits, one bug

| Commit | File | Change | Did it work? |
|---|---|---|---|
| `4c71a6aeb` | `Assets/UI/HUD/GameHud.uss` | `.hud-chat-panel { left: 50%; translate: -50% 0; bottom: 8px; }` | ❌ No — runtime still bottom-left |
| `e52b0d67f` | `Assets/Scripts/UI/GameHudController.cs` | `ApplySafeAreaLayout()` now sets `_chatPanel.style.left = Length.Percent(50f)` + `translate: Percent(-50f)` | ⚠️ Panel moved, but tabs row stayed on left |
| `d9f190bb0` | `Assets/UI/HUD/GameHud.uss` | `.hud-chat-tabs { justify-content: center; margin-left: 0; }` | ✅ Final — panel + tabs both centered |

## Discovery: which C# method was overriding

`grep -n 'style\.\(left\|right\|top\|bottom\|translate\|position\)' Assets/Scripts/UI/GameHudController.cs`
showed a `if (_chatPanel != null) { _chatPanel.style.left = safeX; _chatPanel.style.bottom = safeY + 42f; }`
block inside `ApplySafeAreaLayout()` (called from `Update()` or a similar
per-frame hook). That single `style.left = safeX` line silently overrode
the USS `left: 50%` value every frame, making the USS change invisible at
runtime.

## Why UI Builder didn't catch it

The UI Builder is a design-time tool that shows the **USS-only** state. It
does not run the C# `MonoBehaviour.Update()` path, so layout override code
is invisible there. The first "verification" (looking at UI Builder)
showed the panel centered — but that was a fiction. Only a real PlayMode
or built player shows the runtime result.

The user saw the bug immediately on the next playtest: "tao play vẫn thấy
cũm chat nằm bên trái thay vì giữa".

## The 6-tab-row child bug

After the panel itself moved, the **child row** of channel tabs
(`Tất cả / Mặt / Phòng / Bang hội / Môn phái / Khác`) was still indented
20px from the left. The reason was an old PC-layout rule in USS:
`hud-chat-tabs { margin-left: 20px; /* indent past the slim scroll rail */ }`.
The "slim scroll rail" is a 7-o'clock PC chat-rail element that no longer
made sense once the whole panel moved to center. The third commit removed
the indent and added `justify-content: center` to the tabs row.

## Lesson (encoded in SKILL.md "USS + runtime C# both must change" pitfall)

Before claiming a UI layout change is done, do all 5 verification steps
listed in the pitfall. The single most important is **step 1**: grep
`style.X =` for the element's id in C# controllers. If the property is
written per-frame, your USS change is dead on arrival.

## Worker preemptive comment (for future UI layout tasks)

```
⚠️ If you change UI positioning, ALSO grep Assets/Scripts/ for
   `style.left|top|right|bottom|translate` and update the runtime C# in
   the matching `*Layout()` / `ApplySafeArea*()` method. USS alone is
   silently overridden each frame.
```

## Reusable pattern: how to find the C# override site quickly

```bash
# 1. Identify the USS class and the runtime C# id
grep -rn "name=\"ChatBar\"" Assets/UI/HUD/GameHud.uxml
# → name="ChatBar" → search for "_chatPanel" in C#

# 2. Find every C# site that writes to that element
grep -rn "_chatPanel\.style\." Assets/Scripts/

# 3. For each match, decide if it's a per-frame override (Update/OnEnable)
#    or a one-time set (constructor, button click). Only the per-frame one
#    will silently override USS.
```

For the VLTK-mobile HUD specifically, the offender is
`Assets/Scripts/UI/GameHudController.cs` `ApplySafeAreaLayout()` —
any other HUD element in `GameHud.uss` is at risk of the same trap.
