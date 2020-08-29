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
        private byte[] total;
        private int loopFrom;
        private int curPos;
        private int bytesPerSample;
        private byte[] preBuffer;
        private int prePos;
        private int preLoopFrom;
        
        Task task;
        CancellationTokenSource tokenSource;
        public WaveFormat WaveFormat { get; private set; }

        public MusicStream()
        {
            curPos = 0;
            
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (count % bytesPerSample != 0)
                count = (count / bytesPerSample) * bytesPerSample;


            lock (this)
            {
                for (int i = 0; i < count; i += bytesPerSample)
                {
                    Buffer.BlockCopy(total, curPos, buffer, offset + i, bytesPerSample);
                    curPos += bytesPerSample;
                    if (curPos >= total.Length)
                        curPos = loopFrom;
                }
                #region 淡入淡出

                if (preBuffer != null)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    unsafe
                    {
                        float al, ar, bl, br;
                        fixed (byte* pa = buffer)
                        {
                            fixed (byte* pb = preBuffer)
                            {
                                float* fpa = (float*)(pa + offset);
                                float* fpb = (float*)(pb + prePos);
                                int end = count / bytesPerSample * 2;
                                float x = 0;
                                float w1 = 0, w2 = 0;
                                for (int i = 0; i < end; i += 2)
                                {
                                    al = fpa[i];
                                    ar = fpa[i + 1];
                                    bl = fpb[i];
                                    br = fpb[i + 1];
                                    x = (i + 2) * 1.0f / end;
                                    w1 = x * x;
                                    w2 = 1 - x * 2 + w1;

                                    fpa[i] = al * w1 + bl * w2;
                                    fpa[i + 1] = ar * w1 + br * w2;

                                    if (i * sizeof(float) + prePos >= preBuffer.Length)
                                        fpb = (float*)(pb + preLoopFrom);
                                }
                                Debug.WriteLine($"w1={w1},w2={w2}  time={sw.Elapsed}");
                            }
                        }
                        preBuffer = null;
                        preLoopFrom = loopFrom;
                    }

                }
                
                #endregion
                prePos = curPos;
            }

            return count;
        }

        public void Init(Music music)
        {
            //若上次的读取过程未完成，取消
            if (task != null && !task.IsCompleted)
            {
                tokenSource.Cancel();
            }

            tokenSource = new CancellationTokenSource();
            if (total == null)
            {
                int len;
                int bufferSize;
                using (VorbisWaveReader reader = new VorbisWaveReader(music.FileName))
                {
                    var waveFormat = reader.WaveFormat;
                    var newBytesPerSample = waveFormat.BlockAlign; //得到每个采样所占的字节数
                    bufferSize = newBytesPerSample * waveFormat.SampleRate;//读完1秒的内容后立即播放，减少卡顿时间
                    int totalLength = (int)reader.Length; //字节数
                    int totalSample = totalLength / newBytesPerSample;//采样总数
                    int newLoopFrom = music.LoopFrom * newBytesPerSample;
                    var newTotal = new byte[totalLength]; //前奏部分字节数

                    //读取前1s部分
                    len = reader.Read(newTotal, 0, bufferSize);

                    lock (this)
                    {
                        //准备下一次淡入淡出
                        preLoopFrom = newLoopFrom;

                        bytesPerSample = newBytesPerSample;
                        total = newTotal;
                        loopFrom = newLoopFrom;
                        curPos = 0;
                        WaveFormat = waveFormat;
                    }
                }

                task = Task.Run(new Action(() =>
                {
                    using (VorbisWaveReader reader = new VorbisWaveReader(music.FileName))
                    {
                        reader.Position = len;
                        //继续读剩余部分
                        for (int pos = len; pos < total.Length; pos += len)
                        {

                            len = reader.Read(total, pos, Math.Min(bufferSize, total.Length - pos));
                        }
                    }
                }), tokenSource.Token);
            }
            else
            {
                task = Task.Run(new Action(() => {
                    string file = music.FileName;
                    using (VorbisWaveReader reader = new VorbisWaveReader(file))
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var waveFormat = reader.WaveFormat;
                        var newBytesPerSample = waveFormat.BlockAlign; //得到每个采样所占的字节数
                        int bufferSize = newBytesPerSample * waveFormat.SampleRate;//读完0.1秒的内容后立即播放，减少卡顿时间
                        int totalLength = (int)reader.Length; //采样总数
                        int totalSample = totalLength / newBytesPerSample;
                        int newLoopFrom = music.LoopFrom * newBytesPerSample;
                        var newTotal = new byte[totalLength]; //前奏部分字节数

                        //读取前1s部分
                        int len = reader.Read(newTotal, 0, bufferSize);

                        lock (this)
                        {
                            //淡入淡出
                            preBuffer = total;

                            bytesPerSample = newBytesPerSample;
                            total = newTotal;
                            loopFrom = newLoopFrom;
                            curPos = 0;
                            WaveFormat = waveFormat;
                        }
                        Debug.WriteLine(sw.Elapsed);

                        //继续读剩余部分
                        for (int pos = len; pos < newTotal.Length; pos += len)
                        {
                            if (tokenSource.IsCancellationRequested)
                            {
                                break;
                            }
                            len = reader.Read(newTotal, pos, Math.Min(bufferSize, newTotal.Length - pos));
                        }
                    }
                }), tokenSource.Token);
            }
            

        }
    }
}
