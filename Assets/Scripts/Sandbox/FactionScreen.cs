// -----------------------------------------------------------------------------
// VLTK Mobile — Faction/Sect Info Screen
// Shows faction details, sect skills overview, faction bonuses.
// Vietnamese UI.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Faction detail data.</summary>
    public class FactionInfo
    {
        public CombatFaction faction;
        public string nameVi;
        public string weaponVi;         // Vũ khí chính
        public string elementVi;        // Ngũ hành
        public string descVi;           // Mô tả môn phái
        public string strengthVi;       // Điểm mạnh
        public string weaknessVi;       // Điểm yếu
        public int totalSkills;         // Tổng số skill
        public List<string> notableSkillsVi = new(); // Skill nổi bật
    }

    /// <summary>
    /// Faction screen panel — shows current faction info and all faction details.
    /// Vietnamese UI with faction comparison.
    /// </summary>
    public class FactionPanel : MonoBehaviour
    {
        private GameObject _panelRoot;
        private Transform _factionListRoot;
        private Text _detailText;
        private Font _font;
        private bool _isOpen;
        private readonly Dictionary<CombatFaction, FactionInfo> _factions = new();

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            LoadFactionData();
        }

        public void Initialize()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panelRoot != null)
                _panelRoot.SetActive(_isOpen);
        }

        public FactionInfo GetFactionInfo(CombatFaction faction)
        {
            return _factions.TryGetValue(faction, out var info) ? info : null;
        }

        private void LoadFactionData()
        {
            _factions[CombatFaction.Shaolin] = new FactionInfo
            {
                faction = CombatFaction.Shaolin,
                nameVi = "Thiếu Lâm",
                weaponVi = "Trường Bổng / Côn",
                elementVi = "Kim",
                descVi = "Môn phái Phật giáo, nổi tiếng với quyền pháp và côn pháp. Thiếu Lâm là môn phái phòng thủ tốt nhất với khả năng hấp thụ sát thương.",
                strengthVi = "Phòng thủ cao, HP nhiều, kỹ năng hỗ trợ đội",
                weaknessVi = "Tốc độ chậm, sát thương tầm gần",
                totalSkills = 19,
                notableSkillsVi = new List<string> { "La Hán Quyền", "Kim Cương Chưởng", "Dịch Cân Kinh", "Thiên La Địa Võng" },
            };
            _factions[CombatFaction.TianWang] = new FactionInfo
            {
                faction = CombatFaction.TianWang,
                nameVi = "Thiên Vương Bang",
                weaponVi = "Thương / Giáo",
                elementVi = "Kim",
                descVi = "Bang phái quân sự, thiện chiến với thương pháp. Thiên Vương mạnh về tấn công và có khả năng chống đỡ tốt.",
                strengthVi = "Công thủ toàn diện, HP cao, hiệu ứng khống chế",
                weaknessVi = "Kỹ năng chậm, phụ thuộc trang bị",
                totalSkills = 19,
                notableSkillsVi = new List<string> { "Thiên Vương Thương Pháp", "Kim Long Tiễn", "Bát Phân Thương" },
            };
            _factions[CombatFaction.TangMen] = new FactionInfo
            {
                faction = CombatFaction.TangMen,
                nameVi = "Đường Môn",
                weaponVi = "Ám Khí / Phi Đao",
                elementVi = "Thổ",
                descVi = "Gia tộc ám khí nổi tiếng. Đường Môn sử dụng phi đao và ám khí từ xa, đặc biệt giỏi trong các kỹ năng độc.",
                strengthVi = "Sát thương từ xa cao, độc, bẫy",
                weaknessVi = "HP thấp, cận chiến yếu",
                totalSkills = 16,
                notableSkillsVi = new List<string> { "Mãn Thiên Hoa Vũ", "Phong Lôi Phi Đao", "Độc Khí" },
            };
            _factions[CombatFaction.CaiBang] = new FactionInfo
            {
                faction = CombatFaction.CaiBang,
                nameVi = "Cái Bang",
                weaponVi = "Trường Bổng",
                elementVi = "Thổ",
                descVi = "Bang hội ăn mày nhưng có võ công mạnh nhất. Cái Bang sở hữu Hàng Long Thập Bát Chưởng — chiêu thức mạnh nhất võ lâm.",
                strengthVi = "Sát thương diện rộng, buff đội, linh hoạt",
                weaknessVi = "Phòng thủ trung bình",
                totalSkills = 19,
                notableSkillsVi = new List<string> { "Hàng Long Thập Bát Chưởng", "Thất Long Bổng Pháp", "Tụ Vu Nghiệp Hỏa" },
            };
            _factions[CombatFaction.WuDu] = new FactionInfo
            {
                faction = CombatFaction.WuDu,
                nameVi = "Ngũ Độc Giáo",
                weaponVi = "Đao / Quạt",
                elementVi = "Mộc",
                descVi = "Giáo phái tà đạo sử dụng độc thuật. Ngũ Độc có khả năng gây sát thương theo thời gian và khống chế kẻ địch.",
                strengthVi = "Sát thương DOT, debuff mạnh, hồi phục",
                weaknessVi = "Sát thương tức thời thấp",
                totalSkills = 16,
                notableSkillsVi = new List<string> { "Thiên Tàm Độc Chưởng", "Vô Hình Độc", "Hồi Sinh Thuật" },
            };
            _factions[CombatFaction.TianRen] = new FactionInfo
            {
                faction = CombatFaction.TianRen,
                nameVi = "Thiên Nhẫn Giáo",
                weaponVi = "Pháp Trượng",
                elementVi = "Hỏa",
                descVi = "Giáo phái bí ẩn với thuật hỏa công. Thiên Nhẫn sở hữu phép thuật hệ hỏa mạnh mẽ, có khả năng gây sát thương diện rộng.",
                strengthVi = "Sát thương diện rộng, phép thuật mạnh, khống chế",
                weaknessVi = "HP thấp, dễ bị giết",
                totalSkills = 16,
                notableSkillsVi = new List<string> { "Liệt Hỏa Quyền", "Phục Ma Ấn", "Hồng Liễm Hoa" },
            };
            _factions[CombatFaction.EMei] = new FactionInfo
            {
                faction = CombatFaction.EMei,
                nameVi = "Nga My",
                weaponVi = "Kiếm",
                elementVi = "Thủy",
                descVi = "Môn phái nữ nhi với kiếm pháp thanh nhã. Nga My vừa có khả năng tấn công vừa hỗ trợ hồi máu cho đồng đội.",
                strengthVi = "Hồi máu, buff, công thủ kiêm toàn",
                weaknessVi = "Sát thương đơn mục tiêu thấp",
                totalSkills = 19,
                notableSkillsVi = new List<string> { "Phật Quang Phổ Chiếu", "Miêu Thủ Âm Dương", "Nga My Kiếm Pháp" },
            };
            _factions[CombatFaction.CuiYan] = new FactionInfo
            {
                faction = CombatFaction.CuiYan,
                nameVi = "Thúy Yên Môn",
                weaponVi = "Đao / Song Đao",
                elementVi = "Mộc",
                descVi = "Môn phái lấy linh thú yểm chú. Thúy Yên triệu hồi thú cưng chiến đấu và sử dụng bùa chú.",
                strengthVi = "Triệu hồi thú, debuff, đa dạng chiến thuật",
                weaknessVi = "Phụ thuộc thú triệu hồi",
                totalSkills = 16,
                notableSkillsVi = new List<string> { "Thúy Yên Linh Thú", "Huyền Bùa Thuật", "Chú Linh" },
            };
            _factions[CombatFaction.WuDang] = new FactionInfo
            {
                faction = CombatFaction.WuDang,
                nameVi = "Võ Đang",
                weaponVi = "Kiếm",
                elementVi = "Thủy",
                descVi = "Đạo giáo võ thuật, kiếm pháp tinh diệu. Võ Đang cân bằng giữa tấn công và phòng thủ, có nhiều buff cho bản thân.",
                strengthVi = "Cân bằng, buff mạnh, né tránh cao",
                weaknessVi = "Không mạnh nổi bật ở khía cạnh nào",
                totalSkills = 19,
                notableSkillsVi = new List<string> { "Thái Cực Kiếm", "Chân Võ Thất Tuyệt", "Lưỡng Nghi Tâm Pháp" },
            };
            _factions[CombatFaction.KunLun] = new FactionInfo
            {
                faction = CombatFaction.KunLun,
                nameVi = "Côn Lôn",
                weaponVi = "Đao / Trượng",
                elementVi = "Hỏa",
                descVi = "Môn phái tà đạo tại Tây Vực. Côn Lôn sử dụng thuật hỏa và băng, có khả năng khống chế mạnh.",
                strengthVi = "Khống chế diện rộng, nguyên tố kép (hỏa + băng)",
                weaknessVi = "HP thấp, cooldown dài",
                totalSkills = 16,
                notableSkillsVi = new List<string> { "Côn Lôn Hỏa Thuật", "Băng Tinh Thuật", "Hỗn Độn Ấn" },
            };
        }

        private void BuildUI()
        {
            _panelRoot = new GameObject("FactionPanel");
            _panelRoot.transform.SetParent(transform, false);
            _panelRoot.SetActive(false);

            var mainRt = _panelRoot.AddComponent<RectTransform>();
            mainRt.anchorMin = new Vector2(0.1f, 0.1f);
            mainRt.anchorMax = new Vector2(0.9f, 0.9f);

            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

            // Title
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panelRoot.transform, false);
            var tRt = titleBar.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.92f);
            tRt.anchorMax = new Vector2(1f, 1f);
            var tBg = titleBar.AddComponent<Image>();
            tBg.color = new Color(0.2f, 0.12f, 0.08f, 0.95f);

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar.transform, false);
            var ttRt = titleTextGo.AddComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.sizeDelta = Vector2.zero;
            var tTxt = titleTextGo.AddComponent<Text>();
            tTxt.text = "Môn Phái";
            tTxt.font = _font;
            tTxt.fontSize = 32;
            tTxt.color = new Color(1f, 0.9f, 0.7f);
            tTxt.alignment = TextAnchor.MiddleCenter;

            // Close
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBar.transform, false);
            var cRt = closeGo.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.9f, 0f);
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
            cTxt.fontSize = 24;
            cTxt.color = Color.white;
            cTxt.alignment = TextAnchor.MiddleCenter;

            // Faction list (left side)
            var listGo = new GameObject("FactionList");
            listGo.transform.SetParent(_panelRoot.transform, false);
            var lRt = listGo.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.02f, 0.02f);
            lRt.anchorMax = new Vector2(0.35f, 0.91f);
            _factionListRoot = listGo.transform;
            var vl = listGo.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.spacing = 3f;
            vl.padding = new RectOffset(4, 4, 4, 4);
            var fitter = listGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Populate faction buttons
            foreach (var kv in _factions)
            {
                var faction = kv.Key;
                var info = kv.Value;
                var btnGo = new GameObject($"Btn_{info.nameVi}");
                btnGo.transform.SetParent(_factionListRoot, false);
                var btnRt = btnGo.AddComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(0f, 36f);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() => ShowFactionDetail(faction));

                var btnTextGo = new GameObject("Text");
                btnTextGo.transform.SetParent(btnGo.transform, false);
                var btRt = btnTextGo.AddComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.sizeDelta = Vector2.zero;
                var btnTxt = btnTextGo.AddComponent<Text>();
                btnTxt.text = $"  {info.nameVi} [{info.elementVi}]";
                btnTxt.font = _font;
                btnTxt.fontSize = 20;
                btnTxt.color = FactionColor(faction);
                btnTxt.alignment = TextAnchor.MiddleLeft;
            }

            // Detail panel (right side)
            var detailGo = new GameObject("Detail");
            detailGo.transform.SetParent(_panelRoot.transform, false);
            var dRt = detailGo.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.37f, 0.02f);
            dRt.anchorMax = new Vector2(0.98f, 0.91f);
            _detailText = detailGo.AddComponent<Text>();
            _detailText.font = _font;
            _detailText.fontSize = 20;
            _detailText.color = new Color(0.9f, 0.9f, 0.85f);
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.verticalOverflow = VerticalWrapMode.Overflow;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void ShowFactionDetail(CombatFaction faction)
        {
            if (_detailText == null) return;
            var info = GetFactionInfo(faction);
            if (info == null) { _detailText.text = "Không có thông tin"; return; }

            string detail = $"<b><size=28>{info.nameVi}</size></b>\n\n";
            detail += $"Ngũ Hành: {info.elementVi}\n";
            detail += $"Vũ Khí: {info.weaponVi}\n";
            detail += $"Tổng Skill: {info.totalSkills}\n\n";
            detail += $"<b>Mô Tả:</b>\n{info.descVi}\n\n";
            detail += $"<b><color=#88ff88>Điểm Mạnh:</color></b>\n{info.strengthVi}\n\n";
            detail += $"<b><color=#ff8888>Điểm Yếu:</color></b>\n{info.weaknessVi}\n\n";
            detail += $"<b>Skill Nổi Bật:</b>\n";
            foreach (var skill in info.notableSkillsVi)
                detail += $"  • {skill}\n";

            _detailText.text = detail;
        }

        private static Color FactionColor(CombatFaction faction) => faction switch
        {
            CombatFaction.Shaolin => new Color(1f, 0.85f, 0.4f),
            CombatFaction.TianWang => new Color(0.9f, 0.7f, 0.3f),
            CombatFaction.TangMen => new Color(0.6f, 0.8f, 0.4f),
            CombatFaction.CaiBang => new Color(0.7f, 0.6f, 0.4f),
            CombatFaction.WuDu => new Color(0.5f, 0.9f, 0.5f),
            CombatFaction.TianRen => new Color(1f, 0.4f, 0.3f),
            CombatFaction.EMei => new Color(0.6f, 0.7f, 1f),
            CombatFaction.CuiYan => new Color(0.4f, 0.9f, 0.7f),
            CombatFaction.WuDang => new Color(0.5f, 0.6f, 1f),
            CombatFaction.KunLun => new Color(1f, 0.5f, 0.5f),
            _ => Color.white,
        };
    }
}
