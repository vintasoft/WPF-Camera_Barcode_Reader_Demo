using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Vintasoft.Barcode;
using Vintasoft.Imaging;
using Vintasoft.Imaging.Media;

namespace CameraBarcodeReaderDemo
{
    /// <summary>
    /// Allows to capture images from DirectShow camera (VintaSoft Imaging .NET SDK) and recognize barcodes in captured images (VintaSoft Barcode .NET SDK).
    /// </summary>
    /// <seealso cref="ImageCaptureDevice"/>
    /// <seealso cref="CameraBarcodeScanner"/>
    public class ImagingCameraBarcodeScanner : IDisposable
    {

        #region Fields

        /// <summary>
        /// Monitor for capture devices.
        /// </summary>
        ImageCaptureDevicesMonitor _captureDevicesMonitor;

        /// <summary>
        /// Image capture source for barcode recognition.
        /// </summary>
        ImageCaptureSource _imageCaptureSource;

        #endregion



        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImagingCameraBarcodeScanner"/> class.
        /// </summary>
        public ImagingCameraBarcodeScanner()
        {
            _barcodeScanner = new CameraBarcodeScanner();
            _barcodeScanner.FrameScanException += BarcodeScanner_FrameScanException;
            _barcodeScanner.ScannerSettings.ScanBarcodeTypes = BarcodeType.Code39 | BarcodeType.Code128 | BarcodeType.QR | BarcodeType.DataMatrix;

            _captureDevicesMonitor = new ImageCaptureDevicesMonitor();
            _captureDevicesMonitor.CaptureDevicesChanged += CaptureDevicesMonitor_CaptureDevicesChanged;

            if (!ImagingEnvironment.IsInDesignMode)
                _captureDevicesMonitor.Start();

            _imageCaptureSource = new ImageCaptureSource();
            _imageCaptureSource.CaptureCompleted += ImageCaptureSource_CaptureCompleted;

        }

        #endregion



        #region Properties

        ImageCaptureDevice _captureDevice;
        /// <summary>
        /// Gets or sets current capture device.
        /// </summary>
        public ImageCaptureDevice CaptureDevice
        {
            get
            {
                return _captureDevice;
            }
            set
            {
                if (_captureDevice != value)
                {
                    _captureDevice = value;

                    UpdateCapureFormats();
                }
            }
        }

        ReadOnlyCollection<ImageCaptureFormat> _captureFormats = null;
        /// <summary>
        /// Gets the available capture formats of current capture device.
        /// </summary>
        /// <seealso cref="CaptureDevice"/>
        public ReadOnlyCollection<ImageCaptureFormat> CaptureFormats
        {
            get
            {
                return _captureFormats;
            }
        }

        /// <summary>
        /// Gets available capture devices.
        /// </summary>
        public ReadOnlyCollection<ImageCaptureDevice> CaptureDevices
        {
            get
            {
                return ImageCaptureDeviceConfiguration.GetCaptureDevices();
            }
        }

        CameraBarcodeScanner _barcodeScanner;
        /// <summary>
        /// Gets the barcode scanner.
        /// </summary>
        public CameraBarcodeScanner BarcodeScanner
        {
            get
            {
                return _barcodeScanner;
            }
        }

        /// <summary>
        /// Gets a value indicating whether barcode scanning is started.
        /// </summary>
        public bool IsStarted
        {
            get
            {
                return _imageCaptureSource.State == ImageCaptureState.Started;
            }
        }

        #endregion



        #region Methods

        #region PUBLIC

