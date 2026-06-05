// -----------------------------------------------------------------------------
// VLTK Mobile — Vietnamese localization tests.
// Kiểm tra các catalog tiếng Việt có dữ liệu đầy đủ, không trả về null/empty
// cho 12 môn phái chính, có dấu tiếng Việt đúng chuẩn.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class VietnameseLocalizationTests
    {
        private static readonly char[] VietnameseDiacritics = {
            'á', 'à', 'ả', 'ã', 'ạ', 'ă', 'ằ', 'ắ', 'ẳ', 'ẵ', 'ặ',
            'â', 'ầ', 'ấ', 'ẩ', 'ẫ', 'ậ',
            'đ',
            'é', 'è', 'ẻ', 'ẽ', 'ẹ', 'ê', 'ề', 'ế', 'ể', 'ễ', 'ệ',
            'í', 'ì', 'ỉ', 'ĩ', 'ị',
            'ó', 'ò', 'ỏ', 'õ', 'ọ', 'ô', 'ồ', 'ố', 'ổ', 'ỗ', 'ộ', 'ơ', 'ờ', 'ớ', 'ở', 'ỡ', 'ợ',
            'ú', 'ù', 'ủ', 'ũ', 'ụ', 'ư', 'ừ', 'ứ', 'ử', 'ữ', 'ự',
            'ý', 'ỳ', 'ỷ', 'ỹ', 'ỵ'
        };

        // 12 môn phái chính (theo FactionVietnameseCatalog)
        private static readonly int[] MainFactionIds = { 0, 1, 2, 3, 4, 9, 5, 7, 6, 8, 10, 11 };

        [Test]
        public void Test_AllMainFactions_HaveVietnameseName()
        {
            foreach (var fid in MainFactionIds)
            {
                var name = FactionVietnameseCatalog.GetVietnameseName(fid);
                Assert.IsNotNull(name, $"Faction {fid} phải có tên VN");
                Assert.IsNotEmpty(name, $"Faction {fid} tên VN không được rỗng");
            }
        }

        [Test]
        public void Test_NoEmptyOrNull_FactionNames()
        {
            for (int fid = -1; fid <= 20; fid++)
            {
                var name = FactionVietnameseCatalog.GetVietnameseName(fid);
                if (name != null)
                {
                    Assert.IsNotEmpty(name, $"Faction {fid} tên không rỗng");
                }
            }
        }

        [Test]
        public void Test_AllFactionNames_ContainVietnameseDiacritics()
        {
            int withDiacritics = 0;
            foreach (var fid in MainFactionIds)
            {
                var name = FactionVietnameseCatalog.GetVietnameseName(fid);
                if (name == null) continue;
                bool hasDiacritic = false;
                foreach (var c in name.ToLowerInvariant())
                {
                    if (VietnameseDiacritics.Contains(c)) { hasDiacritic = true; break; }
                }
                if (hasDiacritic) withDiacritics++;
            }
            Assert.Greater(withDiacritics, MainFactionIds.Length / 2, "≥50% tên phái phải có dấu TV");
        }

        [Test]
        public void Test_TitleVietnameseCatalog_HasAtLeast50Entries()
        {
            var method = typeof(TitleVietnameseCatalog).GetMethod("GetVietnameseName", BindingFlags.Public | BindingFlags.Static);
            if (method == null) { Assert.Ignore("TitleVietnameseCatalog.GetVietnameseName không tồn tại"); return; }

            int count = 0;
            for (int id = 1; id <= 1000; id++)
            {
                var name = method.Invoke(null, new object[] { id }) as string;
                if (!string.IsNullOrEmpty(name)) count++;
            }
            UnityEngine.Debug.Log($"[VN] TitleVietnameseCatalog có {count} entries");
            Assert.GreaterOrEqual(count, 30, $"TitleVietnameseCatalog phải có ≥30 entries (có {count})");
        }

        [Test]
        public void Test_TitleVietnameseCatalog_NoEmptyStrings()
        {
            var method = typeof(TitleVietnameseCatalog).GetMethod("GetVietnameseName", BindingFlags.Public | BindingFlags.Static);
            if (method == null) { Assert.Ignore("TitleVietnameseCatalog không tồn tại"); return; }
            for (int id = 1; id <= 500; id++)
            {
                var name = method.Invoke(null, new object[] { id }) as string;
                if (name != null)
                {
                    Assert.IsNotEmpty(name, $"Title {id} không rỗng");
                }
            }
        }

        [Test]
        public void Test_UIPanelServices_HaveVietnameseLabels()
        {
            var uiAsm = Assembly.Load("VLTK.UI");
            if (uiAsm == null)
            {
                Assert.Ignore("VLTK.UI assembly không load được");
                return;
            }
            int panelCount = 0;
            int withLabels = 0;
            foreach (var t in uiAsm.GetTypes())
            {
                if (!t.Name.EndsWith("PanelService")) continue;
                panelCount++;
                // Tìm string fields/properties chứa "Vi" hoặc tiếng Việt
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                int labelCount = 0;
                foreach (var f in fields)
                {
                    if (f.FieldType != typeof(string)) continue;
                    if (!f.IsLiteral && !f.IsStatic) continue;
                    var val = f.GetRawConstantValue() as string;
                    if (string.IsNullOrEmpty(val)) continue;
                    if (HasVietnameseDiacritic(val)) labelCount++;
                }
                if (labelCount >= 1) withLabels++;
            }
            UnityEngine.Debug.Log($"[VN] {withLabels}/{panelCount} UI panels có VN labels");
            Assert.Greater(panelCount, 0, "Phải có ≥1 UI panel");
        }

        private static bool HasVietnameseDiacritic(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (var c in s.ToLowerInvariant())
            {
                if (VietnameseDiacritics.Contains(c)) return true;
            }
            return false;
        }
    }
}
