namespace VLTK.Survivor
{
    /// <summary>
    /// Roguelike upgrade card. parity dhcd RandomSkillConfig (levelup mode).
    /// P1: 5 flat stat bumps. P2: real JX faction skills via bridge.
    /// </summary>
    public enum SkillEffectKind
    {
        Damage,       // +25%
        AttackSpeed,  // +20%
        MoveSpeed,    // +15%
        MultiShot,    // +1 projectile
        MaxHp,        // +1 max hp & heal
    }

    public readonly struct SkillCard
    {
        public readonly SkillEffectKind kind;
        public readonly string title;
        public readonly string desc;

        public SkillCard(SkillEffectKind kind, string title, string desc)
        {
            this.kind = kind;
            this.title = title;
            this.desc = desc;
        }

        public static SkillCard[] Pool => new[]
        {
            new SkillCard(SkillEffectKind.Damage,      "Sát thương",   "+25% sát thương"),
            new SkillCard(SkillEffectKind.AttackSpeed,  "Tốc độ đánh",  "+20% tốc độ đánh"),
            new SkillCard(SkillEffectKind.MoveSpeed,    "Tốc độ di chuyển", "+15% chạy"),
            new SkillCard(SkillEffectKind.MultiShot,    "Đạn kép",      "+1 đạn mỗi lần bắn"),
            new SkillCard(SkillEffectKind.MaxHp,        "Máu tối đa",   "+1 máu & hồi máu"),
        };
    }
}
