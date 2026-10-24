using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FocusTreeManager.DataContract;
using FocusTreeManager.Helper;
using FocusTreeManager.Parsers;
using FocusTreeManager.ViewModel;
using FocusTreeManager.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls.Dialogs;
using MonitoredUndo;

namespace FocusTreeManager.Model.TabModels
{
    public enum RelationMode
    {
        None,
        MutuallyExclusive,
        Prerequisite,
        RelativeTo,
        Add,
        Edit
    }

    public enum RelationModeParam
    {
        Required,
        Optional
    }

    public class FocusGridModel : ObservableObject, ISupportsUndo
    {
        private const int MIN_ROW_COUNT = 7;
        private const int MIN_COLUMN_COUNT = 20;

        private ObservableCollection<CanvasLine> canvasLines;

        private FocusModel ChosenFocusForLink;

        public List<FocusModel> SelectedFocuses;

        public CanvasLine selectedLine { get; set; }

        public RelationMode ModeType {
            get;
            set; }

        public RelationModeParam ModeParam { get; set; }

        private int rowCount;

        private int columnCount;

        private bool DrawPositionLines;

        private string visibleName;

        private string tag;

        private string additionnalMods = "";

        private DataContract.FileInfo fileInfo;

        public RelayCommand<object> AddFocusCommand { get; private set; }

        public RelayCommand<object> LeftClickCommand { get; private set; }

        public RelayCommand<object> HoverCommand { get; private set; }

        public RelayCommand DeleteElementCommand { get; private set; }

        public RelayCommand EditElementCommand { get; private set; }

        public RelayCommand CopyElementCommand { get; private set; }

        public RelayCommand ShowHidePositionLinesCommand { get; private set; }

        public RelayCommand ValidateCommand { get; private set; }

        public RelayCommand CopyCommand { get; private set; }

        public RelayCommand<object> PasteCommand { get; private set; }

        public int RowCount
        {
            get
            {
                return rowCount;
            }
            set
            {
                if (value == rowCount)
                {
                    return;
                }
                rowCount = value;
                RaisePropertyChanged(() => RowCount);
            }
        }

        public int ColumnCount
        {
            get
            {
                return columnCount;
            }
            set
            {
                if (value == columnCount)
                {
                    return;
                }
                columnCount = value;
                RaisePropertyChanged(() => ColumnCount);
            }
        }

        public Guid UniqueID { get; }

        public string VisibleName
        {
            get
            {
                return visibleName;
            }
            set
            {
                if (value == visibleName)
                {
                    return;
                }
                DefaultChangeFactory.Current.OnChanging(this,
                         "VisibleName", visibleName, value, "VisibleName Changed");
                visibleName = value;
                RaisePropertyChanged(() => VisibleName);
            }
        }

        public string TAG
        {
            get
            {
                return tag;
            }
            set
            {
                if (value == tag)
                {
                    return;
                }
                tag = value;
                RaisePropertyChanged(() => TAG);
            }
        }

        public string AdditionnalMods
        {
            get
            {
                return additionnalMods;
            }
            set
            {
                if (value == additionnalMods)
                {
                    return;
                }
                additionnalMods = value;
                RaisePropertyChanged(() => AdditionnalMods);
            }
        }

        public DataContract.FileInfo FileInfo
        {
            get
            {
                return fileInfo;
            }
            set
            {
                if (value == fileInfo)
                {
                    return;
                }
                fileInfo = value;
                RaisePropertyChanged(() => FileInfo);
            }
        }

        public ObservableCollection<FocusModel> FociList { get; private set; }

        public ObservableCollection<CanvasLine> CanvasLines
        {
            get
            {
                return canvasLines;
            }
            set
            {
                canvasLines = value;
                RaisePropertyChanged(() => CanvasLines);
            }
        }

        public FocusGridModel(string Filename)
        {
            visibleName = Filename;
            UniqueID = Guid.NewGuid();
            //Min Row & column Count
            rowCount = MIN_ROW_COUNT;
            columnCount = MIN_COLUMN_COUNT;
            SetupCommons();
        }

