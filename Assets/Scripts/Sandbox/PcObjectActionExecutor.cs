// -----------------------------------------------------------------------------
// VLTK Mobile — executes deterministic PC Region_S object Lua actions.
// Ported object API subset: NewWorld(mapId,x,y), optional SetFightState(),
// safe pickup messages: SetPropState/AddEventItem/AddNote/Msg2Player,
// read-only Say(message), read-only Talk(message...) object scripts,
// PC faction-gated OpenBox()+SetRevPos(id) storage boxes,
// PC camp-gated battlefield OpenBox()/Talk storage boxes, and
// read-only PC task-gated Talk message object scripts, and
// PC pickup scripts with task-gated optional AddNote().
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
        void ShowPrompt(string message, string[] choices);
        void EarnSilver(int amount);
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
            return TryExecute(obj, -1, out result);
        }

        public bool TryExecuteChoice(MapInteractiveObject obj, int choiceIndex, out ObjectActionExecutionResult result)
        {
            return TryExecute(obj, choiceIndex, out result);
        }

        private bool TryExecute(MapInteractiveObject obj, int choiceIndex, out ObjectActionExecutionResult result)
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

            if (action.IsPickupMessage || action.IsTaskOptionalPickupMessage)
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
                int notes = 0;
                if (action.notes != null)
                {
                    foreach (string note in action.notes)
                    {
                        if (string.IsNullOrWhiteSpace(note)) continue;
                        _sideEffects.AddNote(note);
                        notes++;
                    }
                }
                bool taskNoteMatched = false;
                if (action.IsTaskOptionalPickupMessage && action.taskNotes != null)
                {
                    int taskValue = _host.GetTaskValue(action.noteTaskId);
                    taskNoteMatched = taskValue > action.noteTaskMinExclusive && taskValue < action.noteTaskMaxExclusive;
                    if (taskNoteMatched)
                    {
                        foreach (string note in action.taskNotes)
                        {
                            if (string.IsNullOrWhiteSpace(note)) continue;
                            _sideEffects.AddNote(note);
                            notes++;
                        }
                    }
                }
                string taskNote = action.IsTaskOptionalPickupMessage
                    ? $", optionalNoteTask={action.noteTaskId} matched={taskNoteMatched}"
                    : string.Empty;
                result = Success(action,
                    $"{action.actionKind}(msg='{action.message}', items={FormatInts(action.eventItemIds)}, notes={notes}, SetPropState={action.setPropState}{taskNote})");
                result.hideObject = action.setPropState;
                return true;
            }

            if (action.IsTaskMissingItemPickupMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }

                int taskValue = _host.GetTaskValue(action.taskId);
                bool taskMatched = taskValue == action.taskValue;
                bool itemMissing = !_host.HaveItem(action.requiredMissingItemId, 1);
                bool matched = taskMatched && itemMissing;
                int notes = 0;
                if (matched)
                {
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
                        {
                            if (string.IsNullOrWhiteSpace(note)) continue;
                            _sideEffects.AddNote(note);
                            notes++;
                        }
                    }
                }

                result = Success(action,
                    $"TaskMissingItemPickupMessage(GetTask({action.taskId})={taskValue}, expected={action.taskValue}, HaveItem({action.requiredMissingItemId})={!itemMissing}, matched={matched}, items={FormatInts(action.eventItemIds)}, notes={notes}, SetPropState={action.setPropState})");
                result.hideObject = matched && action.setPropState;
                return true;
            }

            if (action.IsTaskItemConsumeMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }

                int taskValue = _host.GetTaskValue(action.taskId);
                bool taskMatched = taskValue == action.taskValue;
                if (!taskMatched)
                {
                    int elseLines = PostMessages(action.elseMessages);
                    result = Success(action,
                        $"TaskItemConsumeMessage(GetTask({action.taskId})={taskValue}, expected={action.taskValue}, matched=False, elseLines={elseLines})");
                    return true;
                }

                bool hasItems = HasAllItems(action.requiredItemIds, action.requiredItemCounts);
                if (!hasItems)
                {
                    int missingLines = PostMessages(action.missingItemMessages);
                    result = Success(action,
                        $"TaskItemConsumeMessage(GetTask({action.taskId})={taskValue}, requiredItems={FormatInts(action.requiredItemIds)}, matchedItems=False, missingLines={missingLines})");
                    return true;
                }

                int preMessages = PostMessages(action.preConsumeMessages);
                if (!TryConsumeItems(action.consumeItemIds ?? action.requiredItemIds, action.consumeItemCounts ?? action.requiredItemCounts))
                {
                    result = Failure(action, $"TaskItemConsumeMessage failed to DelItem({FormatInts(action.consumeItemIds ?? action.requiredItemIds)}) after validation");
                    return true;
                }

                if (action.setTaskId > 0)
                    _host.SetTaskValue(action.setTaskId, action.setTaskValue);
                if (action.eventItemIds != null)
                {
                    foreach (int eventItemId in action.eventItemIds)
                        _sideEffects.AddEventItem(eventItemId);
                }

                int notes = AddNotes(action.notes);
                int messages = PostMessages(action.successMessages ?? action.messages);
                result = Success(action,
                    $"TaskItemConsumeMessage(GetTask({action.taskId})={taskValue}, consumed={FormatInts(action.consumeItemIds ?? action.requiredItemIds)}, SetTask({action.setTaskId},{action.setTaskValue}), items={FormatInts(action.eventItemIds)}, notes={notes}, preMessages={preMessages}, messages={messages})");
                result.hideObject = action.setPropState;
                return true;
            }

            if (action.IsTaskItemBranchMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }

                var branches = action.branches;
                if (branches == null || branches.Length == 0)
                {
                    result = Failure(action, "TaskItemBranchMessage has no branches");
                    return true;
                }

                for (int i = 0; i < branches.Length; i++)
                {
                    var branch = branches[i];
                    if (!BranchMatches(branch)) continue;
                    if (!ValidateBranchConsumes(branch))
                    {
                        result = Failure(action, $"TaskItemBranchMessage branch {i} failed consume precheck");
                        return true;
                    }

                    var stats = ApplyBranchEffects(branch);
                    result = Success(action,
                        $"TaskItemBranchMessage(branch={i}, label='{branch?.label}', effects={stats.effects}, consumed={stats.consumed}, items={stats.eventItems}, notes={stats.notes}, messages={stats.messages}, setTasks={stats.setTasks}, setTaskTemps={stats.setTaskTemps}, randomRewards={stats.randomRewards})");
                    return true;
                }

                result = Success(action, "TaskItemBranchMessage(no matching branch)");
                return true;
            }

            if (action.IsPromptBranchMessage)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "object side-effect host unavailable");
                    return true;
                }

                if (action.branches != null && action.branches.Length > 0)
                    return TryExecutePromptBranches(action, choiceIndex, out result);

                var choices = AvailablePromptChoices(action);
                if (choices.Count == 0)
                {
                    int elseLines = PostMessages(action.elseMessages);
                    result = Success(action, $"PromptBranchMessage(no matching prompt, elseLines={elseLines})");
                    return true;
                }

                if (choiceIndex < 0)
                {
                    _sideEffects.ShowPrompt(choices[0].promptMessage, ChoiceLabels(choices));
                    result = Success(action, $"PromptBranchMessage(prompt='{choices[0].promptMessage}', choices={choices.Count})");
                    return true;
                }

                if (choiceIndex >= choices.Count)
                {
                    result = Failure(action, $"PromptBranchMessage invalid choice {choiceIndex}");
                    return true;
                }

                var choice = choices[choiceIndex];
                var stats = ApplyBranchEffects(choice.effects);
                result = Success(action,
                    $"PromptBranchMessage(choice={choiceIndex}, label='{choice.label}', effects={stats.effects}, consumed={stats.consumed}, notes={stats.notes}, messages={stats.messages}, setTasks={stats.setTasks}, silver={stats.silver})");
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

        private bool BranchMatches(PcObjectActionBranch branch)
        {
            return ConditionsMatch(branch?.conditions);
        }

        private bool ChoiceMatches(PcObjectActionChoice choice)
        {
            return ConditionsMatch(choice?.conditions);
        }

        private bool TryExecutePromptBranches(PcObjectActionCatalogEntry action, int choiceIndex, out ObjectActionExecutionResult result)
        {
            for (int i = 0; i < action.branches.Length; i++)
            {
                var branch = action.branches[i];
                if (!BranchMatches(branch)) continue;

                if (branch?.choices != null && branch.choices.Length > 0)
                {
                    if (choiceIndex < 0)
                    {
                        _sideEffects.ShowPrompt(branch.promptMessage, ChoiceLabels(branch.choices));
                        result = Success(action, $"PromptBranchMessage(branch={i}, prompt='{branch.promptMessage}', choices={branch.choices.Length})");
                        return true;
                    }

                    if (choiceIndex >= branch.choices.Length)
                    {
                        result = Failure(action, $"PromptBranchMessage branch {i} invalid choice {choiceIndex}");
                        return true;
                    }

                    var choice = branch.choices[choiceIndex];
                    if (!ChoiceMatches(choice))
                    {
                        result = Failure(action, $"PromptBranchMessage branch {i} choice {choiceIndex} conditions failed");
                        return true;
                    }

                    var stats = ApplyBranchEffects(choice.effects);
                    result = Success(action,
                        $"PromptBranchMessage(branch={i}, choice={choiceIndex}, label='{choice?.label}', effects={stats.effects}, consumed={stats.consumed}, notes={stats.notes}, messages={stats.messages}, setTasks={stats.setTasks}, silver={stats.silver})");
                    return true;
                }

                var branchStats = ApplyBranchEffects(branch);
                result = Success(action,
                    $"PromptBranchMessage(branch={i}, label='{branch?.label}', effects={branchStats.effects}, consumed={branchStats.consumed}, notes={branchStats.notes}, messages={branchStats.messages}, setTasks={branchStats.setTasks}, silver={branchStats.silver})");
                return true;
            }

            result = Success(action, "PromptBranchMessage(no matching prompt branch)");
            return true;
        }

        private bool ConditionsMatch(PcObjectActionCondition[] conditions)
        {
            if (conditions == null || conditions.Length == 0) return true;
            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                string type = condition.type ?? string.Empty;
                if (string.Equals(type, "TaskEquals", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (_host.GetTaskValue(condition.taskId) != condition.value) return false;
                }
                else if (string.Equals(type, "TaskBetweenInclusive", System.StringComparison.OrdinalIgnoreCase))
                {
                    int taskValue = _host.GetTaskValue(condition.taskId);
                    if (taskValue < condition.minValue || taskValue > condition.maxValue) return false;
                }
                else if (string.Equals(type, "TaskGreaterThan", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (_host.GetTaskValue(condition.taskId) <= condition.value) return false;
                }
                else if (string.Equals(type, "TaskTempEquals", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (_host.GetTaskTempValue(condition.taskId) != condition.value) return false;
                }
                else if (string.Equals(type, "TaskByteEquals", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (GetTaskByte(_host.GetTaskValue(condition.taskId), condition.byteIndex) != condition.value) return false;
                }
                else if (string.Equals(type, "TaskByteBetweenExclusive", System.StringComparison.OrdinalIgnoreCase))
                {
                    int byteValue = GetTaskByte(_host.GetTaskValue(condition.taskId), condition.byteIndex);
                    if (byteValue <= condition.minValue || byteValue >= condition.maxValue) return false;
                }
                else if (string.Equals(type, "TaskBitEquals", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (GetTaskBit(_host.GetTaskValue(condition.taskId), condition.bitIndex) != condition.value) return false;
                }
                else if (string.Equals(type, "HaveItem", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!_host.HaveItem(condition.itemId, condition.count <= 0 ? 1 : condition.count)) return false;
                }
                else if (string.Equals(type, "MissingItem", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (_host.HaveItem(condition.itemId, condition.count <= 0 ? 1 : condition.count)) return false;
                }
            }
            return true;
        }

        private List<PcObjectActionChoice> AvailablePromptChoices(PcObjectActionCatalogEntry action)
        {
            var result = new List<PcObjectActionChoice>();
            if (action?.choices == null) return result;
            string prompt = null;
            foreach (var choice in action.choices)
            {
                if (choice == null || !ChoiceMatches(choice)) continue;
                prompt ??= choice.promptMessage ?? string.Empty;
                if (!string.Equals(prompt, choice.promptMessage ?? string.Empty, System.StringComparison.Ordinal)) continue;
                result.Add(choice);
            }
            return result;
        }

        private static string[] ChoiceLabels(List<PcObjectActionChoice> choices)
        {
            if (choices == null || choices.Count == 0) return System.Array.Empty<string>();
            var labels = new string[choices.Count];
            for (int i = 0; i < choices.Count; i++)
                labels[i] = choices[i]?.label ?? string.Empty;
            return labels;
        }

        private static string[] ChoiceLabels(PcObjectActionChoice[] choices)
        {
            if (choices == null || choices.Length == 0) return System.Array.Empty<string>();
            var labels = new string[choices.Length];
            for (int i = 0; i < choices.Length; i++)
                labels[i] = choices[i]?.label ?? string.Empty;
            return labels;
        }

        private bool ValidateBranchConsumes(PcObjectActionBranch branch)
        {
            if (branch?.effects == null) return true;
            foreach (var effect in branch.effects)
            {
                if (effect == null || !string.Equals(effect.type, "ConsumeItems", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!HasAllItems(effect.itemIds, effect.itemCounts)) return false;
            }
            return true;
        }

        private BranchEffectStats ApplyBranchEffects(PcObjectActionBranch branch)
        {
            return ApplyBranchEffects(branch?.effects);
        }

        private BranchEffectStats ApplyBranchEffects(PcObjectActionEffect[] effects)
        {
            var stats = new BranchEffectStats();
            if (effects == null) return stats;
            foreach (var effect in effects)
            {
                if (effect == null) continue;
                stats.effects++;
                string type = effect.type ?? string.Empty;
                if (string.Equals(type, "ConsumeItems", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (TryConsumeItems(effect.itemIds, effect.itemCounts))
                        stats.consumed += effect.itemIds?.Length ?? 0;
                }
                else if (string.Equals(type, "AddEventItem", System.StringComparison.OrdinalIgnoreCase))
                {
                    _sideEffects.AddEventItem(effect.itemId);
                    stats.eventItems++;
                }
                else if (string.Equals(type, "SetTask", System.StringComparison.OrdinalIgnoreCase))
                {
                    _host.SetTaskValue(effect.taskId, effect.value);
                    stats.setTasks++;
                }
                else if (string.Equals(type, "SetTaskTemp", System.StringComparison.OrdinalIgnoreCase))
                {
                    _host.SetTaskTemp(effect.taskId, effect.value);
                    stats.setTaskTemps++;
                }
                else if (string.Equals(type, "SetTaskByte", System.StringComparison.OrdinalIgnoreCase))
                {
                    int oldValue = _host.GetTaskValue(effect.taskId);
                    _host.SetTaskValue(effect.taskId, SetTaskByte(oldValue, effect.byteIndex, effect.value));
                    stats.setTasks++;
                }
                else if (string.Equals(type, "SetTaskBit", System.StringComparison.OrdinalIgnoreCase))
                {
                    int oldValue = _host.GetTaskValue(effect.taskId);
                    _host.SetTaskValue(effect.taskId, SetTaskBit(oldValue, effect.bitIndex, effect.value));
                    stats.setTasks++;
                }
                else if (string.Equals(type, "PostMessage", System.StringComparison.OrdinalIgnoreCase))
                {
                    stats.messages += PostEffectMessages(effect);
                }
                else if (string.Equals(type, "AddNote", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(effect.message))
                    {
                        _sideEffects.AddNote(effect.message);
                        stats.notes++;
                    }
                }
                else if (string.Equals(type, "RandomAddEventItemIfMissing", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!_host.HaveItem(effect.itemId, 1) && _host.RandomIntInclusive(0, 99) < effect.value)
                    {
                        _sideEffects.AddEventItem(effect.itemId);
                        stats.eventItems++;
                        stats.randomRewards++;
                        if (!string.IsNullOrWhiteSpace(effect.message))
                        {
                            _sideEffects.AddNote(effect.message);
                            stats.notes++;
                        }
                    }
                }
                else if (string.Equals(type, "EarnSilver", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (effect.value > 0)
                    {
                        _sideEffects.EarnSilver(effect.value);
                        stats.silver += effect.value;
                    }
                }
                else if (string.Equals(type, "PostRewardCountMessage", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (effect.messages != null && effect.messages.Length > 0)
                    {
                        int index = stats.randomRewards;
                        if (index < 0) index = 0;
                        if (index >= effect.messages.Length) index = effect.messages.Length - 1;
                        string message = effect.messages[index];
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            _sideEffects.PostMessage(message);
                            stats.messages++;
                        }
                    }
                }
                else if (string.Equals(type, "CompleteIfTaskBytesEqual", System.StringComparison.OrdinalIgnoreCase))
                {
                    int taskValue = _host.GetTaskValue(effect.taskId);
                    int left = GetTaskByte(taskValue, effect.byteIndex);
                    int right = GetTaskByte(taskValue, effect.compareByteIndex);
                    if (left == right)
                    {
                        if (effect.setByteIndex > 0)
                        {
                            _host.SetTaskValue(effect.taskId, SetTaskByte(taskValue, effect.setByteIndex, effect.value));
                            stats.setTasks++;
                        }
                        if (TryConsumeItems(effect.itemIds, effect.itemCounts))
                            stats.consumed += effect.itemIds?.Length ?? 0;
                        stats.messages += PostMessages(effect.messages);
                        if (!string.IsNullOrWhiteSpace(effect.noteMessage))
                        {
                            _sideEffects.AddNote(effect.noteMessage);
                            stats.notes++;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(effect.failureMessage))
                    {
                        _sideEffects.PostMessage(effect.failureMessage);
                        stats.messages++;
                    }
                }
            }
            return stats;
        }

        private int PostEffectMessages(PcObjectActionEffect effect)
        {
            if (effect == null) return 0;
            int count = 0;
            if (!string.IsNullOrWhiteSpace(effect.message))
            {
                _sideEffects.PostMessage(effect.message);
                count++;
            }
            if (effect.messages == null) return count;
            count += PostMessages(effect.messages);
            return count;
        }

        private bool HasAllItems(int[] itemIds, int[] counts)
        {
            if (itemIds == null || itemIds.Length == 0) return true;
            for (int i = 0; i < itemIds.Length; i++)
            {
                int count = CountAt(counts, i);
                if (!_host.HaveItem(itemIds[i], count))
                    return false;
            }
            return true;
        }

        private bool TryConsumeItems(int[] itemIds, int[] counts)
        {
            if (itemIds == null || itemIds.Length == 0) return true;
            if (!HasAllItems(itemIds, counts)) return false;
            for (int i = 0; i < itemIds.Length; i++)
            {
                if (!_host.DelItem(itemIds[i], CountAt(counts, i)))
                    return false;
            }
            return true;
        }

        private int AddNotes(string[] notes)
        {
            int count = 0;
            if (notes == null) return count;
            foreach (string note in notes)
            {
                if (string.IsNullOrWhiteSpace(note)) continue;
                _sideEffects.AddNote(note);
                count++;
            }
            return count;
        }

        private int PostMessages(string[] messages)
        {
            int count = 0;
            if (messages == null) return count;
            foreach (string message in messages)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                _sideEffects.PostMessage(message);
                count++;
            }
            return count;
        }

        private static int CountAt(int[] counts, int index)
            => counts != null && index >= 0 && index < counts.Length && counts[index] > 0 ? counts[index] : 1;

        private static int GetTaskByte(int taskValue, int byteIndex)
        {
            if (byteIndex <= 0) return 0;
            int shift = (byteIndex - 1) * 8;
            if (shift >= 32) return 0;
            return (taskValue >> shift) & 0xff;
        }

        private static int SetTaskByte(int taskValue, int byteIndex, int byteValue)
        {
            if (byteIndex <= 0) return taskValue;
            int shift = (byteIndex - 1) * 8;
            if (shift >= 32) return taskValue;
            int mask = 0xff << shift;
            return (taskValue & ~mask) | ((byteValue & 0xff) << shift);
        }

        private static int GetTaskBit(int taskValue, int bitIndex)
        {
            if (bitIndex <= 0) return 0;
            int shift = bitIndex - 1;
            if (shift >= 32) return 0;
            return (taskValue >> shift) & 0x1;
        }

        private static int SetTaskBit(int taskValue, int bitIndex, int bitValue)
        {
            if (bitIndex <= 0) return taskValue;
            int shift = bitIndex - 1;
            if (shift >= 32) return taskValue;
            int mask = 1 << shift;
            return bitValue == 0 ? (taskValue & ~mask) : (taskValue | mask);
        }

        private struct BranchEffectStats
        {
            public int effects;
            public int consumed;
            public int eventItems;
            public int notes;
            public int messages;
            public int setTasks;
            public int setTaskTemps;
            public int randomRewards;
            public int silver;
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
            SandboxManager.Instance?.QuestItemService?.AddEventItem(eventItemId);
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

        public void ShowPrompt(string message, string[] choices)
        {
            PostMessage(message);
            SubsystemLog.Info("MapObject", $"PC Say prompt choices={FormatInts(choices)}");
        }

        public void EarnSilver(int amount)
        {
            if (amount <= 0) return;
            SandboxManager.Instance?.GameplayLoop?.Economy?.EarnSilver(amount);
            SubsystemLog.Info("MapObject", $"PC Earn({amount}) silver recorded");
        }

        private static string FormatInts(int[] values)
        {
            if (values == null || values.Length == 0) return "[]";
            return "[" + string.Join(",", values) + "]";
        }

        private static string FormatInts(string[] values)
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
