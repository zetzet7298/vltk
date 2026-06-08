// -----------------------------------------------------------------------------
// VLTK Mobile — executes deterministic PC Region_S object Lua actions.
// Ported object API subset: NewWorld(mapId,x,y), optional SetFightState(),
// safe pickup messages: SetPropState/AddEventItem/AddNote/Msg2Player,
// read-only Say(message), read-only Talk(message...) object scripts,
// PC faction-gated OpenBox()+SetRevPos(id) storage boxes,
// PC camp-gated battlefield OpenBox()/Talk storage boxes, and
// read-only PC task-gated Talk message object scripts.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class ObjectActionExecutionResult
    {
        public bool success;
        public string detail;
        public bool hideObject;
    }

    public interface IObjectActionSideEffects
    {
        void PostMessage(string message);
        void AddEventItem(int eventItemId);
        void AddNote(string note);
        void OpenBox();
        void ShowLadder(int[] ladderIds);
    }

    public sealed class PcObjectActionExecutor
    {
        private readonly PcObjectActionCatalogFile _catalog;
        private readonly ITrapTravelHost _host;
        private readonly IObjectActionSideEffects _sideEffects;

        public PcObjectActionExecutor(PcObjectActionCatalogFile catalog, ITrapTravelHost host, IObjectActionSideEffects sideEffects = null)
        {
            _catalog = catalog;
            _host = host;
            _sideEffects = sideEffects;
        }

        public bool HasAction(MapInteractiveObject obj)
            => obj != null && _catalog?.Find(obj.script) != null;

        public bool TryExecute(MapInteractiveObject obj, out ObjectActionExecutionResult result)
        {
            result = null;
            if (obj == null || _catalog == null) return false;
            var action = _catalog.Find(obj.script);
            if (action == null) return false;

            if (_host == null)
            {
                result = Failure(action, "object travel host unavailable");
                return true;
            }

            if (action.IsPickupMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }
                if (!string.IsNullOrWhiteSpace(action.message))
                    _sideEffects.PostMessage(action.message);
                if (action.eventItemIds != null)
                {
                    foreach (int eventItemId in action.eventItemIds)
                        _sideEffects.AddEventItem(eventItemId);
                }
                if (action.notes != null)
                {
                    foreach (string note in action.notes)
                        if (!string.IsNullOrWhiteSpace(note))
                            _sideEffects.AddNote(note);
                }
                result = Success(action,
                    $"PickupMessage(msg='{action.message}', items={FormatInts(action.eventItemIds)}, notes={action.notes?.Length ?? 0}, SetPropState={action.setPropState})");
                result.hideObject = action.setPropState;
                return true;
            }

            if (action.IsSayMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }
                if (!string.IsNullOrWhiteSpace(action.message))
                    _sideEffects.PostMessage(action.message);
                result = Success(action, $"SayMessage(msg='{action.message}')");
                return true;
            }

            if (action.IsTalkMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }
                int posted = 0;
                if (action.messages != null)
                {
                    foreach (string message in action.messages)
                    {
                        if (string.IsNullOrWhiteSpace(message)) continue;
                        _sideEffects.PostMessage(message);
                        posted++;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(action.message))
                {
                    _sideEffects.PostMessage(action.message);
                    posted = 1;
                }
                result = Success(action, $"TalkMessage(lines={posted})");
                return true;
            }

            if (action.IsTaskTalkMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }
                int taskValue = _host.GetTaskValue(action.taskId);
                bool matched = taskValue == action.taskValue;
                string[] selected = matched ? action.messages : action.elseMessages;
                int posted = 0;
                if (selected != null)
                {
                    foreach (string message in selected)
                    {
                        if (string.IsNullOrWhiteSpace(message)) continue;
                        _sideEffects.PostMessage(message);
                        posted++;
                    }
                }
                result = Success(action, $"TaskTalkMessage(GetTask({action.taskId})={taskValue}, expected={action.taskValue}, matched={matched}, lines={posted})");
                return true;
            }

            if (action.IsOpenBox || action.IsFactionOpenBox || action.IsCampOpenBox)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }
                bool factionMatched = action.requiredFactionId <= 0 || _host.GetPlayerFactionId() == action.requiredFactionId;
                bool campMatched = action.requiredCamp <= 0 || _host.GetCurCamp() == action.requiredCamp;
                if (!action.IsCampOpenBox || campMatched)
                    _sideEffects.OpenBox();

                if (action.reviveId > 0 && factionMatched)
                    _host.SetRevPos(_host.GetCurrentMapId(), action.reviveId);
                if (action.IsCampOpenBox && !campMatched && !string.IsNullOrWhiteSpace(action.message))
                    _sideEffects.PostMessage(action.message);

                string faction = action.requiredFactionId > 0
                    ? $", GetFaction()=={action.requiredFaction}#{action.requiredFactionId} matched={factionMatched}"
                    : string.Empty;
                string camp = action.requiredCamp > 0
                    ? $", GetCurCamp()=={action.requiredCamp} matched={campMatched}"
                    : string.Empty;
                string revive = action.reviveId > 0 && factionMatched ? $", SetRevPos({action.reviveId})" : string.Empty;
                string opened = action.IsCampOpenBox && !campMatched ? ", TalkMessage" : string.Empty;
                result = Success(action, $"{action.actionKind}(){faction}{camp}{revive}{opened}");
                return true;
            }

            if (action.IsShowLadder)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }
                _sideEffects.ShowLadder(action.ladderIds);
                result = Success(action, $"ShowLadder({FormatInts(action.ladderIds)})");
                return true;
            }

            if (action.IsNewWorld)
            {
                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }
                if (action.fightState >= 0)
                    _host.SetFightState(action.fightState);
                var target = action.TargetWorldPosition();
                _host.NewWorld(action.targetMapId, target);
                result = Success(action, $"NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            result = Failure(action, $"unsupported object action '{action.actionKind}'");
            return true;
        }

        private static ObjectActionExecutionResult Success(PcObjectActionCatalogEntry action, string detail)
            => new ObjectActionExecutionResult { success = true, detail = Detail(action, detail) };

        private static ObjectActionExecutionResult Failure(PcObjectActionCatalogEntry action, string detail)
            => new ObjectActionExecutionResult { success = false, detail = Detail(action, detail) };

        private static string Detail(PcObjectActionCatalogEntry action, string detail)
        {
            string fight = action.fightState >= 0 ? $", SetFightState({action.fightState})" : string.Empty;
            return $"{detail}{fight}; script={action.scriptPath}";
        }

        private static string FormatInts(int[] values)
        {
            if (values == null || values.Length == 0) return "[]";
            return "[" + string.Join(",", values) + "]";
        }
    }

    public sealed class SandboxObjectActionSideEffects : IObjectActionSideEffects
    {
        private readonly List<int> _eventItemIds = new();
        private readonly List<string> _notes = new();

        public IReadOnlyList<int> EventItemIds => _eventItemIds;
        public IReadOnlyList<string> Notes => _notes;

        public void PostMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var manager = SandboxManager.Instance;
            if (manager?.ChatService != null)
                manager.ChatService.PostSystemMessage(message);
            SubsystemLog.Info("MapObject", $"PC Msg2Player: {message}");
        }

        public void AddEventItem(int eventItemId)
        {
            if (eventItemId <= 0) return;
            _eventItemIds.Add(eventItemId);
            SubsystemLog.Info("MapObject", $"PC AddEventItem({eventItemId}) recorded");
        }

        public void AddNote(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return;
            _notes.Add(note);
            SubsystemLog.Info("MapObject", $"PC AddNote: {note}");
        }

        public void OpenBox()
        {
            SubsystemLog.Info("MapObject", "PC OpenBox() recorded");
        }

        public void ShowLadder(int[] ladderIds)
        {
            SubsystemLog.Info("MapObject", $"PC ShowLadder({FormatInts(ladderIds)}) recorded");
        }

        private static string FormatInts(int[] values)
        {
            if (values == null || values.Length == 0) return "[]";
            return "[" + string.Join(",", values) + "]";
        }
    }

    [DisallowMultipleComponent]
    public sealed class PcMapObjectInteraction : MonoBehaviour
    {
        private MapInteractiveObject _object;
        private PcObjectActionExecutor _executor;

        public MapInteractiveObject Object => _object;

        public void Configure(MapInteractiveObject obj, PcObjectActionExecutor executor)
        {
            _object = obj;
            _executor = executor;
            EnsureClickCollider(obj);
        }

        public ObjectActionExecutionResult Interact()
        {
            if (_executor == null || !_executor.TryExecute(_object, out var result))
                return null;
            if (result.success)
            {
                SubsystemLog.Info("MapObject", $"PC object action applied: {result.detail}");
                if (result.hideObject)
                    gameObject.SetActive(false);
            }
            else
            {
                SubsystemLog.Error("MapObject", $"PC object action failed: {result.detail}");
            }
            return result;
        }

        private void OnMouseDown() => Interact();

        private void EnsureClickCollider(MapInteractiveObject obj)
        {
            if (obj == null || GetComponent<Collider2D>() != null) return;
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            float width = Mathf.Max(1f, obj.imageCgXpos * 2f / 32f);
            float height = Mathf.Max(1f, (obj.height > 0 ? obj.height : obj.imageCgYpos) / 32f);
            box.size = new Vector2(width, height);
            box.offset = new Vector2(0f, height * 0.5f);
        }
    }
}
