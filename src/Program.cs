using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VcvPluginDownload
{
    public class VcvPackageJsonDownloadPlatform
    {
        public string download { get; set; }
        public string sha256 { get; set; }
    }

    public class VcvPackageJsonDownload
    {
        public VcvPackageJsonDownloadPlatform win { get; set; }
        public VcvPackageJsonDownloadPlatform mac { get; set; }
        public VcvPackageJsonDownloadPlatform lin { get; set; }
    }

    public class VcvPackageJson
    {
        public string name { get; set; }
        public string author { get; set; }
        public string homepage { get; set; }
        public string source { get; set; }
        public VcvPackageJsonDownload downloads { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("VcvPluginDownload {destPath}");
                Console.WriteLine("");
                Console.WriteLine("Example:");
                Console.WriteLine("VcvPluginDownload c:\\test");
                return;
            }

            string destPath = args[0];
            string jsonDestPath = $"{destPath}\\JSON";
            string packagesPath = $"{destPath}\\Packages";

            Directory.CreateDirectory(jsonDestPath);
            Directory.CreateDirectory(packagesPath);
            IEnumerable<string> jsonFiles;

            try
            {
                jsonFiles = GetFileListFromGithub("VCVRack", "community", "plugins").Result;
                Console.WriteLine("Got list of VCV Rack plugins files from https://github.com/VCVRack/community/tree/master/plugins");
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't get list of JSON files from GitHub");
                Console.WriteLine(e);
                throw;
            }

            bool newPackageSeen = false;
            foreach (var jsonFile in jsonFiles)
            {
                if (jsonFile == "Fundamental.json" ||
                    jsonFile == "VCV-Console.json" ||
                    jsonFile == "VCV-PulseMatrix.json" || 
                    jsonFile == "UnfilteredVolume1.json"
                    )
                {
                    // Skip these 
                    try
                    {
                        string localJsonFile = $"{jsonDestPath}\\{jsonFile}";
                        File.Delete(localJsonFile);
                    }
                    catch (Exception )
                    {
                    }
                    continue;
                }

                bool localJsonFilePreviouslySeen = false;
                try
                {
                    string localJsonFile = $"{jsonDestPath}\\{jsonFile}";

                    if (System.IO.File.Exists(localJsonFile))
                    {
                        localJsonFilePreviouslySeen = true;
                    }


                    try
                        {
                        DownloadFile($"https://raw.githubusercontent.com/VCVRack/community/master/plugins/{jsonFile}", localJsonFile);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Couldn't download JSON from https://raw.githubusercontent.com/VCVRack/community/master/plugins/{jsonFile}");
                        Console.WriteLine(e);
                        throw;
                    }

                    try
                    {
                        using (StreamReader file = File.OpenText(localJsonFile))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            VcvPackageJson jsonPackage = (VcvPackageJson) serializer.Deserialize(file, typeof(VcvPackageJson));
                            string zipName = GetFileNameFromUrl(jsonPackage.downloads.win.download);
                            string zipFile = $"{packagesPath}\\{zipName}";

                            if (!System.IO.File.Exists(zipFile))
                            {
                                Console.WriteLine($"Downloading {zipFile}");
                                DownloadFile(jsonPackage.downloads.win.download, zipFile);
                                newPackageSeen = true;
                            }

                        }
                        File.Delete(localJsonFile);
                    }
                    catch (Exception)
                    {
                        if (!localJsonFilePreviouslySeen)
                        {
                            string homePage = GetHomePage(localJsonFile);
                            Console.WriteLine($"Couldn't download package from {jsonFile}. {homePage}");
                            newPackageSeen = true;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            if (!newPackageSeen)
            {
                Console.WriteLine($"No new or updated packages detected");
            }
        }

        static string GetHomePage(string localJsonFile)
        {
            try
            {
                using (StreamReader file = File.OpenText(localJsonFile))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    VcvPackageJson jsonPackage = (VcvPackageJson)serializer.Deserialize(file, typeof(VcvPackageJson));
                    if (string.IsNullOrEmpty(jsonPackage.homepage))
                    {
                        return jsonPackage.source;
                    }
                    return jsonPackage.homepage;
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        static string GetFileNameFromUrl(string url)
        {
            Uri uri;
            Uri SomeBaseUri = new Uri("http://canbeanything");
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(SomeBaseUri, url);

            return Path.GetFileName(uri.LocalPath);
        }

        static async Task<IEnumerable<string>> GetFileListFromGithub(string repoOwner, string repoName, string path)
        {
            using (var client = GetGithubHttpClient())
            {
                var resp = await client.GetAsync($"repos/{repoOwner}/{repoName}/contents/{path}");
                var bodyString = await resp.Content.ReadAsStringAsync();
                var bodyJson = JToken.Parse(bodyString);
                return bodyJson.SelectTokens("$.[*].name").Select(token => token.Value<string>());
            }
        }

        private static HttpClient GetGithubHttpClient()
        {
            return new HttpClient
            {
                BaseAddress = new Uri("https://api.github.com"),
                DefaultRequestHeaders =
                {
                    {"User-Agent", "VcvRackPluginDownloader"}
                }
            };
        }

        public static void DownloadFile(string sourceURL, string destinationPath)
        {
            long fileSize = 0;
            int bufferSize = 1024;
            bufferSize *= 1000;
            long existLen = 0;

            System.IO.FileStream saveFileStream;
            if (System.IO.File.Exists(destinationPath))
            {
                return;
            }

            saveFileStream = new System.IO.FileStream(destinationPath,
                System.IO.FileMode.Create,
                System.IO.FileAccess.Write,
                System.IO.FileShare.ReadWrite);

            System.Net.HttpWebRequest httpReq;
            System.Net.HttpWebResponse httpRes;
            httpReq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(sourceURL);
            httpReq.UserAgent = "VcvRackPluginDownloader";
            httpReq.AddRange((int)existLen);
            System.IO.Stream resStream;
            httpRes = (System.Net.HttpWebResponse)httpReq.GetResponse();
            resStream = httpRes.GetResponseStream();

            fileSize = httpRes.ContentLength;

            int byteSize;
            byte[] downBuffer = new byte[bufferSize];

            while ((byteSize = resStream.Read(downBuffer, 0, downBuffer.Length)) > 0)
            {
                saveFileStream.Write(downBuffer, 0, byteSize);
            }
            saveFileStream.Flush();
            saveFileStream.Close();
        }
    }
}