        public FocusGridModel(FociGridContainer container)
        {
            SetupCommons();
            //SetupCommons already subscribed, which made every later change be recorded twice
            //in the undo history (the subscription is done once, after loading, below)
            FociList.CollectionChanged -= FociList_CollectionChanged;
            //Transfer data
            UniqueID = container.IdentifierID;
            visibleName = container.ContainerID;
            tag = container.TAG;
            additionnalMods = container.AdditionnalMods;
            foreach (Focus focus in container.FociList)
            {
                FociList.Add(new FocusModel(focus));
            }
            //Rerun to create sets, with lookups built once instead of searching the lists
            //(and copying the whole list) for every single focus
            NameLookup<Focus> sources = new NameLookup<Focus>(container.FociList,
                f => f.UniqueName);
            NameLookup<FocusModel> models = new NameLookup<FocusModel>(FociList,
                f => f.UniqueName);
            foreach (FocusModel model in FociList)
            {
                model.RepairSets(sources.Find(model.UniqueName), models);
            }
            //Create the remaining stuff
            FociList.CollectionChanged += FociList_CollectionChanged;
            //Min Row & column Count
            EditGridDefinition();
        }

        public FocusGridModel(FocusGridModel model)
        {
            SetupCommons();
            //SetupCommons already subscribed, which made every later change be recorded twice
            //in the undo history (the subscription is done once, after loading, below)
            FociList.CollectionChanged -= FociList_CollectionChanged;
            //Transfer data
            UniqueID = Guid.NewGuid();
            visibleName = model.visibleName;
            tag = model.tag;
            additionnalMods = model.additionnalMods;
            foreach (FocusModel focus in model.FociList)
            {
                FociList.Add(new FocusModel(focus));
            }
            //Rerun to create sets, with lookups built once
            NameLookup<FocusModel> sources = new NameLookup<FocusModel>(model.FociList,
                f => f.UniqueName);
            NameLookup<FocusModel> copies = new NameLookup<FocusModel>(FociList,
                f => f.UniqueName);
            foreach (FocusModel focus in FociList)
            {
                focus.RepairSets(sources.Find(focus.UniqueName), copies);
            }
            //Create the remaining stuff
            FociList.CollectionChanged += FociList_CollectionChanged;
            //Min Row & column Count
            EditGridDefinition();
        }

        private void ValidateTree()
        {
            List<ValidationIssue> issues = FocusTreeValidator.Validate(FociList);
            DialogCoordinator.Instance.ShowMessageAsync(ViewModelLocator.Instance.Main,
                LocalizationHelper.Tr("V_Title", "Tree check"),
                FocusTreeValidator.Summarize(issues, 15));
        }

        internal void SetupCommons()
        {
            ModeType = RelationMode.None;
            SelectedFocuses = new List<FocusModel>();
            canvasLines = new ObservableCollection<CanvasLine>();
            FociList = new ObservableCollection<FocusModel>();
            FociList.CollectionChanged += FociList_CollectionChanged;
            //Commands
            AddFocusCommand = new RelayCommand<object>(AddFocus);
            LeftClickCommand = new RelayCommand<object>(LeftClick);
            HoverCommand = new RelayCommand<object>(Hover);
            DeleteElementCommand = new RelayCommand(SendDeleteSignal);
            EditElementCommand = new RelayCommand(SendEditSignal);
            CopyElementCommand = new RelayCommand(SendCopySignal);
            ShowHidePositionLinesCommand = new RelayCommand(ShowHidePositionLines);
            ValidateCommand = new RelayCommand(ValidateTree);
            CopyCommand = new RelayCommand(Copy, CanCopy);
            PasteCommand = new RelayCommand<object>(Paste, CanPaste);
            //Messenger
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
        }

