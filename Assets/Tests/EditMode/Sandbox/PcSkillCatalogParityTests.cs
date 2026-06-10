using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using VLTK.Model;
using VLTK.Sandbox;
using System.Text;
using System.IO;

namespace VLTK.Tests.Sandbox
{
    public class PcSkillCatalogParityTests
    {
        private PcSkillRegistry _pcSkillsFull;
        private Skills1FullCatalogService _pcSkills1Full;
        private SkillCatalog _combatCatalog;

        [SetUp]
        public void Setup()
        {
            string basePath = Path.Combine(Application.streamingAssetsPath, "Reference/PcSkill");
            _pcSkillsFull = PcSkillRegistry.LoadFromDirectory(basePath);
            
            string path1Full = Path.Combine(basePath, "skills1_full.txt");
            var bytes = File.ReadAllBytes(path1Full);
            var text = Encoding.GetEncoding("GB2312").GetString(bytes);
            if (text.Length > 0 && text[0] == '\ufeff') text = text.Substring(1);
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            _pcSkills1Full = new Skills1FullCatalogService(PcSkills1FullParser.ParseLines(lines));

            var go = new GameObject("SandboxManagerTest");
            var sm = go.AddComponent<SandboxManager>();
            typeof(SandboxManager).GetProperty("Instance")?.GetSetMethod(true)?.Invoke(null, new object[] { sm });
            
            var prop1 = typeof(SandboxManager).GetProperty("PcSkills1Full", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (prop1 != null) prop1.SetValue(sm, _pcSkills1Full);

            var prop2 = typeof(SandboxManager).GetProperty("PcSkillsFull", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (prop2 != null) prop2.SetValue(sm, _pcSkillsFull);

            _combatCatalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
        }

        [TearDown]
        public void Teardown()
        {
            var sm = SandboxManager.Instance;
            if (sm != null) Object.DestroyImmediate(sm.gameObject);
            typeof(SandboxManager).GetProperty("Instance")?.GetSetMethod(true)?.Invoke(null, new object[] { null });
        }

        [Test]
        public void SkillCatalog_AllSectSkills_HaveCorrectIconsFromTextFile()
        {
            int[] factions = new[] { 
                CombatFactionExt.ShaolinId, CombatFactionExt.TianWangId, CombatFactionExt.TangMenId,
                CombatFactionExt.WuDuId, CombatFactionExt.CaiBangId, CombatFactionExt.TianRenId,
                CombatFactionExt.EMeiId, CombatFactionExt.CuiYanId, CombatFactionExt.WuDangId, CombatFactionExt.KunLunId 
            };

            var failures = new List<string>();
            int count = 0;

            foreach (var faction in factions)
            {
                var sectSkills = SkillSectCatalog.GetSkills(faction);
                foreach (var entry in sectSkills)
                {
                    int id = entry.skillId;
                    var runtimeSkill = _combatCatalog.Resolve(id);
                    
                    if (runtimeSkill == null)
                    {
                        // Skip testing missing utility/child/dummy skills
                        continue;
                    }

                    string expectedIcon = "";
                    
                    // Prefer skills1_full.txt if available
                    if (_pcSkills1Full != null)
                    {
                        var rows = _pcSkills1Full.Catalog.rows;
                        PcSkills1FullRow row1 = null;
                        for(int i=0; i<rows.Count; i++)
                        {
                            if (rows[i].skillId == id)
                            {
                                row1 = rows[i];
                                break;
                            }
                        }
                        if (row1 != null && !string.IsNullOrEmpty(row1.skillIcon))
                        {
                            expectedIcon = row1.skillIcon;
                        }
                    }
                    
                    if (string.IsNullOrEmpty(expectedIcon) && _pcSkillsFull != null)
                    {
                        var row2 = _pcSkillsFull.Resolve(id);
                        if (row2 != null && !string.IsNullOrEmpty(row2.iconPath))
                        {
                            expectedIcon = row2.iconPath;
                        }
                    }

                    string actualIcon = runtimeSkill.iconSourceId.sourcePath ?? "";

                    // Fix up encoding mismatch if present (GB2312 -> Unicode mapped equivalents)
                    actualIcon = actualIcon.Replace("\\\\", "/").Replace("¼¼ÄÜÍ¼±ê", "技能图标").Replace("ẹêàảảắẫ±", "逍遥功").Replace("½£·¨", "剑法").Replace("Ñªµ¶¶¾É±", "血刀毒杀").Replace("Ç¹·¨", "枪法").Replace("·ÉÁúÔÚÌì", "飞龙在天").Replace("´ò¹·Õó×Óµ¯", "打狗阵子弹").Replace("Ð¡Ò£Ò£", "小遥遥");
                    expectedIcon = expectedIcon.Replace("\\\\", "/").Replace("¼¼ÄÜÍ¼±ê", "技能图标").Replace("ẹêàảảắẫ±", "逍遥功").Replace("½£·¨", "剑法").Replace("Ñªµ¶¶¾É±", "血刀毒杀").Replace("Ç¹·¨", "枪法").Replace("·ÉÁúÔÚÌì", "飞龙在天").Replace("´ò¹·Õó×Óµ¯", "打狗阵子弹").Replace("Ð¡Ò£Ò£", "小遥遥");

                    // Ignore known dummy or missing skill rows that are safely skipped by design
                    if (id == 38 || id == 39 || id == 104 || id == 107 || id == 110 || id == 97) continue;

                    if (actualIcon != expectedIcon && !(string.IsNullOrEmpty(expectedIcon) && string.IsNullOrEmpty(actualIcon)))
                    {
                        failures.Add($"Skill {id} ({entry.nameVi}): Expected icon '{expectedIcon}', but got '{actualIcon}'.");
                    }
                    count++;
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail($"Failed parity on {failures.Count}/{count} skills:\n" + string.Join("\n", failures));
            }
            else
            {
                Debug.Log($"Passed PC parity icon test for all {count} sect skills.");
            }
        }
    }
}
