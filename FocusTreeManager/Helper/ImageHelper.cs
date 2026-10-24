using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FocusTreeManager.Model;
using FocusTreeManager.ViewModel;
using Imaging.DDSReader;

namespace FocusTreeManager.Helper
{

    public enum ImageType
    {
        Goal,
        Event
    }

    public static class ImageHelper
    {
        private static readonly string[] ARRAY_FILE_NAME =
        {
            "goal_support_democracy",
            "goal_support_fascism",
            "goal_support_communism" ,
            "goal_generic_occupy_states_coastal",
            "goal_generic_occupy_start_war",
            "goal_generic_occupy_states_ongoing_war",
            "goal_generic_build_navy"
        };

        private static readonly string[] ARRAY_ASSOCIATED_TYPO =
        {
            "goal_generic_support_democracy",
            "goal_generic_support_fascism",
            "goal_generic_support_communism",
            "goal_generic_occypy_states_coastal",
            "goal_generic_occypy_start_war",
            "goal_generic_occypy_states_ongoing_war",
            "goal_generic_build_nay"
        };

        private const string GFX_GOAL_FOLDER = @"\gfx\interface\goals\";

        private const string GFX_EVENT_FOLDER = @"\gfx\event_pictures\";

        private const string GFX_EXTENTION = ".dds";

        private const string GFX_ERROR_IMAGE = @"\GFX\Focus\goal_unknown.png";

        private static readonly string[] IMAGE_DO_NOT_LOAD = { "shine_mask", "shine_overlay"};

        private static ImageSource errorImage;

        /// <summary>
        /// The "unknown" image used when a goal/event picture cannot be found.
        /// It used to be re-read from the disk on EVERY call, even when the requested image
        /// existed. It is now loaded once, fully decoded and frozen.
        /// </summary>
        private static ImageSource GetErrorImage()
        {
            ImageSource cached = errorImage;
            if (cached != null) return cached;
            //If we couldn't find the error image, throw an IO exception
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(Directory.GetCurrentDirectory() + GFX_ERROR_IMAGE);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            errorImage = image;
            return image;
        }

        public static ImageSource getImageFromGame(string imageName, ImageType source)
        {
            ImageSource errorSource = GetErrorImage();
            //Try to obtain the image
            try
            {
                Dictionary<string, ImageSource> images;
                switch (source)
                {
                    case ImageType.Goal:
                        images = AsyncImageLoader.AsyncImageLoader.Worker.Focuses;
                        break;
                    case ImageType.Event:
                        images = AsyncImageLoader.AsyncImageLoader.Worker.Events;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(source), source, null);
                }
                //Direct dictionary lookup instead of a linear LastOrDefault scan
                ImageSource value = null;
                if (images != null && imageName != null)
                {
                    images.TryGetValue(imageName, out value);
                }
                //Make sure the value is set, if not, return the error image.
                return value ?? errorSource;
            }
            catch (Exception)
            {
                //If an error occurred, return the error image
                return errorSource;
            }
        }

        private static string GetFolderFor(ImageType source)
        {
            switch (source)
            {
                case ImageType.Goal:
                    return GFX_GOAL_FOLDER;
                case ImageType.Event:
                    return GFX_EVENT_FOLDER;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }

        public static Dictionary<string, ImageSource> findAllGameImages(ImageType source)
        {
            Dictionary<string, ImageSource> list = new Dictionary<string, ImageSource>();
            string rightFolder = GetFolderFor(source);
            LoadImagesFromFolder(Configurator.getGamePath() + rightFolder, list);
            return list;
        }

        public static Dictionary<string, ImageSource> RefreshFromMods(ImageType source)
        {
            Dictionary<string, ImageSource> list = new Dictionary<string, ImageSource>();
            string rightFolder = GetFolderFor(source);
            //For each file in add mod folders
            ProjectModel model = ViewModelLocator.Instance.Main.Project;
            if (model?.ListModFolders == null) return list;
            foreach (string modpath in model.ListModFolders)
            {
                LoadImagesFromFolder(modpath + rightFolder, list);
            }
            return list;
        }

        /// <summary>
        /// Loads every .dds of a folder into the list (shared by the game and mod loaders,
        /// which used to be two copies of the same code).
        /// </summary>
        private static void LoadImagesFromFolder(string fullpath, 
                                                 Dictionary<string, ImageSource> list)
        {
            if (!Directory.Exists(fullpath)) return;
            string[] files = Directory.GetFiles(fullpath, "*" + GFX_EXTENTION,
                                                SearchOption.TopDirectoryOnly)
                .Where(f => !IMAGE_DO_NOT_LOAD.Any(f.Contains)).ToArray();
            string[] names = new string[files.Length];
            ImageSource[] decoded = new ImageSource[files.Length];
            //Decoding a .dds is CPU bound and every file is independent, so decode them in
            //parallel (this is what made the start-up image loading slow). Frozen images
            //can be shared between threads.
            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
            };
            Parallel.For(0, files.Length, options, i =>
            {
                try
                {
                    string imageName = Path.GetFileNameWithoutExtension(files[i]);
                    //try to replace potential broken links because of typos in the file names.
                    int typoIndex = Array.IndexOf(ARRAY_ASSOCIATED_TYPO, imageName);
                    if (typoIndex != -1)
                    {
                        imageName = ARRAY_FILE_NAME[typoIndex];
                    }
                    ImageSource result;
                    //Dispose the GDI+ bitmap as soon as it is converted, it used to leak
                    using (Bitmap bitmap = DDS.LoadImage(files[i]))
                    {
                        result = ImageSourceForBitmap(bitmap);
                    }
                    result.Freeze();
                    names[i] = imageName;
                    decoded[i] = result;
                }
                catch (Exception)
                {
                    // ignored, we don't want to kill the whole process for one missing image
                }
            });
            //Merge in the original file order so that the result is the same as before
            //(a later file with the same name still wins).
            for (int i = 0; i < files.Length; i++)
            {
                if (decoded[i] != null)
                {
                    list[names[i]] = decoded[i];
                }
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

        private static ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            IntPtr handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, 
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}