        internal void ChangePosition(object draggedElement, Point newPos)
        {
            FocusModel element = draggedElement as FocusModel;
            if (element == null) return;
            UndoService.Current[GetUndoRoot()]
                .BeginChangeSetBatch("ChangePosition", false);
            //Move the focuses
            element.DisplayX = (int)Math.Floor(newPos.X / 89);
            element.DisplayY = (int)Math.Floor(newPos.Y / 140);
            //Make all relative to focuses change their positions
            updateRelatives(element);
            EditGridDefinition();
            UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
        }

        public void SelectFocus(Views.UserControls.Focus selected)
        {
            FocusModel model = selected.DataContext as FocusModel;
            if (model == null) return;
            model.IsSelected = true;
            SelectedFocuses.Add(model);
            CopyCommand.RaiseCanExecuteChanged();
        }

        public void ClearSelected()
        {
            foreach (FocusModel selected in SelectedFocuses)
            {
                selected.IsSelected = false;
                selected.IsWaiting = false;
            }
            ModeType = RelationMode.None;
            SelectedFocuses.Clear();
            CopyCommand.RaiseCanExecuteChanged();
            PasteCommand.RaiseCanExecuteChanged();
        }

        #region Copy&Paste

        private List<FocusModel> CopyTempMemory = new List<FocusModel>();

        private void Copy()
        {
            CopyTempMemory = SelectedFocuses;
            PasteCommand.RaiseCanExecuteChanged();
        }

        private void Paste(object sender)
        {
            if (!CanPaste(sender)) return;
            UndoService.Current[GetUndoRoot()]
                .BeginChangeSetBatch("PasteMultiple", false);
            Point mousePos = Mouse.GetPosition((IInputElement)sender);
            if (!CopyTempMemory.Any()) return;
            int XChange = 0;
            int YChange = 0;
            if (sender != null)
            {
                FocusModel First = CopyTempMemory.FirstOrDefault();
                if (First == null) return;
                XChange = (int)Math.Floor(mousePos.X / 89 - 0.4) - First.DisplayX;
                YChange = (int)Math.Floor(mousePos.Y / 140) - First.DisplayY;
            }
            List<FocusModel> newSelected = CopyTempMemory.Select(selected => 
                                            selected.Copy(XChange, YChange)).ToList();
            for (int index = 0; index < CopyTempMemory.Count; index++)
            {
                FocusModel selected = CopyTempMemory[index];
                FocusModel currentFocus = newSelected[index];
                //Repair relative to
                if (selected.CoordinatesRelativeTo != null)
                {
                    //If the relative to can be found in the current new list
                    if (newSelected.Any(f => selected.CoordinatesRelativeTo.UniqueName == f.UniqueName))
                    {
                        currentFocus.CoordinatesRelativeTo = newSelected.FirstOrDefault(f =>
                                    selected.CoordinatesRelativeTo.UniqueName == f.UniqueName);
                    }
                    else
                    {
                        //Otherwise, keep the same
                        currentFocus.CoordinatesRelativeTo = selected.CoordinatesRelativeTo;
                    }
                }
                //Repair prerequisites
                foreach (PrerequisitesSetModel set in selected.Prerequisite)
                {
                    PrerequisitesSetModel newset = new PrerequisitesSetModel(currentFocus);
                    foreach (FocusModel child in set.FociList)
                    {
                        //If the focus to can be found in the current new list
                        if (newSelected.Any(f => child.UniqueName == f.UniqueName))
                        {
                            newset.FociList.Add(newSelected.FirstOrDefault(f => child.UniqueName 
                                                        == f.UniqueName));
                        }
                        else
                        {
                            //Otherwise, keep the same
                            newset.FociList.Add(child);
                        }
                    }
                    currentFocus.Prerequisite.Add(newset);
                }
                //Repair mutually exclusives
                foreach (MutuallyExclusiveSetModel set in selected.MutualyExclusive)
                {
                    FocusModel toFind;
                    FocusModel focus1;
                    FocusModel focus2;
                    if (set.Focus1.UniqueName == currentFocus.UniqueName)
                    {
                        toFind = set.Focus2;
                        focus1 = set.Focus1;
                    }
                    else if(set.Focus2.UniqueName == currentFocus.UniqueName)
                    {
                        toFind = set.Focus1;
                        focus1 = set.Focus2;
                    }
                    else
                    {
                        //Cannot be found, cancel
                        continue;
                    }
                    //If the focus to can be found in the current new list
                    if (newSelected.Any(f => toFind.UniqueName == f.UniqueName))
                    {
                        focus2 = newSelected.FirstOrDefault(f => toFind.UniqueName
                                                    == f.UniqueName);
                    }
                    else
                    {
                        //Otherwise, keep the same
                        focus2 = toFind;
                    }
                    currentFocus.MutualyExclusive.Add(new MutuallyExclusiveSetModel(focus1, focus2));
                }
                currentFocus.IsSelected = true;
                FociList.Add(currentFocus);
            }
            //Unselect all the selected foci
            foreach (FocusModel selected in SelectedFocuses)
            {
                selected.IsSelected = false;
            }
            //Change all copied name to append _Copy
            foreach (FocusModel currentFocus in newSelected)
            {
                currentFocus.UniqueName += "_Copy";
            }
            SelectedFocuses = newSelected;
            UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
        }

