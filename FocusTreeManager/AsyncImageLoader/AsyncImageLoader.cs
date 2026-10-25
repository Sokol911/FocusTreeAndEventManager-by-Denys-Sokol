using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using FocusTreeManager.Helper;

namespace FocusTreeManager.AsyncImageLoader
{
    public class AsyncImageLoader
    {
        private readonly BackgroundWorker StarterWorker;

        private readonly BackgroundWorker ModWorker;

        private bool restartStarterWorker;

        private static readonly object myLock = new object();

        private static AsyncImageLoader backWorkerSingleton = new AsyncImageLoader();

        public Dictionary<string, ImageSource> Focuses { get; set; }

        public Dictionary<string, ImageSource> Events { get; set; }

        private AsyncImageLoader()
        {
            StarterWorker = new BackgroundWorker();
            StarterWorker.WorkerSupportsCancellation = true;
            StarterWorker.DoWork += StarterWorker_DoWork;
            StarterWorker.RunWorkerCompleted += StarterWorker_RunWorkerCompleted;
            ModWorker = new BackgroundWorker();
            ModWorker.DoWork += ModWorker_DoWork;
            ModWorker.RunWorkerCompleted += ModWorker_RunWorkerCompleted;
        }

        private bool restartModWorker;

        private void ModWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Another refresh was asked while this one was running
            if (restartModWorker)
            {
                restartModWorker = false;
                RefreshFromMods();
            }
        }

        private void StarterWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            if (restartStarterWorker)
            {
                restartStarterWorker = false;
                StartTheJob();
            }
        }

        private void StarterWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Focuses = ImageHelper.findAllGameImages(ImageType.Goal);
            Events = ImageHelper.findAllGameImages(ImageType.Event);
        }

        private void ModWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Work on copies and publish them with a single reference assignment, so the UI
            //thread (which reads these dictionaries all the time) never sees a dictionary
            //that is being modified.
            Dictionary<string, ImageSource> focuses = Focuses != null
                ? new Dictionary<string, ImageSource>(Focuses)
                : new Dictionary<string, ImageSource>();
            foreach (KeyValuePair<string, ImageSource> images
                in ImageHelper.RefreshFromMods(ImageType.Goal))
            {
                focuses[images.Key] = images.Value;
            }
            Focuses = focuses;
            Dictionary<string, ImageSource> events = Events != null
                ? new Dictionary<string, ImageSource>(Events)
                : new Dictionary<string, ImageSource>();
            foreach (KeyValuePair<string, ImageSource> images
                in ImageHelper.RefreshFromMods(ImageType.Event))
            {
                events[images.Key] = images.Value;
            }
            Events = events;
        }

        public void RestartStarterWorker()
        {
            if (!StarterWorker.IsBusy)
            {
                StartTheJob();
                return;
            }

            restartStarterWorker = true;
            StarterWorker.CancelAsync();
        }

        public void StartTheJob()
        {
            //RunWorkerAsync throws when the worker is already running
            if (StarterWorker.IsBusy)
            {
                restartStarterWorker = true;
                return;
            }
            StarterWorker.RunWorkerAsync();
        }

        public void RefreshFromMods()
        {
            //RunWorkerAsync throws when the worker is already running (e.g. a project opened
            //or its mods edited while the images of the previous one are still loading)
            if (ModWorker.IsBusy)
            {
                restartModWorker = true;
                return;
            }
            ModWorker.RunWorkerAsync();
        }

        public static AsyncImageLoader Worker
        {
            get
            {
                lock (myLock)
                {
                    if (backWorkerSingleton == null)
                    {
                        backWorkerSingleton = new AsyncImageLoader();
                    }
                }
                return backWorkerSingleton;
            }
        }
    }
}