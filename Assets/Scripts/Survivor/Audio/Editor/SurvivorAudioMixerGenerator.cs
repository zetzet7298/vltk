// -----------------------------------------------------------------------------
// VLTK.Survivor.Editor — SurvivorAudioMixerGenerator
// AudioMixer asset không tạo runtime được → editor script sinh .mixer 1 lần.
// Menu: VLTK/Survivor/Audio/Create Mixer (master/bgm/sfx)
// Output: Assets/Survivor/Audio/Survivor.mixer — 3 groups Master/BGM/SFX,
// exposed params masterVol/bgmVol/sfxVol (SurvivorAudioMgr SetVolume dùng).
// Idempotent: mixer đã tồn tại → ping, không ghi đè.
//
// Lưu ý implementation (Unity 6, AudioMixerController internal):
//  - AudioMixerController.CreateMixerControllerAtPath(path) — static, tạo asset + master group + snapshot.
//  - CreateNewGroup(name, true) — tạo group NHƯNG KHÔNG nối vào master → set children thủ công.
//  - Expose param: AudioGroupParameterPath(groupController, GUID) (AudioParameterPath abstract).
// -----------------------------------------------------------------------------

using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using VLTK.Survivor;

namespace VLTK.Survivor.Editor
{
    public static class SurvivorAudioMixerGenerator
    {
        private const string MenuPath = "VLTK/Survivor/Audio/Create Mixer (master/bgm/sfx)";
        private const string MixerFileName = "Survivor.mixer";
        private const string MixerAssetPath = "Assets/Survivor/Audio/" + MixerFileName;

        private static readonly BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static readonly System.Type ControllerType =
            typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.Audio.AudioMixerController");

        private static readonly System.Type GroupControllerType =
            typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.Audio.AudioMixerGroupController");

        private static readonly System.Type GroupPathType =
            typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");

        [MenuItem(MenuPath)]
        public static void CreateMixer()
        {
            var assetPath = SurvivorAudioBank.MixerAssetPath;
            if (File.Exists(assetPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(assetPath);
                Debug.Log($"[SurvivorAudio] Mixer đã tồn tại: {assetPath}");
                if (existing != null) Selection.activeObject = existing;
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                {
                    var parent = Path.GetDirectoryName(dir);
                    AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, Path.GetFileName(dir));
                }

                // Tạo asset controller (master group + snapshot sẵn).
                var controller = (AudioMixer)CallStatic(ControllerType, "CreateMixerControllerAtPath", assetPath);
                var master = (AudioMixerGroup)GroupController(controller, "Master");

                var bgm = (AudioMixerGroup)Call(controller, "CreateNewGroup", "BGM", true);
                var sfx = (AudioMixerGroup)Call(controller, "CreateNewGroup", "SFX", true);

                // CreateNewGroup không tự nối vào master → set children thủ công.
                var children = System.Array.CreateInstance(GroupControllerType, 2);
                children.SetValue(bgm, 0);
                children.SetValue(sfx, 1);
                GroupControllerType.GetProperty("children", Flags).SetValue(master, children, null);

                // Expose params: AudioGroupParameterPath(group, guid).
                Expose(controller, master, "Master");
                Expose(controller, bgm, "Master\\BGM");
                Expose(controller, sfx, "Master\\SFX");

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[SurvivorAudio] Đã tạo mixer: {assetPath} (groups Master/BGM/SFX, params masterVol/bgmVol/sfxVol)");
                Selection.activeObject = controller;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SurvivorAudio] Tạo mixer thất bại: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void Expose(AudioMixer controller, AudioMixerGroup group, string name)
        {
            var ctor = GroupPathType.GetConstructor(new[] { GroupControllerType, typeof(GUID) });
            var path = ctor.Invoke(new object[] { group, new GUID(System.Guid.NewGuid().ToString("N")) });
            ControllerType.GetMethod("AddExposedParameter", Flags).Invoke(controller, new[] { path });
        }

        private static AudioMixerGroup GroupController(AudioMixer controller, string name)
        {
            var all = (System.Collections.IEnumerable)ControllerType
                .GetMethod("GetAllAudioGroupsSlow", Flags).Invoke(controller, null);
            foreach (var g in all)
                if (((UnityEngine.Object)g).name == name) return (AudioMixerGroup)g;
            return null;
        }

        private static object CallStatic(System.Type type, string method, params object[] args)
        {
            return type.GetMethod(method, Flags).Invoke(null, args);
        }

        private static object Call(object target, string method, params object[] args)
        {
            return target.GetType().GetMethod(method, Flags).Invoke(target, args);
        }
    }
}
