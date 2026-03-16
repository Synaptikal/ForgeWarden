using NUnit.Framework;
using LiveGameDev.RSV.Editor;
using UnityEngine;

namespace LiveGameDev.RSV.Tests
{
    /// <summary>
    /// Unit tests for RsvJsonSchemaInterop import/export functionality.
    /// </summary>
    [TestFixture]
    public class RsvJsonSchemaInteropTests
    {
        [Test]
        public void ImportFromJson_ValidSchema_ReturnsDataSchemaDefinition()
        {
            var jsonSchema = @"{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""$id"": ""test-schema"",
  ""title"": ""Test Schema"",
  ""description"": ""A test schema for unit testing"",
  ""type"": ""object"",
  ""properties"": {
    ""name"": {
      ""type"": ""string"",
      ""description"": ""The name field""
    },
    ""age"": {
      ""type"": ""integer"",
      ""minimum"": 0,
      ""maximum"": 120,
      ""description"": ""The age field""
    },
    ""status"": {
      ""type"": ""string"",
      ""enum"": [""active"", ""inactive""]
    }
  },
  ""required"": [""name"", ""age""]
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.IsNotNull(schema);
            Assert.AreEqual("test-schema", schema.SchemaId);
            Assert.AreEqual("Test Schema", schema.DisplayName);
            Assert.AreEqual("A test schema for unit testing", schema.Description);
            Assert.IsNotNull(schema.RootNodes);
            Assert.AreEqual(3, schema.RootNodes.Count);
        }

        [Test]
        public void ImportFromJson_ParsesStringType()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.AreEqual("name", schema.RootNodes[0].Name);
            Assert.AreEqual(RsvFieldType.String, schema.RootNodes[0].Constraint.FieldType);
        }

