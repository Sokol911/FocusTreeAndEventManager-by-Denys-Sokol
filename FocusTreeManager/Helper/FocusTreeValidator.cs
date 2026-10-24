using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FocusTreeManager.Model;

namespace FocusTreeManager.Helper
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class ValidationIssue
    {
        public ValidationSeverity Severity { get; private set; }

        public string Message { get; private set; }

        public ValidationIssue(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>
    /// Looks for problems in a focus tree that the game would refuse or silently break on:
    /// duplicate ids, links to focuses that are not in the tree, focuses that can never be
    /// unlocked (prerequisite loops), impossible exclusivity, relative position loops and
    /// focuses sitting on top of each other.
    /// The validator only reads the models, it never changes them.
    /// </summary>
    public static class FocusTreeValidator
    {
        public static List<ValidationIssue> Validate(IEnumerable<FocusModel> foci)
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();
            if (foci == null) return issues;
            List<FocusModel> list = foci.Where(f => f != null).ToList();
            HashSet<FocusModel> inTree = new HashSet<FocusModel>(list);
            CheckNames(list, issues);
            CheckLinks(list, inTree, issues);
            CheckUnlockable(list, inTree, issues);
            CheckExclusivity(list, inTree, issues);
            HashSet<FocusModel> badRelative = CheckRelativePositions(list, inTree, issues);
            CheckOverlaps(list, badRelative, issues);
            return issues;
        }

        private static string Name(FocusModel focus)
        {
            return string.IsNullOrWhiteSpace(focus.UniqueName) ? "(no id)" : focus.UniqueName;
        }

        private static void CheckNames(List<FocusModel> list, List<ValidationIssue> issues)
        {
            foreach (FocusModel focus in list.Where(f => string.IsNullOrWhiteSpace(f.UniqueName)))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error,
                    "A focus at (" + focus.X + ", " + focus.Y + ") has no id."));
            }
            foreach (IGrouping<string, FocusModel> group in list
                .Where(f => !string.IsNullOrWhiteSpace(f.UniqueName))
                .GroupBy(f => f.UniqueName).Where(g => g.Count() > 1))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error,
                    "The id '" + group.Key + "' is used by " + group.Count() + " focuses."));
            }
        }

        private static void CheckLinks(List<FocusModel> list, HashSet<FocusModel> inTree,
                                       List<ValidationIssue> issues)
        {
            foreach (FocusModel focus in list)
            {
                if (focus.Prerequisite == null) continue;
                foreach (PrerequisitesSetModel set in focus.Prerequisite)
                {
                    if (set == null || set.FociList == null || set.FociList.Count == 0)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                            "'" + Name(focus) + "' has an empty prerequisite group."));
                        continue;
                    }
                    foreach (FocusModel required in set.FociList)
                    {
                        if (required == focus)
                        {
                            issues.Add(new ValidationIssue(ValidationSeverity.Error,
                                "'" + Name(focus) + "' is a prerequisite of itself."));
                        }
                        else if (required == null || !inTree.Contains(required))
                        {
                            issues.Add(new ValidationIssue(ValidationSeverity.Error,
                                "'" + Name(focus) + "' requires '" +
                                (required == null ? "(nothing)" : Name(required)) +
                                "', which is not in this tree."));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A focus can be taken when, for every prerequisite group, at least one member of
        /// the group can be taken (a group is an "or", the groups together are an "and").
        /// Anything that is still locked once nothing more can be unlocked is stuck forever,
        /// typically because of a loop.
        /// </summary>
        private static void CheckUnlockable(List<FocusModel> list, HashSet<FocusModel> inTree,
                                            List<ValidationIssue> issues)
        {
            HashSet<FocusModel> unlocked = new HashSet<FocusModel>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (FocusModel focus in list)
                {
                    if (unlocked.Contains(focus)) continue;
                    bool canUnlock = true;
                    if (focus.Prerequisite != null)
                    {
                        foreach (PrerequisitesSetModel set in focus.Prerequisite)
                        {
                            //Empty groups are reported elsewhere and do not block anything
                            if (set == null || set.FociList == null || set.FociList.Count == 0)
                            {
                                continue;
                            }
                            //Links to focuses outside of the tree are reported elsewhere
                            if (set.FociList.Any(f => f != null && !inTree.Contains(f)))
                            {
                                continue;
                            }
                            if (!set.FociList.Any(f => f != null && unlocked.Contains(f)))
                            {
                                canUnlock = false;
                                break;
                            }
                        }
                    }
                    if (!canUnlock) continue;
                    unlocked.Add(focus);
                    changed = true;
                }
            }
            foreach (FocusModel focus in list.Where(f => !unlocked.Contains(f)))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error,
                    "'" + Name(focus) + "' can never be unlocked (its prerequisites loop back " +
                    "on it or depend on a focus that can never be unlocked)."));
            }
        }

        private static void CheckExclusivity(List<FocusModel> list, HashSet<FocusModel> inTree,
                                             List<ValidationIssue> issues)
        {
            foreach (FocusModel focus in list)
            {
                if (focus.MutualyExclusive == null) continue;
                HashSet<FocusModel> exclusive = new HashSet<FocusModel>();
                foreach (MutuallyExclusiveSetModel set in focus.MutualyExclusive)
                {
                    if (set == null) continue;
                    FocusModel other = set.Focus1 == focus ? set.Focus2 : set.Focus1;
                    if (other == null)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                            "'" + Name(focus) + "' has an incomplete mutually exclusive link."));
                    }
                    else if (other == focus)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error,
                            "'" + Name(focus) + "' is mutually exclusive with itself."));
                    }
                    else if (!inTree.Contains(other))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error,
                            "'" + Name(focus) + "' is mutually exclusive with '" + Name(other) +
                            "', which is not in this tree."));
                    }
                    else
                    {
                        exclusive.Add(other);
                    }
                }
                if (exclusive.Count == 0 || focus.Prerequisite == null) continue;
                //A prerequisite group whose members are all exclusive with the focus can never
                //be satisfied: taking one of them forbids the focus.
                foreach (PrerequisitesSetModel set in focus.Prerequisite)
                {
                    if (set == null || set.FociList == null || set.FociList.Count == 0) continue;
                    if (set.FociList.All(f => f != null && exclusive.Contains(f)))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error,
                            "'" + Name(focus) + "' can never be taken: every focus of one of " +
                            "its prerequisite groups is mutually exclusive with it."));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the focuses whose position chain is broken. Their position must not be
        /// computed (DisplayX/DisplayY follow the chain and would never end on a loop).
        /// </summary>
        private static HashSet<FocusModel> CheckRelativePositions(List<FocusModel> list,
            HashSet<FocusModel> inTree, List<ValidationIssue> issues)
        {
            HashSet<FocusModel> broken = new HashSet<FocusModel>();
            foreach (FocusModel focus in list)
            {
                HashSet<FocusModel> visited = new HashSet<FocusModel>();
                FocusModel current = focus;
                bool isLoop = false;
                bool isOutside = false;
                while (current != null)
                {
                    if (!visited.Add(current))
                    {
                        isLoop = true;
                        break;
                    }
                    FocusModel next = current.CoordinatesRelativeTo;
                    if (next != null && !inTree.Contains(next))
                    {
                        isOutside = true;
                        break;
                    }
                    current = next;
                }
                if (isLoop)
                {
                    broken.Add(focus);
                    issues.Add(new ValidationIssue(ValidationSeverity.Error,
                        "The position of '" + Name(focus) +
                        "' is relative to a chain of focuses that loops."));
                }
                else if (isOutside)
                {
                    broken.Add(focus);
                    issues.Add(new ValidationIssue(ValidationSeverity.Error,
                        "The position of '" + Name(focus) +
                        "' is relative to a focus that is not in this tree."));
                }
            }
            return broken;
        }

        private static void CheckOverlaps(List<FocusModel> list, HashSet<FocusModel> broken,
                                          List<ValidationIssue> issues)
        {
            foreach (var group in list.Where(f => !broken.Contains(f))
                .GroupBy(f => new KeyValuePair<int, int>(f.DisplayX, f.DisplayY))
                .Where(g => g.Count() > 1))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Warning,
                    "These focuses are on the same spot (" + group.Key.Key + ", " +
                    group.Key.Value + "): " + string.Join(", ", group.Select(Name)) + "."));
            }
        }

        /// <summary>
        /// Text for the user: counts, then the errors first, limited to maxLines lines.
        /// </summary>
        public static string Summarize(List<ValidationIssue> issues, int maxLines)
        {
            if (issues == null || issues.Count == 0)
            {
                return "No problems found.";
            }
            int errors = issues.Count(i => i.Severity == ValidationSeverity.Error);
            int warnings = issues.Count - errors;
            StringBuilder text = new StringBuilder();
            text.AppendLine(errors + " error(s), " + warnings + " warning(s).");
            text.AppendLine();
            IEnumerable<ValidationIssue> ordered = issues
                .OrderByDescending(i => i.Severity).Take(maxLines);
            foreach (ValidationIssue issue in ordered)
            {
                text.AppendLine((issue.Severity == ValidationSeverity.Error ? "ERROR: " : "Warning: ")
                    + issue.Message);
            }
            if (issues.Count > maxLines)
            {
                text.AppendLine();
                text.AppendLine("... and " + (issues.Count - maxLines) + " more.");
            }
            return text.ToString();
        }
    }
}