        private bool CanPaste(object sender)
        {
            return CopyTempMemory.Any();
        }

        private bool CanCopy()
        {
            return SelectedFocuses.Any();
        }

        #endregion

        internal void updateRelatives(FocusModel RelativeTo)
        {
            if (FociList.All(f => f.CoordinatesRelativeTo != RelativeTo))
            {
                return;
            }
            //Select all focuses that are relative to this parent, but not currently in movement
            foreach (FocusModel relative in
                    FociList.Where(f => f.CoordinatesRelativeTo == RelativeTo &&
                                   !SelectedFocuses.Contains(f)))
            {
                relative.RaisePropertyChanged(() => relative.DisplayX);
                relative.RaisePropertyChanged(() => relative.DisplayY);
                updateRelatives(relative);
            }
        }

        public void EditGridDefinition()
        {
            if (!FociList.Any())
            {
                return;
            }
            FocusModel biggestY = FociList.Aggregate((i1, i2) => i1.DisplayY > 
                    i2.DisplayY ? i1 : i2);
            FocusModel biggestX = FociList.Aggregate((i1, i2) => i1.DisplayX > 
                    i2.DisplayX ? i1 : i2);
            RowCount = biggestY.DisplayY >= RowCount ? biggestY.DisplayY + 1 : RowCount;
            ColumnCount = biggestX.DisplayX >= ColumnCount ? biggestX.DisplayX + 1 : ColumnCount;
        }

        public void AddGridCells(int RowsToAdd, int ColumnsToAddt)
        {
            RowCount += RowsToAdd;
            ColumnCount += ColumnsToAddt;
        }

        public void AddFocus(object sender)
        {
            ModeType = RelationMode.Add;
            UndoService.Current[GetUndoRoot()].BeginChangeSetBatch("AddFocus", false);
            Messenger.Default.Send(new NotificationMessage(sender, "ShowAddFocus"));
        }

        public void LeftClick(object param)
        {
            Grid sender = param as Grid;
            if (sender == null) return;
            Point Position = Mouse.GetPosition(sender);
            List<CanvasLine> clickedElements = CanvasLines.Where(line => 
                                               line.ContainsPoint(Position)).ToList();
            if (clickedElements.Any())
            {
                UndoService.Current[GetUndoRoot()].BeginChangeSetBatch("DeleteSetAny", false);
                foreach (CanvasLine line in clickedElements)
                {
                    if (line.InternalSet != null)
                    {
                        line.InternalSet.DeleteSetRelations();
                    }
                    else
                    {
                        //Hit a relative to
                        line.Relative.X = line.Relative.DisplayX;
                        line.Relative.Y = line.Relative.DisplayY;
                        line.Relative.CoordinatesRelativeTo = null;
                    }
                }
                UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
                CanvasLines = new ObservableCollection<CanvasLine>(CanvasLines.
                    Except(clickedElements).ToList());
                DrawOnCanvas();
            }
        }

