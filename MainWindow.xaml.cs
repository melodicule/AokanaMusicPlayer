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
using System.Threading;

namespace AokanaMusicPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    
    public partial class MainWindow : Window
    {
        List<Music> musics;
        WaveOutEvent waveOut;
        MusicStream stream;
        float volume = 0.5f;
        CancellationTokenSource token1;
        CancellationTokenSource token2;
        const int FADE_DUR = 200;


        public MainWindow()
        {
            waveOut = new WaveOutEvent();
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
        }


        private void btPlay_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            if (Lst.SelectedIndex == -1)
            {
                Lst.SelectedIndex = 0;
                return;
            }

            if (bt.Tag.ToString() == "播放")
            {
                if (token2 != null)
                    token2.Cancel();
                token1 = new CancellationTokenSource();
                Task.Run(DelayPlay, token1.Token);
                btPause.Visibility = Visibility.Visible;
                btPlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (token1 != null)
                    token1.Cancel();
                token2 = new CancellationTokenSource();
                Task.Run(DelayPause);
                //waveOut.Pause();
                btPlay.Visibility = Visibility.Visible;
                btPause.Visibility = Visibility.Collapsed;
            }
        }


        private void Play()
        {
            Title = "播放 " + musics[Lst.SelectedIndex].Name;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            if (waveOut.PlaybackState == PlaybackState.Paused) //暂停状态下不使用平滑切换
            {
                waveOut.Stop(); //清除无效buffer
                stream.Init(musics[Lst.SelectedIndex], waveOut); 
                stream.UseSmoothSwitchWhenPause = false;
                if (token1 == null || token1.IsCancellationRequested)
                    token1 = new CancellationTokenSource();
                Task.Run(DelayPlay, token1.Token);
            }
            else
            {
                stream.Init(musics[Lst.SelectedIndex], waveOut);
                waveOut.Play();
            }
            
            sw.Stop();
            Debug.WriteLine($"初始化用时 {sw.ElapsedMilliseconds} ms");
            btPause.Visibility = Visibility.Visible;
            btPlay.Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            waveOut.Stop();
            waveOut.Dispose();
        }

        private void btPre_Click(object sender, RoutedEventArgs e)
        {
            if (Lst.SelectedIndex <= 0)
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
                volume = waveOut.Volume;
            }
        }

        private void Lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Title = "播放" + musics[Lst.SelectedIndex].Name;
            Play();
        }


        private void DelayPause()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < FADE_DUR)
            {
                if (token2.IsCancellationRequested)
                {
                    token2 = new CancellationTokenSource();
                    return;
                }
                waveOut.Volume = (1 - sw.ElapsedMilliseconds * 1.0f / FADE_DUR) * volume;
                Thread.Sleep(1);
            }
            waveOut.Pause();
        }

        private void DelayPlay()
        {
            waveOut.Play();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < FADE_DUR)
            {
                if (token1.IsCancellationRequested)
                {
                    token1 = new CancellationTokenSource();
                    return;
                }
                waveOut.Volume = (sw.ElapsedMilliseconds * 1.0f / FADE_DUR) * volume;
                Thread.Sleep(1);
            }
        }
    }
}
