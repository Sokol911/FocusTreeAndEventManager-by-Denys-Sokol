using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FocusTreeManager.Model;
using FocusTreeManager.Model.TabModels;

namespace FocusTreeManager.Adorners
{
    public class LineAdorner : Adorner
    {
        private readonly FrameworkElement element;

        private Canvas Children;

        public Canvas Child
        {
            get { return Children; }
            set
            {
                if (Children != null)
                {
                    RemoveVisualChild(Children);
                }
                Children = value;
                if (Children != null)
                {
                    AddVisualChild(Children);
                }
            }
        }

        public LineAdorner(UIElement el, FocusGridModel DataContext) : base(el)
        {
            element = el as FrameworkElement;
            this.DataContext = DataContext;
            Children = new Canvas();
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException();
            return Children;
        }

        //Loading Icons.xaml (more than 5000 lines of XAML) for every single render, which
        //happens on every mouse move over a line, was the most expensive thing in here.
        private static VisualBrush scissorBrush;

        private static VisualBrush GetScissorBrush()
        {
            if (scissorBrush != null) return scissorBrush;
            ResourceDictionary resourceLocalization = new ResourceDictionary
            {
                Source = new Uri("/FocusTreeManager;component/Resources/Icons.xaml", UriKind.Relative)
            };
            scissorBrush = new VisualBrush
            {
                Visual = (Visual) resourceLocalization["appbar_scissor"]
            };
            return scissorBrush;
        }

        //What the line shapes were last built from
        private object renderedLines;

        private int renderedVersion = -1;

        private Rectangle scissors;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            FocusGridModel context = DataContext as FocusGridModel;
            if (context == null)
            {
                Child.Children.Clear();
                scissors = null;
                renderedLines = null;
                return;
            }
            //The line shapes are only rebuilt if the model really redrew its lines. A render
            //caused by hovering only has to move the scissors.
            if (!ReferenceEquals(renderedLines, context.CanvasLines) ||
                renderedVersion != context.LinesVersion)
            {
                Child.Children.Clear();
                scissors = null;
                foreach (CanvasLine line in context.CanvasLines)
                {
                    Line newLine = new Line
                    {
                        X1 = line.X1,
                        X2 = line.X2,
                        Y1 = line.Y1,
                        Y2 = line.Y2,
                        Visibility = Visibility.Visible,
                        StrokeThickness = 2,
                        Stroke = line.Color
                    };
                    if (line.Dashes)
                    {
                        newLine.StrokeDashArray = new DoubleCollection(new double[] { 2, 2});
                    }
                    Child.Children.Add(newLine);
                }
                renderedLines = context.CanvasLines;
                renderedVersion = context.LinesVersion;
            }
            //Remove the previous scissors, they are added again if a line is selected
            if (scissors != null)
            {
                Child.Children.Remove(scissors);
                scissors = null;
            }
            if (context.selectedLine == null) return;
            Rectangle visualBoard = new Rectangle
            {
                Width = 25,
                Height = 25
            };
            Point Position = Mouse.GetPosition(this);
            Canvas.SetTop(visualBoard, Position.Y);
            Canvas.SetLeft(visualBoard, Position.X);
            visualBoard.Fill = GetScissorBrush();
            scissors = visualBoard;
            Child.Children.Add(visualBoard);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double desiredWidth = element.ActualWidth;
            double desiredHeight = element.ActualHeight;
            Child.Arrange(new Rect(0, 0, desiredWidth, desiredHeight));
            return finalSize;
        }
    }
}
