/* DISABLED: missing VLTK.Tests.Sandbox
using UnityEditor;
using UnityEngine;
using VLTK.Tests.Sandbox;
public class TestRunner { [MenuItem("Tools/TestRunner")] public static void Run() { var test = new PcSkillCatalogParityTests(); test.Setup(); test.SkillCatalog_AllSectSkills_HaveCorrectIconsFromTextFile(); test.Teardown(); Debug.Log("Manual test run finished."); } }
*/
