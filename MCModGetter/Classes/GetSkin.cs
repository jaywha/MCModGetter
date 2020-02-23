using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MojangSharp.Endpoints
{
    public class GetSkin : IEndpoint<Response>
    {
        public GetSkin(string uuid) {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                var saveFileLoc = $@"C:\Users\{Environment.UserName}\Desktop\MMG_TempSkin_{DateTime.Now:MM-dd-yyyy_hhmmss}.png";
                client.BaseAddress = new Uri("https://sessionserver.mojang.com/session/minecraft/profile/");
                HttpResponseMessage response = client.GetAsync(uuid).Result;
                response.EnsureSuccessStatusCode();
                var fullRes = response.Content.ReadAsStringAsync().Result;
                var embeddedJSON = Encoding.UTF8.GetString(Convert.FromBase64String(fullRes));
                var skinURL = embeddedJSON.Split('{').Last().Split('\"')[3];
                using (var downloader = new WebClient())
                {
                    downloader.DownloadFile(skinURL, saveFileLoc);
                }
                Skin = new FileInfo(saveFileLoc);
            }
        }

        public static string CurrentUUID = "";
        public FileInfo Skin { get; }

        public override Task<Response> PerformRequestAsync()
            => Task.Factory.StartNew(() => new GetSkin(CurrentUUID).Response);
    }
}
