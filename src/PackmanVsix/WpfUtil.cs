﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PackmanVsix
{
    public static class WpfUtil
    {
        public static ImageSource GetIconForImageMoniker(ImageMoniker? imageMoniker, int sizeX, int sizeY)
        {
            if (imageMoniker == null)
            {
                return null;
            }

            IVsImageService2 vsIconService = ServiceProvider.GlobalProvider.GetService(typeof(SVsImageService)) as IVsImageService2;

            if (vsIconService == null)
            {
                return null;
            }

            ImageAttributes imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                LogicalHeight = sizeY,
                LogicalWidth = sizeX,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            IVsUIObject result = vsIconService.GetImage(imageMoniker.Value, imageAttributes);

            object data;
            result.get_Data(out data);
            ImageSource glyph = data as ImageSource;

            if (glyph != null)
            {
                glyph.Freeze();
            }

            return glyph;
        }

        public static ImageSource GetIconForFile(string file, out bool themeIcon)
        {
            return GetImage(file, __VSUIDATAFORMAT.VSDF_WINFORMS, out themeIcon);
        }

        private static ImageSource GetImage(string file, __VSUIDATAFORMAT format, out bool themeIcon)
        {
            IVsImageService imageService = ServiceProvider.GlobalProvider.GetService(typeof(SVsImageService)) as IVsImageService;
            ImageSource result = null;
            uint iconSource = (uint)__VSIconSource.IS_Unknown;

            if (imageService != null && !string.IsNullOrWhiteSpace(file))
            {
                IVsUIObject image = imageService.GetIconForFileEx(file, format, out iconSource);
                if (image != null)
                {
                    var imageData = GetObjectData(image);
                    result = imageData as ImageSource;

                    if (result == null && imageData is Icon)
                    {
                        Icon icon = (Icon) imageData;
                        Bitmap bitmap = icon.ToBitmap();
                        IntPtr hBitmap = bitmap.GetHbitmap();

                        result = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap, IntPtr.Zero, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        DeleteObject(hBitmap);
                    }
                }
            }

            themeIcon = (iconSource == (uint)__VSIconSource.IS_VisualStudio);
            return result;
        }

        private static object GetObjectData(IVsUIObject obj)
        {
            Validate.IsNotNull(obj, "obj");

            object value;
            int result = obj.get_Data(out value);

            if (result != VSConstants.S_OK)
            {
                throw new COMException("Could not get object data", result);
            }

            return (value);
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}