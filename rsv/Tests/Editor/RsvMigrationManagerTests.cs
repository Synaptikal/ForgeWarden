using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using LiveGameDev.RSV.Editor;
using UnityEngine;

namespace LiveGameDev.RSV.Tests
{
    /// <summary>
    /// Unit tests for RsvMigrationManager versioning and migration functionality.
    /// </summary>
    [TestFixture]
    public class RsvMigrationManagerTests
    {
        private DataSchemaDefinition _schema;

        [SetUp]
        public void SetUp()
        {
            _schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            _schema.SchemaId = "test-schema";
            _schema.Version = "2.0.0";
            _schema.MigrationHints = new System.Collections.Generic.List<RsvMigrationHint>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_schema != null)
                ScriptableObject.DestroyImmediate(_schema);
        }

        [Test]
        public void CompareVersions_EqualVersions_ReturnsZero()
        {
            var result = RsvMigrationManager.CompareVersions("1.0.0", "1.0.0");

            Assert.AreEqual(0, result);
        }

        [Test]
        public void CompareVersions_FirstLower_ReturnsNegative()
        {
            var result = RsvMigrationManager.CompareVersions("1.0.0", "2.0.0");

            Assert.IsTrue(result < 0);
        }

        [Test]
        public void CompareVersions_FirstHigher_ReturnsPositive()
        {
            var result = RsvMigrationManager.CompareVersions("2.0.0", "1.0.0");

            Assert.IsTrue(result > 0);
        }

        [Test]
        public void CompareVersions_PatchVersion_ReturnsCorrect()
        {
            var result = RsvMigrationManager.CompareVersions("1.0.1", "1.0.0");

            Assert.IsTrue(result > 0);
        }

        [Test]
        public void CompareVersions_MinorVersion_ReturnsCorrect()
        {
            var result = RsvMigrationManager.CompareVersions("1.1.0", "1.0.0");

            Assert.IsTrue(result > 0);
        }

        [Test]
        public void CompareVersions_MajorVersion_ReturnsCorrect()
        {
            var result = RsvMigrationManager.CompareVersions("2.0.0", "1.9.9");

            Assert.IsTrue(result > 0);
        }

        [Test]
        public void IsBreakingChange_MajorIncrement_ReturnsTrue()
        {
            var result = RsvMigrationManager.IsBreakingChange("1.0.0", "2.0.0");

            Assert.IsTrue(result);
        }

        [Test]
        public void IsBreakingChange_MinorIncrement_ReturnsFalse()
        {
            var result = RsvMigrationManager.IsBreakingChange("1.0.0", "1.1.0");

            Assert.IsFalse(result);
        }

        [Test]
        public void IsBreakingChange_PatchIncrement_ReturnsFalse()
        {
            var result = RsvMigrationManager.IsBreakingChange("1.0.0", "1.0.1");

            Assert.IsFalse(result);
        }

        [Test]
        public void GetMigrationPath_NoHints_ReturnsEmpty()
        {
            var path = RsvMigrationManager.GetMigrationPath(_schema, "1.0.0", "2.0.0");

            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void GetMigrationPath_WithHints_ReturnsCorrectHints()
        {
            _schema.MigrationHints.Add(new RsvMigrationHint("1.5.0", "Added new field"));
            _schema.MigrationHints.Add(new RsvMigrationHint("2.0.0", "Breaking change"));

            var path = RsvMigrationManager.GetMigrationPath(_schema, "1.0.0", "2.0.0");

            Assert.AreEqual(2, path.Count);
            Assert.AreEqual("1.5.0", path[0].TargetVersion);
            Assert.AreEqual("2.0.0", path[1].TargetVersion);
        }

        [Test]
        public void GetMigrationPath_OnlyRelevantHints_ReturnsFiltered()
        {
            _schema.MigrationHints.Add(new RsvMigrationHint("0.5.0", "Old change"));
            _schema.MigrationHints.Add(new RsvMigrationHint("1.5.0", "Relevant change"));
            _schema.MigrationHints.Add(new RsvMigrationHint("2.5.0", "Future change"));

            var path = RsvMigrationManager.GetMigrationPath(_schema, "1.0.0", "2.0.0");

            Assert.AreEqual(1, path.Count);
            Assert.AreEqual("1.5.0", path[0].TargetVersion);
        }

        [Test]
        public void ValidateMigrationHints_EmptyHints_ReturnsPass()
        {
            var report = RsvMigrationManager.ValidateMigrationHints(_schema);

            Assert.AreEqual(ValidationStatus.Pass, report.OverallStatus);
            Assert.AreEqual(0, report.Entries.Count);
        }

        [Test]
        public void ValidateMigrationHints_DuplicateVersions_ReturnsWarning()
        {
            _schema.MigrationHints.Add(new RsvMigrationHint("2.0.0", "First hint"));
            _schema.MigrationHints.Add(new RsvMigrationHint("2.0.0", "Second hint"));

            var report = RsvMigrationManager.ValidateMigrationHints(_schema);

            Assert.AreEqual(ValidationStatus.Warning, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "Migration" && e.Message.Contains("Multiple hints")));
        }

