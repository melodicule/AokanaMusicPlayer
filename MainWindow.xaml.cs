using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using NAudio;
using NAudio.Wave;
using System.Diagnostics;

namespace AokanaMusicPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    
    public partial class MainWindow : Window
    {
        List<Music> musics;
        bool inited = false;
        WaveOut waveOut;
        MusicStream stream;

        public MainWindow()
        {
            waveOut = new WaveOut();
            InitializeComponent();

            try
            {
                musics = new List<Music>();
                MusicInfo.Init(musics);
                Lst.ItemsSource = musics;
            }
            catch
            {
                MessageBox.Show("请确保bgm文件夹下已含有所有文件");
            }

            stream = new MusicStream();
            waveOut.DesiredLatency = 300;
            //stream.Init(musics[0]);
        }


        private void btPlay_Click(object sender, RoutedEventArgs e)
        {
            if (Lst.SelectedIndex == -1)
            {
                Lst.SelectedIndex = 0;
                InitWaveOut();
                return;
            }
            InitWaveOut();

            if (btPlay.Content.ToString() == "播放")
            {
                if (waveOut.PlaybackState == PlaybackState.Paused)
                    waveOut.Resume();
                else
                    waveOut.Play();
                btPlay.Content = "暂停";
                
            }
            else
            {
                waveOut.Pause();
                btPlay.Content = "播放";
            }
        }


        private void Play()
        {
            Title = "播放 " + musics[Lst.SelectedIndex].Name;

            stream.Init(musics[Lst.SelectedIndex]);
            InitWaveOut();
            waveOut.Play();
            btPlay.Content = "暂停";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            waveOut.Stop();
            waveOut.Dispose();
        }

        private void btPre_Click(object sender, RoutedEventArgs e)
        {
            if (Lst.SelectedIndex == 0)
                Lst.SelectedIndex = musics.Count - 1;
            else
                Lst.SelectedIndex--;
        }

        private void btNext_Click(object sender, RoutedEventArgs e)
        {
            Lst.SelectedIndex = (Lst.SelectedIndex + 1) % musics.Count;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (waveOut != null)
            {
                waveOut.Volume = (float)(e.NewValue);
            }
        }

        private void Lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Title = "播放" + musics[Lst.SelectedIndex].Name;
            Play();
        }

        private void InitWaveOut()
        {
            if (!inited)
                waveOut.Init(stream);
            inited = true;
        }
    }
}
