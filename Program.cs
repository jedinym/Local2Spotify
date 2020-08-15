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

namespace TagTest
{
    class Program
    {
        private static readonly string clientId = "91927ac3a2bd4ba4b9f0803733b43c2e";
        private static readonly string clientSecret = "2ca3279f2c584ee0be89ccd9b4655ae6";
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
        private static List<string> _albumNames;

        public static async Task<int> Main()
        {
            Console.WriteLine("Please specify the music library path: ");
            string path = Console.ReadLine();

            _albumNames = GetAlbumListFromPath(path);









            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new NullReferenceException
                (
                  "Please set SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET via environment variables before starting the program"
                );
            }

            await StartAuthentication();

            Console.ReadKey();
            return 0;
        }
        private static async Task Start(AuthorizationCodeTokenResponse _token)
        {
            Console.WriteLine("test");

            _server.Dispose();
            Environment.Exit(0);
        }

        private static async Task StartAuthentication()
        {
            await _server.Start();
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            var request = new LoginRequest(_server.BaseUri, clientId!, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserLibraryModify, UserLibraryRead }
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
              new AuthorizationCodeTokenRequest(clientId!, clientSecret!, response.Code, _server.BaseUri)
            );

            await Start(token);
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
