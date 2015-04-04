﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using TagLib;
using TagLib.Mpeg;
using File = System.IO.File;

namespace MusicTagger
{
    internal class Program
    {
        public const string Version = "1.0";
        public static readonly WebHeaderCollection Headers = new WebHeaderCollection();
        public static readonly string ApiEndpoint = "http://vocadb.net";
        private static readonly List<string> Nulls = new List<string>();
        private static readonly HashSet<AlbumInfo> Albums = new HashSet<AlbumInfo>(AlbumInfo.AlbumInfoComparer);
        private static readonly Dictionary<string, uint> ManualMappings = new Dictionary<string, uint>();

        private static void Main(string[] args)
        {
            const string dir = @"E:\Users\Cayde\Music";
//            const string dir = @"E:\Users\Cayde\Documents\Visual Studio 2013\Projects\MusicTagger\MusicTagger\bin\TestData";
//            var info = AlbumInfo.GetFromId(2278);

            var manualMappings = Path.Combine(dir, "manual_mapping.txt");
            var mappings = File.ReadAllLines(manualMappings);

            foreach (var mapping in (from m in mappings select m.Split('=')).Where(a => a.Length == 2))
                ManualMappings.Add(mapping[0], Convert.ToUInt32(mapping[1]));

            Console.OutputEncoding = Encoding.UTF8;

            IterateDirectory(new DirectoryInfo(dir));
            File.WriteAllLines(Path.Combine(dir, "new-fails.txt"), Nulls);
            Console.ReadKey();
        }

        private static void IterateDirectory(DirectoryInfo di)
        {
            Console.WriteLine("Processing Folder: {0}", di.Name);
            Console.Title = di.Name;

            di.GetDirectories().ToList().ForEach(IterateDirectory);
            di.GetFiles().ToList().ForEach(ProcessFile);
        }

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static void ProcessFile(FileInfo file)
        {
            Console.Write("Processing file: {0}:\t", file.Name);
            try
            {
                var music = TagLib.File.Create(file.FullName) as AudioFile;
                if (music == null || Nulls.Contains(music.Tag.Album))
                {
                    Console.WriteLine("Skipping...");
                    return;
                }

                AlbumInfo album = null;
                if (File.Exists(Path.Combine(file.DirectoryName, "vocadb.txt")))
                {
                    var id = Convert.ToUInt32(File.ReadAllLines(Path.Combine(file.DirectoryName, "vocadb.txt"))[0]);
                    album = Albums.FirstOrDefault(a => a.VocaDbId == id);
                    if (9919 == id) return;
                    if (album == null)
                    {
                        album = AlbumInfo.GetFromId(id);
                        Albums.Add(album);
                    }
                }
                if (album == null) album = GetForAlbum(music.Tag.Album);

                if (album == null)
                {
                    Nulls.Add(music.Tag.Album);
                    return;
                }

                album.WriteToFile(music);
                Console.WriteLine("Done!");
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                //ignored
                if (!(ex is UnsupportedFormatException))
                {
                    Console.WriteLine("Exception! of type: {0}", ex);
                }
            }
        }

        private static AlbumInfo GetForAlbum(string name)
        {
            var album = Albums.FirstOrDefault(a => a.Name == name);

            if (album == null)
            {
                if (ManualMappings.ContainsKey(name))
                {
                    var id = ManualMappings[name];
                    album = Albums.FirstOrDefault(a => a.VocaDbId == id) ??
                            AlbumInfo.GetFromId(id);
                    if (album != null)
                    {
                        Albums.Add(album);
                        return album;
                    }
                }

                album = AlbumInfo.GetFromName(name);
                if (album != null) Albums.Add(album);
            }
            if (album != null) return album;

            Nulls.Add(name);
            return null;
        }
    }
}