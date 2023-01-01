using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MinecraftLaunch.Modules.Enum;
using MinecraftLaunch.Modules.Interface;
using MinecraftLaunch.Modules.Models.Install;
using MinecraftLaunch.Modules.Toolkits;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Model;

namespace MinecraftLaunch.Modules.Installer
{
    public partial class JavaInstaller : InstallerBase
    {
        public static (string, string, PcType) ActiveJdk;

        public static Dictionary<string, KeyValuePair<string, string>[]> OpenJdkDownloadSources
        {
            get
            {
                Dictionary<string, KeyValuePair<string, string>[]> dictionary = new Dictionary<string, KeyValuePair<string, string>[]>();
                dictionary.Add("OpenJDK 8", new KeyValuePair<string, string>[1]
                {
                new KeyValuePair<string, string>("jdk.java.net", "https://download.java.net/openjdk/jdk8u42/ri/openjdk-8u42-b03-windows-i586-14_jul_2022.zip")
                });
                dictionary.Add("OpenJDK 11", new KeyValuePair<string, string>[2]
                {
                new KeyValuePair<string, string>("jdk.java.net", "https://download.java.net/openjdk/jdk11/ri/openjdk-11+28_windows-x64_bin.zip"),
                new KeyValuePair<string, string>("Microsoft", "https://aka.ms/download-jdk/microsoft-jdk-11.0.16-windows-x64.zip")
                });
                dictionary.Add("OpenJDK 17", new KeyValuePair<string, string>[2]
                {
                new KeyValuePair<string, string>("jdk.java.net", "https://download.java.net/openjdk/jdk17/ri/openjdk-17+35_windows-x64_bin.zip"),
                new KeyValuePair<string, string>("Microsoft", "https://aka.ms/download-jdk/microsoft-jdk-17.0.4-windows-x64.zip")
                });
                dictionary.Add("OpenJDK 18", new KeyValuePair<string, string>[1]
                {
                new KeyValuePair<string, string>("jdk.java.net", "https://download.java.net/openjdk/jdk18/ri/openjdk-18+36_windows-x64_bin.zip")
                });
                return dictionary;
            }
        }

        public static string StorageFolder => ActiveJdk.Item2;

        public async ValueTask<JavaInstallerResponse> InstallAsync(Action<(float, string)> action)
        {
            Action<(float, string)> action2 = action;
            IProgress<(float, string)> progress = new Progress<(float, string)>();
            ((Progress<(float, string)>)progress).ProgressChanged += ProgressChanged;
            try
            {
                string item = ActiveJdk.Item1;
                _ = ActiveJdk;
                progress.Report((0.1f, "开始下载 Jdk"));
                HttpDownloadResponse res = await HttpWrapper.HttpDownloadAsync(item, Path.GetTempPath(), (Action<float, string>)delegate (float e, string a)
                {
                    progress.Report((0.1f + e * 0.8f, "下载中：" + a));
                }, (string)null);
                if (res.HttpStatusCode != HttpStatusCode.OK)
                {
                    return new JavaInstallerResponse
                    {
                        Success = false,
                        Exception = null,
                        JavaInfo = null
                    };
                }
                progress.Report((0.8f, "开始解压 Jdk"));
                await Task.Delay(1000);
                ZipFile.ExtractToDirectory(res.FileInfo.FullName, StorageFolder);
                progress.Report((0.95f, "开始删除 下载缓存"));
                res.FileInfo.Delete();
                progress.Report((1f, "安装完成"));
                return new JavaInstallerResponse
                {
                    Success = true,
                    Exception = null,
                    JavaInfo = JavaToolkit.GetJavaInfo(Path.Combine(Directory.GetDirectories(StorageFolder)[0], "bin"))
                };
            }
            catch (Exception ex)
            {
                return new JavaInstallerResponse
                {
                    Success = false,
                    Exception = ex,
                    JavaInfo = null
                };
            }
            void ProgressChanged(object _, (float, string) e)
            {
                action2(e);
            }
        }
    }

    partial class JavaInstaller
    {
        public JavaInstaller() { }

        public JavaInstaller(JdkDownloadSource jdkDownloadSource, OpenJdkType openJdkType, string SavePath, PcType pcType = PcType.Windows)
        {
            if (jdkDownloadSource != 0 && jdkDownloadSource != JdkDownloadSource.Microsoft)
            {
                throw new ArgumentException("选择了错误的下载源");
            }
            if (openJdkType != 0 && openJdkType != OpenJdkType.OpenJdk11 && openJdkType != OpenJdkType.OpenJdk17 && openJdkType != OpenJdkType.OpenJdk18)
            {
                throw new ArgumentException("选择了错误的Jdk版本");
            }
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
            ActiveJdk = (openJdkType.ToDownloadLink(jdkDownloadSource), openJdkType.ToFullJavaPath(SavePath), pcType);
        }
    }
}