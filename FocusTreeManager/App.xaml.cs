using System;
using System.IO;
using System.CodeDom;
using System.Windows;
using System.Runtime.ExceptionServices;
using FocusTreeManager.Helper;

namespace FocusTreeManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //Log only if not in debug
        #if DEBUG
            public bool toLog = false;
        #else
            public bool toLog = true;
        #endif

        public App()
        {
            //Relative paths (Common\Tutorials, GFX...) are relative to the current directory,
            //which is not the application folder when started from a shortcut or from
            //another program (it was C:\Windows\System32 for example, and the app crashed)
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception)
            {
                //Keep the current directory
            }
            AsyncImageLoader.AsyncImageLoader.Worker.StartTheJob();
            //Logging
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.FirstChanceException += HandleFirstChance;
            currentDomain.UnhandledException += HandleCrashes;
        }

        private void HandleFirstChance(object source, FirstChanceExceptionEventArgs e)
        {
            if (!toLog) return;
            LoggingHelper.LogException(e.Exception);
        }

        private  void HandleCrashes(object sender, UnhandledExceptionEventArgs e)
        {
            if (!toLog) return;
            LoggingHelper.LogCrash((Exception)e.ExceptionObject);
        }
    }
}