        [Test]
        public void ValidateMigrationHints_OutOfOrder_ReturnsWarning()
        {
            _schema.MigrationHints.Add(new RsvMigrationHint("2.0.0", "Later hint"));
            _schema.MigrationHints.Add(new RsvMigrationHint("1.5.0", "Earlier hint"));

            var report = RsvMigrationManager.ValidateMigrationHints(_schema);

            Assert.AreEqual(ValidationStatus.Warning, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "Migration" && e.Message.Contains("out of order")));
        }

        [Test]
        public void ValidateMigrationHints_EmptyTargetVersion_ReturnsWarning()
        {
            _schema.MigrationHints.Add(new RsvMigrationHint("", "Hint without version"));

            var report = RsvMigrationManager.ValidateMigrationHints(_schema);

            Assert.AreEqual(ValidationStatus.Warning, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "Migration" && e.Message.Contains("empty TargetVersion")));
        }

        [Test]
        public void GenerateMigrationReport_NoMigrations_ReturnsReport()
        {
            var report = RsvMigrationManager.GenerateMigrationReport(_schema, "1.0.0", "2.0.0");

            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("Migration Report"));
            Assert.IsTrue(report.Contains("No migration steps required"));
        }

        [Test]
        public void GenerateMigrationReport_WithMigrations_ReturnsDetailedReport()
        {
            _schema.MigrationHints.Add(new RsvMigrationHint("1.5.0", "Added new field", true));
            _schema.MigrationHints.Add(new RsvMigrationHint("2.0.0", "Breaking change", true));

            var report = RsvMigrationManager.GenerateMigrationReport(_schema, "1.0.0", "2.0.0");

            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("Migration Report"));
            Assert.IsTrue(report.Contains("**Migration Steps (2):**"));
            Assert.IsTrue(report.Contains("Version 1.5.0"));
            Assert.IsTrue(report.Contains("Version 2.0.0"));
            Assert.IsTrue(report.Contains("**Breaking Change:** ⚠️ Yes"));
        }

        [Test]
        public void GenerateMigrationReport_NonBreakingChange_ReturnsNoBreaking()
        {
            _schema.Version = "1.1.0";

            var report = RsvMigrationManager.GenerateMigrationReport(_schema, "1.0.0", "1.1.0");

            Assert.IsTrue(report.Contains("**Breaking Change:** ✅ No"));
        }

        [Test]
        public void RunMigrationScript_NoScript_ReturnsNull()
        {
            var hint = new RsvMigrationHint("2.0.0", "Test migration");

            var result = RsvMigrationManager.RunMigrationScript(hint, "{}");

            Assert.IsNull(result);
        }

        [Test]
        public void RunMigrationScript_InvalidScriptPath_ReturnsNull()
        {
            var hint = new RsvMigrationHint("2.0.0", "Test migration")
            {
                MigrationScriptPath = "Invalid/Path/To/Script.cs"
            };

            LogAssert.Expect(LogType.Error, new Regex(@"\[RSV\].*"));

            var result = RsvMigrationManager.RunMigrationScript(hint, "{}");

            Assert.IsNull(result);
        }
    }
}
