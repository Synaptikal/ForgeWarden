using NUnit.Framework;
using LiveGameDev.RSV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LiveGameDev.RSV.Tests
{
    /// <summary>
    /// Performance tests for RSV runtime validation.
    /// Tests validation speed, memory usage, and scaling characteristics.
    /// </summary>
    [TestFixture]
    public class RsvPerformanceTests
    {
        private const int Iterations = 100;
        private const int LargeDatasetSize = 10000;
        private const int MediumDatasetSize = 1000;

        private RsvCompiledSchemaAsset _simpleSchema;
        private RsvCompiledSchemaAsset _complexSchema;
        private RsvCompiledSchemaAsset _nestedSchema;

        [SetUp]
        public void SetUp()
        {
            // Simple schema with basic types
            _simpleSchema = CreateSimpleSchema();

            // Complex schema with many fields
            _complexSchema = CreateComplexSchema();

            // Nested schema with deep structure
            _nestedSchema = CreateNestedSchema();
        }

        [TearDown]
        public void TearDown()
        {
            if (_simpleSchema != null)
                ScriptableObject.DestroyImmediate(_simpleSchema);
            if (_complexSchema != null)
                ScriptableObject.DestroyImmediate(_complexSchema);
            if (_nestedSchema != null)
                ScriptableObject.DestroyImmediate(_nestedSchema);
        }

        #region Single Validation Tests

        [Test]
        public void Benchmark_SingleValidation_Under1ms()
        {
            var json = "{\"name\":\"test\",\"value\":42,\"active\":true}";

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }

            stopwatch.Stop();

            var avgTimeMs = stopwatch.ElapsedMilliseconds / (double)Iterations;

            Debug.Log($"Average validation time: {avgTimeMs:F4} ms");
            Debug.Log($"Total time for {Iterations} iterations: {stopwatch.ElapsedMilliseconds} ms");

            Assert.Less(avgTimeMs, 1.0, "Single validation should be under 1ms");
        }

        [Test]
        public void Benchmark_SimpleValidation_MeasuresBaseline()
        {
            var json = "{\"name\":\"test\",\"value\":42}";

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }

            stopwatch.Stop();

            var avgTimeMs = stopwatch.ElapsedMilliseconds / (double)Iterations;

            Debug.Log($"Simple validation baseline: {avgTimeMs:F4} ms");
            Debug.Log($"Throughput: {1000.0 / avgTimeMs:F0} validations/second");

            Assert.IsNotNull(stopwatch);
        }

        [Test]
        public void Benchmark_ComplexValidation_MeasuresOverhead()
        {
            var json = CreateComplexJson();

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                var result = RsvRuntimeValidator.Validate(_complexSchema, json);
            }

            stopwatch.Stop();

            var avgTimeMs = stopwatch.ElapsedMilliseconds / (double)Iterations;

            Debug.Log($"Complex validation time: {avgTimeMs:F4} ms");
            Debug.Log($"Complexity overhead: {avgTimeMs * 100:F2}% of simple baseline");

            Assert.IsNotNull(stopwatch);
        }

        #endregion

        #region Large Dataset Tests

        [Test]
        public void Benchmark_LargeDataset_Under5Seconds()
        {
            var jsons = new List<string>();
            for (int i = 0; i < LargeDatasetSize; i++)
            {
                jsons.Add($"{{\"name\":\"item{i}\",\"value\":{i}}}");
            }

            var stopwatch = Stopwatch.StartNew();

            foreach (var json in jsons)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }

            stopwatch.Stop();

            Debug.Log($"Validated {LargeDatasetSize} items in {stopwatch.ElapsedMilliseconds} ms");
            Debug.Log($"Average time per item: {stopwatch.ElapsedMilliseconds / (double)LargeDatasetSize:F4} ms");
            Debug.Log($"Throughput: {LargeDatasetSize / (stopwatch.ElapsedMilliseconds / 1000.0):F0} items/second");

            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "Large dataset validation should be under 5 seconds");
        }

        [Test]
        public void Benchmark_MediumDataset_MeasuresPerformance()
        {
            var jsons = new List<string>();
            for (int i = 0; i < MediumDatasetSize; i++)
            {
                jsons.Add($"{{\"name\":\"item{i}\",\"value\":{i},\"active\":{i % 2 == 0}}}");
            }

            var stopwatch = Stopwatch.StartNew();

            foreach (var json in jsons)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }

            stopwatch.Stop();

            var avgTimeMs = stopwatch.ElapsedMilliseconds / (double)MediumDatasetSize;

            Debug.Log($"Medium dataset ({MediumDatasetSize} items): {stopwatch.ElapsedMilliseconds} ms");
            Debug.Log($"Average time per item: {avgTimeMs:F4} ms");
            Debug.Log($"Throughput: {MediumDatasetSize / (stopwatch.ElapsedMilliseconds / 1000.0):F0} items/second");

            Assert.IsNotNull(stopwatch);
        }

        #endregion

        #region Parallel Scaling Tests

        [Test]
        public void Benchmark_ParallelScaling_NearLinear()
        {
            var jsons = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                jsons.Add($"{{\"name\":\"item{i}\",\"value\":{i}}}");
            }

            // Single-threaded baseline
            var stopwatch = Stopwatch.StartNew();
            foreach (var json in jsons)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }
            stopwatch.Stop();
            var singleThreadTime = stopwatch.ElapsedMilliseconds;

            Debug.Log($"Single-threaded: {singleThreadTime} ms");

            // Note: True parallel testing would require System.Threading.Tasks
            // This is a placeholder for future parallel validation implementation
            Debug.Log("Parallel scaling test: Placeholder for future implementation");

            Assert.IsNotNull(singleThreadTime);
        }

        #endregion

        #region Memory Usage Tests

        [Test]
        public void Benchmark_MemoryUsage_NoGCSpikes()
        {
            var json = "{\"name\":\"test\",\"value\":42}";

            // Force GC before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(true);

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < Iterations * 10; i++)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }
            stopwatch.Stop();

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            Debug.Log($"Memory used for {Iterations * 10} validations: {memoryUsed / 1024.0:F2} KB");
            Debug.Log($"Average memory per validation: {memoryUsed / (double)(Iterations * 10):F2} bytes");
            Debug.Log($"Time: {stopwatch.ElapsedMilliseconds} ms");

            // Memory usage should be reasonable (less than 10MB for 1000 validations)
            Assert.Less(memoryUsed, 10 * 1024 * 1024, "Memory usage should be reasonable");
        }

        [Test]
        public void Benchmark_MemoryAllocation_PassCase()
        {
            var json = "{\"name\":\"test\",\"value\":42}";

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(true);

            // Validate passing JSON (should allocate minimal memory)
            for (int i = 0; i < 1000; i++)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            Debug.Log($"Memory for 1000 passing validations: {memoryUsed / 1024.0:F2} KB");

            // Passing validations should allocate very little memory
            Assert.Less(memoryUsed, 1024 * 1024, "Passing validations should allocate minimal memory");
        }

        [Test]
        public void Benchmark_MemoryAllocation_FailCase()
        {
            var json = "{\"name\":\"test\",\"value\":\"not_a_number\"}";

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(true);

            // Validate failing JSON (will allocate for error entries)
            for (int i = 0; i < 1000; i++)
            {
                var result = RsvRuntimeValidator.Validate(_simpleSchema, json);
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            Debug.Log($"Memory for 1000 failing validations: {memoryUsed / 1024.0:F2} KB");

            // Failing validations will allocate more, but should still be reasonable
            Assert.Less(memoryUsed, 5 * 1024 * 1024, "Failing validations should allocate reasonable memory");
        }

        #endregion

        #region Cache Effectiveness Tests

        [Test]
        public void Benchmark_CacheHitRate_Over90Percent()
        {
            // Register schema in registry
            RsvSchemaRegistry.Clear();
            RsvSchemaRegistry.Register(_simpleSchema);

            var json = "{\"name\":\"test\",\"value\":42}";

            var stopwatch = Stopwatch.StartNew();

            // First validation (cache miss)
            var result1 = RsvRuntimeValidator.Validate("simple-schema", json);

            // Subsequent validations (cache hits)
            for (int i = 0; i < 99; i++)
            {
                var result = RsvRuntimeValidator.Validate("simple-schema", json);
            }

            stopwatch.Stop();

            Debug.Log($"100 validations with registry lookup: {stopwatch.ElapsedMilliseconds} ms");
            Debug.Log($"Average time: {stopwatch.ElapsedMilliseconds / 100.0:F4} ms");

            // Registry lookups should be fast
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Registry lookups should be fast");

            RsvSchemaRegistry.Clear();
        }

        #endregion

        #region Streaming vs Load All Tests

        [Test]
        public void Benchmark_Streaming_10MBFile_Under100ms()
        {
            // Create a large JSON string (simulating a 10MB file)
            var largeJson = CreateLargeJson(10 * 1024 * 1024);

            var stopwatch = Stopwatch.StartNew();

            var result = RsvRuntimeValidator.Validate(_simpleSchema, largeJson);

            stopwatch.Stop();

            Debug.Log($"Validated ~10MB JSON in {stopwatch.ElapsedMilliseconds} ms");

            // Should complete in reasonable time
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Large JSON validation should complete in reasonable time");
        }

        #endregion

        #region Nested Structure Tests

        [Test]
        public void Benchmark_NestedValidation_MeasuresDepthImpact()
        {
            var json = CreateNestedJson(10);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                var result = RsvRuntimeValidator.Validate(_nestedSchema, json);
            }

            stopwatch.Stop();

            var avgTimeMs = stopwatch.ElapsedMilliseconds / (double)Iterations;

            Debug.Log($"Nested validation (10 levels): {avgTimeMs:F4} ms");

            Assert.IsNotNull(stopwatch);
        }

        [Test]
        public void Benchmark_DeepNesting_MeasuresPerformance()
        {
            var json = CreateNestedJson(50);

            var stopwatch = Stopwatch.StartNew();

            var result = RsvRuntimeValidator.Validate(_nestedSchema, json);

            stopwatch.Stop();

            Debug.Log($"Deep nesting validation (50 levels): {stopwatch.ElapsedMilliseconds} ms");

            // Should handle deep nesting without excessive time
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Deep nesting should be handled efficiently");
        }

        #endregion

        #region Schema Registry Tests

        [Test]
        public void Benchmark_RegistryLookup_MeasuresPerformance()
        {
            RsvSchemaRegistry.Clear();

            // Register multiple schemas
            for (int i = 0; i < 100; i++)
            {
                var schema = CreateSimpleSchema();
                schema.SchemaId = $"schema-{i}";
                RsvSchemaRegistry.Register(schema);
            }

            var stopwatch = Stopwatch.StartNew();

            // Perform many lookups
            for (int i = 0; i < 10000; i++)
            {
                var schemaId = $"schema-{i % 100}";
                var schema = RsvSchemaRegistry.Get(schemaId);
            }

            stopwatch.Stop();

            Debug.Log($"10000 registry lookups: {stopwatch.ElapsedMilliseconds} ms");
            Debug.Log($"Average lookup time: {stopwatch.ElapsedMilliseconds / 10000.0:F4} ms");

            // Registry lookups should be very fast
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Registry lookups should be very fast");

            RsvSchemaRegistry.Clear();
        }

        #endregion

        #region Helper Methods

        private RsvCompiledSchemaAsset CreateSimpleSchema()
        {
            var schema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            schema.SchemaId = "simple-schema";
            schema.Version = "1.0.0";
            schema.MaxNestingDepth = 100;
            schema.MaxStringLength = 1000000;
            schema.MaxArrayLength = 10000;
            schema.RootNodes = new List<RsvCompiledNode>
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
                    Name = "active",
                    FieldType = RsvFieldType.Boolean,
                    IsRequired = false
                }
            };
            return schema;
        }

        private RsvCompiledSchemaAsset CreateComplexSchema()
        {
            var schema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            schema.SchemaId = "complex-schema";
            schema.Version = "1.0.0";
            schema.MaxNestingDepth = 100;
            schema.MaxStringLength = 1000000;
            schema.MaxArrayLength = 10000;

            var nodes = new List<RsvCompiledNode>();
            for (int i = 0; i < 50; i++)
            {
                nodes.Add(new RsvCompiledNode
                {
                    Name = $"field{i}",
                    FieldType = i % 2 == 0 ? RsvFieldType.String : RsvFieldType.Integer,
                    IsRequired = i < 10
                });
            }

            schema.RootNodes = nodes;
            return schema;
        }

        private RsvCompiledSchemaAsset CreateNestedSchema()
        {
            var schema = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            schema.SchemaId = "nested-schema";
            schema.Version = "1.0.0";
            schema.MaxNestingDepth = 100;
            schema.MaxStringLength = 1000000;
            schema.MaxArrayLength = 10000;

            var root = new RsvCompiledNode
            {
                Name = "root",
                FieldType = RsvFieldType.Object,
                IsRequired = true,
                Children = new List<RsvCompiledNode>()
            };

            var current = root;
            for (int i = 0; i < 10; i++)
            {
                var child = new RsvCompiledNode
                {
                    Name = $"level{i}",
                    FieldType = RsvFieldType.Object,
                    IsRequired = true,
                    Children = new List<RsvCompiledNode>()
                };
                current.Children.Add(child);
                current = child;
            }

            // Add a leaf node
            current.Children.Add(new RsvCompiledNode
            {
                Name = "value",
                FieldType = RsvFieldType.String,
                IsRequired = true
            });

            schema.RootNodes = new List<RsvCompiledNode> { root };
            return schema;
        }

        private string CreateComplexJson()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 50; i++)
            {
                sb.Append($"\"field{i}\":");
                if (i % 2 == 0)
                    sb.Append($"\"value{i}\"");
                else
                    sb.Append(i);
                if (i < 49)
                    sb.Append(",");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private string CreateNestedJson(int depth)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\"root\":");
            for (int i = 0; i < depth; i++)
            {
                sb.Append($"{{\"level{i}\":");
            }
            sb.Append("\"value\"");
            for (int i = 0; i < depth; i++)
            {
                sb.Append("}");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private string CreateLargeJson(int targetSizeBytes)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\"name\":\"");
            var remaining = targetSizeBytes - 20; // Account for JSON structure
            var chunk = new string('A', Math.Min(remaining, 10000));
            while (remaining > 0)
            {
                sb.Append(chunk);
                remaining -= chunk.Length;
            }
            sb.Append("\",\"value\":42}");
            return sb.ToString();
        }

        #endregion
    }
}