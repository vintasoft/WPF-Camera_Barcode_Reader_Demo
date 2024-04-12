using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Microsoft.Win32;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Media;
using Vintasoft.Imaging.Wpf;
using Vintasoft.Barcode;
using Vintasoft.Barcode.BarcodeInfo;
using Vintasoft.Barcode.SymbologySubsets;

using WpfDemosCommonCode;
using WpfDemosCommonCode.Imaging.Codecs;
using Vintasoft.Imaging.Drawing;

namespace WpfCameraBarcodeReaderDemo
{
    /// <summary>
    /// A main window of "Camera Barcode Reader Demo" application.
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Fields

        /// <summary>
        /// Current capture device.
        /// </summary>
        ImageCaptureDevice _captureDevice;

        /// <summary>
        /// Capture devices monitor.
        /// </summary>
        ImageCaptureDevicesMonitor _captureDevicesMonitor;

        /// <summary>
        /// ImageCaptureSource for video preview.
        /// </summary>
        ImageCaptureSource _previewImageCaptureSource;

        /// <summary>
        /// ImageCaptureSource for barcode recognition.
        /// </summary>
        ImageCaptureSource _barcodeReaderImageCaptureSource;

        /// <summary>
        /// Barcode reader.
        /// </summary>
        BarcodeReader _barcodeReader;

        /// <summary>
        /// The source recognized image.
        /// </summary>
        VintasoftImage _recognizedSourceImage;

        /// <summary>
        /// F1 hot key.
        /// </summary>
        public static RoutedCommand _aboutCommand = new RoutedCommand();

        /// <summary>
        /// The save recognized image dialog.
        /// </summary>
        SaveFileDialog _saveRecognizedImageDialog = new SaveFileDialog();

        //DateTime _lastRecognitionTime = DateTime.Now;

        #endregion



        #region Constructor

        public MainWindow()
        {
            // register the evaluation license for VintaSoft Imaging .NET SDK
            Vintasoft.Imaging.ImagingGlobalSettings.Register("REG_USER", "REG_EMAIL", "EXPIRATION_DATE", "REG_CODE");

            InitializeComponent();

            this.Title = string.Format("VintaSoft WPF Camera Barcode Reader Demo v{0}", ImagingGlobalSettings.ProductVersion);

            _previewImageCaptureSource = new ImageCaptureSource();
            _previewImageCaptureSource.CaptureCompleted +=
                new EventHandler<ImageCaptureCompletedEventArgs>(PreviewImageCaptureSource_CaptureCompleted);

            _barcodeReaderImageCaptureSource = new ImageCaptureSource();
            _barcodeReaderImageCaptureSource.CaptureCompleted +=
                new EventHandler<ImageCaptureCompletedEventArgs>(BarcodeReaderImageCaptureSource_CaptureCompleted);

            _captureDevicesMonitor = new ImageCaptureDevicesMonitor();
            _captureDevicesMonitor.CaptureDevicesChanged +=
                new EventHandler<ImageCaptureDevicesChangedEventArgs>(CaptureDevicesMonitor_CaptureDevicesChanged);

            CodecsFileFilters.SetFilters(_saveRecognizedImageDialog, false);

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _captureDevicesMonitor.Start();

                InitCamerasComboBox();
                InitBarcodeReader();

                UpdateUI();
            }

            imageViewerForCameraPreview.InputGestureCut = null;
            imageViewerForCameraPreview.InputGestureDelete = null;
            imageViewerForCameraPreview.InputGestureInsert = null;

            imageViewerForCapturedImageWithBarcodes.InputGestureCut = null;
            imageViewerForCapturedImageWithBarcodes.InputGestureDelete = null;
            imageViewerForCapturedImageWithBarcodes.InputGestureInsert = null;

