using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class PortFactorySmokeTests
    {
        [Test]
        public void PortFactorySmokeAssembly_IsDiscoverableByUnityTestRunner()
        {
            Assert.Pass("PortFactorySmoke EditMode test assembly was discovered and executed.");
        }

        [Test]
        public void CurrentRepositoryCommit_IsAccessibleFromUnityProjectRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string commit = ResolveHeadCommit(projectRoot);

            Assert.That(commit, Does.Match("^[0-9a-f]{40}$"));
        }

        private static string ResolveHeadCommit(string projectRoot)
        {
            string gitPath = Path.Combine(projectRoot, ".git");
            string gitDir = ResolveGitDir(projectRoot, gitPath);
            string head = File.ReadAllText(Path.Combine(gitDir, "HEAD")).Trim();

            if (!head.StartsWith("ref: "))
            {
                return head;
            }

            string relativeRef = head.Substring("ref: ".Length).Trim();
            string looseRefPath = Path.Combine(gitDir, relativeRef.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(looseRefPath))
            {
                return File.ReadAllText(looseRefPath).Trim();
            }

            string commonDir = ResolveCommonGitDir(gitDir);
            string commonRefPath = Path.Combine(commonDir, relativeRef.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(commonRefPath))
            {
                return File.ReadAllText(commonRefPath).Trim();
            }

            string packedRefsPath = Path.Combine(commonDir, "packed-refs");
            if (File.Exists(packedRefsPath))
            {
                foreach (string line in File.ReadAllLines(packedRefsPath))
                {
                    if (line.StartsWith("#") || line.StartsWith("^") || line.Length < 41)
                    {
                        continue;
                    }

                    string[] parts = line.Split(' ');
                    if (parts.Length == 2 && parts[1] == relativeRef && Regex.IsMatch(parts[0], "^[0-9a-f]{40}$"))
                    {
                        return parts[0];
                    }
                }
            }

            Assert.Fail("Unable to resolve HEAD commit for ref " + relativeRef);
            return string.Empty;
        }

        private static string ResolveGitDir(string projectRoot, string gitPath)
        {
            if (Directory.Exists(gitPath))
            {
                return gitPath;
            }

            string gitFile = File.ReadAllText(gitPath).Trim();
            const string prefix = "gitdir: ";
            Assert.That(gitFile, Does.StartWith(prefix));

            string rawPath = gitFile.Substring(prefix.Length).Trim();
            return Path.GetFullPath(Path.Combine(projectRoot, rawPath));
        }

        private static string ResolveCommonGitDir(string gitDir)
        {
            string commonDirPath = Path.Combine(gitDir, "commondir");
            if (!File.Exists(commonDirPath))
            {
                return gitDir;
            }

            string rawPath = File.ReadAllText(commonDirPath).Trim();
            return Path.GetFullPath(Path.Combine(gitDir, rawPath));
        }
    }
}
