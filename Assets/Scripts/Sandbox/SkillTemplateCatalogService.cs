// -----------------------------------------------------------------------------
// VLTK Mobile — PC skilltemplate.txt catalog service.
// Source: vl_update_27 Server settings/skilltemplate.txt; 67 schema fields.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class SkillTemplateCatalogService
    {
        public const string LogTag = "SkillTemplateCatalog";
        public const string DefaultSubdir = "Reference/PcSkill";
        public const string FileName = "skilltemplate.txt";

        private readonly PcSkillTemplateCatalog _catalog;

        public SkillTemplateCatalogService(PcSkillTemplateCatalog catalog)
        {
            _catalog = catalog ?? new PcSkillTemplateCatalog();
        }

        public int Count => _catalog.Count;
        public int TotalLineCount => _catalog.totalLineCount;
        public int NonEmptyLineCount => _catalog.nonEmptyLineCount;
        public IReadOnlyList<PcSkillTemplateField> Fields => _catalog.Fields;

        public PcSkillTemplateField GetField(string fieldName) => _catalog.GetField(fieldName);

        public static SkillTemplateCatalogService LoadFromStreamingAssets(string subdir = DefaultSubdir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var catalog = PcSkillTemplateParser.BuildCatalog(dir);
            SubsystemLog.Info(LogTag,
                $"PC skilltemplate schema loaded: fields={catalog.Count}, nonEmptyLines={catalog.nonEmptyLineCount}");
            return new SkillTemplateCatalogService(catalog);
        }
    }
}
