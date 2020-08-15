using System;
using TagLib;
using System.Collections.Generic;
using System.IO;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyAPI.Web.Auth;
using static SpotifyAPI.Web.Scopes;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net.Http.Headers;

namespace TagTest
{
    class Program
    {
        private static readonly string _clientId = "91927ac3a2bd4ba4b9f0803733b43c2e";
        private static readonly string _clientSecret = "";
        
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);

        private static List<KeyValuePair<string,string>> _artistAlbumPairs;

        private static List<KeyValuePair<string, string>> _notAddedArtistAlbumPairs;

        public static async Task<int> Main()
        {
            Console.WriteLine("Please specify the music library path: ");
            string path = Console.ReadLine();

            _artistAlbumPairs = GetArtistAlbumPairListFromPath(path);
            _notAddedArtistAlbumPairs = new List<KeyValuePair<string, string>>();



            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                throw new NullReferenceException
                (
                  "Please set SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET before starting the program"
                );
            }

            await StartAuthentication();

            Console.ReadKey();
            return 0;
        }
        private static async Task Start(AuthorizationCodeTokenResponse _token)
        {
            var spotify = new SpotifyClient(_token.AccessToken);

            SearchRequest sReq;
            SearchResponse sRes;

            LibrarySaveAlbumsRequest librarySaveAlbumsRequest;
            FollowRequest followRequest;

            List<string> IDList = new List<string>();
            HashSet<string> ArtistIDSet = new HashSet<string>();

            List<string> notAddedMusicList = new List<string>();
            List<string> addedMusicList = new List<string>();

            SimpleAlbum album;

            for (int i = 1; i <= _artistAlbumPairs.Count; ++i)
            {
                sReq = new SearchRequest(SearchRequest.Types.Album, _artistAlbumPairs[i-1].Key + " " + _artistAlbumPairs[i-1].Value);
                sRes = spotify.Search.Item(sReq).Result; // maybe need await?

                if (sRes.Albums.Items.Count != 0)
                {
                    var FoundAlbums = sRes.Albums;

                    album = FoundAlbums.Items[0];

                    ArtistIDSet.Add(album.Artists[0].Id);

                    IDList.Add(album.Id);

                    Console.WriteLine("Added to library: " + album.Artists[0].Name + " - " + album.Name);

                    addedMusicList.Add(album.Artists[0].Name + " - " + album.Name);
                }
                else
                {
                    _notAddedArtistAlbumPairs.Add(_artistAlbumPairs[i - 1]);
                }

                if (i % 50 == 0)
                {
                    librarySaveAlbumsRequest = new LibrarySaveAlbumsRequest(IDList);
                    followRequest = new FollowRequest(FollowRequest.Type.Artist, new List<string>(ArtistIDSet));

                    await spotify.Library.SaveAlbums(librarySaveAlbumsRequest);
                    await spotify.Follow.Follow(followRequest);

                    IDList = new List<string>();
                    ArtistIDSet = new HashSet<string>();

                    System.Threading.Thread.Sleep(500);
                }

                if (i == _artistAlbumPairs.Count)
                {
                    if (IDList.Count != 0)
                    {
                        librarySaveAlbumsRequest = new LibrarySaveAlbumsRequest(IDList);
                        followRequest = new FollowRequest(FollowRequest.Type.Artist, new List<string>(ArtistIDSet));

                        await spotify.Library.SaveAlbums(librarySaveAlbumsRequest);
                        await spotify.Follow.Follow(followRequest);

                        break;
                    }

                    break;
                }
            }

            if (_notAddedArtistAlbumPairs != null && _notAddedArtistAlbumPairs.Count != 0)
            {
                foreach (var pair in _notAddedArtistAlbumPairs)
                {
                    Console.WriteLine("Not added to library: " + pair.Key + " - " + pair.Value);
                }

                foreach (var pair in _notAddedArtistAlbumPairs)
                {
                    notAddedMusicList.Add(pair.Key + " - " + pair.Value);
                }
            }

            WriteListToFile(notAddedMusicList, "not_added.txt");
            WriteListToFile(addedMusicList, "added.txt");

            _server.Dispose();
            Environment.Exit(0);
        }

        private static async Task StartAuthentication()
        {
            await _server.Start();
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            var request = new LoginRequest(_server.BaseUri, _clientId!, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserLibraryModify, UserLibraryRead, UserFollowModify}
            };

            Uri uri = request.ToUri();

            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to open URL, manually open: {0}", uri);
            }
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();
            AuthorizationCodeTokenResponse token = await new OAuthClient().RequestToken(
              new AuthorizationCodeTokenRequest(_clientId!, _clientSecret!, response.Code, _server.BaseUri)
            );

            await Start(token);
        }

        private static List<KeyValuePair<string, string>> GetArtistAlbumPairListFromPath(string _path)
        {
            string[] mp3FilePaths = Directory.GetFiles(_path, "*.mp3", SearchOption.AllDirectories);
            string[] flacFilePaths = Directory.GetFiles(_path, "*.flac", SearchOption.AllDirectories);


            TagLib.File f;

            KeyValuePair<string, string> pair;
            HashSet<KeyValuePair<string, string>> ArtistAlbumPairsSet = new HashSet<KeyValuePair<string, string>>();


            for (int i = 0; i < mp3FilePaths.Length; ++i)
            {
                try
                {
                    f = TagLib.File.Create(mp3FilePaths[i]);

                    pair = new KeyValuePair<string, string>(f.Tag.FirstAlbumArtist, f.Tag.Album);

                    ArtistAlbumPairsSet.Add(pair);
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

                    pair = new KeyValuePair<string, string>(f.Tag.FirstAlbumArtist, f.Tag.Album);

                    ArtistAlbumPairsSet.Add(pair);
                }
                catch (CorruptFileException)
                {
                    Console.WriteLine("File " + flacFilePaths[i] + " Has no tags");
                }
            }

            return new List<KeyValuePair<string,string>>(ArtistAlbumPairsSet);
        }

        private static void WriteListToFile(List<string> list, string filename)
        {
            System.IO.File.WriteAllLines(AppDomain.CurrentDomain.BaseDirectory + @"\" + filename, list.ToArray());
        }
    }
}
