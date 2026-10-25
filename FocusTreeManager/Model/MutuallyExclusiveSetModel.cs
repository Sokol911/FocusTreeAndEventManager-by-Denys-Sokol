using FocusTreeManager.ViewModel;
using GalaSoft.MvvmLight;
using MonitoredUndo;
using System;

namespace FocusTreeManager.Model
{
    public class MutuallyExclusiveSetModel : ObservableObject, 
        ISupportsUndo, ISet, IEquatable<MutuallyExclusiveSetModel>
    {
        private FocusModel focus1;

        public FocusModel Focus1
        {
            get
            {
                return focus1;
            }
            set
            {
                if (value == focus1)
                {
                    return;
                }
                DefaultChangeFactory.Current.OnChanging(this,
                         "Focus1", focus1, value, "Focus1 Changed");
                focus1 = value;
                RaisePropertyChanged(() => Focus1);
            }
        }

        private FocusModel focus2;

        public FocusModel Focus2
        {
            get
            {
                return focus2;
            }
            set
            {
                if (value == focus2)
                {
                    return;
                }
                DefaultChangeFactory.Current.OnChanging(this,
                         "Focus2", focus2, value, "Focus2 Changed");
                focus2 = value;
                RaisePropertyChanged(() => Focus2);
            }
        }

        public MutuallyExclusiveSetModel(FocusModel focus1, FocusModel focus2)
        {
            //Set leftmost Focus as Focus 1 and rightmost focus as focus 2
            if (focus1.X < focus2.X)
            {
                Focus1 = focus1;
                Focus2 = focus2;
            }
            else if (focus1.X >= focus2.X)
            {
                Focus2 = focus1;
                Focus1 = focus2;
            }
        }

        public void DeleteSetRelations()
        {
            if (Focus1 == null && Focus2 == null)
            {
                //Already deleted (several lines of the same set under the mouse)
                return;
            }
            RemoveFrom(Focus1);
            RemoveFrom(Focus2);
            Focus1 = null;
            Focus2 = null;
        }

        //The set implements IEquatable, so Collection.Remove(this) removes the FIRST EQUAL set,
        //which can be another instance with the same two foci. The instance being deleted then
        //stayed in the other focus with its foci cleared and the next redraw crashed on it.
        //Remove exactly this instance instead.
        private void RemoveFrom(FocusModel focus)
        {
            if (focus?.MutualyExclusive == null) return;
            for (int i = focus.MutualyExclusive.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(focus.MutualyExclusive[i], this))
                {
                    focus.MutualyExclusive.RemoveAt(i);
                }
            }
        }

        public bool Equals(MutuallyExclusiveSetModel obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.Focus1 == Focus1 && obj.Focus2 == Focus2)
            {
                return true;
            }
            if (obj.Focus2 == Focus1 && obj.Focus1 == Focus2)
            {
                return true;
            }
            return false;
        }

        #region Undo/Redo

        public object GetUndoRoot()
        {
            return ViewModelLocator.Instance.Main;
        }

        #endregion
    }
}