        [Test]
        public void ImportFromJson_ParsesIntegerType()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""count"": { ""type"": ""integer"" }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.AreEqual(RsvFieldType.Integer, schema.RootNodes[0].Constraint.FieldType);
        }

        [Test]
        public void ImportFromJson_ParsesNumberType()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""price"": { ""type"": ""number"" }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.AreEqual(RsvFieldType.Number, schema.RootNodes[0].Constraint.FieldType);
        }

        [Test]
        public void ImportFromJson_ParsesBooleanType()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""active"": { ""type"": ""boolean"" }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.AreEqual(RsvFieldType.Boolean, schema.RootNodes[0].Constraint.FieldType);
        }

        [Test]
        public void ImportFromJson_ParsesObjectType()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""person"": {
      ""type"": ""object"",
      ""properties"": {
        ""name"": { ""type"": ""string"" }
      }
    }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.AreEqual(RsvFieldType.Object, schema.RootNodes[0].Constraint.FieldType);
            Assert.IsNotNull(schema.RootNodes[0].Children);
            Assert.AreEqual(1, schema.RootNodes[0].Children.Count);
        }

        [Test]
        public void ImportFromJson_ParsesArrayType()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""items"": { ""type"": ""array"" }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.AreEqual(RsvFieldType.Array, schema.RootNodes[0].Constraint.FieldType);
        }

        [Test]
        public void ImportFromJson_ParsesMinMax()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""age"": {
      ""type"": ""integer"",
      ""minimum"": 0,
      ""maximum"": 120
    }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.IsTrue(schema.RootNodes[0].Constraint.HasMinMax);
            Assert.AreEqual(0, schema.RootNodes[0].Constraint.Min);
            Assert.AreEqual(120, schema.RootNodes[0].Constraint.Max);
        }

        [Test]
        public void ImportFromJson_ParsesEnum()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""status"": {
      ""type"": ""string"",
      ""enum"": [""active"", ""inactive"", ""pending""]
    }
  }
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(1, schema.RootNodes.Count);
            Assert.IsNotNull(schema.RootNodes[0].Constraint.EnumValues);
            Assert.AreEqual(3, schema.RootNodes[0].Constraint.EnumValues.Length);
            Assert.AreEqual("active", schema.RootNodes[0].Constraint.EnumValues[0]);
        }

        [Test]
        public void ImportFromJson_ParsesRequired()
        {
            var jsonSchema = @"{
  ""$id"": ""test"",
  ""type"": ""object"",
  ""properties"": {
    ""name"": { ""type"": ""string"" },
    ""age"": { ""type"": ""integer"" }
  },
  ""required"": [""name""]
}";

            var schema = RsvJsonSchemaInterop.ImportFromJson(jsonSchema);

            Assert.AreEqual(2, schema.RootNodes.Count);
            Assert.IsTrue(schema.RootNodes[0].Constraint.IsRequired);
            Assert.IsFalse(schema.RootNodes[1].Constraint.IsRequired);
        }

        [Test]
        public void ExportToJson_ValidSchema_ReturnsJsonSchema()
        {
            var schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            schema.SchemaId = "test-schema";
            schema.DisplayName = "Test Schema";
            schema.Description = "A test schema";
            schema.RootNodes = new System.Collections.Generic.List<RsvSchemaNode>
            {
                new RsvSchemaNode
                {
                    Name = "name",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.String,
                        IsRequired = true,
                        Description = "The name field"
                    }
                },
                new RsvSchemaNode
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
                }
            };

            var json = RsvJsonSchemaInterop.ExportToJson(schema);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"$schema\":"));
            Assert.IsTrue(json.Contains("\"$id\": \"test-schema\""));
            Assert.IsTrue(json.Contains("\"title\": \"Test Schema\""));
            Assert.IsTrue(json.Contains("\"description\": \"A test schema\""));
            Assert.IsTrue(json.Contains("\"type\": \"object\""));
            Assert.IsTrue(json.Contains("\"properties\""));
            Assert.IsTrue(json.Contains("\"required\""));
        }

        [Test]
        public void ExportToJson_IncludesRequiredFields()
        {
            var schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            schema.SchemaId = "test";
            schema.RootNodes = new System.Collections.Generic.List<RsvSchemaNode>
            {
                new RsvSchemaNode
                {
                    Name = "requiredField",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.String,
                        IsRequired = true
                    }
                },
                new RsvSchemaNode
                {
                    Name = "optionalField",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.String,
                        IsRequired = false
                    }
                }
            };

            var json = RsvJsonSchemaInterop.ExportToJson(schema);

            Assert.IsTrue(json.Contains("\"required\""));
            Assert.IsTrue(json.Contains("\"requiredField\""));
            // optionalField is in "properties" but must NOT appear inside the "required" array
            var requiredSectionIdx = json.IndexOf("\"required\"");
            Assert.IsTrue(requiredSectionIdx >= 0, "JSON must contain a required key");
            Assert.IsFalse(json.Substring(requiredSectionIdx).Contains("\"optionalField\""),
                "optionalField must not be listed in the required array");
        }

        [Test]
        public void ExportToJson_IncludesMinMax()
        {
            var schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            schema.SchemaId = "test";
            schema.RootNodes = new System.Collections.Generic.List<RsvSchemaNode>
            {
                new RsvSchemaNode
                {
                    Name = "age",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.Integer,
                        HasMinMax = true,
                        Min = 0,
                        Max = 120
                    }
                }
            };

            var json = RsvJsonSchemaInterop.ExportToJson(schema);

            Assert.IsTrue(json.Contains("\"minimum\": 0"));
            Assert.IsTrue(json.Contains("\"maximum\": 120"));
        }

        [Test]
        public void ExportToJson_IncludesEnum()
        {
            var schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            schema.SchemaId = "test";
            schema.RootNodes = new System.Collections.Generic.List<RsvSchemaNode>
            {
                new RsvSchemaNode
                {
                    Name = "status",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.String,
                        EnumValues = new[] { "active", "inactive" }
                    }
                }
            };

            var json = RsvJsonSchemaInterop.ExportToJson(schema);

            Assert.IsTrue(json.Contains("\"enum\""));
            Assert.IsTrue(json.Contains("\"active\""));
            Assert.IsTrue(json.Contains("\"inactive\""));
        }
    }
}
