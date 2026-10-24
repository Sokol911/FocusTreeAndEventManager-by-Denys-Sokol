using System;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;

namespace FocusTreeManager.Model
{
    public class CanvasLine : ObservableObject
    {
        private const int SELECTION_FUZZINESS = 5;

        private double x1;

        public double X1
        {
            get
            {
                return x1;
            }
            set
            {
                Set(() => X1, ref x1, value);
            }
        }

        private double y1;

        public double Y1
        {
            get
            {
                return y1;
            }
            set
            {
                Set(() => Y1, ref y1, value);
            }
        }

        private double x2;

        public double X2
        {
            get
            {
                return x2;
            }
            set
            {
                Set(() => X2, ref x2, value);
            }
        }

        private double y2;

        public double Y2
        {
            get
            {
                return y2;
            }
            set
            {
                Set(() => Y2, ref y2, value);
            }
        }

        private Brush color;

        public Brush Color
        {
            get
            {
                return color;
            }
            set
            {
                Set(() => Color, ref color, value);
            }
        }

        private bool dashes;

        public bool Dashes
        {
            get
            {
                return dashes;
            }
            set
            {
                Set(() => Dashes, ref dashes, value);
            }
        }

        public ISet InternalSet { get; private set; }

        public FocusModel Relative { get; private set; }

        public CanvasLine(double X1, double Y1, double X2, 
            double Y2, Brush Color, bool dashed, 
            ISet set, FocusModel relative = null)
        {
            this.X1 = X1;
            this.Y1 = Y1;
            this.X2 = X2;
            this.Y2 = Y2;
            this.Color = Color;
            dashes = dashed;
            InternalSet = set;
            Relative = relative;
        }

        public bool ContainsPoint(Point point)
        {
            //This is called for every line on every mouse move, so no LineGeometry / Point
            //objects are allocated here anymore (the maths are exactly the same).
            double leftX;
            double leftY;
            double rightX;
            double rightY;
            // Normalize start/end to left right to make the offset calc simpler.
            if (x1 <= x2)
            {
                leftX = x1;
                leftY = y1;
                rightX = x2;
                rightY = y2;
            }
            else
            {
                leftX = x2;
                leftY = y2;
                rightX = x1;
                rightY = y1;
            }
            // If point is out of bounds, no need to do further checks.                  
            if (point.X + SELECTION_FUZZINESS < leftX
                || rightX < point.X - SELECTION_FUZZINESS)
            {
                return false;

            }
            if (point.Y + SELECTION_FUZZINESS < Math.Min(leftY, rightY) ||
                Math.Max(leftY, rightY) < point.Y - SELECTION_FUZZINESS)
            {
                return false;
            }
            double deltaX = rightX - leftX;
            double deltaY = rightY - leftY;
            // If the line is straight, the earlier boundary check is enough
            // to determine that the point is on the line.
            // Also prevents division by zero exceptions.
            if (deltaX == 0 || deltaY == 0)
            {
                return true;
            }
            double slope = deltaY / deltaX;
            double offset = leftY - leftX * slope;
            double calculatedY = point.X * slope + offset;
            // Check calculated Y matches the points Y coord with some easing.
            bool lineContains = point.Y - SELECTION_FUZZINESS 
                <= calculatedY && calculatedY <= point.Y + SELECTION_FUZZINESS;
            return lineContains;
        }
    }
}
