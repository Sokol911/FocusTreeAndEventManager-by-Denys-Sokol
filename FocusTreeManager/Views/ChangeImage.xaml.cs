using FocusTreeManager.Helper;
using FocusTreeManager.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace FocusTreeManager.Views
{
    /// <summary>
    /// Description for ChangeImage.
    /// </summary>
    public partial class ChangeImage : MetroWindow
    {
        /// <summary>
        /// Initializes a new instance of the ChangeImage class.
        /// </summary>
        public ChangeImage()
        {
            InitializeComponent();
            loadLocales();
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
            MaxHeight = SystemParameters.PrimaryScreenHeight * 0.90;
            MaxWidth = SystemParameters.PrimaryScreenWidth * 0.90;
            Height = MaxHeight;
            ChangeImageViewModel model = ViewModelLocator.Instance.ChangeImage;
            DataContext = model;
            //The images are loaded before the window is created, see LoadImages
            Width = Math.Min(MaxWidth, model.WindowWidth > 0 ? model.WindowWidth : MaxWidth);
            //The window is created every time, do not keep the old ones alive
            Closed += (s, e) => Messenger.Default.Unregister(this);
        }

        private void NotificationMessageReceived(NotificationMessage msg)
        {
            if (msg.Notification == "HideChangeImage")
            {
                Close();
            }
            if (msg.Notification == "ChangeLanguage")
            {
                loadLocales();
            }
        }

        private void loadLocales()
        {
            Resources.MergedDictionaries.Add(LocalizationHelper.getLocale());
        }
    }
}