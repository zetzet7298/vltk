// -----------------------------------------------------------------------------
// VLTK Mobile — Party/Team System
// Party formation, member management, EXP distribution.
// PC source: Team management UI, team EXP share logic.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Party member data.</summary>
    [Serializable]
    public class PartyMember
    {
        public int memberId;
        public string nameVi;
        public int level;
        public int factionId;       // Môn phái
        public int hpCurrent;
        public int hpMax;
        public int mpCurrent;
        public int mpMax;
        public bool isLeader;
        public bool isOnline;
        public Vector2 position;
    }

    /// <summary>EXP distribution mode.</summary>
    public enum ExpDistMode
    {
        Equal = 0,          // Chia đều
        LevelBased = 1,     // Theo cấp độ
        LeaderTakes = 2,    // Trưởng lấy hết
    }

    /// <summary>
    /// Party service — manages party formation, member list, EXP distribution.
    /// Pure C# (no MonoBehaviour), fully testable.
    /// </summary>
    public class PartyService
    {
        private readonly List<PartyMember> _members = new();
        private readonly int _maxMembers = 6;
        private int _leaderId;
        private ExpDistMode _expMode = ExpDistMode.Equal;

        public IReadOnlyList<PartyMember> Members => _members;
        public int MemberCount => _members.Count;
        public bool IsInParty => _members.Count > 0;
        public ExpDistMode ExpMode => _expMode;
        public int LeaderId => _leaderId;

        public event Action<PartyMember> OnMemberJoined;
        public event Action<int> OnMemberLeft;
        public event Action OnPartyDisbanded;
        public event Action<ExpDistMode> OnExpModeChanged;

        /// <summary>Create a party with the player as leader.</summary>
        public void CreateParty(int playerId, string playerName, int level, int factionId)
        {
            _members.Clear();
            var leader = new PartyMember
            {
                memberId = playerId,
                nameVi = playerName,
                level = level,
                factionId = factionId,
                isLeader = true,
                isOnline = true,
            };
            _members.Add(leader);
            _leaderId = playerId;
            SubsystemLog.Info("Party", $"Đội được tạo: {playerName} (trưởng)");
        }

        /// <summary>Add a member to the party.</summary>
        public bool AddMember(int memberId, string name, int level, int factionId)
        {
            if (_members.Count >= _maxMembers)
            {
                SubsystemLog.Warn("Party", "Đội đã đủ thành viên (tối đa 6)");
                return false;
            }
            if (_members.Exists(m => m.memberId == memberId))
            {
                SubsystemLog.Warn("Party", $"Thành viên {name} đã trong đội");
                return false;
            }

            var member = new PartyMember
            {
                memberId = memberId,
                nameVi = name,
                level = level,
                factionId = factionId,
                isLeader = false,
                isOnline = true,
            };
            _members.Add(member);
            OnMemberJoined?.Invoke(member);
            SubsystemLog.Info("Party", $"{name} gia nhập đội ({_members.Count}/6)");
            return true;
        }

        /// <summary>Remove a member from the party.</summary>
        public void RemoveMember(int memberId)
        {
            var member = _members.Find(m => m.memberId == memberId);
            if (member == null) return;

            _members.Remove(member);
            OnMemberLeft?.Invoke(memberId);

            if (member.isLeader && _members.Count > 0)
            {
                // Promote first member to leader
                _members[0].isLeader = true;
                _leaderId = _members[0].memberId;
                SubsystemLog.Info("Party", $"{_members[0].nameVi} trở thành trưởng đội");
            }

            if (_members.Count == 0)
            {
                OnPartyDisbanded?.Invoke();
                SubsystemLog.Info("Party", "Đội đã giải tán");
            }
        }

        /// <summary>Leave the party.</summary>
        public void LeaveParty(int memberId)
        {
            RemoveMember(memberId);
        }

        /// <summary>Transfer party leadership to an existing member.</summary>
        public bool TransferLeadership(int memberId)
        {
            var nextLeader = _members.Find(m => m.memberId == memberId);
            if (nextLeader == null)
                return false;

            foreach (var member in _members)
                member.isLeader = false;
            nextLeader.isLeader = true;
            _leaderId = memberId;
            SubsystemLog.Info("Party", $"{nextLeader.nameVi} trở thành trưởng đội");
            return true;
        }

        /// <summary>Disband the whole party, matching the PC Dismiss command.</summary>
        public void DisbandParty()
        {
            if (_members.Count == 0)
                return;
            _members.Clear();
            _leaderId = 0;
            OnPartyDisbanded?.Invoke();
            SubsystemLog.Info("Party", "Đội đã giải tán");
        }

        /// <summary>Distribute EXP among party members.</summary>
        public Dictionary<int, int> DistributeExp(int totalExp)
        {
            var result = new Dictionary<int, int>();
            if (_members.Count == 0 || totalExp <= 0) return result;

            switch (_expMode)
            {
                case ExpDistMode.Equal:
                {
                    int perMember = totalExp / _members.Count;
                    int remainder = totalExp % _members.Count;
                    foreach (var m in _members)
                    {
                        result[m.memberId] = perMember;
                    }
                    // Remainder goes to leader
                    if (remainder > 0 && result.ContainsKey(_leaderId))
                        result[_leaderId] += remainder;
                    break;
                }
                case ExpDistMode.LevelBased:
                {
                    int totalLevel = 0;
                    foreach (var m in _members) totalLevel += m.level;
                    if (totalLevel == 0) totalLevel = 1;
                    foreach (var m in _members)
                    {
                        result[m.memberId] = Mathf.RoundToInt((float)m.level / totalLevel * totalExp);
                    }
                    break;
                }
                case ExpDistMode.LeaderTakes:
                {
                    result[_leaderId] = totalExp;
                    break;
                }
            }

            return result;
        }

        public void SetExpMode(ExpDistMode mode)
        {
            _expMode = mode;
            OnExpModeChanged?.Invoke(mode);
        }

        /// <summary>Get faction name in Vietnamese.</summary>
        public static string FactionNameVi(int factionId) => factionId switch
        {
            1 => "Thiếu Lâm",
            2 => "Võ Đang",
            3 => "Nga My",
            4 => "Thiên Vương",
            5 => "Đường Môn",
            6 => "Ngũ Độc",
            7 => "Cái Bang",
            8 => "Thiên Nhẫn",
            9 => "Thúy Yên",
            10 => "Côn Lôn",
            _ => "Vô Môn Phái",
        };
    }

    /// <summary>
    /// Party UI panel — shows members, EXP mode, and management buttons.
    /// </summary>
    public class PartyPanel : MonoBehaviour
    {
        private PartyService _party;
        private GameObject _panelRoot;
        private Transform _memberListRoot;
        private Font _font;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        public void Initialize(PartyService party)
        {
            _party = party;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panelRoot != null)
                _panelRoot.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        public void Refresh()
        {
            if (_memberListRoot == null || _party == null) return;
            for (int i = _memberListRoot.childCount - 1; i >= 0; i--)
                Destroy(_memberListRoot.GetChild(i).gameObject);

            if (!_party.IsInParty)
            {
                AddLabel(_memberListRoot, "  Chưa tham gia đội", 22, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            foreach (var m in _party.Members)
            {
                string leaderIcon = m.isLeader ? "★ " : "  ";
                string faction = PartyService.FactionNameVi(m.factionId);
                string line = $"{leaderIcon}{m.nameVi} Lv{m.level} [{faction}]";
                AddLabel(_memberListRoot, line, 22, m.isLeader ? new Color(1f, 0.9f, 0.4f) : Color.white);
            }
        }

        private GameObject AddLabel(Transform parent, string text, int fontSize, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, fontSize + 8);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = _font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            return go;
        }

        private void BuildUI()
        {
            _panelRoot = new GameObject("PartyPanel");
            _panelRoot.transform.SetParent(transform, false);
            _panelRoot.SetActive(false);

            var mainRt = _panelRoot.AddComponent<RectTransform>();
            mainRt.anchorMin = new Vector2(0.75f, 0.4f);
            mainRt.anchorMax = new Vector2(1f, 0.85f);

            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);

            // Title
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panelRoot.transform, false);
            var tRt = titleBar.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.9f);
            tRt.anchorMax = new Vector2(1f, 1f);
            var tBg = titleBar.AddComponent<Image>();
            tBg.color = new Color(0.15f, 0.2f, 0.35f, 0.95f);

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar.transform, false);
            var ttRt = titleTextGo.AddComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.sizeDelta = Vector2.zero;
            var tTxt = titleTextGo.AddComponent<Text>();
            tTxt.text = "Đội";
            tTxt.font = _font;
            tTxt.fontSize = 28;
            tTxt.color = new Color(0.8f, 0.9f, 1f);
            tTxt.alignment = TextAnchor.MiddleCenter;

            // Close btn
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBar.transform, false);
            var cRt = closeGo.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.88f, 0f);
            cRt.anchorMax = new Vector2(1f, 1f);
            var cImg = closeGo.AddComponent<Image>();
            cImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var cBtn = closeGo.AddComponent<Button>();
            cBtn.targetGraphic = cImg;
            cBtn.onClick.AddListener(() => Toggle());

            var closeTextGo = new GameObject("CloseText");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var ctRt = closeTextGo.AddComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;
            var cTxt = closeTextGo.AddComponent<Text>();
            cTxt.text = "✕";
            cTxt.font = _font;
            cTxt.fontSize = 22;
            cTxt.color = Color.white;
            cTxt.alignment = TextAnchor.MiddleCenter;

            // Member list
            var listGo = new GameObject("MemberList");
            listGo.transform.SetParent(_panelRoot.transform, false);
            var lRt = listGo.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.02f, 0.02f);
            lRt.anchorMax = new Vector2(0.98f, 0.89f);
            _memberListRoot = listGo.transform;
            var vl = listGo.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.spacing = 4f;
            vl.padding = new RectOffset(6, 6, 6, 6);
            var fitter = listGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
