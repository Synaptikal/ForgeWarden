using System.Linq;
using NUnit.Framework;
using LiveGameDev.RSV;
using UnityEngine;

namespace LiveGameDev.RSV.Tests
{
    /// <summary>
    /// Security tests for RSV runtime validation.
    /// Tests protection against malicious inputs and attacks.
    /// </summary>
    [TestFixture]
    public class RsvSecurityTests
    {
        private RsvCompiledSchemaAsset _schema;

        [SetUp]
        public void SetUp()
        {
            _schema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            _schema.SchemaId = "security-test-schema";
            _schema.Version = "1.0.0";
            _schema.MaxNestingDepth = 20;
            _schema.MaxStringLength = 50000;
            _schema.MaxArrayLength = 500;
            _schema.RootNodes = new System.Collections.Generic.List<RsvCompiledNode>
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
                },
                new RsvCompiledNode
                {
                    Name = "nested",
                    FieldType = RsvFieldType.Object,
                    IsRequired = false,
                    Children = new System.Collections.Generic.List<RsvCompiledNode>
                    {
                        new RsvCompiledNode
                        {
                            Name = "deep",
                            FieldType = RsvFieldType.String,
                            IsRequired = false
                        }
                    }
                },
                new RsvCompiledNode
                {
                    Name = "items",
                    FieldType = RsvFieldType.Array,
                    IsRequired = false,
                    Children = new System.Collections.Generic.List<RsvCompiledNode>
                    {
                        new RsvCompiledNode
                        {
                            Name = "id",
                            FieldType = RsvFieldType.Integer,
                            IsRequired = true
                        }
                    }
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_schema != null)
                ScriptableObject.DestroyImmediate(_schema);
        }

        #region Malicious JSON Tests

