using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WebCamApp
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {


        private DispatcherTimer count_timer;
        private bool _mirroringPreview;
        private static CameraRotationHelper _rotationHelper;
        private bool _externalCamera;
        private MediaCapture _mediaCapture;
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private MediaPlayer mediaPlayer1 = new MediaPlayer();
        private MediaElement media;
        private DeviceInformationCollection _CameraDeviceGroup;
        private DeviceInformation _CurrentCamera;
        private int countdown;
        StorageFile userImageFile;
        BitmapImage userImage;
        private string userImagePath;

        public MainPage()
        {
            this.InitializeComponent();

            //Window.Current.CoreWindow.SizeChanged += CoreWindow_SizeChanged;
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            //step2ColorStoryboard.Begin();
        }

        private void CoreWindow_SizeChanged(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.WindowSizeChangedEventArgs args)
        {
            var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            if (!appView.IsFullScreen)
            {
                appView.TryEnterFullScreenMode();
                //appView.TryEnterViewModeAsync(); 
            }
            args.Handled = true;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
          
            CameraPictureShowing();

            //PopUpWidget1.IsOpen = true;
        }

        private async void CameraButton_Click( object sender, RoutedEventArgs e)
        {
            StartCapture();
        }

        private async void BackButton_Click(object sender , RoutedEventArgs e)
        {
            imagePreview.Visibility = Visibility.Collapsed;
            BackimagePreview.Visibility = Visibility.Visible;
            StartIcon.Visibility = Visibility.Visible;
            BackIcon.Visibility = Visibility.Collapsed;
            cameraIcon.Visibility = Visibility.Collapsed;
            switchIcon.Visibility = Visibility.Collapsed;
            //PopUpWidget2.IsOpen = false;
            //PopUpWidget1.IsOpen = false;
            //step2ColorStoryboard.Stop();
        }

        private async void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            
            _CameraDeviceGroup = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            var cur = await ChangeDevice();

            await InitializeCameraAsync(cur);

            //StartCapture();
        }

        private async  Task<DeviceInformation> ChangeDevice()
        {
            Debug.WriteLine(_CurrentCamera.Name);
            for (int i = 0; i < _CameraDeviceGroup.Count; i++)
            {
                if (_CurrentCamera.Name == _CameraDeviceGroup[i].Name)
                {
                    Debug.WriteLine("hERE..........");
                    if (i < _CameraDeviceGroup.Count -1)
                    {
                        Debug.WriteLine(i.ToString() + " _______ " + _CameraDeviceGroup.Count);
                        Debug.WriteLine(_CameraDeviceGroup[i + 1].Name.ToString());
                        _CurrentCamera = _CameraDeviceGroup[i + 1];
                    }
                    else
                    {
                        Debug.WriteLine(i.ToString() + " _______ " + _CameraDeviceGroup.Count);
                        _CurrentCamera = _CameraDeviceGroup[0];
                    }
                    return _CurrentCamera;
                }
                else
                {
                    Debug.WriteLine("hERE!!!!!!!!!!!!");
                    Debug.WriteLine(_CameraDeviceGroup[0].Name);
                    _CurrentCamera = _CameraDeviceGroup[0];
                    return _CameraDeviceGroup[0];
                }

            }
            return null;
        }
        /// <summary>
        /// Camera Initialize and start capture
        /// timer tick for count down 3 seconds
        /// when finish capture upload to azure blob storage
        /// </summary>
        private async void CameraPictureShowing()
        {

           
            //Debug.WriteLine("Initial CaMERA ......... ");
            var cur = await ChooseCameraDevice();
            await InitializeCameraAsync(cur);
        }

        private async Task<DeviceInformation> ChooseCameraDevice()
        {
            switchIcon.Visibility = Visibility.Visible;
            cameraIcon.Visibility = Visibility.Visible;
            StartIcon.Visibility = Visibility.Collapsed;
            Debug.WriteLine("Choose a Camera Device");
            //Get all Devices
            _CameraDeviceGroup = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            for (int i = 0; i < _CameraDeviceGroup.Count; i++)
            {
                Debug.WriteLine(_CameraDeviceGroup[i].Name.ToString());
            }
            // try to get the back facing device for a phone
            var backFacingDevice = _CameraDeviceGroup
                .First(c => c.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Front);

            // but if that doesn't exist, take the first camera device available
            var preferredDevice = backFacingDevice ?? _CameraDeviceGroup.Last();

            _CurrentCamera = preferredDevice;

            return _CurrentCamera;
        }
        private async Task InitializeCameraAsync(DeviceInformation preferredDevice)
        {
            try
            {
                
                // if (_mediaCapture == null)
                //{
                Debug.WriteLine("open device");
                // Get the camera devices

                try
                {
                    // Create MediaCapture
                    _mediaCapture = new MediaCapture();

                    // Initialize MediaCapture and settings
                    await _mediaCapture.InitializeAsync(
                        new MediaCaptureInitializationSettings
                        {
                            VideoDeviceId = preferredDevice.Id
                        });


                    // Handle camera device location
                    if (preferredDevice.EnclosureLocation == null ||
                        preferredDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        Debug.WriteLine("null");
                        _externalCamera = true;
                    }
                    else
                    {
                        _externalCamera = false;
                        _mirroringPreview = (preferredDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }
                   // ShowMessage(preferredDevice.EnclosureLocation.ToString());
                    _rotationHelper = new CameraRotationHelper(preferredDevice.EnclosureLocation);
                    _rotationHelper.OrientationChanged += RotationHelper_OrientationChanged;
                    _mediaCapture.GetPreviewRotation();
                    // Set the preview source for the CaptureElement
                    PreviewControl.Visibility = Visibility.Visible;
                    PreviewControl.Source = _mediaCapture;
                    PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    // popUpDisplayText1.Text = "Let's take a picture! Smile~";
                    // Start viewing through the CaptureElement 
                    await _mediaCapture.StartPreviewAsync();
                    await SetPreviewRotationAsync();
                }
                catch (Exception e)
                {
                    ShowMessage(e.ToString());
                }
                // }
            }
            catch (UnauthorizedAccessException Error)
            {
                ShowMessage(Error.ToString());
            }
        }

        private async void StartCapture()
        {
        
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//引用stopwatch物件

            sw.Reset();//碼表歸零
            sw.Start();//碼表開始計時

            Debug.WriteLine("StopWatch init");
            countdown = 0;
            count_timer = new DispatcherTimer();
            count_timer.Interval = new TimeSpan(0, 0, 1);
            count_timer.Tick += timer_TickAsync;
            count_timer.Start();
        }

        public async void ShowMessage(string message)
        {
            var msg = new Windows.UI.Popups.MessageDialog(message);
            msg.DefaultCommandIndex = 1;
            await msg.ShowAsync();
        }

        private void OnLayoutUpdated3(object sender, object e)
        {
            if (gdChild3.ActualWidth == 0 && gdChild3.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.PopUpWidget3.HorizontalOffset;
            double ActualVerticalOffset = this.PopUpWidget3.VerticalOffset;

            double a = gdChild3.ActualWidth;
            double b = gdChild3.ActualHeight;

            double NewHorizontalOffset = (RegisterArea.ActualWidth - gdChild3.ActualWidth) / 2 ;
            double NewVerticalOffset = (RegisterArea.ActualHeight - gdChild3.ActualHeight) / 2 - 250;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.PopUpWidget3.HorizontalOffset = NewHorizontalOffset;
                this.PopUpWidget3.VerticalOffset = NewVerticalOffset;
            }
        }

        private void OnLayoutUpdated2(object sender, object e)
        {
            if (gdChild2.ActualWidth == 0 && gdChild2.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.PopUpWidget2.HorizontalOffset;
            double ActualVerticalOffset = this.PopUpWidget2.VerticalOffset;

            double a = gdChild2.ActualWidth;
            double b = gdChild2.ActualHeight;

            double NewHorizontalOffset = (RegisterArea.ActualWidth) / 2 + 400;
            double NewVerticalOffset = (RegisterArea.ActualHeight) / 2 - 50;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.PopUpWidget2.HorizontalOffset = NewHorizontalOffset;
                this.PopUpWidget2.VerticalOffset = NewVerticalOffset;
            }
        }

        private void OnLayoutUpdated1(object sender, object e)
        {
            if (gdChild1.ActualWidth == 0 && gdChild1.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.PopUpWidget1.HorizontalOffset;
            double ActualVerticalOffset = this.PopUpWidget1.VerticalOffset;

            double a = gdChild1.ActualWidth;
            double b = gdChild1.ActualHeight;

            double NewHorizontalOffset = (RegisterArea.ActualWidth) / 2 ;
            double NewVerticalOffset = (RegisterArea.ActualHeight) / 2 - 400;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.PopUpWidget1.HorizontalOffset = NewHorizontalOffset;
                this.PopUpWidget1.VerticalOffset = NewVerticalOffset;
            }
        }


        private async Task<int> UploadToAzureStorage(StorageFile file)
        {
            try
            {
                //  create Azure Storage
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=visitorimageblob;AccountKey=/z6meWEOmL2znURclm+n4q6p2+IQWA0l2EXogWfRCfx/SttPXJkoHrLMI5BQuqGA15VmYQnNwaxx43nZkn0KBQ==;EndpointSuffix=core.windows.net");

                //  create a blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // create storage file in local app storage
                //string time = System.DateTime.Now.ToString("yyyy'-'MM'-'dd", CultureInfo.CurrentUICulture.DateTimeFormat);

                //string myPhotos = Environment.GetFolderPath(Environ
                string p = System.IO.Path.Combine("2018devday");

                //  create a container 
                CloudBlobContainer container = blobClient.GetContainerReference(p);

                await container.CreateIfNotExistsAsync();

                // Set the permissions so the blobs are public. 
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                await container.SetPermissionsAsync(permissions);
                CloudBlockBlob blob = null;
                string filename = null;

                IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                if (null != fileStream)
                {
                    filename = file.Name;
                    blob = container.GetBlockBlobReference(filename);
                    await blob.DeleteIfExistsAsync();
                    await blob.UploadFromFileAsync(file);
                    var blobUrl = blob.Uri.AbsoluteUri;

                    Debug.WriteLine("Upload picture succeed! Filename: " + filename);
                    Debug.WriteLine("Url : " + blobUrl);
                   // PopUpWidget2.IsOpen = true;
                    //step3ColorStoryboard.Begin();
                    // PostPhotoAsync(blobUrl);
                }
                return 1;
            }
            catch
            {
                //  return error
                return 0;
            }
        }

        private async void timer_TickAsync(object sender, object e)
        {
            countdown++;
            mediaPlayer = new MediaPlayer();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/crrect_answer1.mp3"));
            PopUpWidget3.IsOpen = true;
            if (countdown == 1)
            {
                mediaPlayer.Play();
                popUpDisplayText3.Text = "3";
            }
            if (countdown == 2)
            {

                mediaPlayer.Play();
                popUpDisplayText3.Text = "2";
            }
            if (countdown == 3)
            {

                mediaPlayer.Play();
                popUpDisplayText3.Text = "1";

            }
            if (countdown == 4)
            {

                PopUpWidget3.IsOpen = false;
                mediaPlayer1 = new MediaPlayer();
                mediaPlayer1.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/crrect_answer3.mp3"));
                mediaPlayer1.Play();
                ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();

                // create storage file in local app storage
                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                //string myPhotos = Environment.GetFolderPath(Environ
                string p = System.IO.Path.Combine(time + ".png");
                // create storage file in local app storage
                StorageFolder picturelibrary  = KnownFolders.PicturesLibrary;
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    p,
                    CreationCollisionOption.ReplaceExisting);
                //var appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                //Debug.WriteLine(appInstalledFolder.Path);               
                //var picture = await appInstalledFolder.GetFolderAsync("picture");
                // var imageFile = await picture.CreateFileAsync(p);

                using (var captureStream = new InMemoryRandomAccessStream())
                {
                    await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var decoder = await BitmapDecoder.CreateAsync(captureStream);
                        var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                        var properties = new BitmapPropertySet {
                     { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.FlipHorizontal, PropertyType.UInt16) }
                     };
                        await encoder.BitmapProperties.SetPropertiesAsync(properties);

                        await encoder.FlushAsync();
                    }
                }

                // take 
                //await _mediaCapture.CapturePhotoToStorageFileAsync(imgFormat, file);

                // Get photo as a BitmapImage
                // Get photo as a BitmapImage

                // Get photo as a BitmapImage
                Debug.WriteLine(file.Path);
                userImage = new BitmapImage(new Uri(file.Path));
                userImagePath = file.Name;
                userImageFile = file;
                Debug.WriteLine(userImagePath);
                // imagePreview is a <Image> object defined in XAML
                PreviewControl.Visibility = Visibility.Collapsed;
                imagePreview.Visibility = Visibility.Visible;
                imagePreview.Source = userImage;



                cameraIcon.Visibility = Visibility.Collapsed;
                switchIcon.Visibility = Visibility.Collapsed;
                BackIcon.Visibility = Visibility.Visible;


                _mediaCapture = null;
                count_timer.Stop();
                await ConvertBmptoWritableBmp(file.Name);

                //await System.Threading.Tasks.Task.Delay(5000);

            }
        }

        private async Task ConvertBmptoWritableBmp(string fileName)
        { 
            WriteableBitmap wb;
            wb = await Render(fileName);

            // create storage file in local app storage
            string time = System.DateTime.Now.ToString("yyyy'-'MM'-'dd'-'hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            //string myPhotos = Environment.GetFolderPath(Environ
            string p = System.IO.Path.Combine(time + ".png");

            StorageFolder PictureFolder = KnownFolders.PicturesLibrary;
            // create storage file in local app storage
            StorageFile destiFile = await PictureFolder.CreateFileAsync(
                p,
                CreationCollisionOption.ReplaceExisting);
            //StorageFile destiFile = KnownFolders.PicturesLibrary.CreateFileAsync("NEW.");
            using (IRandomAccessStream stream = await destiFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(
                    BitmapEncoder.PngEncoderId, stream);
                Stream pixelStream = wb.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)wb.PixelWidth, (uint)wb.PixelHeight, 96.0, 96.0, pixels);
                await encoder.FlushAsync();
            }
            var bitmp = new BitmapImage();
            using (var strm = await destiFile.OpenReadAsync())
            {
                bitmp.SetSource(strm);
                //BlendedImage.Source = bitmp;
                
            }
            await UploadToAzureStorage(destiFile);
        }

        private async Task<WriteableBitmap> Render(string filePath)
        {
            var Assets = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
            StorageFile file1 = await Assets.GetFileAsync("Background640x480.png");
            StorageFolder PictureFolder = KnownFolders.PicturesLibrary;
            // create storage file in local app storage
            StorageFile file2 = await ApplicationData.Current.LocalFolder.GetFileAsync(filePath);
            BitmapImage i1 = new BitmapImage();
            BitmapImage i2 = new BitmapImage();
            using (IRandomAccessStream strm = await file1.OpenReadAsync())
            {
                i1.SetSource(strm);
            }
            using (IRandomAccessStream strm = await file2.OpenReadAsync())
            {
                i2.SetSource(strm);
            }
            WriteableBitmap img1 = new WriteableBitmap(i1.PixelWidth, i1.PixelHeight);
            WriteableBitmap img2 = new WriteableBitmap(i2.PixelWidth, i2.PixelHeight);
            using (IRandomAccessStream strm = await file1.OpenReadAsync())
            {
                img1.SetSource(strm);
            }
            using (IRandomAccessStream strm = await file2.OpenReadAsync())
            {
                img2.SetSource(strm);
            }
            WriteableBitmap destination = new WriteableBitmap((int)(img1.PixelWidth), (int)(img1.PixelHeight));
            destination.Clear(Colors.White);
            destination.Blit(new Rect(0, 0, (int)img2.PixelWidth, (int)img2.PixelHeight), img2, new Rect(0, 0, (int)img2.PixelWidth, (int)img2.PixelHeight));
            destination.Blit(new Rect(0, 0, (int)img1.PixelWidth, (int)img1.PixelHeight), img1, new Rect(0, 0, (int)img1.PixelWidth, (int)img1.PixelHeight));
           
            return destination;
        }
        /// <summary>
        /// control the camera frame invert , flip to the mirror image
        /// </summary>
        private async Task SetPreviewRotationAsync()
        {
            if (!_externalCamera)
            {
                // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
                var rotation = _rotationHelper.GetCameraPreviewOrientation();
                var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
                Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
                props.Properties.Add(RotationKey, CameraRotationHelper.ConvertSimpleOrientationToClockwiseDegrees(rotation));
                await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
            }
        }

        private async void RotationHelper_OrientationChanged(object sender, bool updatePreview)
        {
            if (updatePreview)
            {
                await SetPreviewRotationAsync();
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                // Rotate the buttons in the UI to match the rotation of the device
                var angle = CameraRotationHelper.ConvertSimpleOrientationToClockwiseDegrees(_rotationHelper.GetUIOrientation());
                var transform = new RotateTransform { Angle = angle };

                // The RenderTransform is safe to use (i.e. it won't cause layout issues) in this case, because these buttons have a 1:1 aspect ratio
                //CapturePhotoButton.RenderTransform = transform;
                //CapturePhotoButton.RenderTransform = transform;
            });
        }
    }
}
