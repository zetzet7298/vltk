// -----------------------------------------------------------------------------
// VLTK Mobile — Compound Recipe Service focused tests
// PC source: settings/item/atlas_compound.txt + script/item/compound/{atlas.lua,compound_header.lua}
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class AtlasCompoundCraftPlanServiceTests
    {
        private static string AtlasPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemFull/atlas_compound.txt");

        [Test]
        public void ParseAtlasCompound_LoadsExactPcRowsAndFirstRowShape()
        {
            var reg = PcRecipeParser.BuildAtlasCompoundRegistry(AtlasPath);

            Assert.AreEqual(1294, reg.Count, "PC atlas_compound.txt has 1,294 data rows");
            var matches = reg.GetByAtlas(6, 1, 239, 0);
            Assert.AreEqual(1, matches.Count);
            var recipe = matches[0];
            Assert.AreEqual(6, recipe.materials.Count);
            Assert.AreEqual(6, recipe.materials[0].genre);
            Assert.AreEqual(1, recipe.materials[0].detailType);
            Assert.AreEqual(200, recipe.materials[0].particular);
            Assert.AreEqual(8, recipe.materials[0].level);
            Assert.AreEqual(-1, recipe.materials[0].series);
            Assert.AreEqual(85, recipe.materials[0].magicId);
            Assert.AreEqual(1, recipe.result.quality);
            Assert.AreEqual(0, recipe.result.genre);
            Assert.AreEqual(0, recipe.result.detailType, "PC atlas.lua subtracts 1 from DES_DETAILTYPE when DES_QUALITY == 1");
            Assert.AreEqual(-1, recipe.result.particular);
            Assert.AreEqual("ATLAS", recipe.result.compoundParam);
        }

        [Test]
        public void BuildPlan_RequiresAtlasNosignXuanjingAndExactMaterials()
        {
            var reg = new PcAtlasCompoundRegistry();
            reg.Register(new PcAtlasCompoundRecipe
            {
                atlas = new PcAtlasCompoundItemSpec { genre = 6, detailType = 1, particular = 239 },
                atlasNoSign = 1,
                result = new PcAtlasCompoundResultSpec { quality = 1, genre = 0, detailType = 0, particular = 1, itemValue = 1000, compoundParam = "ATLAS" },
                materials = new List<PcAtlasCompoundMaterialSpec>
                {
                    new PcAtlasCompoundMaterialSpec { genre = 6, detailType = 1, particular = 200, level = 8, series = -1, magicId = 85 },
                    new PcAtlasCompoundMaterialSpec { genre = 6, detailType = 1, particular = 26, level = -1, series = -1, magicId = -1 },
                }
            });
            var svc = new CompoundRecipeService(reg);

            var missing = svc.BuildAtlasCraftPlan(
                new[] { new PcAtlasCompoundSourceItem(6, 1, 239, 0, 0, 0, 1, 500) },
                new[] { PcAtlasCompoundSourceItem.NosignAtlasPiece(1) });
            Assert.AreEqual(CompoundPlanStatus.LackResource, missing.status);

            var valid = svc.BuildAtlasCraftPlan(
                new[]
                {
                    new PcAtlasCompoundSourceItem(6, 1, 239, 0, 0, 0, 1, 500),
                    new PcAtlasCompoundSourceItem(6, 1, 147, 0, 0, 0, 0, 100),
                    new PcAtlasCompoundSourceItem(6, 1, 200, 8, 0, 85, 0, 600),
                    new PcAtlasCompoundSourceItem(6, 1, 26, 0, 0, 0, 0, 200),
                },
                new[] { PcAtlasCompoundSourceItem.NosignAtlasPiece(1, value: 50) });

            Assert.AreEqual(CompoundPlanStatus.Ready, valid.status);
            Assert.AreEqual(100000, valid.costSilver);
            Assert.AreEqual(1450, valid.sourceItemValueSum);
            Assert.AreEqual(1000, valid.destinationItemValue);
            Assert.AreEqual(1f, valid.successProbability, 0.0001f);
            Assert.AreEqual(5, valid.operations.Count);
            Assert.AreEqual("Pay", valid.operations[0].name);
            Assert.AreEqual("WriteCompoundLog", valid.operations[1].name);
            Assert.AreEqual("RemoveNecessaryItems", valid.operations[2].name);
            Assert.AreEqual("RemoveAlternativeItems", valid.operations[3].name);
            Assert.AreEqual("AddItemEx", valid.operations[4].name);
        }

        [Test]
        public void ExecutePlan_UsesPcRandomThresholdAndHostOps()
        {
            var reg = new PcAtlasCompoundRegistry();
            reg.Register(new PcAtlasCompoundRecipe
            {
                atlas = new PcAtlasCompoundItemSpec { genre = 6, detailType = 1, particular = 239 },
                atlasNoSign = 1,
                result = new PcAtlasCompoundResultSpec { quality = 0, genre = 4, detailType = 2044, particular = 1, level = 0, series = 0, piece = 9, pieceSum = 9, itemValue = 1000, compoundParam = "ATLAS" },
                materials = new List<PcAtlasCompoundMaterialSpec> { new PcAtlasCompoundMaterialSpec { genre = 6, detailType = 1, particular = 26, level = -1, series = -1, magicId = -1 } }
            });
            var svc = new CompoundRecipeService(reg);
            var plan = svc.BuildAtlasCraftPlan(
                new[]
                {
                    new PcAtlasCompoundSourceItem(6, 1, 239, 0, 0, 0, 1, 400),
                    new PcAtlasCompoundSourceItem(6, 1, 147, 0, 0, 0, 0, 100),
                    new PcAtlasCompoundSourceItem(6, 1, 26, 0, 0, 0, 0, 200),
                },
                new[] { PcAtlasCompoundSourceItem.NosignAtlasPiece(1, value: 0) });
            Assert.AreEqual(0.7f, plan.successProbability, 0.0001f);

            var fail = svc.ExecuteAtlasCraftPlan(plan, randomRollInclusive0To1: 0.8f);
            Assert.AreEqual(CompoundPlanStatus.FailedByRng, fail.status);
            CollectionAssert.AreEqual(new[] { "Pay", "WriteCompoundLog", "RemoveNecessaryItems", "RemoveAlternativeItems" }, fail.executedOperationNames);
            Assert.AreEqual(0, fail.resultItemIndex);

            var success = svc.ExecuteAtlasCraftPlan(plan, randomRollInclusive0To1: 0.7f, addedItemIndex: 321);
            Assert.AreEqual(CompoundPlanStatus.Succeeded, success.status);
            CollectionAssert.AreEqual(new[] { "Pay", "WriteCompoundLog", "RemoveNecessaryItems", "RemoveAlternativeItems", "AddItemEx" }, success.executedOperationNames);
            Assert.AreEqual(321, success.resultItemIndex);
        }
    }
}
