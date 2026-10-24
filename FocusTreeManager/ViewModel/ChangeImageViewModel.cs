using FocusTreeManager.Helper;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System;
using System.Windows;
using System.IO;
using System.Windows.Media;

namespace FocusTreeManager.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ChangeImageViewModel : ViewModelBase
    {
        public class ImageData
        {
            public ImageSource Image { get; set; }
            public string Filename { get;  set; }
            public int MaxWidth { get; set; }
            public int CellWidth { get; set; }
        }

        /// <summary>
        /// A row of pictures. The list shows one row per item, so that only the rows that
        /// are visible are created (a game has thousands of pictures and creating all of
        /// them at once took several seconds).
        /// </summary>
        public class ImageRow
        {
            public List<ImageData> Items { get; set; }
        }

        public RelayCommand<string> SelectCommand { get; private set; }

        private string focusImage;

        public string FocusImage
        {
            get
            {
                return focusImage;
            }
            set
            {
                focusImage = value;
                RaisePropertyChanged(() => FocusImage);
            }
        }

        private List<ImageRow> imageList = new List<ImageRow>();

        public List<ImageRow> ImageList
        {
            get { return imageList; }
            private set
            {
                imageList = value;
                RaisePropertyChanged(() => ImageList);
            }
        }

        /// <summary>Width of the window, enough for whole columns of pictures.</summary>
        public double WindowWidth { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ChangeImageViewModel class.
        /// </summary>
        public ChangeImageViewModel()
        {
            SelectCommand = new RelayCommand<string>(SelectFocus);
        }

        public void LoadImages(string SubFolder, string CurrentImage)
        {
            int MaxWidth = 250;
            Dictionary<string, ImageSource> images = new Dictionary<string, ImageSource>();
            switch (SubFolder)
            {
                case "Focus":
                    MaxWidth = 100;
                    images = AsyncImageLoader.AsyncImageLoader.Worker.Focuses;
                    break;
                    //fork case "Event":
                    case "Events":
                    MaxWidth = 250;
                    images = AsyncImageLoader.AsyncImageLoader.Worker.Events;
                    break;
            }
            if (images == null)
            {
                images = new Dictionary<string, ImageSource>();
            }
            //Every picture is as wide as its button or its image, whichever is wider
            int itemWidth = Math.Max(MaxWidth, 150) + 20;
            double available = SystemParameters.PrimaryScreenWidth * 0.9 - 45;
            int columns = Math.Max(1, (int)(available / itemWidth));
            WindowWidth = columns * itemWidth + 45;
            List<ImageRow> rows = new List<ImageRow>();
            ImageRow current = null;
            foreach (KeyValuePair<string, ImageSource> source in images)
            {
                if (current == null || current.Items.Count >= columns)
                {
                    current = new ImageRow { Items = new List<ImageData>(columns) };
                    rows.Add(current);
                }
                current.Items.Add(new ImageData
                {
                    Image = source.Value,
                    Filename = source.Key,
                    MaxWidth = MaxWidth,
                    CellWidth = itemWidth - 20
                });
            }
            ImageList = rows;
            focusImage = CurrentImage;
        }

        public void SelectFocus(string path)
        {
            FocusImage = path;
            Messenger.Default.Send(new NotificationMessage(this, "HideChangeImage"));
        }
    }
}