        public void Hover(object sender)
        {
            Point Position = Mouse.GetPosition((Grid)sender);
            //Called on every mouse move: stop at the first line under the mouse instead of
            //building a list of all of them
            CanvasLine hoveredLine = null;
            foreach (CanvasLine line in CanvasLines)
            {
                if (!line.ContainsPoint(Position)) continue;
                hoveredLine = line;
                break;
            }
            if (hoveredLine != null)
            {
                selectedLine = hoveredLine;
                Messenger.Default.Send(new NotificationMessage("DrawOnCanvas"));
            }
            else
            {
                //If a line is selected
                if (selectedLine == null) return;
                selectedLine = null;
                Messenger.Default.Send(new NotificationMessage("DrawOnCanvas"));
            }
        }

        private void SendDeleteSignal()
        {
            Messenger.Default.Send(new NotificationMessage(this,
                ViewModelLocator.Instance.ProjectView, "SendDeleteItemSignal"));
        }

        private void SendEditSignal()
        {
            Messenger.Default.Send(new NotificationMessage(this,
                ViewModelLocator.Instance.ProjectView, "SendEditItemSignal"));
        }

        private void SendCopySignal()
        {
            Messenger.Default.Send(new NotificationMessage(this,
                ViewModelLocator.Instance.ProjectView, "SendCopyItemSignal"));
        }

        private void ShowHidePositionLines()
        {
            DrawPositionLines = !DrawPositionLines;
            DrawOnCanvas();
        }

        public void RedrawGrid()
        {
            EditGridDefinition();
            DrawOnCanvas();
        }

