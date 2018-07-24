using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net;


// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace SpeechClient_UWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AudioRecorder[] pool_audioRecorder = new AudioRecorder[2];
        int i_audioRecorder = 0;
        AudioRecorder _audioRecorder;
        DispatcherTimer timer;//定义定时器
        string filepath = "";
        
        string url = "http://127.0.0.1:20000/";

        Windows.Storage.StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;

        public MainPage()
        {
            this.InitializeComponent();

            pool_audioRecorder[0] = new AudioRecorder();
            pool_audioRecorder[1] = new AudioRecorder();
            this._audioRecorder = pool_audioRecorder[0];

            MessageBox.Visibility = Visibility.Collapsed;
        }

        private void btn_start_speech_input_Click(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 6);
            timer.Tick += Timer_Tick;//每6秒触发这个事件，以刷新指针
            timer.Start();

            this._audioRecorder.Record();
            MessageBox.Visibility = Visibility.Visible;
        }

        private async void btn_end_speech_input_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            this._audioRecorder.StopRecording();
            string filename = "speechfile_end.wav";
            await this._audioRecorder.SaveAudioToFile(filename);

            MessageBox.Visibility = Visibility.Collapsed;
            string text = "";

            try
            {
                text = await SpeechRecognizeAsync(filename);
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }

            this.text_note.Text += text;
            //this.text_note.Select(this.text_note.Text.Length, 0);
            //滚动到最后
            text_note.SelectionStart = text_note.Text.Length;
            ScrollViewer.ChangeView(0.0f, double.MaxValue, 1.0f);
            // 识别完成后删除文件
            DelWavFile(filename);
        }

        private async void btn_save_file_Click(object sender, RoutedEventArgs e)
        {
            //this.text_note.Text += "1234567890";
            //text_note.Text += await SpeechRecognizeAsync("12345.wav");
            text_note.Text += await SpeechRecognizeAsync("20180506_114631.wav");
            //text_note.Text += await SpeechRecognizeAsync("speechfile4.wav");

            text_note.SelectionStart = text_note.Text.Length;
            ScrollViewer.ChangeView(0.0f, double.MaxValue, 1.0f);

        }

        private async void Timer_Tick(object sender, object e)
        {
            this._audioRecorder.StopRecording();
            string filename = "speechfile" + i_audioRecorder.ToString() + ".wav";
            await this._audioRecorder.SaveAudioToFile(filename);

            //保存完文件后立即继续录音
            i_audioRecorder++;
            this._audioRecorder = pool_audioRecorder[i_audioRecorder % 2];
            this._audioRecorder.Record();
            pool_audioRecorder[(i_audioRecorder + 1) % 2] = new AudioRecorder();
            //timer.Stop();
            //MessageBox.Visibility = Visibility.Collapsed;

            string text = "";

            try
            {
                text = await SpeechRecognizeAsync(filename);
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }

            this.text_note.Text += text;
            //滚动到最后
            text_note.SelectionStart = text_note.Text.Length;
            ScrollViewer.ChangeView(0.0f, double.MaxValue, 1.0f);
            // 识别完成后删除文件
            DelWavFile(filename);


        }
        private async void DelWavFile(string filename)
        {
            //StorageFolder storageFolder = Package.Current.InstalledLocation;
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile storageFile = await storageFolder.GetFileAsync(filename);

            await storageFile.DeleteAsync();

        }

        private async Task<string> SpeechRecognizeAsync(string filename)
        {
            //将wav文件post到服务器进行语音识别
            //将识别回来的文本写入文本框
            wav wave = await WaveAccess(filename);
            Int16[] wavs = wave.wavs;

            int fs = wave.fs;
            string wavs_str = "";

            string[] tmp_strs = new string[wavs.Length];
            for (int i = 0; i < wavs.Length; i++)
            {
                //tmp_strs[i] = "&wavs=" + wavs[i].ToString();
                tmp_strs[i] = wavs[i].ToString();
            }
            wavs_str = string.Join("&wavs=", tmp_strs);


            //string r = await PostDataAsync(url, "qwertasd", wavs_str, fs.ToString());

            string r = await post2(url, "qwertasd", fs.ToString(), wavs_str);
            return r;
        }

        private async Task<string> PostDataAsync(string url, string token, string wavs, string fs)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(url);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/text"));
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("utf-8"));

                string endpoint = @"/";

                try
                {
                    string postdata = "token=" + token + "&fs=" + fs + "&wavs=" + wavs;
                    HttpContent content = new StringContent(postdata, Encoding.UTF8, "application/text");
                    httpClient.Timeout = new TimeSpan(0, 0, 10);
                    HttpResponseMessage response = await httpClient.PostAsync(endpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        //do something with json response here
                        return jsonResponse;
                    }
                    return "";
                }
                catch (Exception)
                {
                    //Could not connect to server
                    //Use more specific exception handling, this is just an example
                    return "";
                }
            }
        }

        private async Task<string> post(string url, string token, string fs, string wavs)
        {
            string resultContent = "";
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", token),
                    new KeyValuePair<string, string>("fs", fs),



                    new KeyValuePair<string, string>("wavs", wavs)

                });

                var result = await client.PostAsync("/", content);
                resultContent = await result.Content.ReadAsStringAsync();
                //Console.WriteLine(resultContent);
            }
            return resultContent;
        }

        private async Task<string> post2(string url, string token, string fs, string wavs)
        {
            string resultContent = "";

            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            //((HttpWebRequest)request).UserAgent = "SpeechRecognitionNote";
            //((HttpWebRequest)request). = "SpeechRecognitionNote";
            request.Method = "POST";
            string postdata = "token=" + token + "&fs=" + fs + "&wavs=" + wavs;
            //request.ContentLength = postdata.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            Stream dataStream = await request.GetRequestStreamAsync();
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postdata);
            dataStream.Write(byteArray, 0, byteArray.Length);
            //dataStream.Close();
            dataStream.Dispose();
            WebResponse response = await request.GetResponseAsync();
            Stream data = response.GetResponseStream();

            StreamReader sr = new StreamReader(data);
            resultContent = sr.ReadToEnd();
            response.Dispose();
            sr.Dispose();
            return resultContent;
        }

        /// <summary>
        /// 读取wav文件
        /// </summary>
        /// <param name="filename"></param>
        private async Task<wav> WaveAccess(string filename)
        {
            try
            {
                byte[] riff = new byte[4];
                byte[] riffSize = new byte[4];
                byte[] waveID = new byte[4];
                byte[] junkID = new byte[4];
                bool hasjunk = false;
                byte[] junklength = new byte[4];

                byte[] fmtID = new byte[4];
                byte[] cksize = new byte[4];
                uint waveType = 0;
                byte[] channel = new byte[2];
                byte[] sample_rate = new byte[4];
                byte[] bytespersec = new byte[4];
                byte[] blocklen_sample = new byte[2];
                byte[] bitNum = new byte[2];
                byte[] unknown = new byte[2];
                byte[] dataID = new byte[4];  //52
                byte[] dataLength = new byte[4];  //56 个字节

                //string longFileName = filepath;

                //FileStream fs = new FileStream(filepath, FileMode.Open);
                //Windows.Storage.StorageFolder s = Windows.ApplicationModel.Package.Current.InstalledLocation;
                //FileStream fs;

                //StorageFolder storageFolder = Package.Current.InstalledLocation;
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

                StorageFile storageFile = await storageFolder.GetFileAsync(filename);

                IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read);
                Stream s = fileStream.AsStream();



                BinaryReader bread = new BinaryReader(s);
                //BinaryReader bread = new BinaryReader(fs);
                riff = bread.ReadBytes(4); // RIFF

                if (BitConverter.ToUInt32(bytesReserve(riff), 0) != 0x52494646)
                {
                    Exception e = new Exception("该文件不是WAVE文件");
                    throw e;
                }

                riffSize = bread.ReadBytes(4); // 文件剩余长度

                if (BitConverter.ToUInt32(riffSize, 0) != bread.BaseStream.Length - bread.BaseStream.Position)
                {
                    //Exception e = new Exception("该WAVE文件损坏，文件长度与标记不一致");
                    //throw e;
                }

                waveID = bread.ReadBytes(4);

                if (BitConverter.ToUInt32(bytesReserve(waveID), 0) != 0x57415645)
                {
                    Exception e = new Exception("该文件不是WAVE文件");
                    throw e;
                }

                byte[] tmp = bread.ReadBytes(4);

                if (BitConverter.ToUInt32(bytesReserve(tmp), 0) == 0x4A554E4B)
                {
                    //包含junk标记的wav
                    junkID = tmp;
                    hasjunk = true;
                    junklength = bread.ReadBytes(4);
                    uint junklen = BitConverter.ToUInt32(junklength, 0);
                    //将不要的junk部分读出
                    bread.ReadBytes((int)junklen);

                    //读fmt 标记
                    fmtID = bread.ReadBytes(4);
                }
                else if (BitConverter.ToUInt32(bytesReserve(tmp), 0) == 0x666D7420)
                {
                    fmtID = tmp;
                }
                else
                {
                    Exception e = new Exception("无法找到WAVE文件的junk和fmt标记");
                    throw e;
                }



                if (BitConverter.ToUInt32(bytesReserve(fmtID), 0) != 0x666D7420)
                {
                    //fmt 标记
                    Exception e = new Exception("无法找到WAVE文件fmt标记");
                    throw e;
                }

                cksize = bread.ReadBytes(4);
                uint p_data_start = BitConverter.ToUInt32(cksize, 0);
                int p_wav_start = (int)p_data_start + 8;

                waveType = bread.ReadUInt16();

                if (waveType != 1)
                {
                    // 非pcm格式，暂不支持
                    Exception e = new Exception("WAVE文件不是pcm格式，暂时不支持");
                    throw e;
                }

                //声道数
                channel = bread.ReadBytes(2);

                //采样频率
                sample_rate = bread.ReadBytes(4);
                int fs = (int)BitConverter.ToUInt32(sample_rate, 0);

                //每秒钟字节数
                bytespersec = bread.ReadBytes(4);

                //每次采样的字节大小，2为单声道，4为立体声道
                blocklen_sample = bread.ReadBytes(2);

                //每个声道的采样精度，默认16bit
                bitNum = bread.ReadBytes(2);

                tmp = bread.ReadBytes(2);
                //寻找da标记
                while (BitConverter.ToUInt16(bytesReserve(tmp), 0) != 0x6461)
                {
                    tmp = bread.ReadBytes(2);
                }
                tmp = bread.ReadBytes(2);

                if (BitConverter.ToUInt16(bytesReserve(tmp), 0) != 0x7461)
                {
                    //ta标记
                    Exception e = new Exception("无法找到WAVE文件data标记");
                    throw e;
                }

                //wav数据byte长度
                uint DataSize = bread.ReadUInt32();
                //计算样本数
                long NumSamples = (long)DataSize / 2;

                if (NumSamples == 0)
                {
                    NumSamples = (bread.BaseStream.Length - bread.BaseStream.Position) / 2;
                }
                //if (BitConverter.ToUInt32(notDefinition, 0) == 18)
                //{
                //    unknown = bread.ReadBytes(2);
                //}
                //dataID = bread.ReadBytes(4);

                Int16[] data = new Int16[NumSamples];

                for (int i = 0; i < NumSamples; i++)
                {
                    //读入2字节有符号整数
                    data[i] = bread.ReadInt16();
                }

                s.Dispose();

                bread.Dispose();

                wav wave = new wav();
                wave.wavs = data;
                wave.fs = fs;
                return wave;
            }
            catch (System.Exception ex)
            {
                //return null;
                throw ex;
            }
        }

        /// <summary>
        /// 字节序列转换，小端序列和大端序列相互转换
        /// </summary>
        /// <param name="sbytes"></param>
        /// <returns></returns>
        private byte[] bytesReserve(byte[] sbytes)
        {
            int length = sbytes.Length;
            byte[] nbytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                nbytes[i] = sbytes[length - i - 1];
            }
            return nbytes;
        }
    }

    public class wav
    {
        public Int16[] wavs;
        public int fs;
    }

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
            await _mediaCapture.StopRecordAsync();
            IsRecording = false;
            //SaveAudioToFile();
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
    }
}
