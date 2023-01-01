using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MinecraftLaunch.Modules.Enum;
using MinecraftLaunch.Modules.Models.Download;
using Natsurainko.Toolkits.Network;
using Newtonsoft.Json.Linq;

namespace MinecraftLaunch.Modules.Toolkits
{
    public partial class CurseForgeToolkit
    {
        public async ValueTask<List<CurseForgeModpack>> SearchModpacksAsync(string searchFilter, ModLoaderType modLoaderType = ModLoaderType.Any, string gameVersion = null, int category = -1)
        {
            var builder = new StringBuilder(API)
                .Append($"/search?gameId=432")
                .Append(string.IsNullOrEmpty(searchFilter) ? string.Empty : $"&searchFilter={searchFilter}")
                .Append($"&modLoaderType={(int)modLoaderType}")
                .Append(string.IsNullOrEmpty(gameVersion) ? string.Empty : $"&gameVersion={gameVersion}")
                .Append(category == -1 ? string.Empty : $"&categoryId={gameVersion}")
                .Append("&sortField=Featured&sortOrder=desc&classId=6");

            var result = new List<CurseForgeModpack>();

            try
            {
                using var responseMessage = await HttpWrapper.HttpGetAsync(builder.ToString(), Headers);
                responseMessage.EnsureSuccessStatusCode();

                var entity = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
                ((JArray)entity["data"]).ToList().ForEach(x => result.Add(ParseCurseForgeModpack((JObject)x)));

                result.Sort((a, b) => a.GamePopularityRank.CompareTo(b.GamePopularityRank));

                return result;
            }
            catch { }

            return null;
        }
        //&sortField=Featured&sortOrder=desc&classId=6
        public async ValueTask<List<CurseForgeModpack>> GetFeaturedModpacksAsync()
        {
            List<CurseForgeModpack> result = new List<CurseForgeModpack>();
            try
            {
                using HttpResponseMessage responseMessage = await HttpWrapper.HttpGetAsync("https://api.curseforge.com/v1/mods/search?gameId=432&modLoaderType=0&sortField=Featured&sortOrder=desc&classId=6", Headers, HttpCompletionOption.ResponseContentRead);
                responseMessage.EnsureSuccessStatusCode();
                var entity = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
                ((JArray)entity["data"]).ToList().ForEach(x => result.Add(ParseCurseForgeModpack((JObject)x)));

                result.Sort((a, b) => a.GamePopularityRank.CompareTo(b.GamePopularityRank));

                return result;
            }
            catch
            {
            }
            return null;
        }

        public async ValueTask<List<CurseForgeModpackCategory>> GetCategories()
        {
            try
            {
                using var responseMessage = await HttpWrapper.HttpGetAsync($"https://api.curseforge.com/v1/categories?gameId=432", Headers);
                responseMessage.EnsureSuccessStatusCode();

                var entity = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());

                return ((JArray)entity["data"]).Select(x => x.ToObject<CurseForgeModpackCategory>()).ToList();
            }
            catch { }

            return null;
        }

        public async ValueTask<string> GetModDescriptionHtmlAsync(int modId)
        {
            string url = $"{API}/{modId}/description";
            try
            {
                using HttpResponseMessage responseMessage = await HttpWrapper.HttpGetAsync(url, Headers);
                responseMessage.EnsureSuccessStatusCode();
                return (await responseMessage.Content.ReadAsStringAsync()).ToJsonEntity<DataModel<string>>().Data;
            }
            catch
            {
            }
            return null;
        }

        protected CurseForgeModpack ParseCurseForgeModpack(JObject entity)
        {
            var modpack = entity.ToObject<CurseForgeModpack>();

            if (entity.ContainsKey("logo") && entity["logo"].Type != JTokenType.Null)
                modpack.IconUrl = (string)entity["logo"]["url"];

            modpack.LatestFilesIndexes.ForEach(x =>
            {
                x.DownloadUrl = $"https://edge.forgecdn.net/files/{x.FileId.ToString().Insert(4, "/")}/{x.FileName}";

                if (!modpack.Files.ContainsKey(x.SupportedVersion))
                    modpack.Files.Add(x.SupportedVersion, new());

                modpack.Files[x.SupportedVersion].Add(x);
            });

            modpack.Links.Where(x => string.IsNullOrEmpty(x.Value)).Select(x => x.Key).ToList().ForEach(x => modpack.Links.Remove(x));
            modpack.Files = modpack.Files.OrderByDescending(x => (int)(float.Parse(x.Key.Substring(2)) * 100)).ToDictionary(x => x.Key, x => x.Value);
            modpack.SupportedVersions = modpack.Files.Keys.ToArray();

            return modpack;
        }

        [Obsolete]
        protected HttpRequestMessage SetHttpHeaders(HttpMethod method, string url)
        {
            return new HttpRequestMessage(method, url)
            {
                Headers = { { "x-api-key", Key } }
            };
        }
    }

    partial class CurseForgeToolkit
    {
        private const string API = "https://api.curseforge.com/v1/mods";

        private readonly string Key = string.Empty;

        private HttpClient hc = new HttpClient();

        private Dictionary<string, string> Headers => new Dictionary<string, string> { { "x-api-key", Key } };

        public CurseForgeToolkit(string accesskey)
        {
            Key = accesskey;
        }
    }
}