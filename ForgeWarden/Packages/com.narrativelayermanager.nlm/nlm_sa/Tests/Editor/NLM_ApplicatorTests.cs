using NUnit.Framework;
using UnityEngine;

namespace NarrativeLayerManager.Tests
{
    /// <summary>
    /// Unit tests for the NLM_Applicator class.
    /// </summary>
    public class NLM_ApplicatorTests
    {
        private GameObject _testObject;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestObject");
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testObject);
        }

        [Test]
        public void ApplyOverride_NullTarget_DoesNotThrow()
        {
            var ovr = new NarrativePropertyOverride { Type = OverrideType.SetActive, ActiveState = true };
            Assert.DoesNotThrow(() => NLM_Applicator.ApplyOverride(null, ovr));
        }

        [Test]
        public void ApplyOverride_NullOverride_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => NLM_Applicator.ApplyOverride(_testObject, null));
        }

        [Test]
        public void ApplyOverride_SetActive_TogglesObject()
        {
            _testObject.SetActive(true);
            var ovr = new NarrativePropertyOverride { Type = OverrideType.SetActive, ActiveState = false };
            
            NLM_Applicator.ApplyOverride(_testObject, ovr);
            
            Assert.IsFalse(_testObject.activeSelf);
        }

        [Test]
        public void ApplyOverride_PositionOffset_AddsToLocalPosition()
        {
            _testObject.transform.localPosition = Vector3.zero;
            var ovr = new NarrativePropertyOverride { Type = OverrideType.PositionOffset, PositionOffset = new Vector3(1, 2, 3) };
            
            NLM_Applicator.ApplyOverride(_testObject, ovr);
            
            Assert.AreEqual(new Vector3(1, 2, 3), _testObject.transform.localPosition);
        }

        [Test]
        public void ApplyOverride_ScaleMultiplier_MultipliesLocalScale()
        {
            _testObject.transform.localScale = new Vector3(2, 2, 2);
            var ovr = new NarrativePropertyOverride { Type = OverrideType.ScaleMultiplier, ScaleMultiplier = new Vector3(0.5f, 1f, 2f) };
            
            NLM_Applicator.ApplyOverride(_testObject, ovr);
            
            Assert.AreEqual(new Vector3(1, 2, 4), _testObject.transform.localScale);
        }

        [Test]
        public void ApplyOverride_EnableComponent_EnablesBehaviour()
        {
            var collider = _testObject.AddComponent<BoxCollider>();
            collider.enabled = false;
            var ovr = new NarrativePropertyOverride { Type = OverrideType.EnableComponent, ComponentTypeName = "BoxCollider" };
            
            NLM_Applicator.ApplyOverride(_testObject, ovr);
            
            Assert.IsTrue(collider.enabled);
        }

        [Test]
        public void ApplyOverride_DisableComponent_DisablesBehaviour()
        {
            var collider = _testObject.AddComponent<BoxCollider>();
            collider.enabled = true;
            var ovr = new NarrativePropertyOverride { Type = OverrideType.DisableComponent, ComponentTypeName = "BoxCollider" };
            
            NLM_Applicator.ApplyOverride(_testObject, ovr);
            
            Assert.IsFalse(collider.enabled);
        }

        [Test]
        public void ApplyOverride_EnableComponent_MissingComponent_DoesNotThrow()
        {
            var ovr = new NarrativePropertyOverride { Type = OverrideType.EnableComponent, ComponentTypeName = "NonExistentComponent" };
            Assert.DoesNotThrow(() => NLM_Applicator.ApplyOverride(_testObject, ovr));
        }

        [Test]
        public void ApplyOverride_ReplaceGameObject_DeactivatesTarget()
        {
            _testObject.SetActive(true);
            var replacement = new GameObject("Replacement");
            replacement.SetActive(false);
            var ovr = new NarrativePropertyOverride { Type = OverrideType.ReplaceGameObject, ReplacementObject = replacement };
            
            NLM_Applicator.ApplyOverride(_testObject, ovr);
            
            Assert.IsFalse(_testObject.activeSelf);
            Assert.IsTrue(replacement.activeSelf);
            
            Object.DestroyImmediate(replacement);
        }
    }
}
