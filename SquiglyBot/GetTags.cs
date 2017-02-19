using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.Audio;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using TagLib;

namespace SquiglyBot
{
    class GetTags
    {
        public string NowPlaying(string sPath)
        {
            TagLib.File music = TagLib.File.Create(sPath);
            String title = music.Tag.Title;
            String artist = music.Tag.FirstPerformer;
            String length = music.Properties.Duration.ToString(@"mm\:ss");
            return $"{artist} - {title} ({length})";
        }

        public string Title(string sPath)
        {
            TagLib.File music = TagLib.File.Create(sPath);
            String title = music.Tag.Title;
            return title;
        }

        public string Artist(string sPath)
        {
            TagLib.File music = TagLib.File.Create(sPath);
            String artist = music.Tag.FirstPerformer;
            return artist;
        }

        public string Length(string sPath)
        {
            TagLib.File music = TagLib.File.Create(sPath);
            String length = music.Properties.Duration.ToString(@"mm\:ss");
            return length;
        }
    }
}
