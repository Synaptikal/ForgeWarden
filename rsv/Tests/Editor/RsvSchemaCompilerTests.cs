using System.Linq;
using NUnit.Framework;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using LiveGameDev.RSV.Editor;
using UnityEngine;

namespace LiveGameDev.RSV.Tests
{
    /// <summary>
    /// Unit tests for RsvSchemaCompiler schema compilation and evaluation.
    /// </summary>
    [TestFixture]
    public class RsvSchemaCompilerTests
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
        public void Compile_ValidSchema_ReturnsCompiledSchema()
        {
            _schema.RootNodes.Add(new RsvSchemaNode
            {
                Name = "testField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                }
            });

            var compiled = RsvSchemaCompiler.Compile(_schema);

            Assert.IsNotNull(compiled);
            Assert.IsNotNull(compiled.Nodes);
            Assert.AreEqual(1, compiled.Nodes.Count);
        }

        [Test]
        public void Compile_EmptySchema_ReturnsEmptyCompiledSchema()
        {
            var compiled = RsvSchemaCompiler.Compile(_schema);

            Assert.IsNotNull(compiled);
            Assert.IsNotNull(compiled.Nodes);
            Assert.AreEqual(0, compiled.Nodes.Count);
        }

        [Test]
        public void EvaluateNode_NullToken_RequiredField_AddsError()
        {
            var node = new RsvSchemaNode
            {
                Name = "requiredField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                }
            };

            var report = new LGD_ValidationReport("RSV");
            RsvSchemaCompiler.EvaluateNode(null, node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "MissingField"));
        }

        [Test]
        public void EvaluateNode_NullToken_OptionalField_DoesNotAddError()
        {
            var node = new RsvSchemaNode
            {
                Name = "optionalField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = false
                }
            };

            var report = new LGD_ValidationReport("RSV");
            RsvSchemaCompiler.EvaluateNode(null, node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Pass, report.OverallStatus);
            Assert.AreEqual(0, report.Entries.Count);
        }

        [Test]
        public void EvaluateNode_TypeMismatch_AddsError()
        {
            var node = new RsvSchemaNode
            {
                Name = "numberField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.Integer,
                    IsRequired = true
                }
            };

            var report = new LGD_ValidationReport("RSV");
            var token = Newtonsoft.Json.Linq.JToken.Parse("{ \"numberField\": \"string\" }");
            RsvSchemaCompiler.EvaluateNode(token["numberField"], node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "TypeMismatch"));
        }

        [Test]
        public void EvaluateNode_RangeViolation_AddsError()
        {
            var node = new RsvSchemaNode
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
            };

            var report = new LGD_ValidationReport("RSV");
            var token = Newtonsoft.Json.Linq.JToken.Parse("{ \"age\": 150 }");
            RsvSchemaCompiler.EvaluateNode(token["age"], node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "RangeViolation"));
        }

        [Test]
        public void EvaluateNode_EnumViolation_AddsError()
        {
            var node = new RsvSchemaNode
            {
                Name = "status",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true,
                    EnumValues = new[] { "active", "inactive" }
                }
            };

            var report = new LGD_ValidationReport("RSV");
            var token = Newtonsoft.Json.Linq.JToken.Parse("{ \"status\": \"deleted\" }");
            RsvSchemaCompiler.EvaluateNode(token["status"], node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "EnumViolation"));
        }

        [Test]
        public void EvaluateNode_ValidValue_DoesNotAddError()
        {
            var node = new RsvSchemaNode
            {
                Name = "name",
                Constraint = new RsvFieldConstraint
                {
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                }
            };

            var report = new LGD_ValidationReport("RSV");
            var token = Newtonsoft.Json.Linq.JToken.Parse("{ \"name\": \"John\" }");
            RsvSchemaCompiler.EvaluateNode(token["name"], node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Pass, report.OverallStatus);
            Assert.AreEqual(0, report.Entries.Count);
        }

        [Test]
        public void EvaluateNode_NestedObject_ValidatesChildren()
        {
            var node = new RsvSchemaNode
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
            };

            var report = new LGD_ValidationReport("RSV");
            var token = Newtonsoft.Json.Linq.JToken.Parse("{ \"person\": { } }");
            RsvSchemaCompiler.EvaluateNode(token["person"], node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "MissingField"));
        }

        [Test]
        public void EvaluateNode_Array_ValidatesItems()
        {
            var node = new RsvSchemaNode
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
            };

            var report = new LGD_ValidationReport("RSV");
            var token = Newtonsoft.Json.Linq.JToken.Parse("{ \"items\": [ { \"id\": \"not a number\" } ] }");
            RsvSchemaCompiler.EvaluateNode(token["items"], node, "", report, depth: 0);

            Assert.AreEqual(ValidationStatus.Error, report.OverallStatus);
            Assert.IsTrue(report.Entries.Any(e => e.Category == "TypeMismatch"));
        }
    }
}
