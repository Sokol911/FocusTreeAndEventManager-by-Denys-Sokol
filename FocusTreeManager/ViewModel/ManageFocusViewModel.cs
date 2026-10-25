using FocusTreeManager.Model;
using FocusTreeManager.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FocusTreeManager.Model.TabModels;
using FocusTreeManager.Helper;

namespace FocusTreeManager.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ManageFocusViewModel : ViewModelBase
    {
        private FocusModel focus;

        public FocusModel Focus
        {
            get
            {
                return focus;
            }
            set
            {
                if (focus != null)
                {
                    focus.PropertyChanged -= Focus_PropertyChanged;
                }
                focus = value;
                if (focus != null)
                {
                    focus.PropertyChanged += Focus_PropertyChanged;
                }
                RaisePropertyChanged(() => Focus);
                RefreshIssues();
            }
        }

        #region Live warnings

        /// <summary>
        /// Problems of the focus being edited, refreshed on every change of its id, text key
        /// or position, so the user sees them while typing.
        /// </summary>
        public ObservableCollection<ValidationIssue> Issues { get; } =
            new ObservableCollection<ValidationIssue>();

        public bool HasIssues => Issues.Count > 0;

        public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

        private void Focus_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "UniqueName":
                case "Text":
                case "X":
                case "Y":
                case "DisplayX":
                case "DisplayY":
                    RefreshIssues();
                    break;
            }
        }

        public void RefreshIssues()
        {
            List<ValidationIssue> found = new List<ValidationIssue>();
            try
            {
                FocusGridModel grid = ViewModelLocator.Instance.Main.SelectedTab as FocusGridModel;
                if (focus != null && grid != null)
                {
                    List<KeyValuePair<string, IEnumerable<FocusModel>>> otherTrees =
                        new List<KeyValuePair<string, IEnumerable<FocusModel>>>();
                    ProjectModel project = ViewModelLocator.Instance.Main.Project;
                    if (project?.fociList != null)
                    {
                        foreach (FocusGridModel tree in project.fociList.Where(t =>
                                 t != null && !ReferenceEquals(t, grid) && t.FociList != null))
                        {
                            otherTrees.Add(new KeyValuePair<string, IEnumerable<FocusModel>>(
                                tree.VisibleName, tree.FociList));
                        }
                    }
                    found = FocusLiveChecker.Check(focus, grid.FociList, otherTrees);
                }
            }
            catch (Exception)
            {
                //A warning must never get in the way of editing
                found = new List<ValidationIssue>();
            }
            Issues.Clear();
            foreach (ValidationIssue issue in found.OrderByDescending(i => i.Severity))
            {
                Issues.Add(issue);
            }
            RaisePropertyChanged(() => HasIssues);
            RaisePropertyChanged(() => HasErrors);
        }

        #endregion

        public FocusGridModel sender { get; set; }

        public RelayCommand FocusCommand { get; private set; }

        public RelayCommand ChangeImageCommand { get; private set; }

        public RelayCommand EditScriptCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the EditFocusViewModel class.
        /// </summary>
        public ManageFocusViewModel()
        {
            FocusCommand = new RelayCommand(CloseEditFocus);
            ChangeImageCommand = new RelayCommand(ChangeImage);
            EditScriptCommand = new RelayCommand(EditScript);
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
        }

        public ManageFocusViewModel SetupFlyout(object sender, ModeType Mode)
        {
            if (Mode == ModeType.Create)
            {
                Point mousePos = Mouse.GetPosition((IInputElement)sender);
                FocusModel localFocus = new FocusModel();
                FocusGridModel firstOrDefault = ViewModelLocator.Instance.Main.SelectedTab as FocusGridModel;
                if (firstOrDefault != null)
                {
                    localFocus.setDefaults(firstOrDefault
                        .FociList.Count);
                    Focus = localFocus;
                    //minus 0.4 because if you hit the border of a cell, it will add it to the next one... Annoying
                    Focus.X = (int)Math.Floor(mousePos.X / 89 - 0.4);
                    Focus.Y = (int)Math.Floor(mousePos.Y / 140);
                }
            }
            else
            {
                Focus = (FocusModel)sender;
            }
            RaisePropertyChanged(() => Focus);
            return this;
        }

        public void CloseEditFocus()
        {
            Messenger.Default.Send(new NotificationMessage(this, 
                ViewModelLocator.Instance.Main.SelectedTab, "CloseEditFocus"));
        }

        public void ChangeImage()
        {
            ViewModelLocator.Instance.ChangeImage.LoadImages("Focus", Focus.Image);
            ChangeImage view = new ChangeImage();
            view.ShowDialog();
            Focus.Image = ViewModelLocator.Instance.ChangeImage.FocusImage;
        }

        public void EditScript()
        {
            ScripterViewModel ViewModel = ViewModelLocator.Instance.Scripter;
            ViewModel.ScriptType = ScripterType.FocusTree;
            ViewModel.ManagedScript = focus.InternalScript;
            EditScript dialog = new EditScript();
            dialog.ShowDialog();
            focus.InternalScript = ViewModel.ManagedScript;
        }

        private static void NotificationMessageReceived(NotificationMessage msg)
        {
        }
    }
}