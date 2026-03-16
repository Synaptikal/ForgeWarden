using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="NarrativeObjectBinding"/>.
    /// </summary>
    /// <remarks>
    /// Shows:
    /// <list type="bullet">
    /// <item>Reorderable rule list with per-rule foldouts</item>
    /// <item>Live condition evaluation preview against a user-selected state</item>
    /// <item>Per-rule summary of active overrides</item>
    /// <item>Add/remove rules and overrides inline</item>
    /// <item>Copy/paste rules between bindings</item>
    /// <item>Variable dropdown with available variables</item>
    /// <item>"Find in NLM" button to open the main window</item>
    /// </list>
    /// </remarks>
    [CustomEditor(typeof(NarrativeObjectBinding))]
    public class NarrativeObjectBindingEditor : UnityEditor.Editor
    {
        private NarrativeObjectBinding _binding;
        private ReorderableList _ruleList;
        private NarrativeStateDefinition _previewState;
        private bool _showPreview = true;
        private List<bool> _ruleFoldouts = new();
        private int _selectedRuleIndex = -1;
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            _binding = (NarrativeObjectBinding)target;
            BuildRuleList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawLivePreview();
            DrawRules();

            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }

        #region Drawing Methods

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Narrative Object Binding", EditorStyles.boldLabel);
            if (GUILayout.Button("Open NLM Window", GUILayout.Width(140)))
                NLM_MainWindow.Open();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HideInNLMList"));
            EditorGUILayout.Space(8);
        }

        private void DrawLivePreview()
        {
            _showPreview = EditorGUILayout.Foldout(_showPreview, "Live Condition Preview", true, EditorStyles.foldoutHeader);
            if (!_showPreview) return;

            EditorGUI.indentLevel++;
            _previewState = (NarrativeStateDefinition)EditorGUILayout.ObjectField(
                "Preview State", _previewState, typeof(NarrativeStateDefinition), false);

            if (_previewState != null)
            {
                var triggered = NLM_Evaluator.ResolveBinding(_binding, _previewState);
                if (triggered.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"✔  Rule '{triggered[0].RuleName}' fires.\n" +
                        $"   {triggered[0].Overrides.Count} override(s) will apply.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("No rule matches this state.", MessageType.Warning);
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }

        private void DrawRules()
        {
            EditorGUILayout.LabelField("Rules  (evaluated top → bottom, first match wins)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Add an empty-condition rule at the bottom as your always-true fallback.",
                MessageType.None);

            // Keep foldout list in sync
            while (_ruleFoldouts.Count < _binding.Rules.Count) _ruleFoldouts.Add(true);
            while (_ruleFoldouts.Count > _binding.Rules.Count) _ruleFoldouts.RemoveAt(_ruleFoldouts.Count - 1);

            for (int ri = 0; ri < _binding.Rules.Count; ri++)
            {
                DrawRuleGUI(ri, _binding.Rules[ri]);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Rule"))
            {
                Undo.RecordObject(_binding, "Add Binding Rule");
                _binding.Rules.Add(new NarrativeObjectBinding.BindingRule
                {
                    RuleName = $"Rule {_binding.Rules.Count}"
                });
                EditorUtility.SetDirty(_binding);
            }

            GUILayout.FlexibleSpace();

            // Copy/Paste buttons
            GUI.enabled = _selectedRuleIndex >= 0 && _selectedRuleIndex < _binding.Rules.Count;
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                CopyRule(_selectedRuleIndex);
            }
            GUI.enabled = NLM_Clipboard.HasRule();
            if (GUILayout.Button("Paste", GUILayout.Width(60)))
            {
                PasteRule();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuleGUI(int ri, NarrativeObjectBinding.BindingRule rule)
        {
            // Determine if rule is active in preview
            bool isActive = _previewState != null && NLM_Evaluator.EvaluateGroup(rule.Condition, _previewState);
            Color bgColor = isActive ? new Color(0.1f, 0.28f, 0.12f) : new Color(0.18f, 0.18f, 0.18f);

            // Selection highlight
            if (_selectedRuleIndex == ri)
            {
                bgColor = new Color(0.2f, 0.35f, 0.55f);
            }

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            // Rule header
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_selectedRuleIndex == ri, "", GUILayout.Width(20)))
            {
                _selectedRuleIndex = ri;
            }

            _ruleFoldouts[ri] = EditorGUILayout.Foldout(_ruleFoldouts[ri],
                $"Rule {ri}: {rule.RuleName}", true, EditorStyles.foldoutHeader);

            // Move buttons
            GUI.enabled = ri > 0;
            if (GUILayout.Button("↑", GUILayout.Width(22))) SwapRules(ri, ri - 1);
            GUI.enabled = ri < _binding.Rules.Count - 1;
            if (GUILayout.Button("↓", GUILayout.Width(22))) SwapRules(ri, ri + 1);
            GUI.enabled = true;

            // Delete button
            GUI.backgroundColor = new Color(0.75f, 0.2f, 0.2f);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                DeleteRule(ri);
                GUI.backgroundColor = prevBg;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();

            if (!_ruleFoldouts[ri]) { EditorGUILayout.EndVertical(); return; }

            // Rule properties
            EditorGUI.BeginChangeCheck();
            rule.RuleName = EditorGUILayout.TextField("Rule Name", rule.RuleName);
            rule.FallThrough = EditorGUILayout.Toggle("Fall Through", rule.FallThrough);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_binding);

            // Conditions
            DrawConditions(rule);

            // Overrides
            DrawOverrides(rule);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawConditions(NarrativeObjectBinding.BindingRule rule)
        {
            EditorGUILayout.LabelField("Conditions", EditorStyles.miniBoldLabel);
            if (rule.Condition.IsEmpty)
                EditorGUILayout.HelpBox("No conditions — this rule always fires (fallback).", MessageType.None);

            // Get available variables for dropdown
            var availableVars = GetAvailableVariables();

            for (int ci = 0; ci < rule.Condition.Conditions.Count; ci++)
            {
                var cond = rule.Condition.Conditions[ci];
                EditorGUILayout.BeginHorizontal();

                // Variable dropdown or text field
                if (availableVars.Count > 0)
                {
                    int varIndex = availableVars.IndexOf(cond.VariableName);
                    if (varIndex < 0) varIndex = 0;

                    var newIndex = EditorGUILayout.Popup(varIndex, availableVars.ToArray(), GUILayout.Width(120));
                    if (newIndex != varIndex || string.IsNullOrEmpty(cond.VariableName))
                    {
                        cond.VariableName = availableVars[newIndex];
                        EditorUtility.SetDirty(_binding);
                    }
                }
                else
                {
                    cond.VariableName = EditorGUILayout.TextField(cond.VariableName, GUILayout.Width(120));
                }

                cond.Operator = (ConditionOperator)EditorGUILayout.EnumPopup(cond.Operator, GUILayout.Width(100));
                cond.Value = EditorGUILayout.TextField(cond.Value, GUILayout.Width(80));

                if (ci < rule.Condition.Conditions.Count - 1)
                    cond.LogicToNext = (ConditionLogic)EditorGUILayout.EnumPopup(cond.LogicToNext, GUILayout.Width(46));

                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    DeleteCondition(rule, ci);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Condition", GUILayout.Width(100)))
            {
                Undo.RecordObject(_binding, "Add Condition");
                rule.Condition.Conditions.Add(new NarrativeCondition
                {
                    VariableName = availableVars.Count > 0 ? availableVars[0] : ""
                });
                EditorUtility.SetDirty(_binding);
            }
        }

        private void DrawOverrides(NarrativeObjectBinding.BindingRule rule)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Property Overrides", EditorStyles.miniBoldLabel);

            for (int oi = 0; oi < rule.Overrides.Count; oi++)
            {
                var ovr = rule.Overrides[oi];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                ovr.Type = (OverrideType)EditorGUILayout.EnumPopup(ovr.Type, GUILayout.Width(160));
                EditorGUILayout.LabelField(ovr.Summary(), EditorStyles.miniLabel);
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    DeleteOverride(rule, oi);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();
                DrawOverrideFields(ovr);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Override", GUILayout.Width(100)))
            {
                Undo.RecordObject(_binding, "Add Override");
                rule.Overrides.Add(new NarrativePropertyOverride());
                EditorUtility.SetDirty(_binding);
            }
        }

        private static void DrawOverrideFields(NarrativePropertyOverride ovr)
        {
            EditorGUI.indentLevel++;
            switch (ovr.Type)
            {
                case OverrideType.SetActive:
                    ovr.ActiveState = EditorGUILayout.Toggle("Active State", ovr.ActiveState);
                    break;
                case OverrideType.SwapMaterial:
                    ovr.MaterialIndex = EditorGUILayout.IntField("Material Index", ovr.MaterialIndex);
                    ovr.TargetMaterial = (Material)EditorGUILayout.ObjectField("Target Material", ovr.TargetMaterial, typeof(Material), false);
                    break;
                case OverrideType.PositionOffset:
                    ovr.PositionOffset = EditorGUILayout.Vector3Field("Position Offset", ovr.PositionOffset);
                    break;
                case OverrideType.ScaleMultiplier:
                    ovr.ScaleMultiplier = EditorGUILayout.Vector3Field("Scale Multiplier", ovr.ScaleMultiplier);
                    break;
                case OverrideType.EnableComponent:
                case OverrideType.DisableComponent:
                    ovr.ComponentTypeName = EditorGUILayout.TextField("Component Type", ovr.ComponentTypeName);
                    break;
                case OverrideType.SetColor:
                    ovr.TargetColor = EditorGUILayout.ColorField("Target Color", ovr.TargetColor);
                    break;
                case OverrideType.ReplaceGameObject:
                    ovr.ReplacementObject = (GameObject)EditorGUILayout.ObjectField("Replacement", ovr.ReplacementObject, typeof(GameObject), true);
                    break;
            }
            EditorGUI.indentLevel--;
        }

        #endregion

        #region Helper Methods

        private void BuildRuleList() { } // Kept for future ReorderableList upgrade

        private List<string> GetAvailableVariables()
        {
            var vars = new HashSet<string>();

            // Collect from all layers
            var layers = NLM_EditorAssetUtility.FindAllAssetsOfType<NarrativeLayerDefinition>();
            foreach (var layer in layers)
            {
                if (layer?.Beats == null) continue;
                foreach (var beat in layer.Beats)
                {
                    if (beat?.State?.Variables == null) continue;
                    foreach (var v in beat.State.Variables)
                    {
                        if (!string.IsNullOrWhiteSpace(v.Name))
                            vars.Add(v.Name);
                    }
                }
            }

            return vars.ToList();
        }

        private void SwapRules(int a, int b)
        {
            Undo.RecordObject(_binding, "Reorder Binding Rules");
            (_binding.Rules[a], _binding.Rules[b]) = (_binding.Rules[b], _binding.Rules[a]);
            (_ruleFoldouts[a], _ruleFoldouts[b]) = (_ruleFoldouts[b], _ruleFoldouts[a]);
            if (_selectedRuleIndex == a) _selectedRuleIndex = b;
            else if (_selectedRuleIndex == b) _selectedRuleIndex = a;
            EditorUtility.SetDirty(_binding);
        }

        private void DeleteRule(int index)
        {
            Undo.RecordObject(_binding, "Remove Binding Rule");
            _binding.Rules.RemoveAt(index);
            _ruleFoldouts.RemoveAt(index);
            if (_selectedRuleIndex == index) _selectedRuleIndex = -1;
            else if (_selectedRuleIndex > index) _selectedRuleIndex--;
            EditorUtility.SetDirty(_binding);
        }

        private void DeleteCondition(NarrativeObjectBinding.BindingRule rule, int index)
        {
            Undo.RecordObject(_binding, "Remove Condition");
            rule.Condition.Conditions.RemoveAt(index);
            EditorUtility.SetDirty(_binding);
        }

        private void DeleteOverride(NarrativeObjectBinding.BindingRule rule, int index)
        {
            Undo.RecordObject(_binding, "Remove Override");
            rule.Overrides.RemoveAt(index);
            EditorUtility.SetDirty(_binding);
        }

        private void CopyRule(int index)
        {
            if (index < 0 || index >= _binding.Rules.Count) return;
            NLM_Clipboard.CopyRule(_binding.Rules[index]);
            EditorUtility.DisplayDialog("Rule Copied", "Rule copied to clipboard. Select another binding and paste to duplicate.", "OK");
        }

        private void PasteRule()
        {
            var rule = NLM_Clipboard.PasteRule();
            if (rule == null)
            {
                EditorUtility.DisplayDialog("Paste Failed", "No rule in clipboard to paste.", "OK");
                return;
            }

            Undo.RecordObject(_binding, "Paste Binding Rule");
            _binding.Rules.Add(rule);
            _ruleFoldouts.Add(true);
            EditorUtility.SetDirty(_binding);
        }

        #endregion
    }
}
