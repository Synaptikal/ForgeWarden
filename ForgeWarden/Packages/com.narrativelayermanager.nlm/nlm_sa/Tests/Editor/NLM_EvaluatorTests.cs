using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NarrativeLayerManager.Tests
{
    /// <summary>
    /// Unit tests for the NLM_Evaluator class.
    /// </summary>
    public class NLM_EvaluatorTests
    {
        #region Test Helpers

        private static NarrativeStateDefinition State(params (string n, object v)[] vars)
        {
            var s = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            s.Variables = new List<NarrativeVariable>();
            foreach (var (n, v) in vars)
            {
                var nv = new NarrativeVariable { Name = n };
                switch (v)
                {
                    case bool b: nv.Type = NarrativeVariableType.Bool; nv.BoolValue = b; break;
                    case int i: nv.Type = NarrativeVariableType.Int; nv.IntValue = i; break;
                    case float f: nv.Type = NarrativeVariableType.Float; nv.FloatValue = f; break;
                    case string st: nv.Type = NarrativeVariableType.String; nv.StringValue = st; break;
                }
                s.Variables.Add(nv);
            }
            return s;
        }

        private static NarrativeConditionGroup G(params (string n, ConditionOperator op, string v, ConditionLogic l)[] c)
        {
            var g = new NarrativeConditionGroup();
            foreach (var (n, op, v, l) in c)
                g.Conditions.Add(new NarrativeCondition { VariableName = n, Operator = op, Value = v, LogicToNext = l });
            return g;
        }

        #endregion

        #region Empty Group Tests

        [Test]
        public void EmptyGroup_AlwaysTrue()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(new NarrativeConditionGroup(), null));

        [Test]
        public void NullGroup_ReturnsTrue()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(null, State()));

        #endregion

        #region Integer Tests

        [Test]
        public void Int_GTE_Pass()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.GreaterOrEqual, "3", ConditionLogic.And)), State(("Ch", 3))));

        [Test]
        public void Int_GTE_Fail()
            => Assert.IsFalse(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.GreaterOrEqual, "3", ConditionLogic.And)), State(("Ch", 2))));

        [Test]
        public void Int_GT_Pass()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.GreaterThan, "3", ConditionLogic.And)), State(("Ch", 4))));

        [Test]
        public void Int_GT_Fail()
            => Assert.IsFalse(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.GreaterThan, "3", ConditionLogic.And)), State(("Ch", 3))));

        [Test]
        public void Int_LTE_Pass()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.LessOrEqual, "3", ConditionLogic.And)), State(("Ch", 3))));

        [Test]
        public void Int_LT_Pass()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.LessThan, "3", ConditionLogic.And)), State(("Ch", 2))));

        [Test]
        public void Int_Equals_Pass()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.Equals, "3", ConditionLogic.And)), State(("Ch", 3))));

        [Test]
        public void Int_NotEquals_Pass()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Ch", ConditionOperator.NotEquals, "3", ConditionLogic.And)), State(("Ch", 2))));

        #endregion

        #region String Tests

        [Test]
        public void String_Equals_CaseInsensitive()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Phase", ConditionOperator.Equals, "PostWar", ConditionLogic.And)), State(("Phase", "postwar"))));

        [Test]
        public void String_NotEquals()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Phase", ConditionOperator.NotEquals, "War", ConditionLogic.And)), State(("Phase", "Peace"))));

        [Test]
        public void String_GreaterThan_Alphabetical()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Name", ConditionOperator.GreaterThan, "Alice", ConditionLogic.And)), State(("Name", "Bob"))));

        #endregion

        #region Boolean Tests

        [Test]
        public void Bool_IsTrue_Pass()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Done", ConditionOperator.IsTrue, "", ConditionLogic.And)), State(("Done", true))));

        [Test]
        public void Bool_IsTrue_Fail()
            => Assert.IsFalse(NLM_Evaluator.EvaluateGroup(G(("Done", ConditionOperator.IsTrue, "", ConditionLogic.And)), State(("Done", false))));

        [Test]
        public void Bool_IsFalse_OnMissingVar()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Done", ConditionOperator.IsFalse, "", ConditionLogic.And)), State()));

        [Test]
        public void Bool_IsFalse_OnFalse()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Done", ConditionOperator.IsFalse, "", ConditionLogic.And)), State(("Done", false))));

        [Test]
        public void Bool_IsFalse_OnWrongType()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Done", ConditionOperator.IsFalse, "", ConditionLogic.And)), State(("Done", "string"))));

        #endregion

        #region Float Tests

        [Test]
        public void Float_Equals_WithTolerance()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Health", ConditionOperator.Equals, "1.5", ConditionLogic.And)), State(("Health", 1.5f))));

        [Test]
        public void Float_GreaterThan()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(G(("Health", ConditionOperator.GreaterThan, "50.5", ConditionLogic.And)), State(("Health", 75.0f))));

        #endregion

        #region Logic Chain Tests

        [Test]
        public void And_ShortCircuit_BothTrue()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(
                G(("Ch", ConditionOperator.GreaterOrEqual, "2", ConditionLogic.And),
                  ("Phase", ConditionOperator.Equals, "war", ConditionLogic.And)),
                State(("Ch", 2), ("Phase", "war"))));

        [Test]
        public void And_ShortCircuit_FirstFalse()
            => Assert.IsFalse(NLM_Evaluator.EvaluateGroup(
                G(("Ch", ConditionOperator.GreaterOrEqual, "5", ConditionLogic.And),
                  ("Phase", ConditionOperator.Equals, "war", ConditionLogic.And)),
                State(("Ch", 1), ("Phase", "war"))));

        [Test]
        public void Or_ShortCircuit_FirstTrue()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(
                G(("Phase", ConditionOperator.Equals, "war", ConditionLogic.Or),
                  ("Ch", ConditionOperator.GreaterOrEqual, "99", ConditionLogic.And)),
                State(("Ch", 1), ("Phase", "war"))));

        [Test]
        public void Or_ShortCircuit_BothFalse()
            => Assert.IsFalse(NLM_Evaluator.EvaluateGroup(
                G(("Phase", ConditionOperator.Equals, "peace", ConditionLogic.Or),
                  ("Ch", ConditionOperator.GreaterOrEqual, "99", ConditionLogic.And)),
                State(("Ch", 1), ("Phase", "war"))));

        [Test]
        public void ComplexChain_AndOrMix()
            // AND binds tighter than OR: A[Or]B[And]C = A OR (B AND C).
            // A=false, B=true, C=true → false OR (true AND true) = true.
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(
                G(("A", ConditionOperator.Equals, "1", ConditionLogic.Or),
                  ("B", ConditionOperator.Equals, "2", ConditionLogic.And),
                  ("C", ConditionOperator.Equals, "3", ConditionLogic.Or)),
                State(("A", "0"), ("B", "2"), ("C", "3"))));

        #endregion

        #region Missing Variable Tests

        [Test]
        public void MissingVar_NotEquals_Passes()
            => Assert.IsTrue(NLM_Evaluator.EvaluateGroup(
                G(("Ghost", ConditionOperator.NotEquals, "anything", ConditionLogic.And)),
                State()));

        [Test]
        public void MissingVar_Equals_Fails()
            => Assert.IsFalse(NLM_Evaluator.EvaluateGroup(
                G(("Ghost", ConditionOperator.Equals, "anything", ConditionLogic.And)),
                State()));

        [Test]
        public void MissingVar_GreaterThan_Fails()
            => Assert.IsFalse(NLM_Evaluator.EvaluateGroup(
                G(("Ghost", ConditionOperator.GreaterThan, "0", ConditionLogic.And)),
                State()));

        #endregion

        #region Binding Resolution Tests

        [Test]
        public void BindingRule_FirstMatchWins()
        {
            var s = State(("Ch", 5));
            var go = new GameObject();
            var b = go.AddComponent<NarrativeObjectBinding>();
            b.Rules = new List<NarrativeObjectBinding.BindingRule>
            {
                new() { RuleName="Ruins",   Condition=G(("Ch", ConditionOperator.GreaterOrEqual,"5",ConditionLogic.And)) },
                new() { RuleName="Damaged", Condition=G(("Ch", ConditionOperator.GreaterOrEqual,"2",ConditionLogic.And)) },
                new() { RuleName="Intact",  Condition=new NarrativeConditionGroup() }
            };
            var triggered = NLM_Evaluator.ResolveBinding(b, s);
            Assert.AreEqual(1, triggered.Count);
            Assert.AreEqual("Ruins", triggered[0].RuleName);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void FallThrough_TriggersMultipleRules()
        {
            var s = State(("Ch", 5));
            var go = new GameObject();
            var b = go.AddComponent<NarrativeObjectBinding>();
            b.Rules = new List<NarrativeObjectBinding.BindingRule>
            {
                new() { RuleName="A", FallThrough=true,  Condition=G(("Ch",ConditionOperator.GreaterOrEqual,"1",ConditionLogic.And)) },
                new() { RuleName="B", FallThrough=false, Condition=G(("Ch",ConditionOperator.GreaterOrEqual,"1",ConditionLogic.And)) },
                new() { RuleName="C", FallThrough=false, Condition=new NarrativeConditionGroup() }
            };
            var triggered = NLM_Evaluator.ResolveBinding(b, s);
            Assert.AreEqual(2, triggered.Count, "FallThrough=true should let evaluation continue to next rule.");
            Assert.AreEqual("A", triggered[0].RuleName);
            Assert.AreEqual("B", triggered[1].RuleName);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void NoRules_ReturnsEmpty()
        {
            var s = State(("Ch", 1));
            var go = new GameObject();
            var b = go.AddComponent<NarrativeObjectBinding>();
            b.Rules = new List<NarrativeObjectBinding.BindingRule>();
            var triggered = NLM_Evaluator.ResolveBinding(b, s);
            Assert.AreEqual(0, triggered.Count);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void NullBinding_ReturnsEmpty()
        {
            var s = State(("Ch", 1));
            var triggered = NLM_Evaluator.ResolveBinding(null, s);
            Assert.AreEqual(0, triggered.Count);
        }

        [Test]
        public void NullState_ReturnsEmpty()
        {
            var go = new GameObject();
            var b = go.AddComponent<NarrativeObjectBinding>();
            b.Rules = new List<NarrativeObjectBinding.BindingRule>
            {
                new() { RuleName="Test", Condition=G(("Ch", ConditionOperator.Equals,"1",ConditionLogic.And)) }
            };
            var triggered = NLM_Evaluator.ResolveBinding(b, null);
            Assert.AreEqual(0, triggered.Count);
            UnityEngine.Object.DestroyImmediate(go);
        }

        #endregion

        #region Validation Report Tests

        [Test]
        public void ValidationReport_HasErrors_CorrectlyReflected()
        {
            var r = new NLM_ValidationReport("Test");
            Assert.IsFalse(r.HasErrors);
            r.Add(NLM_Status.Error, "Test", "Something broke.");
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(NLM_Status.Error, r.OverallStatus);
        }

        [Test]
        public void ValidationReport_HasWarnings_CorrectlyReflected()
        {
            var r = new NLM_ValidationReport("Test");
            Assert.IsFalse(r.HasWarnings);
            r.Add(NLM_Status.Warning, "Test", "Something concerning.");
            Assert.IsTrue(r.HasWarnings);
            Assert.AreEqual(NLM_Status.Warning, r.OverallStatus);
        }

        [Test]
        public void ValidationReport_PassStatus()
        {
            var r = new NLM_ValidationReport("Test");
            r.Add(NLM_Status.Pass, "Test", "All good.");
            Assert.IsFalse(r.HasErrors);
            Assert.IsFalse(r.HasWarnings);
            Assert.AreEqual(NLM_Status.Pass, r.OverallStatus);
        }

        [Test]
        public void ValidationReport_ToMarkdown_ContainsStatus()
        {
            var r = new NLM_ValidationReport("Unit");
            r.Add(NLM_Status.Warning, "Beat", "Missing state.", suggestedFix: "Assign a state.");
            var md = r.ToMarkdown();
            Assert.IsTrue(md.Contains("Warning"), "Markdown should contain status text.");
            Assert.IsTrue(md.Contains("Assign a state."), "Markdown should contain suggested fix.");
        }

        [Test]
        public void ValidationReport_ToCsv_ContainsData()
        {
            var r = new NLM_ValidationReport("Unit");
            r.Add(NLM_Status.Error, "Variable", "Duplicate name.", "Assets/Test.asset", "Rename variable.");
            var csv = r.ToCsv();
            Assert.IsTrue(csv.Contains("Error"));
            Assert.IsTrue(csv.Contains("Duplicate name."));
        }

        [Test]
        public void ValidationReport_Clear_RemovesEntries()
        {
            var r = new NLM_ValidationReport("Test");
            r.Add(NLM_Status.Error, "Test", "Error");
            Assert.AreEqual(1, r.Entries.Count);
            r.Clear();
            Assert.AreEqual(0, r.Entries.Count);
        }

        #endregion

        #region State Definition Tests

        [Test]
        public void StateDefinition_Validate_DuplicateName_ReturnsError()
        {
            var state = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            state.Variables = new List<NarrativeVariable>
            {
                new() { Name = "Chapter", Type = NarrativeVariableType.Int, IntValue = 1 },
                new() { Name = "Chapter", Type = NarrativeVariableType.Int, IntValue = 2 }
            };
            var report = state.Validate();
            Assert.IsTrue(report.HasErrors, "Duplicate variable name should produce an Error.");
        }

        [Test]
        public void StateDefinition_Validate_EmptyName_ReturnsWarning()
        {
            var state = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            state.Variables = new List<NarrativeVariable>
            {
                new() { Name = "", Type = NarrativeVariableType.Int, IntValue = 1 }
            };
            var report = state.Validate();
            Assert.IsTrue(report.HasWarnings, "Empty variable name should produce a Warning.");
        }

        [Test]
        public void StateDefinition_Validate_Valid_ReturnsPass()
        {
            var state = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            state.Variables = new List<NarrativeVariable>
            {
                new() { Name = "Chapter", Type = NarrativeVariableType.Int, IntValue = 1 },
                new() { Name = "Complete", Type = NarrativeVariableType.Bool, BoolValue = false }
            };
            var report = state.Validate();
            Assert.AreEqual(NLM_Status.Pass, report.OverallStatus);
        }

        [Test]
        public void StateDefinition_GetVariable_ReturnsCorrect()
        {
            var state = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            state.Variables = new List<NarrativeVariable>
            {
                new() { Name = "Chapter", Type = NarrativeVariableType.Int, IntValue = 5 }
            };
            var v = state.GetVariable("Chapter");
            Assert.IsNotNull(v);
            Assert.AreEqual(5, v.IntValue);
        }

        [Test]
        public void StateDefinition_GetVariable_MissingReturnsNull()
        {
            var state = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            var v = state.GetVariable("NonExistent");
            Assert.IsNull(v);
        }

        [Test]
        public void StateDefinition_GetTypedValues_ReturnCorrect()
        {
            var state = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            state.Variables = new List<NarrativeVariable>
            {
                new() { Name = "BoolVar", Type = NarrativeVariableType.Bool, BoolValue = true },
                new() { Name = "IntVar", Type = NarrativeVariableType.Int, IntValue = 42 },
                new() { Name = "FloatVar", Type = NarrativeVariableType.Float, FloatValue = 3.14f },
                new() { Name = "StringVar", Type = NarrativeVariableType.String, StringValue = "test" }
            };

            Assert.IsTrue(state.GetBool("BoolVar"));
            Assert.AreEqual(42, state.GetInt("IntVar"));
            Assert.AreEqual(3.14f, state.GetFloat("FloatVar"), 0.01f);
            Assert.AreEqual("test", state.GetString("StringVar"));
        }

        [Test]
        public void StateDefinition_GetTypedValues_FallbacksWork()
        {
            var state = ScriptableObject.CreateInstance<NarrativeStateDefinition>();

            Assert.AreEqual(false, state.GetBool("Missing", false));
            Assert.AreEqual(99, state.GetInt("Missing", 99));
            Assert.AreEqual(1.5f, state.GetFloat("Missing", 1.5f));
            Assert.AreEqual("default", state.GetString("Missing", "default"));
        }

        [Test]
        public void StateDefinition_DeepClone_CreatesIndependentCopy()
        {
            var original = ScriptableObject.CreateInstance<NarrativeStateDefinition>();
            original.name = "Original";
            original.DisplayName = "Original Display";
            original.Variables = new List<NarrativeVariable>
            {
                new() { Name = "Chapter", Type = NarrativeVariableType.Int, IntValue = 1 }
            };

            var clone = original.DeepClone();

            Assert.AreNotSame(original, clone);
            Assert.AreEqual("Original_Clone", clone.name);
            Assert.AreEqual("Original Display", clone.DisplayName);
            Assert.AreEqual(1, clone.Variables.Count);

            // Modify clone shouldn't affect original
            clone.Variables[0].IntValue = 99;
            Assert.AreEqual(1, original.Variables[0].IntValue);
        }

        #endregion

        #region Layer Definition Tests

        [Test]
        public void LayerDefinition_RebuildIndices_AssignsCorrectly()
        {
            var layer = ScriptableObject.CreateInstance<NarrativeLayerDefinition>();
            layer.Beats = new List<NarrativeBeat>
            {
                new() { BeatName = "Beat0" },
                new() { BeatName = "Beat1" },
                new() { BeatName = "Beat2" }
            };

            layer.RebuildIndices();

            Assert.AreEqual(0, layer.Beats[0].Index);
            Assert.AreEqual(1, layer.Beats[1].Index);
            Assert.AreEqual(2, layer.Beats[2].Index);
        }

        [Test]
        public void LayerDefinition_GetBeat_ValidIndex()
        {
            var layer = ScriptableObject.CreateInstance<NarrativeLayerDefinition>();
            layer.Beats = new List<NarrativeBeat>
            {
                new() { BeatName = "First" }
            };

            var beat = layer.GetBeat(0);
            Assert.IsNotNull(beat);
            Assert.AreEqual("First", beat.BeatName);
        }

        [Test]
        public void LayerDefinition_GetBeat_InvalidIndex()
        {
            var layer = ScriptableObject.CreateInstance<NarrativeLayerDefinition>();
            layer.Beats = new List<NarrativeBeat>();

            Assert.IsNull(layer.GetBeat(0));
            Assert.IsNull(layer.GetBeat(-1));
            Assert.IsNull(layer.GetBeat(5));
        }

        [Test]
        public void LayerDefinition_Validate_EmptyBeats_ReturnsWarning()
        {
            var layer = ScriptableObject.CreateInstance<NarrativeLayerDefinition>();
            layer.name = "TestLayer";
            layer.Beats = new List<NarrativeBeat>();

            var report = layer.Validate();
            Assert.IsTrue(report.HasWarnings);
            Assert.IsTrue(report.Entries[0].Message.Contains("no beats"));
        }

        [Test]
        public void LayerDefinition_Validate_BeatWithoutState_ReturnsError()
        {
            var layer = ScriptableObject.CreateInstance<NarrativeLayerDefinition>();
            layer.name = "TestLayer";
            layer.Beats = new List<NarrativeBeat>
            {
                new() { BeatName = "TestBeat", State = null }
            };

            var report = layer.Validate();
            Assert.IsTrue(report.HasErrors);
        }

        #endregion

        #region Narrative Variable Tests

        [Test]
        public void NarrativeVariable_GetValue_ReturnsCorrectType()
        {
            var boolVar = new NarrativeVariable { Type = NarrativeVariableType.Bool, BoolValue = true };
            var intVar = new NarrativeVariable { Type = NarrativeVariableType.Int, IntValue = 42 };
            var floatVar = new NarrativeVariable { Type = NarrativeVariableType.Float, FloatValue = 3.14f };
            var stringVar = new NarrativeVariable { Type = NarrativeVariableType.String, StringValue = "test" };

            Assert.IsInstanceOf<bool>(boolVar.GetValue());
            Assert.IsInstanceOf<int>(intVar.GetValue());
            Assert.IsInstanceOf<float>(floatVar.GetValue());
            Assert.IsInstanceOf<string>(stringVar.GetValue());
        }

        [Test]
        public void NarrativeVariable_SetFromString_ParsesCorrectly()
        {
            var boolVar = new NarrativeVariable { Type = NarrativeVariableType.Bool };
            boolVar.SetFromString("true");
            Assert.IsTrue(boolVar.BoolValue);

            var intVar = new NarrativeVariable { Type = NarrativeVariableType.Int };
            intVar.SetFromString("42");
            Assert.AreEqual(42, intVar.IntValue);

            var floatVar = new NarrativeVariable { Type = NarrativeVariableType.Float };
            floatVar.SetFromString("3.14");
            Assert.AreEqual(3.14f, floatVar.FloatValue, 0.01f);

            var stringVar = new NarrativeVariable { Type = NarrativeVariableType.String };
            stringVar.SetFromString("hello");
            Assert.AreEqual("hello", stringVar.StringValue);
        }

        [Test]
        public void NarrativeVariable_Clone_CreatesIndependentCopy()
        {
            var original = new NarrativeVariable
            {
                Name = "Test",
                Type = NarrativeVariableType.Int,
                IntValue = 5
            };

            var clone = original.Clone();

            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.Name, clone.Name);
            Assert.AreEqual(original.Type, clone.Type);
            Assert.AreEqual(original.IntValue, clone.IntValue);

            clone.IntValue = 99;
            Assert.AreEqual(5, original.IntValue);
        }

        [Test]
        public void NarrativeVariable_ToString_FormatsCorrectly()
        {
            var v = new NarrativeVariable
            {
                Name = "Chapter",
                Type = NarrativeVariableType.Int,
                IntValue = 3
            };

            Assert.AreEqual("Chapter (Int) = 3", v.ToString());
        }

        #endregion

        #region Property Override Tests

        [Test]
        public void PropertyOverride_Summary_ReturnsCorrectDescription()
        {
            var setActive = new NarrativePropertyOverride
            {
                Type = OverrideType.SetActive,
                ActiveState = true
            };
            Assert.AreEqual("SetActive(True)", setActive.Summary());

            var swapMat = new NarrativePropertyOverride
            {
                Type = OverrideType.SwapMaterial,
                MaterialIndex = 0,
                TargetMaterial = null
            };
            Assert.AreEqual("SwapMaterial[0]=null", swapMat.Summary());

            var posOffset = new NarrativePropertyOverride
            {
                Type = OverrideType.PositionOffset,
                PositionOffset = new Vector3(1, 2, 3)
            };
            Assert.AreEqual("PosOffset((1.00, 2.00, 3.00))", posOffset.Summary());
        }

        #endregion
    }
}