        private void NotificationMessageReceived(NotificationMessage msg)
        {
            //If this is not the intended target
            if (msg.Target != null && msg.Target != this) return;
            //If this is a dead tab waiting to be destroyed
            if (VisibleName == null) return;
            FocusModel Model = msg.Sender as FocusModel;
            switch (msg.Notification)
            {
                case "CloseEditFocus":
                    switch (ModeType)
                    {
                        case RelationMode.Add:
                            ManageFocusViewModel viewModel = msg.Sender as ManageFocusViewModel;
                            if (viewModel != null) addFocusToList(viewModel.Focus);
                            DrawOnCanvas();
                            break;
                        case RelationMode.Edit:
                            EditGridDefinition();
                            DrawOnCanvas();
                            break;
                    }
                    ModeType = RelationMode.None;
                    break;
                case "DeleteFocus":
                    if (Model == null) return;
                    //Check if model is in the selected foci
                    if (Model.IsSelected && SelectedFocuses.Contains(Model))
                    {
                        //Delete everyone
                        foreach (FocusModel item in SelectedFocuses)
                        {
                            DeleteFocus(item);
                        }
                        break;
                    }
                    //Otherwise, kill the current focus
                    DeleteFocus(Model);
                    break;
                case "AddFocusMutually":
                    ModeType = RelationMode.MutuallyExclusive;
                    if (Model != null)
                    {
                        ChosenFocusForLink = Model;
                        Model.IsWaiting = true;
                    }
                    break;
                case "FinishAddFocusMutually":
                    if (ChosenFocusForLink != null && ChosenFocusForLink != Model &&
                        FociList.Any(f => f == Model))
                    {
                        UndoService.Current[GetUndoRoot()]
                            .BeginChangeSetBatch("AddMutuallyExclusive", false);
                        ModeType = RelationMode.None;
                        ChosenFocusForLink.IsWaiting = false;
                        MutuallyExclusiveSetModel tempo = 
                            new MutuallyExclusiveSetModel(ChosenFocusForLink, Model);
                        ChosenFocusForLink.MutualyExclusive.Add(tempo);
                        Model?.MutualyExclusive.Add(tempo);
                        ChosenFocusForLink = null;
                        Messenger.Default.Send(new NotificationMessage(this,
                            ViewModelLocator.Instance.StatusBar, "Clear_message"));
                        UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
                        RaisePropertyChanged(() => FociList);
                        DrawOnCanvas();
                    }
                    break;
                case "AddFocusPrerequisite":
                    ModeType = RelationMode.Prerequisite;
                    if (Model != null)
                    {
                        ChosenFocusForLink = Model;
                        Model.IsWaiting = true;
                    }
                    break;
                case "FinishAddFocusPrerequisite":
                    if (ChosenFocusForLink != null && ChosenFocusForLink != Model &&
                        FociList.Any(f => f == Model))
                    {
                        UndoService.Current[GetUndoRoot()]
                            .BeginChangeSetBatch("AddPrerequisite", false);
                        ModeType = RelationMode.None;
                        if (ModeParam == RelationModeParam.Required)
                        {
                            //Create new set
                            PrerequisitesSetModel set = new PrerequisitesSetModel(ChosenFocusForLink);
                            set.FociList.Add(Model);
                            ChosenFocusForLink.Prerequisite.Add(set);
                        }
                        else if (ModeParam == RelationModeParam.Optional)
                        {
                            //Create new set if no exist
                            if (!ChosenFocusForLink.Prerequisite.Any())
                            {
                                PrerequisitesSetModel set = new PrerequisitesSetModel(ChosenFocusForLink);
                                ChosenFocusForLink.Prerequisite.Add(set);
                            }
                            //Add Model to last Set
                            ChosenFocusForLink.Prerequisite.Last().FociList.Add(Model);
                        }
                        ChosenFocusForLink.IsWaiting = false;
                        ChosenFocusForLink = null;
                        Messenger.Default.Send(new NotificationMessage(this,
                            ViewModelLocator.Instance.StatusBar, "Clear_message"));
                        UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
                        RaisePropertyChanged(() => FociList);
                        DrawOnCanvas();
                    }
                    break;
                case "MakeRelativeTo":
                    ModeType = RelationMode.RelativeTo;
                    if (Model != null)
                    {
                        ChosenFocusForLink = Model;
                        Model.IsWaiting = true;
                    }
                    break;
                case "FinishMakeRelativeTo":
                    if (ChosenFocusForLink != null && ChosenFocusForLink != Model &&
                        FociList.Any(f => f == Model))
                    {
                        if (Model != null)
                        {
                            UndoService.Current[GetUndoRoot()]
                                .BeginChangeSetBatch("MakeRelativeTo", false);
                            ModeType = RelationMode.None;
                            ChosenFocusForLink.IsWaiting = false;
                            ChosenFocusForLink.X = ChosenFocusForLink.DisplayX - Model.DisplayX;
                            ChosenFocusForLink.Y = ChosenFocusForLink.DisplayY - Model.DisplayY;
                            ChosenFocusForLink.CoordinatesRelativeTo = Model;
                            ChosenFocusForLink = null;
                            Messenger.Default.Send(new NotificationMessage(this,
                                ViewModelLocator.Instance.StatusBar, "Clear_message"));
                            UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
                            RaisePropertyChanged(() => FociList);
                            DrawOnCanvas();
                        }
                    }
                    break;
                case "PositionChanged":
                    if (Model != null)
                    {
                        foreach (FocusModel focus in FociList.Where(f => f.CoordinatesRelativeTo == Model))
                        {
                            focus.RaisePropertyChanged(() => focus.DisplayX);
                            focus.RaisePropertyChanged(() => focus.DisplayY);
                        }
                        DrawOnCanvas();
                    }
                    UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
                    break;
            }
            if (msg.Target == this)
            {
                //Resend to the tutorial View model if this was the target
                Messenger.Default.Send(new NotificationMessage(msg.Sender,
                ViewModelLocator.Instance.Tutorial, msg.Notification));
            }
        }

        private bool redrawPending;

