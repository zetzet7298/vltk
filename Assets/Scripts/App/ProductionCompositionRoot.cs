using UnityEngine;
using VLTK.Production.UI.Runtime;
using VLTK.Production.World.Unity;

namespace VLTK.Production.App
{
    public sealed class ProductionComposition
    {
        public ProductionMapRenderer mapRenderer;
        public ProductionLocalAvatarController avatarController;
        public ProductionAvatarVisual avatarVisual;
        public ProductionJoystick joystick;
    }

    public static class ProductionCompositionRoot
    {
        public static ProductionComposition Create(Transform parent)
        {
            GameObject root = new GameObject("ProductionRuntime");
            if (parent != null)
                root.transform.SetParent(parent, false);

            GameObject map = new GameObject("Map53Runtime");
            map.transform.SetParent(root.transform, false);

            GameObject avatar = new GameObject("LocalAvatar");
            avatar.transform.SetParent(root.transform, false);

            GameObject joystick = new GameObject("JoystickIntent");
            joystick.transform.SetParent(root.transform, false);

            var visual = avatar.AddComponent<ProductionAvatarVisual>();
            var controller = avatar.AddComponent<ProductionLocalAvatarController>();
            controller.visual = visual;

            return new ProductionComposition
            {
                mapRenderer = map.AddComponent<ProductionMapRenderer>(),
                avatarVisual = visual,
                avatarController = controller,
                joystick = joystick.AddComponent<ProductionJoystick>()
            };
        }
    }
}
