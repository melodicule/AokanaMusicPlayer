using AokanaMusicPlayer.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;

namespace AokanaMusicPlayer
{
    class MusicInfo
    {

        public static void Init(List<Music> musics)
        {
            char[] separator = new char[] { '|' };
            string list = Resources.bgmlist;

            using (StringReader reader = new StringReader(list))
            {
                string info;
                while ((info = reader.ReadLine()) != null)
                {
                    var split = info.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length != 6) //filename|jumppoint|jp|en|cn|tw
                        throw new ArgumentException("读取歌曲信息时出现异常");
                    Music music = new Music()
                    {
                        Name = split[4],
                        FileName = @".\bgm\" + split[0] + ".ogg",
                        LoopFrom = int.Parse(split[1])
                    };
                    musics.Add(music);
                }
                    
            }
            
            
        }

    }
}
