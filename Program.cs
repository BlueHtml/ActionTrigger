using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ActionTrigger
{
    class Program
    {
        static Conf _conf;
        static HttpClient _scClient;

        static async Task Main()
        {
            _conf = Deserialize<Conf>(GetEnvValue("CONF"));
            if (!string.IsNullOrWhiteSpace(_conf.ScKey))
            {
                _scClient = new HttpClient();
            }
            string owner = GetEnvValue("GITHUB_ACTOR");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", owner);
            client.DefaultRequestHeaders.Add("Authorization", $"token {_conf.Token}");
            Console.WriteLine("Action触发器开始运行...");

            foreach (string repo in _conf.Repos)
            {
                string badInfo = null;
                try
                {
                    var httpResponseMessage = await client.PostAsync($"https://api.github.com/repos/{owner}/{repo}/dispatches", new StringContent($@"{{""event_type"":""{_conf.EventType}""}}", Encoding.UTF8, "application/json"));
                    if (httpResponseMessage.StatusCode != HttpStatusCode.NoContent)
                    {//请求失败
                        badInfo = $"请求失败. code: {httpResponseMessage.StatusCode}, msg: {await httpResponseMessage.Content.ReadAsStringAsync()}";
                    }
                }
                catch (Exception ex)
                {
                    badInfo = $"ex: {ex.Message}";
                }
                await Notify($"{repo}...{badInfo ?? "ok"}", badInfo != null);
            }
            Console.WriteLine("Action运行完毕");
        }

        static async Task Notify(string msg, bool isFailed = false)
        {
            Console.WriteLine(msg);
            if (_conf.ScType == "Always" || (isFailed && _conf.ScType == "Failed"))
            {
                await _scClient?.GetAsync($"https://sc.ftqq.com/{_conf.ScKey}.send?text={msg}");
            }
        }

        static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);

        static string GetEnvValue(string key) => Environment.GetEnvironmentVariable(key);

        #region Conf

        class Conf
        {
            public string Token { get; set; }
            public string[] Repos { get; set; }
            public string EventType { get; set; }
            public string ScKey { get; set; }
            public string ScType { get; set; }
        }

        #endregion
    }
}
