// -----------------------------------------------------------------------------
// VLTK Mobile — shared EditMode test catalog cache.
//
// Goal: avoid re-building the same SkillCatalog across hundreds of tests
//   that all do `PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog()`
//   on the same fixture. The catalog itself is immutable after construction
//   (skills are read-only dictionary entries), so we cache one instance
//   per catalog flavor and share it across all tests in the same AppDomain.
//
// Caveat: only use this from tests that DO NOT mutate the returned catalog.
//   Tests that mutate (e.g. dynamic registration, search-and-remove) must
//   call `PcCombatCatalogFactory.CreateXxxCatalog()` directly to get a fresh
//   copy.
//
// Usage:
//   var cat = TestCatalogCache.NoviceAndCaiBang;       // singleton, fast
//   var cat2 = TestCatalogCache.NoviceAndCoreSect;     // singleton, fast
// -----------------------------------------------------------------------------

using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public static class TestCatalogCache
    {
        private static SkillCatalog _noviceAndCaiBang;
        private static SkillCatalog _noviceAndCoreSect;
        private static SkillCatalog _noviceAndAllSect;

        /// <summary>Singleton PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog().</summary>
        public static SkillCatalog NoviceAndCaiBang
            => _noviceAndCaiBang ??= PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();

        /// <summary>Singleton PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog().</summary>
        public static SkillCatalog NoviceAndCoreSect
            => _noviceAndCoreSect ??= PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();

        /// <summary>Singleton CreateNoviceAndCoreSectCatalog với tất cả 10 core sects.</summary>
        public static SkillCatalog NoviceAndAllSect
            => _noviceAndAllSect ??= PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(
                includeWuDang: true, includeShaolin: true, includeTangMen: true,
                includeEMei: true, includeTianWang: true, includeWuDu: true,
                includeCuiYan: true, includeTianRen: true, includeKunLun: true);

        /// <summary>Drop cache (tests can call this in [TearDown] nếu cần fresh state).</summary>
        public static void Reset()
        {
            _noviceAndCaiBang = null;
            _noviceAndCoreSect = null;
            _noviceAndAllSect = null;
        }
    }
}
