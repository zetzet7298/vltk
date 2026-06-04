using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.EditMode.Sandbox
{
    public class MinimapCoordinateMapperTests
    {
        [Test]
        public void MinimapLocalToWorld_MapsCornersAndCenterToSourceBounds()
        {
            var map = new MapDefinition
            {
                sourceBoundsRect = new RectDef
                {
                    x = 100f,
                    y = -500f,
                    width = 400f,
                    height = 300f,
                },
            };
            var minimapRect = new Rect(-64f, -32f, 128f, 64f);

            AssertWorld(new Vector2(100f, -500f), MinimapCoordinateMapper.MinimapLocalToWorld(map, new Vector2(-64f, -32f), minimapRect));
            AssertWorld(new Vector2(500f, -500f), MinimapCoordinateMapper.MinimapLocalToWorld(map, new Vector2(64f, -32f), minimapRect));
            AssertWorld(new Vector2(100f, -200f), MinimapCoordinateMapper.MinimapLocalToWorld(map, new Vector2(-64f, 32f), minimapRect));
            AssertWorld(new Vector2(500f, -200f), MinimapCoordinateMapper.MinimapLocalToWorld(map, new Vector2(64f, 32f), minimapRect));
            AssertWorld(new Vector2(300f, -350f), MinimapCoordinateMapper.MinimapLocalToWorld(map, Vector2.zero, minimapRect));
        }

        [Test]
        public void MinimapLocalToWorld_ClampsOutsideLocalRect()
        {
            var map = new MapDefinition
            {
                sourceBoundsRect = new RectDef
                {
                    x = 10f,
                    y = 20f,
                    width = 30f,
                    height = 40f,
                },
            };
            var minimapRect = new Rect(0f, 0f, 100f, 100f);

            AssertWorld(new Vector2(10f, 60f), MinimapCoordinateMapper.MinimapLocalToWorld(map, new Vector2(-25f, 125f), minimapRect));
        }

        private static void AssertWorld(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
        }
    }
}
