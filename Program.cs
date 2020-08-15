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
            string[] mp3FilePaths = Directory.GetFiles(@"D:\Music", "*.mp3", SearchOption.AllDirectories);
            string[] flacFilePaths = Directory.GetFiles(@"D:\Music", "*.flac", SearchOption.AllDirectories);

            List<string> filePaths = new List<string>();
            
            foreach (string path in mp3FilePaths)
            {
                filePaths.Add(path);
            }

            foreach (string path in flacFilePaths)
            {
                filePaths.Add(path);
            }


            HashSet<string> albumNames = new HashSet<string>();

            TagLib.File f = TagLib.File.Create(@"D:\Music\Air\Moon Safari (Album)\02 Sexy Boy.mp3");

            Stopwatch sw = new Stopwatch();

            foreach (string path in filePaths)
            {
                //sw.Start();
                try
                {
                    f = TagLib.File.Create(path);
                }
                catch (TagLib.CorruptFileException)
                {
                    Console.WriteLine("Path " + path + " Has no tags");
                }
                //sw.Stop();

                //Console.WriteLine(sw.ElapsedMilliseconds);
                //sw.Reset();


                albumNames.Add(f.Tag.Album);

            }

            List<string> albumNameList = new List<string>(albumNames);
            albumNameList.Sort();

            foreach (string name in albumNameList)
            {
                Console.WriteLine(name);
            }

            Console.WriteLine(albumNameList.Count);

            //TagLib.File f = TagLib.File.Create(@"D:\Music\Amon Tobin\Out From Out Where (Album)\01 Back From Space.mp3");
            //Console.WriteLine(f.Tag.Title);
            
        }
    }
}