        public void UpdateFocus(FocusModel sender)
        {
            if (ViewModelLocator.Instance.Main.SelectedTab != this) return;
            //Every focus calls this when its position changes, so opening a tree or moving
            //several foci used to redraw ALL the lines once per focus. The redraws asked
            //for in the same burst are now merged into one.
            if (redrawPending) return;
            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                EditGridDefinition();
                DrawOnCanvas();
                return;
            }
            redrawPending = true;
            dispatcher.BeginInvoke(new Action(() =>
            {
                redrawPending = false;
                EditGridDefinition();
                DrawOnCanvas();
            }), DispatcherPriority.Render);
        }

        private void DeleteFocus(FocusModel Model)
        {
            //Kill the set that might have this focus as parent
            foreach (FocusModel focus in FociList)
            {
                foreach (PrerequisitesSetModel set in focus.Prerequisite.ToList())
                {
                    if (set.FociList.Contains(Model))
                    {
                        set.DeleteSetRelations();
                    }
                }
                foreach (MutuallyExclusiveSetModel set in focus.MutualyExclusive.ToList())
                {
                    if (set.Focus2 == Model || set.Focus1 == Model)
                    {
                        set.DeleteSetRelations();
                    }
                }
            }
            //Remove the focus
            FociList.Remove(Model);
            UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
            EditGridDefinition();
            DrawOnCanvas();
        }

        public void addFocusToList(FocusModel FocusToAdd)
        {
            FociList.Add(FocusToAdd);
            UndoService.Current[GetUndoRoot()].EndChangeSetBatch();
            RowCount = FocusToAdd.Y >= RowCount ? FocusToAdd.Y + 1 : RowCount;
            ColumnCount = FocusToAdd.X >= ColumnCount ? FocusToAdd.X + 1 : ColumnCount;
            DrawOnCanvas();
        }

        private const double PRE_LINE_HEIGHT = 20;

        /// <summary>
        /// Incremented every time the lines are redrawn, lets the adorner know when its line
        /// shapes are really out of date.
        /// </summary>
        public int LinesVersion { get; private set; }

        private struct LineKey : IEquatable<LineKey>
        {
            private readonly double x1;
            private readonly double y1;
            private readonly double x2;
            private readonly double y2;

            public LineKey(double x1, double y1, double x2, double y2)
            {
                this.x1 = x1;
                this.y1 = y1;
                this.x2 = x2;
                this.y2 = y2;
            }

            public bool Equals(LineKey other)
            {
                return x1 == other.x1 && y1 == other.y1 && x2 == other.x2 && y2 == other.y2;
            }

            public override bool Equals(object obj)
            {
                return obj is LineKey && Equals((LineKey) obj);
            }

            public override int GetHashCode()
            {
                //"+ 0.0" turns -0.0 into 0.0, they are equal for == so they need the same hash
                unchecked
                {
                    int hash = (x1 + 0.0).GetHashCode();
                    hash = hash * 397 ^ (y1 + 0.0).GetHashCode();
                    hash = hash * 397 ^ (x2 + 0.0).GetHashCode();
                    hash = hash * 397 ^ (y2 + 0.0).GetHashCode();
                    return hash;
                }
            }
        }

        private static void AddLine(List<CanvasLine> lines, HashSet<LineKey> keys, 
                                    CanvasLine line)
        {
            lines.Add(line);
            keys.Add(new LineKey(line.X1, line.Y1, line.X2, line.Y2));
        }

