// -----------------------------------------------------------------------------
// VLTK Mobile — JX Friend list panel state (E4)
// Port source: /home/zet/Projects/jx-cocos/client/Classes/vn/gameui/KuiFriendListVN.cpp
//
// Source data model:
//  - 3 relation groups stored as std::list<RelationInfo*> keyed by section code:
//      section 2 = m_brothers  (Hảo Hữu / friends)   FRIEND_UNITNAME
//      section 3 = m_pks       (Cừu Nhân / enemies)   ENEMY_UNITNAME
//      section 5 = m_blackList (Hắc Danh / blacklist) BLACKLIST_UNITNAME
//    (section 1 old m_friends is commented out in source; sections 4/7 unused.)
//  - RelationInfo { BYTE State; char m_szName[64]; }: State 0 = offline, !0 = online.
//  - Each group enforces name uniqueness: AddList/AddFriend skip if name exists.
//  - Global mutators then InitPage(section) to reload only the visible tab.
//
// Source rendering (AddCustomItem):
//  - ListView size (124,267) at (3,27); each item anchor (114,16).
//  - online -> WHITE, offline -> ccc3(150,150,150).
//  - __nSelIndex default -1; selecting an item stores its name (used by delete).
//  - default section = 2 (Hảo Hữu).
//
// Pure C# state, EditMode-testable. No MonoBehaviour.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.UI.JxCocos
{
    /// <summary>Friend relation group — section code matches source switch.</summary>
    public enum JxFriendRelation
    {
        Friend = 2,    // m_brothers  — Hảo Hữu
        Enemy = 3,     // m_pks       — Cừu Nhân
        Blacklist = 5, // m_blackList — Hắc Danh
    }

    public readonly struct JxFriendEntry
    {
        public readonly string Name;
        public readonly bool Online;

        public JxFriendEntry(string name, bool online)
        {
            Name = name ?? string.Empty;
            Online = online;
        }
    }

    /// <summary>
    /// Pure friend list panel state. Mirrors the 3 std::list groups + panel
    /// section/selection. Source dedup semantics: an Add into a group that
    /// already contains the name is a no-op for that group.
    /// </summary>
    public sealed class JxFriendPanelState
    {
        // --- Source constants (KuiFriendListVN.cpp) ---
        public const JxFriendRelation DefaultRelation = JxFriendRelation.Friend;
        public const float ListViewWidth = 124f;
        public const float ListViewHeight = 267f;
        public const float ListViewPosX = 3f;
        public const float ListViewPosY = 27f;
        public const float ItemWidth = 114f;
        public const float ItemHeight = 16f;
        public const int NoSelection = -1;

        // online -> WHITE (255,255,255); offline -> ccc3(150,150,150).
        public static readonly UnityEngine.Color OnlineColor =
            new UnityEngine.Color(1f, 1f, 1f, 1f);
        public static readonly UnityEngine.Color OfflineColor =
            new UnityEngine.Color(150f / 255f, 150f / 255f, 150f / 255f, 1f);

        private readonly Dictionary<JxFriendRelation, List<JxFriendEntry>> _groups =
            new()
            {
                { JxFriendRelation.Friend, new List<JxFriendEntry>() },
                { JxFriendRelation.Enemy, new List<JxFriendEntry>() },
                { JxFriendRelation.Blacklist, new List<JxFriendEntry>() },
            };

        /// <summary>Whether the panel is currently shown (source isOpen).</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Currently visible relation tab (source `section`).</summary>
        public JxFriendRelation CurrentRelation { get; private set; } = DefaultRelation;

        /// <summary>Selected row index in the current tab, or -1 (source __nSelIndex).</summary>
        public int SelectedIndex { get; private set; } = NoSelection;

        /// <summary>Name of the selected entry in the current tab (source mName, for delete).</summary>
        public string SelectedName { get; private set; } = string.Empty;

        // --- Panel lifecycle ---

        public void Open()
        {
            IsOpen = true;
            CurrentRelation = DefaultRelation;
            SelectedIndex = NoSelection;
            SelectedName = string.Empty;
        }

        public void Close()
        {
            IsOpen = false;
            SelectedIndex = NoSelection;
            SelectedName = string.Empty;
        }

        /// <summary>
        /// Switch relation tab (source buttonCallBackFunc case 2/3/5 → section=, LoadPage).
        /// Resetting selection mirrors a fresh LoadPage. Returns whether the tab changed.
        /// </summary>
        public bool SelectRelation(JxFriendRelation relation)
        {
            bool changed = CurrentRelation != relation;
            CurrentRelation = relation;
            SelectedIndex = NoSelection;
            SelectedName = string.Empty;
            return changed;
        }

        /// <summary>Select a row in the current tab (source selectedMainListItemEvent).</summary>
        public bool Select(int index)
        {
            var list = _groups[CurrentRelation];
            if (index < 0 || index >= list.Count)
            {
                SelectedIndex = NoSelection;
                SelectedName = string.Empty;
                return false;
            }
            SelectedIndex = index;
            SelectedName = list[index].Name;
            return true;
        }

        // --- Mutators (source global functions) ---

        /// <summary>
        /// Add an entry into a group. Source dedup: if the group already contains
        /// the name, nothing happens. Returns true if added.
        /// </summary>
        public bool Add(JxFriendRelation relation, string name, bool online)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var list = _groups[relation];
            if (IndexOfName(list, name) >= 0) return false;
            list.Add(new JxFriendEntry(name, online));
            return true;
        }

        /// <summary>Source AddFriend: brothers group, online default true.</summary>
        public bool AddFriend(string name) => Add(JxFriendRelation.Friend, name, true);

        /// <summary>Source AddBlackList: blacklist group, online default true.</summary>
        public bool AddBlacklist(string name) => Add(JxFriendRelation.Blacklist, name, true);

        /// <summary>
        /// Source EraseFriend: remove the name from all 3 groups. Returns the
        /// number of groups it was removed from.
        /// </summary>
        public int Erase(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            int removed = 0;
            foreach (var kv in _groups)
            {
                int idx = IndexOfName(kv.Value, name);
                if (idx >= 0)
                {
                    kv.Value.RemoveAt(idx);
                    removed++;
                }
            }
            if (!IsSelectedStillValid()) ClearSelection();
            return removed;
        }

        /// <summary>
        /// Source SyncState: update online state of the name in all 3 groups.
        /// Returns the number of groups updated.
        /// </summary>
        public int SyncState(string name, bool online)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            int updated = 0;
            foreach (var kv in _groups)
            {
                int idx = IndexOfName(kv.Value, name);
                if (idx >= 0)
                {
                    kv.Value[idx] = new JxFriendEntry(name, online);
                    updated++;
                }
            }
            return updated;
        }

        // --- Read accessors ---

        public IReadOnlyList<JxFriendEntry> GetEntries(JxFriendRelation relation) => _groups[relation];

        public IReadOnlyList<JxFriendEntry> CurrentEntries => _groups[CurrentRelation];

        public int Count(JxFriendRelation relation) => _groups[relation].Count;

        public int CurrentCount => _groups[CurrentRelation].Count;

        /// <summary>Resolve the online color for an entry (source AddCustomItem).</summary>
        public static UnityEngine.Color ColorFor(bool online) => online ? OnlineColor : OfflineColor;

        private bool IsSelectedStillValid()
        {
            if (SelectedIndex == NoSelection) return true;
            return SelectedIndex < _groups[CurrentRelation].Count;
        }

        private void ClearSelection()
        {
            SelectedIndex = NoSelection;
            SelectedName = string.Empty;
        }

        private static int IndexOfName(List<JxFriendEntry> list, string name)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Name == name) return i;
            return -1;
        }
    }
}
