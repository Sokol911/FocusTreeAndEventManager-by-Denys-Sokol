using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FocusTreeManager.Adorners;
using FocusTreeManager.Helper;
using FocusTreeManager.Model;
using FocusTreeManager.Model.TabModels;
using FocusTreeManager.ViewModel;
using GalaSoft.MvvmLight.Messaging;

namespace FocusTreeManager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for FocusGrid.xaml
    /// </summary>
    public partial class FocusGrid : UserControl
    {
        public FocusGrid()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            InitializeComponent();
            loadLocales();
            //Zoom
            AdornerDeco.LayoutTransform = zoomTransform;
            PreviewMouseWheel += FocusGrid_PreviewMouseWheel;
            //Messenger
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
        }

        #region Zoom

        private const double ZOOM_STEP = 1.1;

        private const double MIN_ZOOM = 0.1;

        private const double MAX_ZOOM = 3.0;

        private double zoom = 1.0;

        //A layout transform scales the whole grid (foci, lines, adorners) and makes the
        //scroll area shrink or grow with it, so zooming out shows more of the tree.
        private readonly ScaleTransform zoomTransform = new ScaleTransform(1.0, 1.0);

        private ScrollViewer FindScrollViewer()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is ScrollViewer))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as ScrollViewer;
        }

        /// <summary>
        /// Changes the zoom, keeping the point of the tree that is under the given point of
        /// the scroll area (the mouse cursor, usually) at the same place on the screen.
        /// </summary>
        private void SetZoom(double value, Point? anchorInViewport)
        {
            value = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, value));
            if (Math.Abs(value - zoom) < 0.0001) return;
            ScrollViewer viewer = FindScrollViewer();
            Point inGrid = new Point(0, 0);
            bool keepAnchor = viewer != null && anchorInViewport.HasValue;
            if (keepAnchor)
            {
                try
                {
                    inGrid = viewer.TranslatePoint(anchorInViewport.Value, SelectionGrid);
                }
                catch (InvalidOperationException)
                {
                    keepAnchor = false;
                }
            }
            zoom = value;
            zoomTransform.ScaleX = zoom;
            zoomTransform.ScaleY = zoom;
            if (!keepAnchor) return;
            //Let the layout take the new size into account, then scroll so that the anchor
            //did not move
            viewer.UpdateLayout();
            Point after = SelectionGrid.TranslatePoint(inGrid, viewer);
            viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset + after.X - anchorInViewport.Value.X);
            viewer.ScrollToVerticalOffset(viewer.VerticalOffset + after.Y - anchorInViewport.Value.Y);
        }

        private void FocusGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
            ScrollViewer viewer = FindScrollViewer();
            Point? anchor = viewer != null ? e.GetPosition(viewer) : (Point?)null;
            SetZoom(e.Delta > 0 ? zoom * ZOOM_STEP : zoom / ZOOM_STEP, anchor);
            e.Handled = true;
        }

        private void MenuResetZoom_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(1.0, null);
        }

        /// <summary>
        /// Zooms out (or in) so that the whole tree fits in the visible area.
        /// </summary>
        private void MenuFitZoom_Click(object sender, RoutedEventArgs e)
        {
            ScrollViewer viewer = FindScrollViewer();
            if (viewer == null) return;
            double width = SelectionGrid.ActualWidth;
            double height = SelectionGrid.ActualHeight;
            if (width <= 0 || height <= 0 || viewer.ViewportWidth <= 0 ||
                viewer.ViewportHeight <= 0)
            {
                return;
            }
            //The actual size is in unscaled units, so this does not depend on the current zoom
            double fit = Math.Min(viewer.ViewportWidth / width, viewer.ViewportHeight / height);
            SetZoom(fit * 0.97, null);
            viewer.ScrollToHorizontalOffset(0);
            viewer.ScrollToVerticalOffset(0);
        }

        #endregion

        private void NotificationMessageReceived(NotificationMessage msg)
        {
            if (msg.Notification == "DrawOnCanvas")
            {
                AdornerLayer.GetAdornerLayer(ListGrid).Update();
            }
            if (msg.Notification == "ChangeLanguage")
            {
                loadLocales();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Adorner
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(ListGrid);
            LineAdorner Adorner = new LineAdorner(ListGrid, (FocusGridModel)DataContext);
            adornerLayer.Add(Adorner);
            setupInternalFoci();
            Mouse.OverrideCursor = null;
            //Keydown
            Window MainWindow = Application.Current.Windows.OfType<Window>()
                .SingleOrDefault(x => x.IsActive);
            if (MainWindow == null) return;
            MainWindow.KeyDown += FocusGrid_OnKeyDown;
            MainWindow.KeyUp += FocusGrid_OnKeyUp;
            //Check Tutorial
            ViewModelLocator.Instance.Tutorial.StartCommand.RaiseCanExecuteChanged();
        }

        public void setupInternalFoci()
        {
            //Build display
            foreach (Focus focus in UiHelper.FindVisualChildren<Focus>(ListGrid))
            {
                focus.DetectPositionPoints();
            }
        }

        private void loadLocales()
        {
            Resources.MergedDictionaries.Add(LocalizationHelper.getLocale());
        }

        private Point anchorPoint;
        private Point currentPoint;
        private Focus Source;
        private List<Focus> DraggedElement = new List<Focus>();
        private bool IsDragging;

        private void Focus_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FocusGridModel model = DataContext as FocusGridModel;
            if (model == null) return;
            if (e.ClickCount > 1 || model.ModeType != RelationMode.None) return;
            DraggedElement.Clear();
            Focus element = sender as Focus;
            if (element == null) return;
            anchorPoint = e.GetPosition(ListGrid);
            IsDragging = true;
            element.Cursor = ((TextBlock)Resources["CursorGrabbing"]).Cursor;
        }

        private readonly TranslateTransform transform = new TranslateTransform();

        private void Focus_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            FocusGridModel context = DataContext as FocusGridModel;
            if (e.OriginalSource is TextBox || context == null)
            {
                return;
            }
            Focus element = sender as Focus;
            if (element != null && IsDragging && e.LeftButton == MouseButtonState.Pressed &&
                !DraggedElement.Any())
            {
                //Check if the dragged element is in the selected focus list
                if (context.SelectedFocuses.Any(f => f == element.DataContext))
                {
                    //Add all the selected
                    IEnumerable<Focus> selectedFoci = UiHelper.FindVisualChildren<Focus>(this)
                    .Where(f =>
                        {
                            FocusModel selectedContext = f.DataContext as FocusModel;
                            return selectedContext != null &&
                                   context.SelectedFocuses.Any(f2 => f2 == selectedContext);
                        });
                    DraggedElement.AddRange(selectedFoci);
                }
                else
                {
                    //We don't want to have selected AND dragged
                    context.ClearSelected();
                    DraggedElement.Add(element);
                }
                Source = element;
                Source.CaptureMouse();
            }
            if (!DraggedElement.Any() || Source == null || !Source.IsMouseCaptured) return;
            currentPoint = e.GetPosition(ListGrid);
            transform.X += currentPoint.X - anchorPoint.X;
            transform.Y += currentPoint.Y - anchorPoint.Y;
            foreach (Focus focus in DraggedElement)
            {
               focus.RenderTransform = transform;
               //A render transform does not cause a layout, tell the focus that it moved
               focus.DetectPositionPoints();
            }
            anchorPoint = currentPoint;
        }

        private void Focus_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FocusGridModel model = DataContext as FocusGridModel;
            if (model == null) return;
            Focus element = sender as Focus;
            //Check if the focus id not move at all and is not a double click
            if (element != null && !element.IsMouseCaptured && model.ModeType == RelationMode.None &&
                IsDragging)
            {
                //If not holding CTRL
                if (Keyboard.Modifiers != ModifierKeys.Control)
                {
                    //Clear all selected
                    model.ClearSelected();
                }
                //If it was just a click, add this focus to the selected
                model.SelectFocus(element);
                IsDragging = false;
                Source = null;
                DraggedElement.Clear();
            }
            //Otherwise, manage as a move
            else if (model.ModeType == RelationMode.None && IsDragging)
            {
                if (Source == null)
                {
                    return;
                }
                Source.ReleaseMouseCapture();
                foreach (Focus focus in DraggedElement)
                {
                    Image image = UiHelper.FindChildWithName(focus, "FocusIcon") as Image;
                    if (image == null) return;
                    Point basepos = image.TranslatePoint(new Point(0, 0), ListGrid);
                    //Kill the transform 
                    focus.RenderTransform = new TranslateTransform();
                    //Change the position after applying the transform
                    ((FocusGridModel)DataContext).ChangePosition(focus.DataContext, 
                        new Point(basepos.X + 45, basepos.Y));
                    //The transform is gone, the focus might be back on the same cell and
                    //then no layout will happen, so refresh its connection points here
                    focus.DetectPositionPoints();
                }
                IsDragging = false;
                transform.X = 0;
                transform.Y = 0;
                Source.Cursor = null;
                Source = null;
                DraggedElement.Clear();
                e.Handled = true;
            }
        }

        private bool IsShown;

        private void FocusGrid_OnKeyUp(object sender, KeyEventArgs e)
        {
            if ((!IsShown || e.SystemKey != Key.LeftAlt) && e.SystemKey != Key.RightAlt) return;
            IsShown = false;
            FocusGridModel model = (FocusGridModel) DataContext;
            model.ShowHidePositionLinesCommand.Execute(this);
        }

        private void FocusGrid_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsShown && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt))
            {
                IsShown = true;
                FocusGridModel model = (FocusGridModel)DataContext;
                model.ShowHidePositionLinesCommand.Execute(this);
                e.Handled = true;
            }
            else if (e.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                FocusGridModel model = (FocusGridModel)DataContext;
                model.CopyCommand.Execute(null);
            }
            else if (e.Key == Key.V && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                FocusGridModel model = (FocusGridModel)DataContext;
                model.PasteCommand.Execute(null);
            }
            else if (IsVisible && (Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                     (e.Key == Key.D0 || e.Key == Key.NumPad0))
            {
                //Ctrl + 0 : back to 100%
                SetZoom(1.0, null);
                e.Handled = true;
            }
            else if (IsVisible && (Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                     (e.Key == Key.OemPlus || e.Key == Key.Add))
            {
                SetZoom(zoom * ZOOM_STEP, null);
                e.Handled = true;
            }
            else if (IsVisible && (Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                     (e.Key == Key.OemMinus || e.Key == Key.Subtract))
            {
                SetZoom(zoom / ZOOM_STEP, null);
                e.Handled = true;
            }
        }

        private bool IsSelecting;

        private Point mouseDownPos;

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Always send the event to the data context
            FocusGridModel dataContext = DataContext as FocusGridModel;
            if (dataContext == null) return;
            dataContext.LeftClickCommand.Execute(ListGrid);
            //Check if we are on the grid
            if (!(e.OriginalSource is GriddedGrid)) return;
            //Make sure we aren't dragging already
            if (IsDragging) return;
            // Capture and track the mouse.
            IsSelecting = true;
            //The foci cannot change while the selection box is being dragged, so their
            //positions are measured once here instead of on every mouse move
            selectionCandidates = BuildSelectionCandidates();
            highlighted.Clear();
            mouseDownPos = e.GetPosition(SelectionGrid);
            SelectionGrid.CaptureMouse();
            // Initial placement of the drag selection box.         
            Canvas.SetLeft(selectionBox, mouseDownPos.X);
            Canvas.SetTop(selectionBox, mouseDownPos.Y);
            selectionBox.Width = 0;
            selectionBox.Height = 0;
            // Make the drag selection box visible.
            selectionBox.Visibility = Visibility.Visible;
            FocusGridModel model = DataContext as FocusGridModel;
            model?.ClearSelected();
        }

        private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsSelecting) return;
            // Release the mouse capture and stop tracking it.
            IsSelecting = false;
            SelectionGrid.ReleaseMouseCapture();
            // Hide the drag selection box.
            selectionBox.Visibility = Visibility.Collapsed;
            Point mouseUpPos = e.GetPosition(SelectionGrid);
            Rect selectedBoundaries = new Rect(mouseDownPos, mouseUpPos);
            List<Focus> selectedFoci = GetFociInside(selectedBoundaries);
            //Clear the highlight of everything that was highlighted but is not selected
            foreach (Focus focus in highlighted)
            {
                if (!selectedFoci.Contains(focus))
                {
                    RaiseMouseEvent(focus, Mouse.MouseLeaveEvent);
                }
            }
            highlighted.Clear();
            selectionCandidates = null;
            FocusGridModel model = DataContext as FocusGridModel;
            foreach (Focus focus in selectedFoci)
            {
                RaiseMouseEvent(focus, Mouse.MouseLeaveEvent);
                model?.SelectFocus(focus);
            }
        }

        private struct SelectionCandidate
        {
            public Focus Element;

            public Point Center;
        }

        private List<SelectionCandidate> selectionCandidates;

        private readonly HashSet<Focus> highlighted = new HashSet<Focus>();

        private List<SelectionCandidate> BuildSelectionCandidates()
        {
            List<SelectionCandidate> list = new List<SelectionCandidate>();
            foreach (Focus focus in UiHelper.FindVisualChildren<Focus>(this))
            {
                Image icon = UiHelper.FindChildWithName(focus, "FocusIcon") as Image;
                if (icon == null) continue;
                //Same coordinate space as the mouse position of the selection box (which is
                //inside of the zoomed part)
                Point pos = icon.TranslatePoint(new Point(0, 0), SelectionGrid);
                list.Add(new SelectionCandidate
                {
                    Element = focus,
                    Center = new Point(pos.X + focus.ActualWidth / 2,
                                       pos.Y + focus.ActualHeight / 2)
                });
            }
            return list;
        }

        private List<Focus> GetFociInside(Rect selectedBoundaries)
        {
            List<Focus> found = new List<Focus>();
            if (selectionCandidates == null) return found;
            foreach (SelectionCandidate candidate in selectionCandidates)
            {
                if (selectedBoundaries.Contains(candidate.Center))
                {
                    found.Add(candidate.Element);
                }
            }
            return found;
        }

        private static void RaiseMouseEvent(Focus focus, RoutedEvent routedEvent)
        {
            MouseEventArgs args = new MouseEventArgs(Mouse.PrimaryDevice, 0)
            {
                RoutedEvent = routedEvent
            };
            focus.RaiseEvent(args);
        }

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsSelecting) return;
            // When the mouse is held down, reposition the drag selection box.
            Point mousePos = e.GetPosition(SelectionGrid);
            if (mouseDownPos.X < mousePos.X)
            {
                Canvas.SetLeft(selectionBox, mouseDownPos.X);
                selectionBox.Width = mousePos.X - mouseDownPos.X;
            }
            else
            {
                Canvas.SetLeft(selectionBox, mousePos.X);
                selectionBox.Width = mouseDownPos.X - mousePos.X;
            }
            if (mouseDownPos.Y < mousePos.Y)
            {
                Canvas.SetTop(selectionBox, mouseDownPos.Y);
                selectionBox.Height = mousePos.Y - mouseDownPos.Y;
            }
            else
            {
                Canvas.SetTop(selectionBox, mousePos.Y);
                selectionBox.Height = mouseDownPos.Y - mousePos.Y;
            }
            Rect selectedBoundaries = new Rect(mouseDownPos, mousePos);
            //Only the foci that entered or left the selection box get a mouse enter / leave,
            //instead of sending a leave to EVERY focus and walking the whole visual tree twice
            //on each mouse move. The final highlight is the same.
            List<Focus> selectedFoci = GetFociInside(selectedBoundaries);
            HashSet<Focus> nowSelected = new HashSet<Focus>(selectedFoci);
            foreach (Focus focus in highlighted)
            {
                if (!nowSelected.Contains(focus))
                {
                    RaiseMouseEvent(focus, Mouse.MouseLeaveEvent);
                }
            }
            foreach (Focus focus in selectedFoci)
            {
                if (!highlighted.Contains(focus))
                {
                    RaiseMouseEvent(focus, Mouse.MouseEnterEvent);
                }
            }
            highlighted.Clear();
            foreach (Focus focus in selectedFoci)
            {
                highlighted.Add(focus);
            }
        }
    }
}
