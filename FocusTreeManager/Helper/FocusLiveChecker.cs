using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FocusTreeManager.Model;
using FocusTreeManager.Model.TabModels;

namespace FocusTreeManager.Helper
{
    /// <summary>
    /// Quick checks of ONE focus while the user is typing its properties (the full tree
    /// check is <see cref="FocusTreeValidator"/>). It is cheap enough to run on every key
    /// stroke and it only reads the models.
    /// </summary>
    public static class FocusLiveChecker
    {
        public static List<ValidationIssue> Check(FocusModel focus,
            IEnumerable<FocusModel> treeFoci,
            IEnumerable<KeyValuePair<string, IEnumerable<FocusModel>>> otherTrees)
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();
            if (focus == null) return issues;
            List<FocusModel> others = (treeFoci ?? Enumerable.Empty<FocusModel>())
                .Where(f => f != null && !ReferenceEquals(f, focus)).ToList();
            CheckId(focus, others, otherTrees, issues);
            CheckTextKey(focus, issues);
            CheckPosition(focus, others, issues);
            return issues;
        }

        private static void CheckId(FocusModel focus, List<FocusModel> others,
            IEnumerable<KeyValuePair<string, IEnumerable<FocusModel>>> otherTrees,
            List<ValidationIssue> issues)
        {
            string id = focus.UniqueName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error,
                    LocalizationHelper.Tr("Live_Empty", "The focus id is empty.")));
                return;
            }
            if (id.Any(char.IsWhiteSpace))
            {
                string suggestion = Regex.Replace(id.Trim(), @"\s+", "_");
                issues.Add(new ValidationIssue(ValidationSeverity.Error,
                    LocalizationHelper.Tr("Live_Space",
                        "The id '{0}' contains a space, so the game reads it as several " +
                        "words. Use _ instead, e.g. '{1}'.", id, suggestion)));
            }
            //Anything but Latin letters, digits and the underscore (spaces are reported above)
            List<char> risky = id.Where(c => !char.IsWhiteSpace(c) && !IsSafeIdChar(c))
                .Distinct().ToList();
            if (risky.Count > 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                    LocalizationHelper.Tr("Live_Chars",
                        "The id contains characters that are risky in game scripts: {0}. " +
                        "Use only Latin letters, digits and _.",
                        string.Join(" ", risky.Take(8)))));
            }
            if (char.IsDigit(id.TrimStart()[0]))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                    LocalizationHelper.Tr("Live_Digit",
                        "The id starts with a digit, which the game may read as a number.")));
            }
            string trimmed = id.Trim();
            if (others.Any(f => string.Equals(f.UniqueName, id, StringComparison.Ordinal) ||
                                string.Equals(f.UniqueName?.Trim(), trimmed,
                                              StringComparison.Ordinal)))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error,
                    LocalizationHelper.Tr("Live_Dup",
                        "Another focus in this tree already uses the id '{0}'.", trimmed)));
            }
            else
            {
                FocusModel sameCase = others.FirstOrDefault(f =>
                    string.Equals(f.UniqueName?.Trim(), trimmed,
                                  StringComparison.OrdinalIgnoreCase));
                if (sameCase != null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                        LocalizationHelper.Tr("Live_DupCase",
                            "Another focus has the same id except for letter case ('{0}').",
                            sameCase.UniqueName)));
                }
            }
            if (otherTrees != null)
            {
                foreach (KeyValuePair<string, IEnumerable<FocusModel>> tree in otherTrees)
                {
                    if (tree.Value != null && tree.Value.Any(f => f != null &&
                        string.Equals(f.UniqueName?.Trim(), trimmed, StringComparison.Ordinal)))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                            LocalizationHelper.Tr("Live_DupTree",
                                "The id '{0}' is also used in the tree '{1}'. Focus ids must " +
                                "be unique across all trees.", trimmed, tree.Key)));
                        break;
                    }
                }
            }
        }

        private static bool IsSafeIdChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                   (c >= '0' && c <= '9') || c == '_';
        }

        private static void CheckTextKey(FocusModel focus, List<ValidationIssue> issues)
        {
            string key = focus.Text;
            if (!string.IsNullOrEmpty(key) && key.Any(char.IsWhiteSpace))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                    LocalizationHelper.Tr("Live_TextSpace",
                        "The text key '{0}' contains a space. Localisation keys must not " +
                        "contain spaces; use _ instead.", key)));
            }
        }

        private static void CheckPosition(FocusModel focus, List<FocusModel> others,
                                          List<ValidationIssue> issues)
        {
            try
            {
                FocusModel same = others.FirstOrDefault(f =>
                    f.DisplayX == focus.DisplayX && f.DisplayY == focus.DisplayY);
                if (same != null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                        LocalizationHelper.Tr("Live_Overlap",
                            "This focus is on the same spot ({0}, {1}) as '{2}'.",
                            focus.DisplayX, focus.DisplayY,
                            string.IsNullOrWhiteSpace(same.UniqueName) ? "?" : same.UniqueName)));
                }
            }
            catch (Exception)
            {
                //A broken position chain is reported by the full validator
            }
        }
    }
}
