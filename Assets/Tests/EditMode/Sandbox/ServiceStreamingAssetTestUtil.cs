using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    internal static class ServiceStreamingAssetTestUtil
    {
        public static T AssertLoadMatchesCommittedData<T>(Func<T> loader) where T : class
        {
            T service = null;
            try
            {
                service = loader();
            }
            catch (Exception e)
            {
                Assert.Fail($"{typeof(T).Name} loader threw instead of reporting loaded/missing-data state: {e.Message}");
            }
            Assert.That(service, Is.Not.Null);

            string relativePath = GetDefaultStreamingDir(typeof(T));
            bool hasData = HasCommittedData(relativePath);
            int count = GetCount(service);

            if (count > 0)
            {
                Assert.That(count, Is.GreaterThan(0), $"{typeof(T).Name} loaded committed data from {relativePath}.");
            }
            else if (count == 0)
            {
                Assert.That(count, Is.EqualTo(0),
                    hasData
                        ? $"{typeof(T).Name} found a source directory at {relativePath}, but no service-specific rows were parsed; treat it as unavailable/missing-data, not loaded."
                        : $"{typeof(T).Name} has no committed source at {relativePath} and must not pretend to be loaded.");
            }

            return service;
        }

        private static string GetDefaultStreamingDir(Type serviceType)
        {
            var field = serviceType.GetField("DefaultStreamingDir", BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null) as string;
        }

        private static int GetCount(object service)
        {
            var prop = service.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || prop.PropertyType != typeof(int)) return -1;
            return (int)prop.GetValue(service);
        }

        private static bool HasCommittedData(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (File.Exists(fullPath)) return true;
            if (!Directory.Exists(fullPath)) return false;

            foreach (var file in Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.StartsWith(".", StringComparison.Ordinal)) continue;
                return true;
            }
            return false;
        }
    }
}
