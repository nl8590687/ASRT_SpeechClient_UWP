using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.ApplicationModel;

namespace ailemon
{
    class AudioRecorder
    {
        private MediaCapture _mediaCapture;
        private InMemoryRandomAccessStream _memoryBuffer;
        public bool IsRecording { get; set; }

        private const string DEFAULT_AUDIO_FILENAME = "1.wav";
        private string _fileName;

        public AudioRecorder()
        {

        }

        public void Initialize()
        {
            _memoryBuffer = new InMemoryRandomAccessStream();
        }

        public async void Record()
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Recording already in progress!");
            }
            Initialize();
            //await DeleteExistingFile();
            MediaCaptureInitializationSettings settings =
              new MediaCaptureInitializationSettings
              {
                  StreamingCaptureMode = StreamingCaptureMode.Audio
              };
            settings.AudioProcessing = Windows.Media.AudioProcessing.Raw;
            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync(settings);

            await _mediaCapture.StartRecordToStreamAsync(
              MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low), _memoryBuffer);

            //MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto)
            IsRecording = true;
        }

        public async void StopRecording()
        {
            if(_mediaCapture != null)
            {
                await _mediaCapture.StopRecordAsync();
                IsRecording = false;
                //SaveAudioToFile();
            }
        }

        public async Task<bool> SaveAudioToFile(string filename = DEFAULT_AUDIO_FILENAME)
        {
            IRandomAccessStream audioStream = _memoryBuffer.CloneStream();
            //StorageFolder storageFolder = Package.Current.InstalledLocation;
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

            StorageFile storageFile = await storageFolder.CreateFileAsync(
              filename, CreationCollisionOption.GenerateUniqueName);
            this._fileName = storageFile.Name;
            using (IRandomAccessStream fileStream =
              await storageFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                await RandomAccessStream.CopyAndCloseAsync(
                  audioStream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                await audioStream.FlushAsync();
                audioStream.Dispose();
            }
            return true;
        }
        public void Play()
        {
            MediaElement playbackMediaElement = new MediaElement();
            playbackMediaElement.SetSource(_memoryBuffer, "MP3");
            playbackMediaElement.Play();
        }
        public async Task PlayFromDisk(CoreDispatcher dispatcher)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                MediaElement playbackMediaElement = new MediaElement();
                StorageFolder storageFolder = Package.Current.InstalledLocation;
                StorageFile storageFile = await storageFolder.GetFileAsync(this._fileName);
                IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read);
                playbackMediaElement.SetSource(stream, storageFile.FileType);
                playbackMediaElement.Play();
            });
        }
        public void Close()
        {
            _mediaCapture.Dispose();
            _memoryBuffer.Dispose();
        }
    }
}
