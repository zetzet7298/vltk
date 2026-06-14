// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.2 Player Level Service
// Manages EXP accumulation, level up flow, potential points, and skill points.
// Source: PC level cap 99, 5 potential points per level, 1 skill point.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public struct LevelUpEvent
    {
        public int oldLevel;
        public int newLevel;
        public int potentialPointsGranted;
        public int skillPointsGranted;
    }

    /// <summary>
    /// Service quản lý cấp độ, kinh nghiệm và điểm kỹ năng/tiềm năng của người chơi.
    /// </summary>
    public class PlayerLevelService
    {
        public const int MaxPlayerLevel = 99;
        public const int PotentialPointsPerLevel = 5;
        public const int SkillPointsPerLevel = 1;

        private readonly IPlayerLevelHost _host;
        private int _level = 1;
        private long _currentExp;
        private int _potentialPoints;
        private int _skillPoints;

        // Điểm tiềm năng cơ bản
        public int Strength { get; private set; } = 20;
        public int Dexterity { get; private set; } = 20;
        public int Vitality { get; private set; } = 20;
        public int InnerStrength { get; private set; } = 20;

        public int Level => _level;
        public long CurrentExp => _currentExp;
        public int PotentialPoints => _potentialPoints;
        public int SkillPoints => _skillPoints;

        /// <summary>Event kích hoạt khi người chơi lên cấp.</summary>
        public event Action<LevelUpEvent> OnLevelUp;

        /// <summary>Event kích hoạt khi EXP thay đổi.</summary>
        public event Action<long, long> OnExpChanged; // (current, required)

        public PlayerLevelService(int initialLevel = 1) : this(initialLevel, null) { }

        public PlayerLevelService(int initialLevel, IPlayerLevelHost host)
        {
            _host = host;
            _level = Mathf.Clamp(initialLevel, 1, MaxPlayerLevel);
            // PC: nhân vật sinh ra ở cấp L sẽ nhận (L-1) potential + skill points.
            int levelsGained = _level - 1;
            _potentialPoints = levelsGained * PotentialPointsPerLevel;
            _skillPoints = levelsGained * SkillPointsPerLevel;
            // Reset basic attributes according to level
            Strength = 20 + (_level - 1) * 2;
            Dexterity = 20 + (_level - 1) * 1;
            Vitality = 20 + (_level - 1) * 1;
            InnerStrength = 20 + (_level - 1) * 1;
        }

        /// <summary>
        /// Cộng thêm kinh nghiệm cho nhân vật. Tự động kiểm tra lên cấp.
        /// </summary>
        public void AddExp(long amount)
        {
            if (_level >= MaxPlayerLevel || amount <= 0) return;

            _currentExp += amount;
            long req = PlayerStatService.GetExpRequired(_level);

            int oldLevel = _level;
            int leveledUp = 0;

            while (_currentExp >= req && _level < MaxPlayerLevel)
            {
                _currentExp -= req;
                _level++;
                leveledUp++;

                // Cộng điểm tiềm năng & kỹ năng
                _potentialPoints += PotentialPointsPerLevel;
                _skillPoints += SkillPointsPerLevel;

                // Tự động tăng nhẹ stats cơ bản như PC
                Strength += 2;
                Dexterity += 1;
                Vitality += 1;
                InnerStrength += 1;

                req = PlayerStatService.GetExpRequired(_level);
            }

            if (_host != null) _host.OnExpChanged(_currentExp, req);
            OnExpChanged?.Invoke(_currentExp, req);

            if (leveledUp > 0)
            {
                int potentialGranted = leveledUp * PotentialPointsPerLevel;
                int skillGranted = leveledUp * SkillPointsPerLevel;
                SubsystemLog.Info("LevelService", $"Level up: {oldLevel} → {_level}");
                OnLevelUp?.Invoke(new LevelUpEvent
                {
                    oldLevel = oldLevel,
                    newLevel = _level,
                    potentialPointsGranted = potentialGranted,
                    skillPointsGranted = skillGranted
                });
                if (_host != null)
                {
                    _host.OnLevelUp(oldLevel, _level, potentialGranted, skillGranted);
                    _host.TryPlayLevelUpSfx();
                    _host.LogLevelUpNotice(oldLevel, _level);
                    _host.GrantLevelUpReward(oldLevel, _level);
                }
            }
        }

        /// <summary>
        /// Cộng thêm điểm kỹ năng (dành cho các nhiệm vụ đặc biệt như Dã Tẩu, Võ Lâm Mật Tịch).
        /// </summary>
        public void GrantSkillPoint(int count = 1)
        {
            if (count <= 0) return;
            _skillPoints += count;
            SubsystemLog.Info("LevelService", $"Granted {count} extra skill point(s). Current: {_skillPoints}");
        }

        /// <summary>
        /// Phân bổ điểm tiềm năng vào thuộc tính mong muốn.
        /// </summary>
        public bool DistributePotential(int str, int dex, int vit, int inner)
        {
            int totalNeeded = str + dex + vit + inner;
            if (totalNeeded <= 0 || _potentialPoints < totalNeeded) return false;

            Strength += str;
            Dexterity += dex;
            Vitality += vit;
            InnerStrength += inner;
            _potentialPoints -= totalNeeded;

            SubsystemLog.Info("LevelService", $"Distributed {totalNeeded} potential points: Str+{str}, Dex+{dex}, Vit+{vit}, Inner+{inner}");
            return true;
        }

        /// <summary>
        /// Reset điểm tiềm năng về mặc định của cấp độ hiện tại để phân bổ lại.
        /// </summary>
        public void ResetPotential()
        {
            int levelsGained = _level - 1;
            _potentialPoints = levelsGained * PotentialPointsPerLevel;
            Strength = 20 + levelsGained * 2;
            Dexterity = 20 + levelsGained * 1;
            Vitality = 20 + levelsGained * 1;
            InnerStrength = 20 + levelsGained * 1;
            SubsystemLog.Info("LevelService", "Reset potential points successful.");
        }

        /// <summary>
        /// Sử dụng điểm kỹ năng.
        /// </summary>
        public bool SpendSkillPoints(int count)
        {
            if (count <= 0 || _skillPoints < count) return false;
            _skillPoints -= count;
            return true;
        }

        /// <summary>
        /// Trả lại điểm kỹ năng (khi reset skill).
        /// </summary>
        public void RefundSkillPoints(int count)
        {
            if (count <= 0) return;
            _skillPoints += count;
        }
    }
}
