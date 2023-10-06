using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;

using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;

using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace YUVplayer_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 


    public sealed partial class MainPage : Page
    {
        //private StoreContext context = null;
        //private StoreAppLicense appLicense = null;


        StorageFile file;
        IRandomAccessStream stream;
        //IInputStream inputStream;
        DataReader dataReader;
        ulong fileSize;
        ulong frameSize = 3110400;
        ulong maxFrameInd;
        int width = 1920;
        int height = 1080;
        ulong curInd = 1;
        string format = "NV12";


        // Call this while your app is initializing.
        //private async void InitializeLicense()
        //{
        //    if (context == null)
        //    {
        //        context = StoreContext.GetDefault();
        //        // If your app is a desktop app that uses the Desktop Bridge, you
        //        // may need additional code to configure the StoreContext object.
        //        // For more info, see https://aka.ms/storecontext-for-desktop.
        //    }

        //    workingProgressRing.IsActive = true;
        //    appLicense = await context.GetAppLicenseAsync();
        //    workingProgressRing.IsActive = false;

        //    // Register for the licenced changed event.
        //    context.OfflineLicensesChanged += context_OfflineLicensesChanged;
        //}

        //private async void context_OfflineLicensesChanged(StoreContext sender, object args)
        //{
        //    // Reload the license.
        //    workingProgressRing.IsActive = true;
        //    appLicense = await context.GetAppLicenseAsync();
        //    workingProgressRing.IsActive = false;

        //    if (appLicense.IsActive)
        //    {
        //        if (appLicense.IsTrial)
        //        {
        //            textBlock.Text = $"This is the trial version. Expiration date: {appLicense.ExpirationDate}";

        //            // Show the features that are available during trial only.
        //        }
        //        else
        //        {
        //            // Show the features that are available only with a full license.
        //        }
        //    }
        //}

        public MainPage()
        {
            this.InitializeComponent();

            format_combo.Items.Add("YV12");
            format_combo.Items.Add("YUY2");
            format_combo.Items.Add("UYVY");
            //format_combo.Items.Add("P010");
            //format_combo.Items.Add("P210");

            format_combo.SelectedIndex = 0;
        }

        private async void OpenFileClick(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;

            picker.FileTypeFilter.Add(".rgb");
            picker.FileTypeFilter.Add(".yuv");
            picker.FileTypeFilter.Add(".nv12");
            picker.FileTypeFilter.Add(".yuy2");
            picker.FileTypeFilter.Add(".uyvy");
            //picker.FileTypeFilter.Add(".p010");
            //picker.FileTypeFilter.Add(".p210");
            picker.FileTypeFilter.Add("*");
            //picker.FileTypeFilter.Add("*.*");

            StorageFile tfile = await picker.PickSingleFileAsync();
            if (tfile != null)
            {
                file = tfile;
                stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                dataReader = new Windows.Storage.Streams.DataReader(stream);
                fileSize = stream.Size;
                if(fileSize == 0)
                {
                    var msgDialog = new Windows.UI.Popups.MessageDialog("File size is 0. Please open another file") { Title = "open err" };
                    msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("ok"));
                    await msgDialog.ShowAsync();
                }
                else
                {
                    Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = file.DisplayName;
                    frameSize = GetFrameSize();
                    maxFrameInd = (fileSize + frameSize - 1) / frameSize;
                    slider.Minimum = 1;
                    slider.Maximum = maxFrameInd;
                    slider.Value = 1;
                    curInd = 1;
                    ReloadFile();
                    slider.IsEnabled = true;
                }
            }
            else
            {
                this.logtext.Text = "Operation cancelled.";
            }
        }

        private void PreviousClick(object sender, RoutedEventArgs e)
        {
            if(file!=null && curInd > 1)
            {
                curInd--;
                slider.Value--;
            }
        }

        private void PlayPauseClick(object sender, RoutedEventArgs e)
        {
            if (file != null)
            {
                //logtext.Text = "PlayPauseClick pressed";
            }
        }

        private void NextClick(object sender, RoutedEventArgs e)
        {
            if (file != null && curInd < maxFrameInd)
            {
                curInd++;
                slider.Value++;
            }
        }

        void UpdateParam()
        {
            slider.IsEnabled = false;
            frameSize = GetFrameSize();
            maxFrameInd = (fileSize + frameSize - 1) / frameSize;
            slider.Minimum = 1;
            slider.Maximum = maxFrameInd;
            slider.Value = 1;
            curInd = 1;
            slider.IsEnabled = true;
        }


        private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (file != null)
            {
                curInd = 1;
                format = format_combo.SelectedItem.ToString();
                UpdateParam();
                seek();
                ReloadFile();
            }
        }

        private async void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            NumberBox nb = sender as NumberBox;
            int newValue = (int)nb.Value;
            if (newValue % 2 == 1)
            {
                var msgDialog = new Windows.UI.Popups.MessageDialog("Width and height must be even number.") { Title = "Err" };
                msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { nb.Value = ++newValue; }));
                await msgDialog.ShowAsync();
            }
            if (nb.Name == "w")
            {
                if(newValue==width)
                {
                    return;
                }
                width = newValue;
            }
            else if (nb.Name == "h")
            {
                if(newValue==height)
                {
                    return;
                }
                height = newValue;
            }
            else
            {
                //never happened
                var msgDialog = new Windows.UI.Popups.MessageDialog("unexpected error") { Title = "Err" };
                msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { nb.Value = ++newValue; }));
                await msgDialog.ShowAsync();
            }
            UpdateParam();
            //refresh first frame;
            if (file!=null)
            {
                curInd = 1;
                try
                {
                    seek();
                    ReloadFile();
                }
                catch
                {

                }
            }
            
        }

        void seek(ulong ind = 0)
        {
            try
            {
                if (stream != null)
                    stream.Seek(ind);
            }
            catch
            {

            }
        }

        async void ReloadFile()
        {
            try
            {
                //update frameSize frome w h f

                uint numBytesLoaded = await dataReader.LoadAsync((uint)frameSize);
                if (numBytesLoaded == (uint)frameSize)
                {
                    var sourcebuffer = dataReader.ReadBuffer((uint)frameSize);
                    byte[] ar = new byte[width * height * 4];
                    {
                        switch(format)
                        {
                            case "NV12":
                                ConvertNV12toBGRA8(sourcebuffer, ar);
                                break;
                            case "BGRA32":
                                sourcebuffer.CopyTo(ar);
                                break;
                            case "RGBA32":
                                ConvertRGBA32toBGRA8(sourcebuffer, ar);
                                break;
                            case "RGB24":
                                ConvertRGB24toBGRA8(sourcebuffer, ar);
                                break;
                            case "YV12":
                                ConvertYV12toBGRA8(sourcebuffer, ar);
                                break;
                            case "YUY2":
                                ConvertYUY2toBGRA8(sourcebuffer, ar);
                                break;
                            case "UYVY":
                                ConvertUYVYtoBGRA8(sourcebuffer, ar);
                                break;
                            case "P010":
                                //ConvertP010toBGRA8(sourcebuffer, ar);
                                break;
                            case "P210":
                                //ConvertP210toBGRA8(sourcebuffer, ar);
                                break;
                            default:
                                break;
                        }
                    }
                    IBuffer b = CryptographicBuffer.CreateFromByteArray(ar);
                    SoftwareBitmap sb = SoftwareBitmap.CreateCopyFromBuffer(b, BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);//bgra8
                    var source = new SoftwareBitmapSource();
                    await source.SetBitmapAsync(sb);
                    image.Source = source;
                }
                else if (numBytesLoaded < (uint)frameSize)
                {
                    var sourcebuffer = dataReader.ReadBuffer((uint)numBytesLoaded);
                    byte[] src = new byte[frameSize];
                    byte[] ar = new byte[width * height * 4];
                    var ttt = sourcebuffer.ToArray();
                    ttt.CopyTo(src, 0);
                    {
                        switch (format)
                        {
                            case "NV12":
                                ConvertNV12toBGRA8(src.AsBuffer(), ar);
                                break;
                            case "BGRA32":
                                src.CopyTo(ar, 0);
                                break;
                            case "RGBA32":
                                ConvertRGBA32toBGRA8(src.AsBuffer(), ar);
                                break;
                            case "RGB24":
                                ConvertRGB24toBGRA8(src.AsBuffer(), ar);
                                break;
                            case "YV12":
                                ConvertYV12toBGRA8(sourcebuffer, ar);
                                break;
                            case "YUY2":
                                ConvertYUY2toBGRA8(sourcebuffer, ar);
                                break;
                            case "UYVY":
                                ConvertUYVYtoBGRA8(sourcebuffer, ar);
                                break;
                            case "P010":
                                //ConvertP010toBGRA8(sourcebuffer, ar);
                                break;
                            case "P210":
                                //ConvertP210toBGRA8(sourcebuffer, ar);
                                break;
                            default:
                                break;
                        }
                        //ConvertNV12toBGRA8(src.AsBuffer(), ar);
                    }
                    IBuffer b = CryptographicBuffer.CreateFromByteArray(ar);
                    SoftwareBitmap sb = SoftwareBitmap.CreateCopyFromBuffer(b, BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);//bgra8
                    var source = new SoftwareBitmapSource();
                    await source.SetBitmapAsync(sb);
                    image.Source = source;
                }
                logtext.Text = curInd + "/" + maxFrameInd;
            }
            catch
            {

            }
        }

        private void slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if(slider.IsEnabled && file != null)
            {
                curInd = (ulong)((sender as Slider).Value);
                try
                {
                    seek((curInd - 1) * frameSize);
                    ReloadFile();
                }
                catch
                {

                }
            }
        }

        ulong GetFrameSize()
        {
            switch (format)
            {
                case "NV12":
                    return (ulong)width * (ulong)height * 3 / 2;
                case "BGRA32":
                case "RGBA32":
                    return (ulong)width * (ulong)height * 4;
                case "RGB24":
                    return (ulong)width * (ulong)height * 3;
                case "YV12":
                    return (ulong)width * (ulong)height * 3 / 2;
                case "YUY2":
                case "UYVY":
                    return (ulong)width * (ulong)height * 2;
                case "P010":
                    return (ulong)width * (ulong)height * 2;
                case "P210":
                    return (ulong)width * (ulong)height * 2;
                default:
                    break;
            }
            return 0;
        }

        void ConvertRGB24toBGRA8(IBuffer buffer, byte[] des)
        {
            var src = buffer.ToArray();
            ulong srcInd = 0;
            ulong desInd = 0;
            for(int i=0;i<width*height;i++)
            {
                des[desInd++] = src[srcInd + 2];
                des[desInd++] = src[srcInd + 1];
                des[desInd++] = src[srcInd];
                des[desInd++] = 255;
                srcInd += 3;
            }
        }

        void ConvertRGBA32toBGRA8(IBuffer buffer, byte[] des)
        {
            var src = buffer.ToArray();
            ulong srcInd = 0;
            ulong desInd = 0;
            for (int i = 0; i < width * height; i++)
            {
                des[desInd++] = src[srcInd + 2];
                des[desInd++] = src[srcInd + 1];
                des[desInd++] = src[srcInd];
                des[desInd++] = src[srcInd + 4];
                srcInd += 4;
            }
        }

        void ConvertNV12toBGRA8(IBuffer buffer, byte[] des)
        {
            var src = buffer.ToArray();
            //y
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    des[(i * width + j) * 4] = src[i * width + j];
                }
            }
            //u
            for (int i = 0; i < height / 2; i++)
            {
                for (int j = 0; j < width / 2; j++)
                {
                    des[(i * 2 * width + j * 2) * 4 + 1] = src[width * height + i * width + j * 2];
                    des[(i * 2 * width + j * 2) * 4 + 2] = src[width * height + i * width + j * 2 + 1];

                    des[(i * 2 * width + j * 2 + 1) * 4 + 1] = src[width * height + i * width + j * 2];
                    des[(i * 2 * width + j * 2 + 1) * 4 + 2] = src[width * height + i * width + j * 2 + 1];

                    des[(i * 2 * width + width + j * 2) * 4 + 1] = src[width * height + i * width + j * 2];
                    des[(i * 2 * width + width + j * 2) * 4 + 2] = src[width * height + i * width + j * 2 + 1];

                    des[(i * 2 * width + width + j * 2 + 1) * 4 + 1] = src[width * height + i * width + j * 2];
                    des[(i * 2 * width + width + j * 2 + 1) * 4 + 2] = src[width * height + i * width + j * 2 + 1];
                }
            }


            //fill alpha
            byte b, g, r;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    b = getb(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1]);
                    g = getg(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1], des[(i * width + j) * 4 + 2]);
                    r = getr(des[(i * width + j) * 4], des[(i * width + j) * 4 + 2]);
                    des[(i * width + j) * 4] = b;
                    des[(i * width + j) * 4 + 1] = g;
                    des[(i * width + j) * 4 + 2] = r;
                    des[(i * width + j) * 4 + 3] = 255;
                }
            }
        }

        void ConvertYV12toBGRA8(IBuffer buffer, byte[] des)
        {
            var src = buffer.ToArray();
            //y
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    des[(i * width + j) * 4] = src[i * width + j];
                }
            }
            //u
            for (int i = 0; i < height / 2; i++)
            {
                for (int j = 0; j < width / 2; j++)
                {
                    des[(i * 2 * width + j * 2) * 4 + 1] = src[width * height * 5 / 4 + i * width / 2 + j];
                    des[(i * 2 * width + j * 2 + 1) * 4 + 1] = src[width * height * 5 / 4 + i * width / 2 + j];
                    des[(i * 2 * width + width + j * 2) * 4 + 1] = src[width * height * 5 / 4 + i * width / 2 + j];
                    des[(i * 2 * width + width + j * 2 + 1) * 4 + 1] = src[width * height * 5 / 4 + i * width / 2 + j];
                }
            }
            //v
            for (int i = 0; i < height / 2; i++)
            {
                for (int j = 0; j < width / 2; j++)
                {
                    des[(i * 2 * width + j * 2) * 4 + 2] = src[width * height  + i * width / 2 + j];
                    des[(i * 2 * width + j * 2 + 1) * 4 + 2] = src[width * height + i * width / 2 + j];
                    des[(i * 2 * width + width + j * 2) * 4 + 2] = src[width * height + i * width / 2 + j];
                    des[(i * 2 * width + width + j * 2 + 1) * 4 + 2] = src[width * height + i * width / 2 + j];
                }
            }


            //fill alpha
            byte b, g, r;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    b = getb(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1]);
                    g = getg(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1], des[(i * width + j) * 4 + 2]);
                    r = getr(des[(i * width + j) * 4], des[(i * width + j) * 4 + 2]);
                    des[(i * width + j) * 4] = b;
                    des[(i * width + j) * 4 + 1] = g;
                    des[(i * width + j) * 4 + 2] = r;
                    des[(i * width + j) * 4 + 3] = 255;
                }
            }
        }

        void ConvertYUY2toBGRA8(IBuffer buffer, byte[] des)
        {
            var src = buffer.ToArray();
            //y
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j+=2)
                {
                    des[(i * width + j) * 4] = src[(i * width + j) * 2];
                    des[(i * width + j) * 4 + 1] = src[(i * width + j) * 2 + 1];
                    des[(i * width + j) * 4 + 2] = src[(i * width + j) * 2 + 3];
                    des[(i * width + j) * 4 + 4] = src[(i * width + j) * 2 + 2];
                    des[(i * width + j) * 4 + 5] = src[(i * width + j) * 2 + 1];
                    des[(i * width + j) * 4 + 6] = src[(i * width + j) * 2 + 3];
                }
            }


            //fill alpha
            byte b, g, r;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    b = getb(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1]);
                    g = getg(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1], des[(i * width + j) * 4 + 2]);
                    r = getr(des[(i * width + j) * 4], des[(i * width + j) * 4 + 2]);
                    des[(i * width + j) * 4] = b;
                    des[(i * width + j) * 4 + 1] = g;
                    des[(i * width + j) * 4 + 2] = r;
                    des[(i * width + j) * 4 + 3] = 255;
                }
            }
        }

        void ConvertUYVYtoBGRA8(IBuffer buffer, byte[] des)
        {
            var src = buffer.ToArray();
            //y
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j += 2)
                {
                    des[(i * width + j) * 4] = src[(i * width + j) * 2 + 1];
                    des[(i * width + j) * 4 + 1] = src[(i * width + j) * 2];
                    des[(i * width + j) * 4 + 2] = src[(i * width + j) * 2 + 2];
                    des[(i * width + j) * 4 + 4] = src[(i * width + j) * 2 + 3];
                    des[(i * width + j) * 4 + 5] = src[(i * width + j) * 2];
                    des[(i * width + j) * 4 + 6] = src[(i * width + j) * 2 + 2];
                }
            }


            //fill alpha
            byte b, g, r;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    b = getb(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1]);
                    g = getg(des[(i * width + j) * 4], des[(i * width + j) * 4 + 1], des[(i * width + j) * 4 + 2]);
                    r = getr(des[(i * width + j) * 4], des[(i * width + j) * 4 + 2]);
                    des[(i * width + j) * 4] = b;
                    des[(i * width + j) * 4 + 1] = g;
                    des[(i * width + j) * 4 + 2] = r;
                    des[(i * width + j) * 4 + 3] = 255;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        byte getr(byte y, byte v)
        {
            int res = (int)y + (int)(1.403 * ((int)v - 128));
            if (res < 0) res = 0;
            return (byte)(res > 255 ? 255 : res);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        byte getg(byte y, byte u, byte v)
        {
            int res = (int)y - (int)(0.344 * ((int)u - 128)) - (int)(0.714 * ((int)v - 128));
            if (res < 0) res = 0;
            return (byte)(res > 255 ? 255 : res);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        byte getb(byte y, byte u)
        {
            int res = (int)y + (int)(1.77 * ((int)u - 128));
            if (res < 0) res = 0;
            return (byte)(res > 255 ? 255 : res);
        }

        private async void image_Drop(object sender, DragEventArgs e)
        {
            var defer = e.GetDeferral();
            try
            {
                DataPackageView dpv = e.DataView;
                if (dpv.Contains(StandardDataFormats.StorageItems))
                {
                    //List<StorageFile> files1 = new List<StorageFile>();
                    var files = await dpv.GetStorageItemsAsync();
                    if(files.Count == 1)
                    {
                        IStorageItem item = files[0];
                        if(item.IsOfType(StorageItemTypes.File))
                        {
                            file = item as StorageFile;
                            stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                            dataReader = new Windows.Storage.Streams.DataReader(stream);
                            fileSize = stream.Size;
                            if (fileSize == 0)
                            {
                                var msgDialog = new Windows.UI.Popups.MessageDialog("File size is 0. Please open another file") { Title = "open err" };
                                msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("ok"));
                                await msgDialog.ShowAsync();
                            }
                            else
                            {
                                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = file.DisplayName;
                                frameSize = GetFrameSize();
                                maxFrameInd = (fileSize + frameSize - 1) / frameSize;
                                slider.Minimum = 1;
                                slider.Maximum = maxFrameInd;
                                slider.Value = 1;
                                curInd = 1;
                                ReloadFile();
                                slider.IsEnabled = true;
                            }
                        }
                    }
                }
            }
            finally
            {
                defer.Complete();
            }
        }

        private void image_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "Open";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            e.Handled = true;
            
        }
    }
}
