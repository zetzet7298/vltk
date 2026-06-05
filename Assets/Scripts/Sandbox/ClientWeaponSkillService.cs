// -----------------------------------------------------------------------------
// VLTK Mobile — ST Client Weapon Skill runtime service
// Source: PC settings/clientweaponskill.txt (Reference/PcSkill).
// Quản lý skill vũ khí client-side (theo loại vũ khí).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Client Weapon Skill (skill vũ khí client-side).
    /// </summary>
    public class ClientWeaponSkillService
    {
        private PcClientWeaponSkillRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ClientWeaponSkillService() { }
        public ClientWeaponSkillService(PcClientWeaponSkillRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcClientWeaponSkillRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("ClientWeaponSkill", "Client weapon skill registry rỗng");
        }

        public static ClientWeaponSkillService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcSkill");
            var reg = PcClientWeaponSkillParser.BuildRegistry(root);
            return new ClientWeaponSkillService(reg);
        }

        public PcClientWeaponSkillEntry GetSkill(int weaponType) => _reg != null ? _reg.Get(weaponType) : null;
        public IReadOnlyList<PcClientWeaponSkillEntry> GetByLevel(int level)
            => _reg != null ? _reg.GetByLevel(level) : System.Array.Empty<PcClientWeaponSkillEntry>();
        public IReadOnlyList<PcClientWeaponSkillEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcClientWeaponSkillEntry>();
    }
}
