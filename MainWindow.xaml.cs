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

namespace AokanaMusicPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    
    public partial class MainWindow : Window
    {
        List<Music> musics;
        int curIndex = 0;
        WaveOut waveOut;
        MusicStream stream;

        public MainWindow()
        {
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
            stream.Init(musics[curIndex]);
            waveOut = new WaveOut();
            waveOut.Init(stream);
        }


        private void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string name = (sender as Button).Content.ToString();
            Title = "播放" + name;
            curIndex = musics.FindIndex((Music m) => m.Name == name);
            Lst.SelectedIndex = curIndex;

            Play();
        }

        private void btPlay_Click(object sender, RoutedEventArgs e)
        {
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
            Title = "播放 " + musics[curIndex].Name;
            Lst.SelectedIndex = curIndex;

            waveOut.Stop();
            waveOut.Dispose();

            stream.Init(musics[curIndex]);
            waveOut = new WaveOut();
            waveOut.Init(stream);
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
            if (curIndex == 0)
                curIndex = musics.Count;
            curIndex--;
            Play();
        }

        private void btNext_Click(object sender, RoutedEventArgs e)
        {
            curIndex = (curIndex + 1) % musics.Count;
            Play();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (waveOut != null)
            {
                waveOut.Volume = (float)(e.NewValue);
            }
        }
    }
}
