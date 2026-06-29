# Delta for HUD

## MODIFIED Requirements

### Requirement: Bottom-center reserved for chat

The bottom-center lane SHALL remain clear of combat slots, quick slots, action buttons, menu
buttons, and the old PC `快捷栏` full strip. The lane hosts the PC-parity chat bar (the 聊天条
surface defined in the **chat** domain), which is now implemented rather than reserved for the
future. The chat bar is the only content permitted in the bottom-center lane; it SHALL NOT be
displaced or overlapped by combat, quick-slot, action-button, menu, or toolbar clusters.
(Previously: the bottom-center lane was described as reserved for a "future mobile chat
canvas"; the chat bar is now implemented by change `port-pc-chat-bar-parity`.)

#### Scenario: No PC bottom strip

- GIVEN the HUD loads
- THEN the old full-width `快捷栏` bottom toolbar is absent
- AND bottom-center contains only the PC-parity chat bar

#### Scenario: Bottom-center hosts the chat bar without overlap

- GIVEN the HUD loads with the chat bar present in the bottom-center lane
- THEN the combat cluster, quick slots, action buttons, top-gap menu, top bar, and minimap do
  not overlap the chat bar
- AND the bottom-left joystick input lane remains touchable
