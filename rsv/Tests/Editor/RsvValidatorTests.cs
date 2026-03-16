using System.Linq;
using NUnit.Framework;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using LiveGameDev.RSV.Editor;
using UnityEngine;

namespace LiveGameDev.RSV.Tests
{
    /// <summary>
    /// Unit tests for RsvValidator validation logic.
    /// </summary>
    [TestFixture]
    public class RsvValidatorTests
    {
        private DataSchemaDefinition _schema;

        [SetUp]
        public void SetUp()
        {
            _schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            _schema.SchemaId = "test-schema";
            _schema.Version = "1.0.0";
            _schema.RootNodes = new System.Collections.Generic.List<RsvSchemaNode>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_schema != null)
                ScriptableObject.DestroyImmediate(_schema);
        }

        [Test]
        public void Validate_NullSchema_ReturnsError()
        {
            var report = RsvValidator.Validate(null, "{}");

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "Setup"));
        }

        [Test]
        public void Validate_EmptyJson_ReturnsError()
        {
            var report = RsvValidator.Validate(_schema, "");

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "Source"));
        }

        [Test]
        public void Validate_InvalidJson_ReturnsCritical()
        {
            var report = RsvValidator.Validate(_schema, "{ invalid json }");

            Assert.AreEqual(ValidationStatus.Critical, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "ParseError"));
        }

        [Test]
        public void Validate_MissingRequiredField_ReturnsError()
        {
            // Create schema with required field
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "requiredField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                }
            });

            var json = "{}";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "MissingField"));
        }

        [Test]
        public void Validate_TypeMismatch_ReturnsError()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "numberField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.Integer,
                    IsRequired = true
                }
            });

            var json = "{ \"numberField\": \"not a number\" }";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "TypeMismatch"));
        }

        [Test]
        public void Validate_RangeViolation_ReturnsError()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "age",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.Integer,
                    IsRequired = true,
                    HasMinMax = true,
                    Min = 0,
                    Max = 120
                }
            });

            var json = "{ \"age\": 150 }";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "RangeViolation"));
        }

        [Test]
        public void Validate_EnumViolation_ReturnsError()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "status",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true,
                    EnumValues = new[] { "active", "inactive", "pending" }
                }
            });

            var json = "{ \"status\": \"deleted\" }";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "EnumViolation"));
        }

        [Test]
        public void Validate_ValidJson_ReturnsPass()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "name",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                }
            });

            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "age",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.Integer,
                    IsRequired = true,
                    HasMinMax = true,
                    Min = 0,
                    Max = 120
                }
            });

            var json = "{ \"name\": \"John\", \"age\": 30 }";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Pass, report.OverallStatus);
            Assert.AreEqual(0, report.Entries.Count);
        }

        [Test]
        public void Validate_NestedObject_ValidatesChildren()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "person",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.Object,
                    IsRequired = true
                },
                Children = new System.Collections.Generic.List<RsvSchemaNode>
                {
                    new RsvSchemaNode
                    {
                        Name = "name",
                        Constraint = new RsvFieldConstraint
                        {
                            FieldType = RsvFieldType.String,
                            IsRequired = true
                        }
                    }
                }
            });

            var json = "{ \"person\": { } }";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "MissingField" && e.Message.Contains("name")));
        }

        [Test]
        public void Validate_Array_ValidatesItems()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "items",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.Array,
                    IsRequired = true
                },
                Children = new System.Collections.Generic.List<RsvSchemaNode>
                {
                    new RsvSchemaNode
                    {
                        Name = "id",
                        Constraint = new RsvFieldConstraint
                        {
                            FieldType = RsvFieldType.Integer,
                            IsRequired = true
                        }
                    }
                }
            });

            var json = "{ \"items\": [ { \"id\": \"not a number\" } ] }";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "TypeMismatch"));
        }

        [Test]
        public void Validate_OptionalField_Missing_ReturnsPass()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "requiredField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                }
            });

            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "optionalField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = false
                }
            });

            var json = "{ \"requiredField\": \"value\" }";
            var report = RsvValidator.Validate(_schema, json);

            Assert.AreEqual(ValidationStatus.Pass, report.OverallStatus);
        }
    }
}
