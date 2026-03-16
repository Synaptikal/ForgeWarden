using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LiveGameDev.ZDHG;
using LiveGameDev.ZDHG.Editor;

namespace LiveGameDev.ZDHG.Tests
{
    public class ZDHG_CoreTests
    {
        [Test]
        public void GridBuilder_WorldToGrid_ReturnsCorrectCoordinates()
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(100, 0, 100));
            float cellSize = 10f;

            Vector2Int gridPos = ZDHG_GridBuilder.WorldToGrid(new Vector3(-45, 0, -45), bounds, cellSize);
            Assert.AreEqual(0, gridPos.x);
            Assert.AreEqual(0, gridPos.y);

            gridPos = ZDHG_GridBuilder.WorldToGrid(new Vector3(45, 0, 45), bounds, cellSize);
            Assert.AreEqual(9, gridPos.x);
            Assert.AreEqual(9, gridPos.y);
        }

        [Test]
        public void GridBuilder_GridToWorld_ReturnsCenteredBounds()
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(100, 0, 100));
            float cellSize = 10f;

            Bounds cell = ZDHG_GridBuilder.GridToWorld(new Vector2Int(0, 0), bounds, cellSize);
            Assert.AreEqual(new Vector3(-45, 0, -45), cell.center);
            Assert.AreEqual(new Vector3(10, 0, 10), cell.size);
        }

        [Test]
        public void ZoneDefinition_ContainsPoint_WorksWithBounds()
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.ZoneId = "TestZone";
            zone.ZoneBounds = new Bounds(Vector3.zero, new Vector3(10, 10, 10));
            zone.UseCustomPolygon = false;

            Assert.IsTrue(zone.ContainsPoint(new Vector3(0, 0, 0)));
            Assert.IsFalse(zone.ContainsPoint(new Vector3(20, 0, 20)));
        }

        [Test]
        public void ZoneDefinition_ContainsPoint_WorksWithPolygon()
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.ZoneId = "TestZone";
            zone.UseCustomPolygon = true;
            zone.CustomPolygon = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(10, 0, 0),
                new Vector3(10, 0, 10),
                new Vector3(0, 0, 10)
            };

            Assert.IsTrue(zone.ContainsPoint(new Vector3(5, 0, 5)));
            Assert.IsFalse(zone.ContainsPoint(new Vector3(15, 0, 15)));
            Assert.IsFalse(zone.ContainsPoint(new Vector3(-5, 0, -5)));
        }

        [Test]
        public void ZoneDefinition_ContainsPoint_HandlesConcavePolygon()
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.UseCustomPolygon = true;
            // L-shaped polygon
            zone.CustomPolygon = new[]
            {
                new Vector3(0,0,0), new Vector3(10,0,0), new Vector3(10,0,10),
                new Vector3(5,0,10), new Vector3(5,0,5), new Vector3(0,0,5)
            };

            Assert.IsTrue(zone.ContainsPoint(new Vector3(2, 0, 2))); // Inner corner
            Assert.IsTrue(zone.ContainsPoint(new Vector3(8, 0, 8))); // Upper branch
            Assert.IsFalse(zone.ContainsPoint(new Vector3(2, 0, 8))); // Outside the "L" notch
        }

        [Test]
        public void ZoneDefinition_Validation_FlagsZeroBounds()
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.ZoneId = "NullZone";
            zone.UseCustomPolygon = false;
            zone.ZoneBounds = new Bounds(Vector3.zero, Vector3.zero);
            
            var report = new LiveGameDev.Core.LGD_ValidationReport("ZDHG");
            zone.Validate(report);
            Assert.IsTrue(report.Entries.Any(e => e.Message.Contains("zero-size bounds")));
        }

        [Test]
        public void ZoneDefinition_Validation_FlagsTooFewPolyPoints()
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.ZoneId = "WeakPoly";
            zone.UseCustomPolygon = true;
            zone.CustomPolygon = new[] { Vector3.zero, Vector3.one };
            
            var report = new LiveGameDev.Core.LGD_ValidationReport("ZDHG");
            zone.Validate(report);
            Assert.AreEqual(LiveGameDev.Core.ValidationStatus.Error, report.OverallStatus);
        }

        [Test]
        public void ZoneDefinition_Validation_FlagsInvalidDensityRange()
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.ZoneId = "BadDensity";
            zone.TargetDensityMin = 10f;
            zone.TargetDensityMax = 1f;
            
            var report = new LiveGameDev.Core.LGD_ValidationReport("ZDHG");
            zone.Validate(report);
            Assert.AreEqual(LiveGameDev.Core.ValidationStatus.Error, report.OverallStatus);
        }

        [Test]
        public void ZoneDefinition_Validation_FlagsMissingId()
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.ZoneId = "";
            var report = new LiveGameDev.Core.LGD_ValidationReport("ZDHG");
            zone.Validate(report);
            Assert.AreEqual(LiveGameDev.Core.ValidationStatus.Error, report.OverallStatus);
        }
    }
}
