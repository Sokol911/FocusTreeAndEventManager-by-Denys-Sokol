using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Media;

namespace FocusTreeManager.Helper
{
    /// <summary>
    /// Helper methods for UI-related tasks.
    /// </summary>
    public static class UiHelper
    {
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) 
            where T : DependencyObject
        {
            if (depObj == null) yield break;
            //Same depth first (parents before their children) order as before, but with an
            //explicit stack. The nested recursive iterators made every element returned from
            //deep in the tree go through one iterator per level, and this is called on
            //every mouse move while selecting.
            Stack<DependencyObject> pending = new Stack<DependencyObject>();
            PushChildren(pending, depObj);
            while (pending.Count > 0)
            {
                DependencyObject current = pending.Pop();
                T match = current as T;
                if (match != null)
                {
                    yield return match;
                }
                PushChildren(pending, current);
            }
        }

        private static void PushChildren(Stack<DependencyObject> stack, DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            //Pushed backwards so the first child is the first one popped
            for (int i = count - 1; i >= 0; i--)
            {
                stack.Push(VisualTreeHelper.GetChild(parent, i));
            }
        }

        public static T FindVisualParent<T>(DependencyObject depObj, FrameworkElement HighestParent) 
            where T : DependencyObject
        {
            while (true)
            {
                if (depObj == null || Equals(depObj, HighestParent)) return null;
                DependencyObject parent = VisualTreeHelper.GetParent(depObj);
                T visualParent = parent as T;
                if (visualParent != null)
                {
                    return visualParent;
                }
                depObj = parent;
            }
        }

        public static bool RessourceExists(string ressourceName)
        {
            return GetResourceNames().Any(item => item.Contains(ressourceName));
        }

        private static IEnumerable<string> GetResourceNames()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resName = assembly.GetName().Name + ".g.resources";
            using (Stream stream = assembly.GetManifestResourceStream(resName))
            {
                if (stream == null) return null;
                using (ResourceReader reader = new ResourceReader(stream))
                {
                    return reader.Cast<DictionaryEntry>().Select(entry =>
                        (string) entry.Key).ToArray();
                }
            }
        }

        public static DependencyObject FindChildWithName(DependencyObject parent, string childName)
        {
            if (parent == null) return null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                FrameworkElement frameworkElement = child as FrameworkElement;
                // If the child's name is set for search
                if (frameworkElement == null || frameworkElement.Name != childName)
                {
                    DependencyObject foundChild = FindChildWithName(child, childName);
                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null)
                    {
                        return foundChild;
                    }
                }
                else if (frameworkElement.Name == childName)
                {
                    // if the child's name is of the request name
                    return child;
                }
            }
            return null;
        }

        public static DependencyObject FindChildWithType(DependencyObject parent, Type ChildType)
        {
            if (parent == null) return null;
            DependencyObject foundChild = parent;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                if (child.GetType() != ChildType)
                {
                    // recursively drill down the tree
                    foundChild = FindChildWithType(child, ChildType);
                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else
                {
                    // child element found.
                    foundChild = child;
                    break;
                }
            }
            return foundChild;
        }
    }
}