            recognitionTypeComboBox.SelectedIndex = 0;
        }

        #endregion



        #region Properties

        /// <summary>
        /// Gets or sets the preview image.
        /// </summary>
        public VintasoftImage PreviewImage
        {
            get
            {
                return imageViewerForCameraPreview.Image;
            }
            set
            {
                VintasoftImage oldImage = imageViewerForCameraPreview.Image;
                imageViewerForCameraPreview.Image = value;
                if (oldImage != null)
                    oldImage.Dispose();
            }
        }

        /// <summary>
        /// Gets or sets the image with recognized barcodes.
        /// </summary>
        public VintasoftImage RecognizedImage
        {
            get
            {
                return imageViewerForCapturedImageWithBarcodes.Image;
            }
            set
            {
                VintasoftImage oldImage = imageViewerForCapturedImageWithBarcodes.Image;
                imageViewerForCapturedImageWithBarcodes.Image = value;
                if (oldImage != null)
                    oldImage.Dispose();
            }
        }

        #endregion



        #region Methods

        #region Init

        /// <summary>
        /// Inits the combo box with camera names.
        /// </summary>
        private void InitCamerasComboBox()
        {
            camerasComboBox.Items.Clear();

            ReadOnlyCollection<ImageCaptureDevice> captureDevices = ImageCaptureDeviceConfiguration.GetCaptureDevices();

            foreach (ImageCaptureDevice device in captureDevices)
            {
                camerasComboBox.Items.Add(device);
            }

            if (captureDevices.Contains(_captureDevice))
            {
                camerasComboBox.SelectedItem = _captureDevice;
            }
            else
            {
                _captureDevice = null;

                if (camerasComboBox.Items.Count > 0)
                {
                    _captureDevice = captureDevices[0];
                    camerasComboBox.SelectedItem = _captureDevice;
                }
            }
            UpdateSupportedFormats();
        }

        /// <summary>
        /// Inits the barcode reader.
        /// </summary>
        private void InitBarcodeReader()
        {
            _barcodeReader = new BarcodeReader();
            _barcodeReader.Settings.MaximumThreadCount = Environment.ProcessorCount + 1;
            _barcodeReader.Settings.ScanDirection = ScanDirection.Horizontal | ScanDirection.Vertical;
            _barcodeReader.Settings.ScanBarcodeTypes = BarcodeType.Aztec | BarcodeType.EAN13 | BarcodeType.Code39 | BarcodeType.Code128 | BarcodeType.DataMatrix | BarcodeType.QR;
            _barcodeReader.Settings.MinConfidence = 95;

            _barcodeReader.Settings.AutomaticRecognition = true;

            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Aztec | BarcodeType.DataMatrix | BarcodeType.QR | BarcodeType.PDF417 | BarcodeType.PDF417Compact);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Code39 | BarcodeType.Code128 | BarcodeType.EAN13 | BarcodeType.UPCA | BarcodeType.UPCE);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Aztec);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.DataMatrix);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.DotCode);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.QR);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.MicroQR);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.HanXinCode);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.PDF417 | BarcodeType.PDF417Compact);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.MicroPDF417);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.MaxiCode);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Code16K);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.IATA2of5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Matrix2of5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Code11);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Codabar);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Code128);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Code39);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Code93);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.EAN13);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.EAN13Plus2);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.EAN13Plus5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.EAN8);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.UPCA);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.UPCE);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Interleaved2of5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Standard2of5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.MSI);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Pharmacode);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.RSS14);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.RSSExpanded);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.RSSLimited);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Telepen);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.AustralianPost);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.IntelligentMail);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.RoyalMail);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Planet);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Postnet);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Mailmark4StateC);
            scanBarcodeTypeComboBox.Items.Add(BarcodeType.Mailmark4StateL);

            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.Code32);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.Code39Extended);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.EANVelocity);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1DataBarLimited);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISBN);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISBNPlus2);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISBNPlus5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISMN);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISMNPlus2);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISMNPlus5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISSN);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISSNPlus2);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ISSNPlus5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.JAN13);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.JAN13Plus2);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.JAN13Plus5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.JAN8);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.JAN8Plus2);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.JAN8Plus5);
            scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.NumlyNumber);

            if (!BarcodeGlobalSettings.IsDemoVersion)
            {
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.OPC);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.VIN);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.SSCC18);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.SwissPostParcel);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.VicsBol);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.VicsScacPro);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.ITF14);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.FedExGround96);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.DhlAwb);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.DeutschePostIdentcode);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.DeutschePostLeitcode);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.MailmarkCmdmType7);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.MailmarkCmdmType9);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.MailmarkCmdmType29);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1Aztec);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1DataMatrix);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1QR);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1_128);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1DataBar);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1DataBarStacked);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1DataBarExpanded);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1DataBarExpandedStacked);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.GS1DataBarLimited);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.PPN);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.PZN);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.XFACompressedAztec);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.XFACompressedDataMatrix);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.XFACompressedPDF417);
                scanBarcodeTypeComboBox.Items.Add(BarcodeSymbologySubsets.XFACompressedQRCode);
            }

            // sort supported barcode list
            object[] barcodes = new object[scanBarcodeTypeComboBox.Items.Count];
            scanBarcodeTypeComboBox.Items.CopyTo(barcodes, 0);
            string[] names = new string[barcodes.Length];
            for (int i = 0; i < barcodes.Length; i++)
                names[i] = barcodes[i].ToString();
            Array.Sort(names, barcodes);
            scanBarcodeTypeComboBox.Items.Clear();
            foreach (object item in barcodes)
                scanBarcodeTypeComboBox.Items.Add(item);

            scanBarcodeTypeComboBox.SelectedItem = BarcodeType.Code39 | BarcodeType.Code128 | BarcodeType.EAN13 | BarcodeType.UPCA | BarcodeType.UPCE;
        }

        #endregion


        #region 'File' menu

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion


        #region 'Help' menu

        /// <summary>
        /// Shows an "About" dialog.
        /// </summary>
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder description = new StringBuilder();

            description.AppendLine("This project demonstrates the following SDK capabilities:");
            description.AppendLine();
            description.AppendLine("- Get a list of available webcams.");
            description.AppendLine();
            description.AppendLine("- Select and configure webcam.");
            description.AppendLine();
            description.AppendLine("- Preview video from webcam.");
            description.AppendLine();
            description.AppendLine("- Capture image from camera.");
            description.AppendLine();
            description.AppendLine("- Read barcodes from captured image.");
            description.AppendLine();
            description.AppendLine();
            description.Append("The project is available in C# and VB.NET for Visual Studio .NET.");

            WpfAboutBoxBaseWindow dlg = new WpfAboutBoxBaseWindow("vsimaging-dotnet");
            dlg.Description = description.ToString();
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        #endregion


        #region UI

        /// <summary>
        /// Updates UI.
        /// </summary>
        private void UpdateUI()
        {
            bool isCapturingStarted = _previewImageCaptureSource.State != ImageCaptureState.Stopped;
            startImageCapturingButton.IsEnabled = _captureDevice != null && !isCapturingStarted;
            startImageCapturingMenuItem.IsEnabled = _captureDevice != null && !isCapturingStarted;
            stopImageCapturingButton.IsEnabled = _captureDevice != null && isCapturingStarted;
            stopImageCapturingMenuItem.IsEnabled = _captureDevice != null && isCapturingStarted;
            configureCameraButton.IsEnabled = _captureDevice != null;
            configureCameraMenuItem.IsEnabled = _captureDevice != null;

            bool isBarcodeReadingStarted = _barcodeReaderImageCaptureSource.State != ImageCaptureState.Stopped;
            startBarcodeReadingButton.IsEnabled = isCapturingStarted && !isBarcodeReadingStarted;
            if (!isBarcodeReadingStarted)
                startBarcodeReadingButton.Content = "Start Barcode Reading";
            startBarcodeReadingMenuItem.IsEnabled = isCapturingStarted && !isBarcodeReadingStarted;
            stopBarcodeReadingButton.IsEnabled = isCapturingStarted && isBarcodeReadingStarted;
            stopBarcodeReadingMenuItem.IsEnabled = isCapturingStarted && isBarcodeReadingStarted;

            camerasComboBox.IsEnabled = !isCapturingStarted;
            supportedFormatsComboBox.IsEnabled = _captureDevice != null && !isCapturingStarted;
        }

        /// <summary>
        /// Starts the image capturing from camera.
        /// </summary>
        private void startImageCapturingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartCapturing();

                UpdateUI();
            }
            catch (Exception ex)
            {
                StopCapturing();
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Stops the image capturing from camera.
        /// </summary>
        private void stopImageCapturingButton_Click(object sender, RoutedEventArgs e)
        {
            StopBarcodeReading();
            StopCapturing();

            UpdateUI();
        }

        /// <summary>
        /// Configures the camera.
        /// </summary>
        private void configureCameraButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _captureDevice = (ImageCaptureDevice)camerasComboBox.SelectedItem;
                _captureDevice.ShowPropertiesDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Recognition type is changed.
        /// </summary>
        private void recognitionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_barcodeReader == null)
                return;
            if (recognitionTypeComboBox.SelectedIndex == 0)
            {
                // Automatic Recognition
                _barcodeReader.Settings.AutomaticRecognition = true;
            }
            else if (recognitionTypeComboBox.SelectedIndex == 1)
            {
                // Threshold (Auto)
                _barcodeReader.Settings.AutomaticRecognition = false;
                _barcodeReader.Settings.ThresholdMode = ThresholdMode.Automatic;
            }
            else
            {
                // Threshold
                _barcodeReader.Settings.AutomaticRecognition = false;
                _barcodeReader.Settings.ThresholdMode = ThresholdMode.Manual;
                _barcodeReader.Settings.Threshold = 50 + (recognitionTypeComboBox.SelectedIndex - 1) * 50;
            }
        }

        /// <summary>
        /// Scan barcode type is changed.
        /// </summary>
        private void scanBarcodeTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_barcodeReader == null)
                return;
            
            if (scanBarcodeTypeComboBox.SelectedItem == null)
                return;

            _barcodeReader.Settings.ScanBarcodeTypes = BarcodeType.None;

            if (scanBarcodeTypeComboBox.SelectedItem is BarcodeSymbologySubset)
            {
                _barcodeReader.Settings.ScanBarcodeSubsets.Clear();
                _barcodeReader.Settings.ScanBarcodeSubsets.Add((BarcodeSymbologySubset)scanBarcodeTypeComboBox.SelectedItem);
            }
            else
            {
                _barcodeReader.Settings.ScanBarcodeTypes = (BarcodeType)scanBarcodeTypeComboBox.SelectedItem;
            }
        }

        /// <summary>
        /// Starts the barcode reading from captured image.
        /// </summary>
        private void startBarcodeReadingButton_Click(object sender, RoutedEventArgs e)
        {
            StartBarcodeReading();

            UpdateUI();
        }

        /// <summary>
        /// Stops the barcode reading from captured image.
        /// </summary>
        private void stopBarcodeReadingButton_Click(object sender, RoutedEventArgs e)
        {
            StopBarcodeReading();

            UpdateUI();
        }

        /// <summary>
        /// Handles the Closing event of Window object.
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            StopBarcodeReading();
            StopCapturing();
        }

        #endregion


        #region Capture devices monitor

        /// <summary>
        /// Collection of available capture devices is changed.
        /// </summary>
        private void CaptureDevicesMonitor_CaptureDevicesChanged(object sender, ImageCaptureDevicesChangedEventArgs e)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
            {
                Dispatcher.Invoke(new CaptureDevicesMonitor_CaptureDevicesChangedDelegate(CaptureDevicesMonitor_CaptureDevicesChanged), sender, e);
            }
            else
            {
                List<ImageCaptureDevice> removedCameras = new List<ImageCaptureDevice>(e.RemovedDevices);

                if (removedCameras.Contains(_captureDevice))
                {
                    StopBarcodeReading();
                    StopCapturing();
                }

                foreach (ImageCaptureDevice removedDevice in e.RemovedDevices)
                    captureDeviceMonitorTextBox.AppendText(string.Format("Device '{0}' is disconnected.{1}", removedDevice.FriendlyName, Environment.NewLine));
                foreach (ImageCaptureDevice addedDevice in e.AddedDevices)
                    captureDeviceMonitorTextBox.AppendText(string.Format("Device '{0}' is connected.{1}", addedDevice.FriendlyName, Environment.NewLine));

                InitCamerasComboBox();

                UpdateUI();
            }
        }

        #endregion


        #region Camera

        /// <summary>
        /// Handles the SelectionChanged event of camerasComboBox object.
        /// </summary>
        private void camerasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_previewImageCaptureSource == null)
                return;
            if (_previewImageCaptureSource.State == ImageCaptureState.Stopped)
                PreviewImage = null;
            UpdateSupportedFormats();
        }

        private void UpdateSupportedFormats()
        {
            supportedFormatsComboBox.Items.Clear();
            ImageCaptureDevice device = (ImageCaptureDevice)camerasComboBox.SelectedItem;
            if (device != null && device.SupportedFormats != null)
            {
                List<uint> imageCaptureFormatSizes = new List<uint>();
                for (int i = 0; i < device.SupportedFormats.Count; i++)
                {
                    // if format has bit depth less or equal than 12 bit
                    if (device.SupportedFormats[i].BitsPerPixel <= 12)
                        // ignore formats with bit depth less or equal than 12 bit because they may cause issues on Windows 8
                        continue;

                    uint imageCaptureFormatSize = (uint)(device.SupportedFormats[i].Width | (device.SupportedFormats[i].Height << 16));
                    if (!imageCaptureFormatSizes.Contains(imageCaptureFormatSize))
                    {
                        imageCaptureFormatSizes.Add(imageCaptureFormatSize);

                        supportedFormatsComboBox.Items.Add(device.SupportedFormats[i]);
                    }
                }

                if (device.DesiredFormat != null)
                    supportedFormatsComboBox.SelectedItem = device.DesiredFormat;
                else
                    supportedFormatsComboBox.SelectedItem = null;
            }
        }

        /// <summary>
        /// Show captured preview image and initialize new image capture request.
        /// </summary>
        private void PreviewImageCaptureSource_CaptureCompleted(object sender, ImageCaptureCompletedEventArgs e)
        {
            VintasoftImage image = e.GetCapturedImage();

            // if automatic recognition is not used then
            if (!_barcodeReader.Settings.AutomaticRecognition &&
                _barcodeReaderImageCaptureSource.State == ImageCaptureState.Started)
            {
                // process barcode image
                using (BarcodeReader tempBarcodeReader = new BarcodeReader())
                {
                    tempBarcodeReader.Settings = _barcodeReader.Settings.Clone();
                    using (VintasoftBitmap bmp = image.GetAsVintasoftBitmap())
                    {
                        VintasoftImage processedImage = new VintasoftImage(tempBarcodeReader.ProcessImage(bmp), true);
                        image.Dispose();
                        image = processedImage;
                    }
                }
            }

            PreviewImage = image;

            if (_previewImageCaptureSource.State == ImageCaptureState.Started)
                _previewImageCaptureSource.CaptureAsync();
        }

        /// <summary>
        /// Starts image capturing from camera.
        /// </summary>
        private void StartCapturing()
        {
            _captureDevice = (ImageCaptureDevice)camerasComboBox.SelectedItem;
            _captureDevice.DesiredFormat = (ImageCaptureFormat)supportedFormatsComboBox.SelectedItem;
            _previewImageCaptureSource.CaptureDevice = _captureDevice;
            _previewImageCaptureSource.Start();
            _previewImageCaptureSource.CaptureAsync();

            captureDeviceMonitorTextBox.AppendText(string.Format("Image capturing started ({0}).", _captureDevice.FriendlyName));
            captureDeviceMonitorTextBox.AppendText(Environment.NewLine);

        }

        /// <summary>
        /// Stops image capturing from camera.
        /// </summary>
        private void StopCapturing()
        {
            if (_previewImageCaptureSource.State != ImageCaptureState.Stopped)
            {
                _previewImageCaptureSource.Stop();

                captureDeviceMonitorTextBox.AppendText(string.Format("Image capturing stopped ({0}).", _captureDevice.FriendlyName));
                captureDeviceMonitorTextBox.AppendText(Environment.NewLine);
            }
        }

        #endregion


        #region Read barcodes

        /// <summary>
        /// Starts barcode reading from camera.
        /// </summary>
        private void StartBarcodeReading()
        {
            _barcodeReaderImageCaptureSource.CaptureDevice = _captureDevice;
            _barcodeReaderImageCaptureSource.Start();
            _barcodeReaderImageCaptureSource.CaptureAsync();

            readerResultsTextBox.AppendText("Barcode recognition started.");
            readerResultsTextBox.AppendText(Environment.NewLine);
        }

        /// <summary>
        /// Stops barcode reading from camera.
        /// </summary>
        private void StopBarcodeReading()
        {
            if (_barcodeReaderImageCaptureSource.State != ImageCaptureState.Stopped)
            {
                _barcodeReaderImageCaptureSource.Stop();
                readerResultsTextBox.AppendText("Barcode recognition stopped.");
                readerResultsTextBox.AppendText(Environment.NewLine);
            }
        }

        /// <summary>
        /// Recognize barcode on captured image and initialize new image capture request.
        /// </summary>
        private void BarcodeReaderImageCaptureSource_CaptureCompleted(object sender, ImageCaptureCompletedEventArgs e)
        {
            try
            {
                IBarcodeInfo[] barcodeInfo;

                // gets captured image
                VintasoftImage image = e.GetCapturedImage();

                // read barcodes from image
                using (VintasoftBitmap bitmap = image.GetAsVintasoftBitmap())
                    barcodeInfo = _barcodeReader.ReadBarcodes(bitmap);

                // if barcode reading started then
                if (_barcodeReaderImageCaptureSource.State == ImageCaptureState.Started)
                {
                    // show FPS
                    //TimeSpan timeSpan = DateTime.Now - _lastRecognitionTime;
                    //string fps = string.Format("{0:f2} FPS", 1000 / timeSpan.TotalMilliseconds);
                    //Dispatcher.Invoke(new SetButtonTextDelegate(SetButtonText), new object[] { startBarcodeReadingButton, fps });
                    //_lastRecognitionTime = DateTime.Now;

                    // if barcode recognized then
                    if (barcodeInfo.Length > 0)
                    {
                        // show reader results
                        Dispatcher.Invoke(new ShowRecognitionResultsDelegate(ShowRecognitionResults), new object[] { barcodeInfo, image });

                        // show captured image in image viewer
                        RecognizedImage = image;
                    }
                    else
                    {
                        // dispose captured image
                        image.Dispose();
                    }

                    // capture next image 
                    _barcodeReaderImageCaptureSource.CaptureAsync();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageAsync(ex.Message);
            }
        }


        private string GetBarcodesInfo(IBarcodeInfo[] barcodes)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < barcodes.Length; i++)
                result.AppendLine(GetBarcodeInfo(i, barcodes[i]));

            return result.ToString();
        }

        /// <summary>
        /// Gets a barcode information as text.
        /// </summary>
        private string GetBarcodeInfo(int index, IBarcodeInfo info)
        {
            info.ShowNonDataFlagsInValue = true;

            string value = info.Value;
            if (info.BarcodeType == BarcodeType.UPCE)
                value += string.Format(" ({0})", (info as UPCEANInfo).UPCEValue);

            string confidence;
            if (info.Confidence == ReaderSettings.ConfidenceNotAvailable)
                confidence = "N/A";
            else
                confidence = Math.Round(info.Confidence).ToString() + "%";

            if (info is BarcodeSubsetInfo)
            {
                value = string.Format("{0}{1}Base value: {2}",
                    RemoveSpecialCharacters(value), Environment.NewLine,
                    RemoveSpecialCharacters(((BarcodeSubsetInfo)info).BaseBarcodeInfo.Value));
            }
            else
            {
                value = RemoveSpecialCharacters(value);
            }

            string barcodeTypeValue;
            if (info is BarcodeSubsetInfo)
                barcodeTypeValue = ((BarcodeSubsetInfo)info).BarcodeSubset.ToString();
            else
                barcodeTypeValue = info.BarcodeType.ToString();


            StringBuilder result = new StringBuilder();
            result.AppendLine(string.Format("[{0}:{1}]", index + 1, barcodeTypeValue));
            result.AppendLine(string.Format("Value: {0}", value));
            result.AppendLine(string.Format("Confidence: {0}", confidence));
            result.AppendLine(string.Format("Reading quality: {0}", info.ReadingQuality));
            result.AppendLine(string.Format("Threshold: {0}", info.Threshold));
            result.AppendLine(string.Format("Region: {0}", info.Region));
            return result.ToString();
        }

        private string RemoveSpecialCharacters(string text)
        {
            StringBuilder sb = new StringBuilder();
            if (text != null)
                for (int i = 0; i < text.Length; i++)
                    if (text[i] >= ' ' || text[i] == '\n' || text[i] == '\r' || text[i] == '\t')
                        sb.Append(text[i]);
                    else
                        sb.Append('?');
            return sb.ToString();
        }

        #endregion


        #region Show barcode recognition results

        /// <summary>
        /// Shows barcode recognition results.
        /// </summary>
        private void ShowRecognitionResults(IBarcodeInfo[] barcodeInfo, VintasoftImage image)
        {
            // show barcode recognition in text box
            readerResultsTextBox.Text = GetBarcodesInfo(barcodeInfo);

            // set source recognized image
            if (_recognizedSourceImage != null)
                _recognizedSourceImage.Dispose();
            _recognizedSourceImage = (VintasoftImage)image.Clone();

            // draw rectangles of searched barcodes on captured image
            DrawBarcodeRectangles(barcodeInfo, image);

            // enable save image button
            saveImageAsButton.IsEnabled = true;
        }

        /// <summary>
        /// Draws rectangles of searched barcodes on captured image.
        /// </summary>
        private void DrawBarcodeRectangles(IBarcodeInfo[] barcodes, VintasoftImage image)
        {
            Color fillColor = Color.FromArgb(64, Color.Green);
            Color stokeColor = Color.Green;

            // open Graphics
            using (DrawingEngine graphics = image.CreateDrawingEngine())
            {
                // for each recognized barcode
                foreach (IBarcodeInfo barcode in barcodes)
                {
                    // draw barcode rectangle
                    float x = barcode.Region.LeftTop.X;
                    float y = barcode.Region.LeftTop.Y;
                    PointF leftTop = new PointF(x, y);
                    PointF rightTop = new PointF(barcode.Region.RightTop.X, barcode.Region.RightTop.Y);
                    PointF rightBottom = new PointF(barcode.Region.RightBottom.X, barcode.Region.RightBottom.Y);
                    PointF leftBottom = new PointF(barcode.Region.LeftBottom.X, barcode.Region.LeftBottom.Y);
                    using (IDrawingPen pen = graphics.DrawingFactory.CreatePen(Color.FromArgb(128, Color.Blue), 1))
                        graphics.DrawPolygon(pen, new PointF[] { leftTop, rightTop, rightBottom, leftBottom });

                    // draw bounding rectangle                
                    Rectangle boundRect = GdiConverter.Convert(barcode.Region.Rectangle);
                    PointF[] boundRectPoints = new PointF[] {
                        new PointF(boundRect.X, boundRect.Y),
                        new PointF(boundRect.X + boundRect.Width, boundRect.Y),
                        new PointF(boundRect.X + boundRect.Width, boundRect.Y + boundRect.Height),
                        new PointF(boundRect.X, boundRect.Y + boundRect.Height)
                    };
                    using (IDrawingBrush fillBrush = graphics.DrawingFactory.CreateSolidBrush(fillColor))
                        graphics.FillPolygon(fillBrush, boundRectPoints);
                    using (IDrawingPen skrokePen = graphics.DrawingFactory.CreatePen(stokeColor, 2))
                        graphics.DrawPolygon(skrokePen, boundRectPoints);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of saveImageAsButton object.
        /// </summary>
        private void saveImageAsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_saveRecognizedImageDialog.ShowDialog().Value)
                try
                {
                    _recognizedSourceImage.Save(_saveRecognizedImageDialog.FileName);
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
        }

        #endregion


        #region Utils

        private void ShowErrorMessageAsync(string message)
        {
            Dispatcher.Invoke(new ShowErrorMessageDelegate(ShowErrorMessage));
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message);
        }

        private void SetButtonText(Button button, string text)
        {
            button.Content = text;
        }

        #endregion

        #endregion



        #region Delegates

        delegate void SetButtonTextDelegate(Button button, string text);

        delegate void ShowRecognitionResultsDelegate(IBarcodeInfo[] barcodeInfo, VintasoftImage image);

        delegate void ShowErrorMessageDelegate(string message);

        delegate void CaptureDevicesMonitor_CaptureDevicesChangedDelegate(object sender, ImageCaptureDevicesChangedEventArgs e);

        #endregion

    }
}
