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
using ailemon;
using ailemon.asrt;


// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace SpeechClient_UWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        SpeechRecognizer asr;

        public MainPage()
        {
            this.InitializeComponent();

            asr = new SpeechRecognizer("http://127.0.0.1:20000/", "qwertasd");
            asr.OnReceiveText += receive_text;
            MessageBox.Visibility = Visibility.Collapsed;
        }

        private void btn_start_speech_input_Click(object sender, RoutedEventArgs e)
        {

            asr.Start();

            MessageBox.Visibility = Visibility.Visible;
        }

        private async void btn_end_speech_input_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Visibility = Visibility.Collapsed;

            await asr.StopAsync();
        }

        private void btn_change_url_Click(object sender, RoutedEventArgs e)
        {
            if(!asr.isRecognizing)
            {
                asr = new SpeechRecognizer(textbox_url.Text, "qwertasd");
                asr.OnReceiveText += receive_text;
            }

        }

        private void receive_text(object sender, string text)
        {
            text_note.Text += text;

            text_note.SelectionStart = text_note.Text.Length;
            ScrollViewer.ChangeView(0.0f, double.MaxValue, 1.0f);
        }
    }
        
}
