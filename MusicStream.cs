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

namespace AokanaMusicPlayer
{
    class MusicStream: IWaveProvider
    {
        private byte[] intro;
        private byte[] loop;
        private int curPos;
        private int totalLength;
        private int bytesPerSample;
        private int bufferSize;
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

            int end = count + offset;
            for (int i = offset; i < end; i += bytesPerSample)
            {
                if (curPos >= totalLength) //跳回loop
                {
                    curPos = intro.Length;
                }
                if (curPos >= intro.Length) //播放loop部分
                {
                    Buffer.BlockCopy(loop, curPos - intro.Length, buffer, i, bytesPerSample);
                }
                else                        //播放前奏
                {
                    Buffer.BlockCopy(intro, curPos, buffer, i, bytesPerSample);
                }
                curPos += bytesPerSample;
            }

            return count;
        }

        public void Init(Music music)
        {
            curPos = 0;
            
            //若上次的读取过程未完成，取消
            if (task != null && !task.IsCompleted)
            {
                tokenSource.Cancel();
            }

            //理论上可以换成其他格式的Reader
            using (VorbisWaveReader reader = new VorbisWaveReader(music.FileName))
            {
                WaveFormat = reader.WaveFormat;
                bytesPerSample = WaveFormat.BlockAlign; //得到每个采样所占的字节数
                bufferSize = bytesPerSample * 44100 * 10;//读完10秒的内容后立即播放，减少卡顿时间
                totalLength = (int)reader.Length / bytesPerSample; //采样总数
                int loopFrom = music.LoopFrom;

                intro = new byte[bytesPerSample * loopFrom]; //前奏部分字节数
                loop = new byte[bytesPerSample * (totalLength - loopFrom)]; //循环部分字节数
                totalLength *= bytesPerSample;  //总字节数

                reader.Read(intro, 0, intro.Length);  //读取前奏部分
                if (loopFrom == 0)  //若不含前奏
                    reader.Read(loop, 0, bufferSize);    //先读取循环的一部分
            }

            tokenSource = new CancellationTokenSource();
            task = Task.Run(new Action(() =>
            {
                using (VorbisWaveReader reader = new VorbisWaveReader(music.FileName))
                {
                    if (music.LoopFrom == 0)
                        reader.Position = bufferSize;
                    else
                        reader.Position = intro.Length;

                    reader.Read(loop, music.LoopFrom == 0 ? bufferSize : 0, loop.Length);    //读取循环部分
                }
            }), tokenSource.Token);
            GC.Collect();
        }
    }
}
