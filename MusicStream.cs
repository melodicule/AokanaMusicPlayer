using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio;
using NAudio.Vorbis;
using NVorbis;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace AokanaMusicPlayer
{
    class MusicStream: IWaveProvider
    {
        private MemoryStream file;
        private VorbisWaveReader reader;
        private int loopFrom;

        
        private MemoryStream preFile;
        private VorbisWaveReader preReader;
        private int preLoopFrom;

        private byte[] buffer1;
        private int bytesPerSample;
        public WaveFormat WaveFormat { get; private set; }

        public bool UseSmoothSwitchWhenPause { get; set; } = true;

        public MusicStream()
        {
            file = new MemoryStream();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (count % bytesPerSample != 0)
                count = (count / bytesPerSample) * bytesPerSample;

            lock (this)
            {
                for (int i = 0; i < count;)
                {
                    if (reader.Length - reader.Position <= count - i)
                    {
                        i += reader.Read(buffer, offset + i, (int)reader.Length - (int)reader.Position);
                        reader.Position = loopFrom;
                    }
                    else
                    {
                        i += reader.Read(buffer, offset + i, count - i);
                    }
                }
                

                #region 淡入淡出
                if (preReader != null)
                {
                    if (UseSmoothSwitchWhenPause)
                    {
                        unsafe
                        {
                            if (buffer1 == null || buffer1.Length < count)
                                buffer1 = new byte[count];
                            for (int i = 0; i < count;)
                            {
                                if (preReader.Length - preReader.Position <= count - i)
                                {
                                    i += preReader.Read(buffer1, i, (int)preReader.Length - (int)preReader.Position);
                                    preReader.Position = preLoopFrom;
                                }
                                else
                                {
                                    i += preReader.Read(buffer1, i, count - i);
                                }
                            }
                            float al, ar, bl, br;
                            fixed (byte* pa = buffer)
                            {
                                fixed (byte* pb = buffer1)
                                {
                                    float* fpa = (float*)(pa + offset);
                                    float* fpb = (float*)pb;
                                    int end = count / bytesPerSample * 2;
                                    float x;
                                    for (int i = 0; i < end; i += 2)
                                    {
                                        al = fpa[i];
                                        ar = fpa[i + 1];
                                        bl = fpb[i];
                                        br = fpb[i + 1];
                                        x = (i + 1) * 1.0f / end;

                                        fpa[i] = al * x + bl * (1 - x);
                                        fpa[i + 1] = ar * x + br * (1 - x);
                                    }
                                }
                            }
                            
                        }
                    }

                    preReader.Dispose();
                    preFile.Dispose();
                    preReader = null;
                    preFile = null;
                    UseSmoothSwitchWhenPause = true;
                }

                #endregion
            }
            return count;
        }

        public void Init(Music music, IWavePlayer waveOut)
        {
            if (reader == null)  //第一次读取
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                using (FileStream fs = new FileStream(music.FileName, FileMode.Open))
                {
                    fs.CopyTo(file);
                }
                Debug.WriteLine($"读取文件用时 {sw.ElapsedMilliseconds} ms");
                reader = new VorbisWaveReader(file, false);
                Debug.WriteLine($"打开文件用时 {sw.ElapsedMilliseconds} ms");
                WaveFormat = reader.WaveFormat;
                bytesPerSample = WaveFormat.BlockAlign;
                loopFrom = music.LoopFrom *  bytesPerSample;
            }
            else //第一次之后，记得释放资源
            {
                preFile = file;
                preReader = reader;
                file = new MemoryStream();
                using (FileStream fs = new FileStream(music.FileName, FileMode.Open))
                {
                    fs.CopyTo(file);
                }
                lock (this)
                {
                    reader = new VorbisWaveReader(file, false);
                    WaveFormat = reader.WaveFormat;
                    bytesPerSample = WaveFormat.BlockAlign;
                    preLoopFrom = loopFrom;
                    loopFrom = music.LoopFrom * bytesPerSample;
                }
            }

            if (waveOut.PlaybackState == PlaybackState.Stopped)
                waveOut.Init(this);
        }

    }
}
