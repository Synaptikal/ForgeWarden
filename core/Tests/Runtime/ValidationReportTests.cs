using NUnit.Framework;
using LiveGameDev.Core;

namespace LiveGameDev.Core.Tests
{
    public class ValidationReportTests
    {
        [Test]
        public void OverallStatus_EmptyReport_ReturnsPass()
        {
            var report = new LGD_ValidationReport("TEST");
            Assert.AreEqual(ValidationStatus.Pass, report.OverallStatus);
        }

        [Test]
        public void OverallStatus_ReturnsHighestSeverity()
        {
            var report = new LGD_ValidationReport("TEST");
            report.Add(ValidationStatus.Warning, "Cat", "msg1");
            report.Add(ValidationStatus.Error,   "Cat", "msg2");
            report.Add(ValidationStatus.Info,    "Cat", "msg3");
            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
        }

        [Test]
        public void HasErrors_ReturnsTrueWhenErrorPresent()
        {
            var report = new LGD_ValidationReport("TEST");
            report.Add(ValidationStatus.Error, "Cat", "msg");
            Assert.IsTrue(report.HasErrors);
        }

        [Test]
        public void HasCritical_ReturnsFalseWithNoCritical()
        {
            var report = new LGD_ValidationReport("TEST");
            report.Add(ValidationStatus.Error, "Cat", "msg");
            Assert.IsFalse(report.HasCritical);
        }

        [Test]
        public void ToMarkdown_ContainsToolId()
        {
            var report = new LGD_ValidationReport("RSV");
            Assert.IsTrue(report.ToMarkdown().Contains("RSV"));
        }

        [Test]
        public void ToCsv_ContainsHeader()
        {
            var report = new LGD_ValidationReport("RSV");
            Assert.IsTrue(report.ToCsv().StartsWith("Status,Category"));
        }

        [Test]
        public void EventBus_PublishAndReceive()
        {
            int callCount = 0;
            LGD_EventBus.Subscribe<Events.SchemaValidatedEvent>(_ => callCount++);
            LGD_EventBus.Publish(new Events.SchemaValidatedEvent("guid-1",
                new LGD_ValidationReport("RSV")));
            Assert.AreEqual(1, callCount);
            LGD_EventBus.Clear<Events.SchemaValidatedEvent>();
        }
    }
}