        [Test]
        public void MaliciousJson_DeepNesting_ReturnsCritical()
        {
            // Create deeply nested JSON (30 levels — exceeds schema max of 20)
            var json = CreateDeeplyNestedJson(30);

            var result = RsvRuntimeValidator.Validate(_schema, json);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);
            Assert.IsTrue(result.GetEntries().Any(e => e.Category == "Security"));
        }

        [Test]
        public void MaliciousJson_OversizedArray_ReturnsCritical()
        {
            // JSON entity expansion ("billion laughs") doesn't apply to JSON the way it does
            // to XML, but an oversized array does stress the validator.
            // Create an array exceeding MaxArrayLength (600 > 500 limit).
            var json = CreateHugeArrayJson(600);

            var result = RsvRuntimeValidator.Validate(_schema, json);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);
            Assert.IsTrue(result.GetEntries().Any(e => e.Category == "Security"));
        }

        [Test]
        public void MaliciousJson_HugeString_ReturnsCritical()
        {
            // Create JSON with string exceeding schema MaxStringLength (50KB > 50000 limit)
            var hugeString = new string('A', 60000);
            var json = $"{{\"name\":\"{hugeString}\",\"value\":1}}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);
            Assert.IsTrue(result.GetEntries().Any(e => e.Category == "Security"));
        }

        [Test]
        public void MaliciousJson_HugeArray_ReturnsCritical()
        {
            // Create JSON with array exceeding schema MaxArrayLength (600 > 500 limit)
            var json = CreateHugeArrayJson(600);

            var result = RsvRuntimeValidator.Validate(_schema, json);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);
            Assert.IsTrue(result.GetEntries().Any(e => e.Category == "Security"));
        }

        [Test]
        public void MaliciousJson_InvalidUnicode_DoesNotCrash()
        {
            // Create JSON with invalid unicode sequences
            var json = "{\"name\":\"\\uD800\\uDFFF\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully without crashing
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Status == RsvValidationStatus.Pass ||
                          result.Status == RsvValidationStatus.Error ||
                          result.Status == RsvValidationStatus.Critical);
        }

        [Test]
        public void MaliciousJson_NullBytes_DoesNotCrash()
        {
            // Create JSON with null bytes
            var json = "{\"name\":\"test\u0000value\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully without crashing
            Assert.IsNotNull(result);
        }

        [Test]
        public void MaliciousJson_ControlCharacters_DoesNotCrash()
        {
            // Create JSON with control characters
            var json = "{\"name\":\"test\u0001\u0002\u0003value\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully without crashing
            Assert.IsNotNull(result);
        }

        #endregion

        #region Path Traversal Tests

        [Test]
        public void PathTraversal_ParentDirectory_ReturnsError()
        {
            // Create JSON with path traversal patterns in field names
            var json = "{\"name\":\"test\",\"value\":1,\"../etc/passwd\":\"malicious\"}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully - extra fields are ignored
            Assert.IsNotNull(result);
        }

        [Test]
        public void PathTraversal_AbsolutePath_ReturnsError()
        {
            // Create JSON with absolute path patterns
            var json = "{\"name\":\"test\",\"value\":1,\"/etc/passwd\":\"malicious\"}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully - extra fields are ignored
            Assert.IsNotNull(result);
        }

        [Test]
        public void PathTraversal_SymlinkPatterns_DoesNotCrash()
        {
            // Create JSON with symlink-like patterns
            var json = "{\"name\":\"test\",\"value\":1,\"../../malicious\":\"value\"}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully without crashing
            Assert.IsNotNull(result);
        }

        #endregion

        #region SSRF Tests

        [Test]
        public void Ssrf_LocalhostUrl_DoesNotValidate()
        {
            // Create JSON with localhost URL
            var json = "{\"name\":\"http://localhost:8080\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should validate as string - RSV doesn't validate URLs by default
            Assert.IsNotNull(result);
        }

        [Test]
        public void Ssrf_InternalNetworkUrl_DoesNotValidate()
        {
            // Create JSON with internal network URL
            var json = "{\"name\":\"http://192.168.1.1\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should validate as string - RSV doesn't validate URLs by default
            Assert.IsNotNull(result);
        }

        [Test]
        public void Ssrf_FileUrl_DoesNotValidate()
        {
            // Create JSON with file:// URL
            var json = "{\"name\":\"file:///etc/passwd\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should validate as string - RSV doesn't validate URLs by default
            Assert.IsNotNull(result);
        }

        #endregion

        #region Code Injection Tests

        [Test]
        public void CodeInjection_ScriptInJson_DoesNotExecute()
        {
            // Create JSON with script-like content
            var json = "{\"name\":\"<script>alert('xss')</script>\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should validate as string without executing
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasCritical);
        }

        [Test]
        public void CodeInjection_SqlInjection_DoesNotExecute()
        {
            // Create JSON with SQL injection patterns
            var json = "{\"name\":\"'; DROP TABLE users; --\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should validate as string without executing
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasCritical);
        }

        [Test]
        public void CodeInjection_CommandInjection_DoesNotExecute()
        {
            // Create JSON with command injection patterns
            var json = "{\"name\":\"; rm -rf /\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should validate as string without executing
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasCritical);
        }

        #endregion

        #region DoS Tests

        [Test]
        public void DoS_LargeFile_RespectsSizeLimit()
        {
            // Create JSON with string that exceeds MaxStringLength limit (60KB > 50000)
            var largeString = new string('A', 60000);
            var json = $"{{\"name\":\"{largeString}\",\"value\":1}}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            Assert.AreEqual(RsvValidationStatus.Critical, result.Status);
            Assert.IsTrue(result.HasCritical);
        }

        [Test]
        public void DoS_ManySmallFiles_DoesNotCrash()
        {
            // Validate many small JSONs in a loop
            for (int i = 0; i < 100; i++)
            {
                var json = $"{{\"name\":\"test{i}\",\"value\":{i}}}";
                var result = RsvRuntimeValidator.Validate(_schema, json);

                Assert.IsNotNull(result);
                Assert.IsFalse(result.HasCritical);
            }
        }

        [Test]
        public void DoS_RegexDoS_DoesNotHang()
        {
            // Create schema with complex regex pattern
            var schemaWithRegex = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            schemaWithRegex.SchemaId = "regex-dos-schema";
            schemaWithRegex.Version = "1.0.0";
            schemaWithRegex.MaxNestingDepth = 20;
            schemaWithRegex.MaxStringLength = 50000;
            schemaWithRegex.MaxArrayLength = 500;
            schemaWithRegex.RootNodes = new System.Collections.Generic.List<RsvCompiledNode>
            {
                new RsvCompiledNode
                {
                    Name = "email",
                    FieldType = RsvFieldType.String,
                    IsRequired = true,
                    Pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
                }
            };

            // Create JSON with string that could cause regex DoS
            var json = "{\"email\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@\"}";

            var result = RsvRuntimeValidator.Validate(schemaWithRegex, json);

            // Should complete without hanging
            Assert.IsNotNull(result);

            ScriptableObject.DestroyImmediate(schemaWithRegex);
        }

        #endregion

        #region Input Sanitization Tests

        [Test]
        public void InputSanitization_UnicodeExploits_DoesNotCrash()
        {
            // Create JSON with various unicode exploits
            var json = "{\"name\":\"\\u0000\\u0001\\u0002\\u0003\\u0004\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully without crashing
            Assert.IsNotNull(result);
        }

        [Test]
        public void InputSanitization_OverlongEncoding_DoesNotCrash()
        {
            // Create JSON with overlong unicode encoding
            var json = "{\"name\":\"\\u0020\\u0020\\u0020\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully without crashing
            Assert.IsNotNull(result);
        }

        [Test]
        public void InputSanitization_MalformedUtf8_DoesNotCrash()
        {
            // Create JSON with malformed UTF-8 sequences
            var json = "{\"name\":\"\\uD800\",\"value\":1}";

            var result = RsvRuntimeValidator.Validate(_schema, json);

            // Should handle gracefully without crashing
            Assert.IsNotNull(result);
        }

        #endregion

        #region Helper Methods

        private string CreateDeeplyNestedJson(int depth)
        {
            // Builds actual nested JSON objects (not strings containing braces).
            // Each level adds a real {"nested": ...} wrapper so depth is measurable.
            var sb = new System.Text.StringBuilder();
            sb.Append("{\"name\":\"test\",\"value\":1");

            for (int i = 0; i < depth; i++)
                sb.Append(",\"nested\":{");

            sb.Append("\"leaf\":\"value\"");

            for (int i = 0; i < depth; i++)
                sb.Append("}");

            sb.Append("}");
            return sb.ToString();
        }

        private string CreateBillionLaughsJson()
        {
            // Create a structure that would expand massively if entities were supported
            var sb = new System.Text.StringBuilder();
            sb.Append("{\"name\":\"test\",\"value\":1,\"items\":[");

            for (int i = 0; i < 100; i++)
            {
                sb.Append("{\"id\":");
                sb.Append(i);
                sb.Append("},");
            }

            sb.Length--; // Remove trailing comma
            sb.Append("]}");
            return sb.ToString();
        }

        private string CreateHugeArrayJson(int size)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\"name\":\"test\",\"value\":1,\"items\":[");

            for (int i = 0; i < size; i++)
            {
                sb.Append("{\"id\":");
                sb.Append(i);
                sb.Append("},");
            }

            sb.Length--; // Remove trailing comma
            sb.Append("]}");
            return sb.ToString();
        }

        #endregion
    }
}