        /// <summary>
        /// Starts the capturing of images from seleced device and recognition of barcodes in captured images.
        /// </summary>
        public void StartScanning()
        {
            try
            {
                if (CaptureDevice == null)
                    throw new ArgumentNullException("CaptureDevice");

                if (CaptureFormats == null || CaptureFormats.Count == 0)
                    throw new ArgumentNullException("CaptureFormats");

                if (_imageCaptureSource.State != ImageCaptureState.Started)
                {
                    if (CaptureDevice.DesiredFormat == null)
                        CaptureDevice.DesiredFormat = CaptureFormats[0];

                    _barcodeScanner.StartScanning();

                    _imageCaptureSource.CaptureDevice = _captureDevice;
                    _imageCaptureSource.Start();
                    _imageCaptureSource.CaptureAsync();

                    OnScanningStart(EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                OnScanningException(new ExceptionEventArgs(ex));
            }
        }

        /// <summary>
        /// Stops the capturing of images from seleced device and recognition of barcodes in captured images.
        /// </summary>
        public void StopScanning()
        {
            try
            {
                if (_imageCaptureSource.State != ImageCaptureState.Stopped)
                {
                    _barcodeScanner.StopScanning();
                    _imageCaptureSource.Stop();

                    OnScanningStop(EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                OnScanningException(new ExceptionEventArgs(ex));
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _captureDevicesMonitor.CaptureDevicesChanged -= CaptureDevicesMonitor_CaptureDevicesChanged;
            _barcodeScanner.BarcodeReader.Dispose();
        }

        #endregion


        #region PROTECTED

        /// <summary>
        /// Raises the <see cref="CaptureDevicesChanged" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ImageCaptureDevicesChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnCaptureDevicesChanged(ImageCaptureDevicesChangedEventArgs args)
        {
            if (CaptureDevicesChanged != null)
                CaptureDevicesChanged(this, args);
        }

        /// <summary>
        /// Raises the <see cref="ScanningException" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ExceptionEventArgs"/> instance containing the event data.</param>
        protected virtual void OnScanningException(ExceptionEventArgs args)
        {
            if (ScanningException != null)
                ScanningException(this, args);
        }

        /// <summary>
        /// Raises the <see cref="ScanningStart" /> event.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnScanningStart(EventArgs args)
        {
            if (ScanningStart != null)
                ScanningStart(this, args);
        }

        /// <summary>
        /// Raises the <see cref="ScanningStop" /> event.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnScanningStop(EventArgs args)
        {
            if (ScanningStop != null)
                ScanningStop(this, args);
        }

        #endregion


        #region PRIVATE

        /// <summary>
        /// Handles the FrameScanException event of the BarcodeScanner control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FrameScanExceptionEventArgs"/> instance containing the event data.</param>
        private void BarcodeScanner_FrameScanException(object sender, FrameScanExceptionEventArgs e)
        {
            OnScanningException(new ExceptionEventArgs(e.Exception));
        }

        /// <summary>
        /// Handles the CaptureCompleted event of the ImageCaptureSource.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ImageCaptureCompletedEventArgs"/> instance containing the event data.</param>
        private void ImageCaptureSource_CaptureCompleted(object sender, ImageCaptureCompletedEventArgs e)
        {
            try
            {
                // get captured image
                VintasoftImage image = e.GetCapturedImage();

                // recognize barcodes from captured image
                using (VintasoftBitmap bitmap = image.GetAsVintasoftBitmap())
                    _barcodeScanner.ScanFrame(bitmap);

                // if image capturing is started
                if (_imageCaptureSource.State == ImageCaptureState.Started)
                {
                    // capture next image 
                    _imageCaptureSource.CaptureAsync();
                }
            }
            catch (Exception ex)
            {
                OnScanningException(new ExceptionEventArgs(ex));
            }
        }

        /// <summary>
        /// Handles the CaptureDevicesChanged event of the CaptureDevicesMonitor.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ImageCaptureDevicesChangedEventArgs"/> instance containing the event data.</param>
        private void CaptureDevicesMonitor_CaptureDevicesChanged(object sender, ImageCaptureDevicesChangedEventArgs e)
        {
            if (Array.IndexOf(e.RemovedDevices, CaptureDevice) >= 0)
            {
                StopScanning();
            }

            OnCaptureDevicesChanged(e);
        }

        /// <summary>
        /// Updates the capture formats.
        /// </summary>
        private void UpdateCapureFormats()
        {
            ImageCaptureDevice device = CaptureDevice;
            if (device == null || device.SupportedFormats == null)
            {
                _captureFormats = null;
            }
            else
            {
                List<ImageCaptureFormat> captureFormats = new List<ImageCaptureFormat>();

                HashSet<string> imageCaptureFormatKeys = new HashSet<string>();
                for (int i = 0; i < device.SupportedFormats.Count; i++)
                {
                    ImageCaptureFormat captureFormat = device.SupportedFormats[i];

                    // if format has bit depth less or equal than 12 bit
                    if (captureFormat.BitsPerPixel <= 12)
                        // ignore formats with bit depth less or equal than 12 bit because they may cause issues on Windows 8
                        continue;
                    string imageCaptureFormatKey = captureFormat.Width + "X" + captureFormat.Height + " " + captureFormat.FramesPerSecond;
                    if (!imageCaptureFormatKeys.Contains(imageCaptureFormatKey))
                    {
                        imageCaptureFormatKeys.Add(imageCaptureFormatKey);

                        captureFormats.Add(captureFormat);
                    }
                }
                _captureFormats = captureFormats.AsReadOnly();
            }
        }

        #endregion

        #endregion



        #region Events

        /// <summary>
        /// Occurs when barcode scanning is started.
        /// </summary>
        public event EventHandler ScanningStart;

        /// <summary>
        /// Occurs when barcode scanning is stopped.
        /// </summary>
        public event EventHandler ScanningStop;

        /// <summary>
        /// Occurs when barcode scanning error occurs.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ScanningException;

        /// <summary>
        /// Occurs when <see cref="CaptureDevices"/> property is changed.
        /// </summary>
        public event EventHandler<ImageCaptureDevicesChangedEventArgs> CaptureDevicesChanged;

        #endregion

    }
}
