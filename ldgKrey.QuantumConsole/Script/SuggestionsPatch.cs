using HarmonyLib;
using Mod.QFSW.QC;
using Mod.QFSW.QC.Pooling;
using Mod.QFSW.QC.Utilities;
using QFSW.QC;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace LDGKrey.QCEnabler
{
    [HarmonyPatch]
    public static class SuggestionsPatch
    {
        #region Adds
        public static SuggestionStack _suggestionStack;
        public static event Action<SuggestionSet> OnSuggestionSetGenerated;
        static StringBuilderPool _stringBuilderPool = new StringBuilderPool();

        private static void InitializeSuggestionStack()
        {
            if (_suggestionStack == null)
            {
                _suggestionStack = new SuggestionStack();
                _suggestionStack.OnSuggestionSetCreated += OnSuggestionSetGenerated;
            }
        }

        #endregion

        #region Patches
        [HarmonyPatch(typeof(QuantumConsole), "Initialize")]
        [HarmonyPostfix]
        static void Initialize_AddSuggestionInitialize_Patch()
        {
            InitializeSuggestionStack();

            //var one = typeof(SuggestionStack).GetField("_suggestor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_suggestionStack);
            //QuantumConsoleMod.log.Log($"Quantum suggestor: {one != null}");

            //var two = typeof(QuantumSuggestor).GetField("_suggestors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(one) as IQcSuggestor[];
            //QuantumConsoleMod.log.Log($"IQcSuggestor: {two != null}");
            //QuantumConsoleMod.log.Log($"IQcSuggestor: {two.Length}");

            //foreach (var item in two)
            //{
            //    QuantumConsoleMod.log.Log($"IQcSuggestor: {item.GetType().Name}"); ;
            //}
        }



        [HarmonyPatch(typeof(QuantumConsole), "OnTextChange")]
        [HarmonyPrefix]
        static bool OnTextChange_Override_Patch()
        {
            if (_selectedPreviousCommandIndex >= 0 && _currentText.Trim() !=
               _previousCommands[_previousCommands.Count - _selectedPreviousCommandIndex - 1])
            {
                ClearHistoricalSuggestions();
            }

            //normally here would be a if check for autocomplete but i omit i for now.... Todo?
            UpdateSuggestions();

            //skip original implementation
            return false;
        }

        static void UpdateSuggestions()
        {
            //normally here would be more settings action but i decide to omit it for now... Todo?
            SuggestorOptions options = new SuggestorOptions
            {
                CaseSensitive = false,
                Fuzzy = true,
                CollapseOverloads = true,
            };

            _suggestionStack.UpdateStack(_currentText, options);

            UpdateSuggestionText();
            UpdatePopupDisplay();
        }

        static void UpdateSuggestionText()
        {
            Color suggestionColor = _theme
                ? _theme.SuggestionColor
                : Color.gray;

            StringBuilder buffer = _stringBuilderPool.GetStringBuilder();
            buffer.AppendColoredText(_currentText, Color.clear);
            buffer.AppendColoredText(_suggestionStack.GetCompletionTail(), suggestionColor);

            _consoleSuggestionText.text = _stringBuilderPool.ReleaseAndToString(buffer);
        }

        static void UpdatePopupDisplay()
        {
            SuggestionSet suggestionSet = _suggestionStack.TopmostSuggestionSet;
            if (suggestionSet == null || suggestionSet.Suggestions.Count == 0)
            {
                ClearPopupDisplay();
            }
            else
            {
                string formattedSuggestions = GetFormattedSuggestions(suggestionSet);
                if (_suggestionDisplayOrder == SortOrder.Ascending)
                {
                    formattedSuggestions = formattedSuggestions.ReverseItems('\n');
                }

                _suggestionPopupRect.gameObject.SetActive(true);
                _suggestionPopupText.text = formattedSuggestions;
            }
        }

        static string GetFormattedSuggestions(SuggestionSet suggestionSet)
        {
            StringBuilder buffer = _stringBuilderPool.GetStringBuilder();
            GetFormattedSuggestions(suggestionSet, buffer);
            return _stringBuilderPool.ReleaseAndToString(buffer);
        }

        static void GetFormattedSuggestions(SuggestionSet suggestionSet, StringBuilder buffer)
        {
            int displaySize = suggestionSet.Suggestions.Count;
            if (_maxSuggestionDisplaySize > 0)
            {
                displaySize = Mathf.Min(displaySize, _maxSuggestionDisplaySize + 1);
            }

            for (int i = 0; i < displaySize; i++)
            {
                if (_maxSuggestionDisplaySize > 0 && i >= _maxSuggestionDisplaySize)
                {
                    const string remainingSuggestion = "...";
                    if (_theme && suggestionSet.SelectionIndex >= _maxSuggestionDisplaySize)
                    {
                        buffer.AppendColoredText(remainingSuggestion, _theme.SelectedSuggestionColor);
                    }
                    else
                    {
                        buffer.Append(remainingSuggestion);
                    }
                }
                else
                {
                    bool selected = i == suggestionSet.SelectionIndex;

                    buffer.Append("<link=");
                    buffer.Append(i);
                    buffer.Append(">");
                    FormatSuggestion(suggestionSet.Suggestions[i], selected, buffer);
                    buffer.AppendLine("</link>");
                }
            }
        }

        static void FormatSuggestion(IQcSuggestion suggestion, bool selected, StringBuilder buffer)
        {
            if (!_theme)
            {
                buffer.Append(suggestion.FullSignature);
                return;
            }

            Color primaryColor = Color.white;
            Color secondaryColor = _theme.SuggestionColor;
            if (selected)
            {
                primaryColor *= _theme.SelectedSuggestionColor;
                secondaryColor *= _theme.SelectedSuggestionColor;
            }

            buffer.AppendColoredText(suggestion.PrimarySignature, primaryColor);
            buffer.AppendColoredText(suggestion.SecondarySignature, secondaryColor);
        }

        [HarmonyPatch(typeof(QuantumConsole), "ClearSuggestions")]
        [HarmonyPrefix]
        static bool ClearSuggestions_Override_Patch()
        {
            _suggestionStack.Clear();
            _consoleSuggestionText.text = string.Empty;

            //skip original implementation
            return false;
        }

        [HarmonyPatch(typeof(QuantumConsole), "ProcessAutocomplete")]
        [HarmonyPrefix]
        static bool ProcessAutocomplete_Override_Patch()
        {
            if (QuantumConsoleMod.Instance.keyConfig.SuggestNextCommandKey.IsPressed() || QuantumConsoleMod.Instance.keyConfig.SuggestPreviousCommandKey.IsPressed())
            {
                SuggestionSet set = _suggestionStack.TopmostSuggestionSet;
                if (set != null && set.Suggestions.Count > 0)
                {
                    if (QuantumConsoleMod.Instance.keyConfig.SuggestNextCommandKey.IsPressed()) { set.SelectionIndex++; }
                    if (QuantumConsoleMod.Instance.keyConfig.SuggestPreviousCommandKey.IsPressed()) { set.SelectionIndex--; }

                    set.SelectionIndex += set.Suggestions.Count;
                    set.SelectionIndex %= set.Suggestions.Count;
                    SetSuggestion_Override_Patch(set.SelectionIndex);
                }
            }

            //skip original implementation
            return false;
        }

        [HarmonyPatch(typeof(QuantumConsole), "SetCommandSuggestion", typeof(int))]
        [HarmonyPrefix]
        static bool SetSuggestion_Override_Patch(int suggestionIndex)
        {
            if (!_suggestionStack.SetSuggestionIndex(suggestionIndex))
            {
                throw new ArgumentException($"Cannot set suggestion to index {suggestionIndex}.");
            }

            OverrideConsoleInput(_suggestionStack.GetCompletion());
            UpdateSuggestionText();

            //skip original implementation
            return false;
        }

        #endregion

        #region ReflectionMagic

        static FieldInfo _selectedPreviousCommandIndex_Field = typeof(QuantumConsole).GetField("_selectedPreviousCommandIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        static int _selectedPreviousCommandIndex 
        {
            get => (int)_selectedPreviousCommandIndex_Field.GetValue(QuantumConsole.Instance);
            set => _selectedPreviousCommandIndex_Field.SetValue(QuantumConsole.Instance, value);
        }

        static FieldInfo _currentText_Field = typeof(QuantumConsole).GetField("_currentText", BindingFlags.Instance | BindingFlags.NonPublic);
        static string _currentText
        {
            get => (string)_currentText_Field.GetValue(QuantumConsole.Instance);
            set => _currentText_Field.SetValue(QuantumConsole.Instance, value);
        }

        static FieldInfo _previousCommands_Field = typeof(QuantumConsole).GetField("_previousCommands", BindingFlags.Instance | BindingFlags.NonPublic);
        static List<string> _previousCommands
        {
            get => (List<string>)_previousCommands_Field.GetValue(QuantumConsole.Instance);
        }

        static MethodInfo ClearHistoricalSuggestions_Method = typeof(QuantumConsole).GetMethod("ClearHistoricalSuggestions", BindingFlags.Instance | BindingFlags.NonPublic);
        static void ClearHistoricalSuggestions() => ClearHistoricalSuggestions_Method.Invoke(QuantumConsole.Instance, new object[] { });

        static FieldInfo _consoleSuggestionText_Field = typeof(QuantumConsole).GetField("_consoleSuggestionText", BindingFlags.Instance | BindingFlags.NonPublic);
        static TextMeshProUGUI _consoleSuggestionText
        {
            get => (TextMeshProUGUI)_consoleSuggestionText_Field.GetValue(QuantumConsole.Instance);
        }

        static FieldInfo _theme_Field = typeof(QuantumConsole).GetField("_theme", BindingFlags.Instance | BindingFlags.NonPublic);
        static QuantumTheme _theme
        {
            get => (QuantumTheme)_theme_Field.GetValue(QuantumConsole.Instance);
        }

        static MethodInfo ClearPopupDisplay_Method = typeof(QuantumConsole).GetMethod("ClearPopup", BindingFlags.Instance | BindingFlags.NonPublic);
        static void ClearPopupDisplay() => ClearPopupDisplay_Method.Invoke(QuantumConsole.Instance, new object[] { });

        static FieldInfo _suggestionDisplayOrder_Field = typeof(QuantumConsole).GetField("_suggestionDisplayOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        static SortOrder _suggestionDisplayOrder
        {
            get => (SortOrder)_suggestionDisplayOrder_Field.GetValue(QuantumConsole.Instance);
            set => _suggestionDisplayOrder_Field.SetValue(QuantumConsole.Instance, value);
        }

        static FieldInfo _suggestionPopupRect_Field = typeof(QuantumConsole).GetField("_suggestionPopupRect", BindingFlags.Instance | BindingFlags.NonPublic);
        static RectTransform _suggestionPopupRect
        {
            get => (RectTransform)_suggestionPopupRect_Field.GetValue(QuantumConsole.Instance);
        }

        static FieldInfo _suggestionPopupText_Field = typeof(QuantumConsole).GetField("_suggestionPopupText", BindingFlags.Instance | BindingFlags.NonPublic);
        static TextMeshProUGUI _suggestionPopupText
        {
            get => (TextMeshProUGUI)_suggestionPopupText_Field.GetValue(QuantumConsole.Instance);
        }

        static FieldInfo _maxSuggestionDisplaySize_Field = typeof(QuantumConsole).GetField("_maxSuggestionDisplaySize", BindingFlags.Instance | BindingFlags.NonPublic);
        static int _maxSuggestionDisplaySize
        {
            get => (int)_maxSuggestionDisplaySize_Field.GetValue(QuantumConsole.Instance);
        }

        static MethodInfo OverrideConsoleInput_Method = typeof(QuantumConsole).GetMethod("OverrideConsoleInput", BindingFlags.Instance | BindingFlags.Public);
        static void OverrideConsoleInput(string newInput, bool shouldFocus = true) => OverrideConsoleInput_Method.Invoke(QuantumConsole.Instance, new object[] { newInput, shouldFocus });

        #endregion
    }
}