        public void DrawOnCanvas()
        {
            if (FociList == null)
            {
                return;
            }
            //The lines are collected in a plain list, the duplicates check uses a hash set
            //(it used to scan every existing line, and every Add notified the collection)
            List<CanvasLine> lines = new List<CanvasLine>();
            HashSet<LineKey> keys = new HashSet<LineKey>();
            foreach (FocusModel focus in FociList)
            {
                //Draw relatives
                if (DrawPositionLines && focus.CoordinatesRelativeTo != null)
                {
                    CanvasLine newline = new CanvasLine(
                            focus.FocusTop.X,
                            focus.FocusTop.Y,
                            focus.CoordinatesRelativeTo.FocusBottom.X,
                            focus.CoordinatesRelativeTo.FocusBottom.Y,
                            System.Windows.Media.Brushes.Gold, false, null, focus);
                    AddLine(lines, keys, newline);
                }
                //Draw Prerequisites
                foreach (PrerequisitesSetModel set in focus.Prerequisite)
                {
                    //Draw line from top of first Focus 
                    CanvasLine newline = new CanvasLine(
                        set.Focus.FocusTop.X,
                        set.Focus.FocusTop.Y,
                        set.Focus.FocusTop.X,
                        set.Focus.FocusTop.Y - PRE_LINE_HEIGHT,
                        System.Windows.Media.Brushes.Teal, set.isRequired(), set);
                    AddLine(lines, keys, newline);
                    foreach (FocusModel model in set.FociList)
                    {
                        FocusModel Prerequisite = model;
                        if (Prerequisite == null) continue;
                        //Draw horizontal lines to prerequisite pos
                        newline = new CanvasLine(set.Focus.FocusTop.X, 
                            set.Focus.FocusTop.Y - PRE_LINE_HEIGHT, Prerequisite.FocusBottom.X, 
                            set.Focus.FocusTop.Y - PRE_LINE_HEIGHT, System.Windows.Media.Brushes.Teal,
                            set.isRequired(), set);
                        AddLine(lines, keys, newline);
                        //Draw line to prerequisite bottom
                        newline = new CanvasLine(Prerequisite.FocusBottom.X, 
                            set.Focus.FocusTop.Y - PRE_LINE_HEIGHT, Prerequisite.FocusBottom.X, 
                            Prerequisite.FocusBottom.Y, System.Windows.Media.Brushes.Teal, 
                            set.isRequired(), set);
                        AddLine(lines, keys, newline);
                    }
                }
                //Draw Mutually exclusives
                foreach (MutuallyExclusiveSetModel set in focus.MutualyExclusive)
                {
                    CanvasLine newline = new CanvasLine(
                        set.Focus1.FocusRight.X,
                        set.Focus1.FocusRight.Y,
                        set.Focus2.FocusLeft.X,
                        set.Focus2.FocusLeft.Y,
                        System.Windows.Media.Brushes.Red, false, set);
                    if (!keys.Contains(new LineKey(newline.X1, newline.Y1, 
                                                   newline.X2, newline.Y2)))
                    {
                        AddLine(lines, keys, newline);
                    }
                }
            }
            canvasLines = new ObservableCollection<CanvasLine>(lines);
            LinesVersion++;
            RaisePropertyChanged(() => CanvasLines);
            Messenger.Default.Send(new NotificationMessage("DrawOnCanvas"));
        }

        public async void CheckForChanges()
        {
            DataContract.FileInfo info = FileInfo;
            //check the fileinfo data
            if (info == null) return;
            //If the file exists
            if (!File.Exists(info.Filename)) return;
            //If the file was modified after the last modification date
            if (File.GetLastWriteTime(info.Filename) <= info.LastModifiedDate) return;
            //Then we can show a message
            MessageDialogResult Result = await ViewModelLocator.Instance
                .Main.ShowFileChangedDialog();
            if (Result == MessageDialogResult.Affirmative)
            {
                string oldText = FocusTreeParser.ParseTreeForCompare(this);
                string newText = FocusTreeParser.ParseTreeScriptForCompare(info.Filename);
                SideBySideDiffModel model = new SideBySideDiffBuilder(
                    new Differ()).BuildDiffModel(oldText, newText);
                ViewModelLocator.Instance.CodeComparator.DiffModel = model;
                new CompareCode().ShowDialog();
            }
        }

        #region Undo/Redo

        private void FociList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Current.OnCollectionChanged(this, 
                "FociList", FociList, e, "FociList Changed");
        }
        
        public object GetUndoRoot()
        {
            return (ViewModelLocator.Instance).Main;
        }

        #endregion

    }
}
