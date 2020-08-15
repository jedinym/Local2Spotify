using System;
using TagLib;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace TagTest
{
    class Program
    {
        static void Main(string[] args)
        {
            GetAlbumListFromPath(@"D:\Music");
        }

        private static List<string> GetAlbumListFromPath(string _path)
        {
            string[] mp3FilePaths = Directory.GetFiles(_path, "*.mp3", SearchOption.AllDirectories);
            string[] flacFilePaths = Directory.GetFiles(_path, "*.flac", SearchOption.AllDirectories);

            HashSet<string> albumNames = new HashSet<string>();

            TagLib.File f;

            for (int i = 0; i < mp3FilePaths.Length; ++i)
            {
                try
                {
                    f = TagLib.File.Create(mp3FilePaths[i]);
                    albumNames.Add(f.Tag.Album);
                }
                catch (CorruptFileException)
                {
                    Console.WriteLine("File " + mp3FilePaths[i] + " Has no tags");
                }
            }

            for (int i = 0; i < flacFilePaths.Length; ++i)
            {
                try
                {
                    f = TagLib.File.Create(flacFilePaths[i]);
                    albumNames.Add(f.Tag.Album);
                }
                catch (CorruptFileException)
                {
                    Console.WriteLine("File " + flacFilePaths[i] + " Has no tags");
                }
            }

            return new List<string>(albumNames);
        }
    }
}
