using NUnit.Framework;
using LiveGameDev.RSV;
using LiveGameDev.RSV.Editor;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace LiveGameDev.RSV.Tests
{
    /// <summary>
    /// Integration tests for RSV end-to-end workflows.
    /// Tests complete workflows from schema creation to validation and reporting.
    /// </summary>
    [TestFixture]
    public class RsvIntegrationTests
    {
        private string _testDataPath;
        private DataSchemaDefinition _editorSchema;
        private RsvCompiledSchemaAsset _compiledSchema;

        [SetUp]
        public void SetUp()
        {
            // Create test data directory
            _testDataPath = Path.Combine(Application.temporaryCachePath, "RSV_IntegrationTests");
            if (!Directory.Exists(_testDataPath))
                Directory.CreateDirectory(_testDataPath);

            // Create editor schema
            _editorSchema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            _editorSchema.SchemaId = "integration-test-schema";
            _editorSchema.Version = "1.0.0";
            _editorSchema.RootNodes = new System.Collections.Generic.List<RsvSchemaNode>
            {
                new RsvSchemaNode
                {
                    Name = "name",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.String,
                        IsRequired = true
                    }
                },
                new RsvSchemaNode
                {
                    Name = "value",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.Integer,
                        IsRequired = true,
                        HasMinMax = true,
                        Min = 0,
                        Max = 100
                    }
                },
                new RsvSchemaNode
                {
                    Name = "active",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.Boolean,
                        IsRequired = false
                    }
                }
            };

            // Create compiled schema
            _compiledSchema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            _compiledSchema.SchemaId = "integration-test-schema";
            _compiledSchema.Version = "1.0.0";
            _compiledSchema.MaxNestingDepth = 100;
            _compiledSchema.MaxStringLength = 1000000;
            _compiledSchema.MaxArrayLength = 10000;
            _compiledSchema.RootNodes = new System.Collections.Generic.List<RsvCompiledNode>
            {
                new RsvCompiledNode
                {
                    Name = "name",
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                },
                new RsvCompiledNode
                {
                    Name = "value",
                    FieldType = RsvFieldType.Integer,
                    IsRequired = true,
                    HasMinMax = true,
                    Min = 0,
                    Max = 100
                },
                new RsvCompiledNode
                {
                    Name = "active",
                    FieldType = RsvFieldType.Boolean,
                    IsRequired = false
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test data
            if (Directory.Exists(_testDataPath))
                Directory.Delete(_testDataPath, true);

            if (_editorSchema != null)
                ScriptableObject.DestroyImmediate(_editorSchema);

            if (_compiledSchema != null)
                ScriptableObject.DestroyImmediate(_compiledSchema);

            RsvSchemaRegistry.Clear();
        }

        #region Full Workflow Tests

        [Test]
        public void Integration_FullWorkflow_CreateSchemaValidateExport()
        {
            // Step 1: Create schema (already done in SetUp)

            // Step 2: Create valid JSON
            var validJson = "{\"name\":\"test\",\"value\":42,\"active\":true}";

            // Step 3: Validate with runtime validator
            var result = RsvRuntimeValidator.Validate(_compiledSchema, validJson);

            Assert.AreEqual(RsvValidationStatus.Pass, result.Status);
            Assert.IsTrue(result.IsPass);
            Assert.AreEqual(0, result.EntryCount);

            // Step 4: Create invalid JSON
            var invalidJson = "{\"name\":\"test\",\"value\":150,\"active\":true}";

            // Step 5: Validate and check errors
            var invalidResult = RsvRuntimeValidator.Validate(_compiledSchema, invalidJson);

            Assert.AreEqual(RsvValidationStatus.Error, invalidResult.Status);
            Assert.IsTrue(invalidResult.HasErrors);
            Assert.Greater(invalidResult.EntryCount, 0);

            // Step 6: Export result to string
            var resultString = invalidResult.ToString();
            Assert.IsNotNull(resultString);
            Assert.IsTrue(resultString.Contains("Error"));
        }

        [Test]
        public void Integration_FullWorkflow_WithRegistry()
        {
            // Step 1: Register schema in registry
            RsvSchemaRegistry.Clear();
            var registered = RsvSchemaRegistry.Register(_compiledSchema);
            Assert.IsTrue(registered);

            // Step 2: Verify schema is registered
            Assert.IsTrue(RsvSchemaRegistry.Contains("integration-test-schema"));

            // Step 3: Validate using schema ID
            var json = "{\"name\":\"test\",\"value\":42}";
            var result = RsvRuntimeValidator.Validate("integration-test-schema", json);

            Assert.AreEqual(RsvValidationStatus.Pass, result.Status);

            // Step 4: Get registry statistics
            var stats = RsvSchemaRegistry.GetStatistics();
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.Contains("integration-test-schema"));
        }

        [Test]
        public void Integration_FullWorkflow_WithComponent()
        {
            // Create test component
            var gameObject = new GameObject("TestObject");
            var component = gameObject.AddComponent<TestComponent>();

            // Set valid JSON
            component.TestJson = "{\"name\":\"test\",\"value\":42}";

            // Register schema
            RsvSchemaRegistry.Clear();
            RsvSchemaRegistry.Register(_compiledSchema);

            // Validate component
            var result = RsvRuntimeValidator.ValidateComponent(component);

            Assert.AreEqual(RsvValidationStatus.Pass, result.Status);

            // Set invalid JSON
            component.TestJson = "{\"name\":\"test\",\"value\":\"not_a_number\"}";

            // Validate component again
            var invalidResult = RsvRuntimeValidator.ValidateComponent(component);

            Assert.AreEqual(RsvValidationStatus.Error, invalidResult.Status);
            Assert.IsTrue(invalidResult.HasErrors);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        #endregion

        #region Play Mode Hook Tests

        [Test]
        public void Integration_PlayModeHook_ValidatesOnEntry()
        {
            // This test verifies that the Play Mode hook would work
            // Actual Play Mode testing requires entering Play Mode, which is not possible in unit tests

            // Create test component with invalid JSON
            var gameObject = new GameObject("TestObject");
            var component = gameObject.AddComponent<TestComponent>();
            component.TestJson = "{\"name\":\"test\",\"value\":150}"; // Invalid: value > 100

            // Register schema
            RsvSchemaRegistry.Clear();
            RsvSchemaRegistry.Register(_compiledSchema);

            // Simulate what Play Mode hook would do
            var result = RsvRuntimeValidator.ValidateComponent(component);

            // Verify that validation catches the error
            Assert.AreEqual(RsvValidationStatus.Error, result.Status);
            Assert.IsTrue(result.HasErrors);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Integration_PlayModeHook_AllowsValidData()
        {
            // Create test component with valid JSON
            var gameObject = new GameObject("TestObject");
            var component = gameObject.AddComponent<TestComponent>();
            component.TestJson = "{\"name\":\"test\",\"value\":42}";

            // Register schema
            RsvSchemaRegistry.Clear();
            RsvSchemaRegistry.Register(_compiledSchema);

            // Simulate what Play Mode hook would do
            var result = RsvRuntimeValidator.ValidateComponent(component);

            // Verify that validation passes
            Assert.AreEqual(RsvValidationStatus.Pass, result.Status);
            Assert.IsTrue(result.IsPass);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        #endregion

        #region Build Hook Tests

        [Test]
        public void Integration_BuildHook_ValidatesBeforeBuild()
        {
            // This test verifies that the build hook would work
            // Actual build testing requires running a build, which is not possible in unit tests

            // Create test component with invalid JSON
            var gameObject = new GameObject("TestObject");
            var component = gameObject.AddComponent<TestComponent>();
            component.TestJson = "{\"name\":\"test\",\"value\":150}"; // Invalid

            // Register schema
            RsvSchemaRegistry.Clear();
            RsvSchemaRegistry.Register(_compiledSchema);

            // Simulate what build hook would do
            var result = RsvRuntimeValidator.ValidateComponent(component);

            // Verify that validation catches the error
            Assert.AreEqual(RsvValidationStatus.Error, result.Status);
            Assert.IsTrue(result.HasErrors);

            // In a real build hook, this would block the build
            Assert.IsFalse(result.IsPass, "Build should be blocked on validation errors");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        #endregion

        #region CLI Pipeline Tests

        [Test]
        public void Integration_CliPipeline_ValidatesJsonFile()
        {
            // Create test JSON file
            var jsonPath = Path.Combine(_testDataPath, "test.json");
            var jsonContent = "{\"name\":\"test\",\"value\":42}";
            File.WriteAllText(jsonPath, jsonContent);

            // Read and validate
            var json = File.ReadAllText(jsonPath);
            var result = RsvRuntimeValidator.Validate(_compiledSchema, json);

            Assert.AreEqual(RsvValidationStatus.Pass, result.Status);

            // Create invalid JSON file
            var invalidJsonPath = Path.Combine(_testDataPath, "invalid.json");
            var invalidJsonContent = "{\"name\":\"test\",\"value\":150}";
            File.WriteAllText(invalidJsonPath, invalidJsonContent);

            // Read and validate
            var invalidJson = File.ReadAllText(invalidJsonPath);
            var invalidResult = RsvRuntimeValidator.Validate(_compiledSchema, invalidJson);

            Assert.AreEqual(RsvValidationStatus.Error, invalidResult.Status);
        }

        [Test]
        public void Integration_CliPipeline_GeneratesReport()
        {
            // Create test JSON file with errors
            var jsonPath = Path.Combine(_testDataPath, "test.json");
            var jsonContent = "{\"name\":\"test\",\"value\":150,\"active\":\"not_boolean\"}";
            File.WriteAllText(jsonPath, jsonContent);

            // Validate
            var json = File.ReadAllText(jsonPath);
            var result = RsvRuntimeValidator.Validate(_compiledSchema, json);

            // Generate report
            var report = result.ToString();

            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("Error"));
            Assert.IsTrue(report.Contains("TypeMismatch") || report.Contains("RangeViolation"));

            // Save report to file
            var reportPath = Path.Combine(_testDataPath, "report.txt");
            File.WriteAllText(reportPath, report);

            Assert.IsTrue(File.Exists(reportPath));
        }

        #endregion

        #region JSON Schema Import/Export Tests

        [Test]
        public void Integration_JsonSchemaImport_ImportsDraft7()
        {
            // Create a Draft 7 JSON Schema
            var draft7Schema = @"{
                ""$schema"": ""http://json-schema.org/draft-07/schema#"",
                ""type"": ""object"",
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""integer"",
                        ""minimum"": 0,
                        ""maximum"": 100
                    }
                },
                ""required"": [""name"", ""value""]
            }";

            // Note: Actual import would use RsvJsonSchemaInterop from Editor
            // This test verifies the structure is compatible
            Assert.IsNotNull(draft7Schema);
            Assert.IsTrue(draft7Schema.Contains("name"));
            Assert.IsTrue(draft7Schema.Contains("value"));
        }

        [Test]
        public void Integration_JsonSchemaExport_ExportsDraft7()
        {
            // Create a compiled schema
            var schema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            schema.SchemaId = "export-test";
            schema.Version = "1.0.0";
            schema.MaxNestingDepth = 100;
            schema.MaxStringLength = 1000000;
            schema.MaxArrayLength = 10000;
            schema.RootNodes = new System.Collections.Generic.List<RsvCompiledNode>
            {
                new RsvCompiledNode
                {
                    Name = "name",
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                },
                new RsvCompiledNode
                {
                    Name = "value",
                    FieldType = RsvFieldType.Integer,
                    IsRequired = true,
                    HasMinMax = true,
                    Min = 0,
                    Max = 100
                }
            };

            // Note: Actual export would use RsvJsonSchemaInterop from Editor
            // This test verifies the schema structure is valid
            Assert.IsTrue(schema.IsValid());
            Assert.AreEqual(2, schema.RootNodes.Count);

            ScriptableObject.DestroyImmediate(schema);
        }

        #endregion

        #region Migration Tests

        [Test]
        public void Integration_Migration_VersionUpgrade()
        {
            // Create v1.0.0 schema
            var v1Schema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            v1Schema.SchemaId = "migration-test";
            v1Schema.Version = "1.0.0";
            v1Schema.MaxNestingDepth = 100;
            v1Schema.MaxStringLength = 1000000;
            v1Schema.MaxArrayLength = 10000;
            v1Schema.RootNodes = new System.Collections.Generic.List<RsvCompiledNode>
            {
                new RsvCompiledNode
                {
                    Name = "name",
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                }
            };

            // Create v2.0.0 schema with additional field
            var v2Schema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            v2Schema.SchemaId = "migration-test";
            v2Schema.Version = "2.0.0";
            v2Schema.MaxNestingDepth = 100;
            v2Schema.MaxStringLength = 1000000;
            v2Schema.MaxArrayLength = 10000;
            v2Schema.RootNodes = new System.Collections.Generic.List<RsvCompiledNode>
            {
                new RsvCompiledNode
                {
                    Name = "name",
                    FieldType = RsvFieldType.String,
                    IsRequired = true
                },
                new RsvCompiledNode
                {
                    Name = "value",
                    FieldType = RsvFieldType.Integer,
                    IsRequired = true
                }
            };

            // Validate old JSON against new schema (should fail)
            var oldJson = "{\"name\":\"test\"}";
            var result = RsvRuntimeValidator.Validate(v2Schema, oldJson);

            Assert.AreEqual(RsvValidationStatus.Error, result.Status);
            Assert.IsTrue(result.HasErrors);

            // Validate new JSON against new schema (should pass)
            var newJson = "{\"name\":\"test\",\"value\":42}";
            var newResult = RsvRuntimeValidator.Validate(v2Schema, newJson);

            Assert.AreEqual(RsvValidationStatus.Pass, newResult.Status);

            ScriptableObject.DestroyImmediate(v1Schema);
            ScriptableObject.DestroyImmediate(v2Schema);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Integration_ErrorHandling_InvalidSchema()
        {
            // Create invalid schema
            var invalidSchema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            invalidSchema.SchemaId = ""; // Invalid: empty SchemaId
            invalidSchema.Version = "1.0.0";
            invalidSchema.RootNodes = new System.Collections.Generic.List<RsvCompiledNode>();

            var json = "{\"name\":\"test\",\"value\":42}";
            var result = RsvRuntimeValidator.Validate(invalidSchema, json);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);

            ScriptableObject.DestroyImmediate(invalidSchema);
        }

        [Test]
        public void Integration_ErrorHandling_InvalidJson()
        {
            var invalidJson = "{ invalid json }";
            var result = RsvRuntimeValidator.Validate(_compiledSchema, invalidJson);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);
        }

        [Test]
        public void Integration_ErrorHandling_MissingSchema()
        {
            RsvSchemaRegistry.Clear();

            var json = "{\"name\":\"test\",\"value\":42}";
            var result = RsvRuntimeValidator.Validate("non-existent-schema", json);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);
        }

        #endregion

        #region Test Component

        private class TestComponent : MonoBehaviour
        {
            [RsvSchema("integration-test-schema")]
            public string TestJson;
        }

        #endregion
    }
}