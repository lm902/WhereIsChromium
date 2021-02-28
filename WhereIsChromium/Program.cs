using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace WhereIsChromium
{
    class MainClass
    {
        private static readonly string Source = "https://raw.githubusercontent.com/vikyd/chromium-history-version-position/master/json/ver-pos-os/version-position-Win_x64.json";
        private static readonly string Storage = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\WhereIsChromium";
        private static readonly string RepositoryFile = Storage + "\\repository.json";

        public static void Main(string[] args)
        {
            Directory.CreateDirectory(Storage);
            Directory.CreateDirectory(Storage + "\\Versions");
            try
            {
                switch (args[0])
                {
                    case "update":
                        Update();
                        break;
                    case "get":
                        Get(args[1]);
                        break;
                    case "run":
                        Run(args[1], args);
                        break;
                    case "manage":
                        Manage();
                        break;
                    default:
                        Usage();
                        break;
                }
            } catch (IndexOutOfRangeException)
            {
                Usage();
            }
        }

        private static void Manage()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = Storage;
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }

        private static void Run(string v, string[] args)
        {
            var versions = Directory.EnumerateDirectories(Storage + "\\Versions");
            var query = from version in versions where Path.GetFileName(version).StartsWith(v) select version;
            if (query.Count() == 0)
            {
                if (Get(v))
                {
                    Run(v, args);
                    return;
                } else
                {
                    return;
                }
            }
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = query.First() + "\\chrome-win\\chrome.exe";
            startInfo.Arguments = string.Join(" ", args.Skip(2));
            startInfo.UseShellExecute = true;
            Console.WriteLine(startInfo.FileName + " " + startInfo.Arguments);
            Process.Start(startInfo);
        }

        private static bool Get(string v)
        {
            var query = from key in Repository.Keys where key.StartsWith(v) select key;
            if (query.Count() == 0)
            {
                Console.WriteLine($"未找到{v}开头的版本，可能需要更新版本库？");
                return false;
            }
            var target = query.First();
            var position = Repository[target];
            var url = $"https://www.googleapis.com/download/storage/v1/b/chromium-browser-snapshots/o/Win_x64%2F{position}%2Fchrome-win.zip?alt=media";
            try
            {
                Console.WriteLine($"正在下载: {url}");
                var zip = Path.GetTempFileName();
                new WebClient().DownloadFile(url, zip);
                Console.WriteLine($"正在解压缩: {zip}");
                ZipFile.ExtractToDirectory(zip, Storage + "\\Versions\\" + target);
                Console.WriteLine($"已安装到: {Storage + "\\Versions\\" + target}");
                File.Delete(zip);
                return true;
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        private static Dictionary<string, string> Repository {
            get
            {
                if (!File.Exists(RepositoryFile))
                {
                    Update();
                }
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(RepositoryFile));
            }
        }

        private static void Update()
        {
            Console.Write("正在更新版本信息…");
            new WebClient().DownloadFile(Source, RepositoryFile);
            Console.WriteLine("\r已更新版本信息。\t");
        }

        private static void Usage()
        {
            Console.WriteLine(@"
查找并运行历史版本的Chromium
使用方式：
wic update
wic get <版本>
wic run <版本>
wic manage
");
        }
    }
}
