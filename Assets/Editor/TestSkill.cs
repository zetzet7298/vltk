/* DISABLED: SourceAssetId.assetPath not found
using UnityEditor;
using UnityEngine;
using VLTK.Sandbox;
public class TestSkill { [MenuItem("Tools/TestSkill")] public static void Run() { var go = new UnityEngine.GameObject("SandboxManager"); var sm = go.AddComponent<VLTK.Sandbox.SandboxManager>(); sm.BootstrapCombatForTests(); var skill = sm.CombatSkillCatalog.Resolve(1); Debug.Log(skill.iconSourceId.assetPath); } }
*/
