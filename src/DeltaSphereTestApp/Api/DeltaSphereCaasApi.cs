using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DeltaSphereTestApp.Entities;
using DeltaSphereTestApp.Helpers;
using Newtonsoft.Json;

namespace DeltaSphereTestApp.Api
{
    public class DeltaSphereCaasApi
    {
        private const string ProcessesUriId = "processes";
        private const string JobsUriId = "jobs";
        private const string FilesUriId = "files";

        private readonly string sessionId;
        private readonly Uri baseUri;

        private string errorMessage;

        public DeltaSphereCaasApi(string serverAddress, string sessionId)
        {
            this.sessionId = sessionId;
            baseUri = new Uri(serverAddress);
        }

        internal async Task<string> GetLoginName()
        {
            var loginNames = await GetStringFromAsync(new Uri(baseUri, "me"));
            var userData = JsonConvert.DeserializeObject<User>(loginNames);
            return userData?.Email;
        }

        internal async Task<IList<Process>> GetProcesses()
        {
            var modelJson = await GetStringFromAsync(new Uri(baseUri, ProcessesUriId));
            return JsonConvert.DeserializeObject<List<Process>>(modelJson);
        }

        internal async Task<IList<Job>> GetJobsFor(Process process)
        {
            if (process == null)
            {
                errorMessage = "Could not retrieve jobs because no process has been selected";
                return null;
            }
            var jobsJson = await GetStringFromAsync(new Uri(baseUri, $"{ProcessesUriId}/{process.Id}/{JobsUriId}"));
            return JsonConvert.DeserializeObject<List<Job>>(jobsJson);
        }

        internal async Task CreateJob(CreateFmJobData fmJobData)
        {
            var uri = new Uri(baseUri, $"{ProcessesUriId}/{fmJobData.ProcessId}/{JobsUriId}");
            await PostDataTo(uri, JsonConvert.SerializeObject(fmJobData.CreateFlexibleMeshInputs()));
        }

        internal async Task DeleteJob(string processId, string jobId)
        {
            var url = new Uri(baseUri, $"{ProcessesUriId}/{processId}/{JobsUriId}/{jobId}");
            await DoWithClientAsync(url, async client =>
            {
                await Task.Run(() => client.UploadValues(url, "DELETE", new NameValueCollection()));
            });
        }

        internal async Task<IList<string>> GetFiles()
        {
            var filesJson = await GetStringFromAsync(new Uri(baseUri, FilesUriId));
            return JsonConvert.DeserializeObject<List<string>>(filesJson);
        }

        internal async Task<IList<FileUploadObject>> UploadFiles(IList<string> filePaths, string processId, string jobName, IProgress<string> progress = null)
        {
            var remotePaths = new List<FileUploadObject>();

            foreach (var filePath in filePaths)
            {
                var fileUploadUri = new Uri(baseUri, $"files/{processId}/{jobName}/{Path.GetFileName(filePath)}");

                FileUploadObject generatedRemotePath = null;
                await DoWithClientAsync(fileUploadUri, async client =>
                {
                    var cloudPath = await client.UploadStringTaskAsync(fileUploadUri, "");
                    generatedRemotePath = JsonConvert.DeserializeObject<FileUploadObject>(cloudPath);
                });

                remotePaths.Add(generatedRemotePath);
                progress?.Report($"Uploading {Path.GetFileName(filePath)} to {generatedRemotePath.RemotePath}");

                await Task.Run(() =>
                {
                    using (var stream = File.Open(filePath, FileMode.Open))
                    {
                        var files = new[]
                        {
                            new UploadFile
                            {
                                Name = "file",
                                Filename = Path.GetFileName(filePath),
                                ContentType = "text/plain",
                                Stream = stream
                            }
                        };

                        var values = new NameValueCollection();
                        foreach (var field in generatedRemotePath.Fields)
                        {
                            values.Add(field.Key, field.Value);
                        }

                        FileHelper.UploadFiles(generatedRemotePath.Url, files, values);
                    }
                });
            }

            return remotePaths;
        }

        internal async Task<bool> DeleteFile(string s3FilePath)
        {
            var escapedS3Path = EscapeS3Path(s3FilePath);

            var url = new Uri(baseUri, $"{FilesUriId}/{escapedS3Path}");
            var deleteSuccessful = false;
            await DoWithClientAsync(url, async client =>
            {
                await Task.Run(() => client.UploadValues(url, "DELETE", new NameValueCollection()));
                deleteSuccessful = true;
            });

            return deleteSuccessful;
        }

        internal async Task DownloadFile(string s3FilePath, string destinationPath)
        {
            var escapedS3Path = EscapeS3Path(s3FilePath);

            var url = new Uri(baseUri, $"{FilesUriId}/{escapedS3Path}");
            await DoWithClientAsync(url, async client =>
            {
                await client.DownloadFileTaskAsync(url, destinationPath);
            });
        }

        private static string EscapeS3Path(string s3FilePath)
        {
            return s3FilePath.Replace(Path.DirectorySeparatorChar.ToString(), "%2F");
        }

        private async Task PostDataTo(Uri uri, byte[] byteData)
        {
            await DoWithClientAsync(uri, async client =>
            {
                await client.UploadDataTaskAsync(uri, byteData);
            });
        }

        private async Task PostDataTo(Uri uri, string body)
        {
            await DoWithClientAsync(uri, async client =>
            {
                client.Headers.Add(HttpRequestHeader.Accept, "application/json");
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                await client.UploadStringTaskAsync(uri, body);
            });
        }

        private async Task<string> GetStringFromAsync(Uri uri)
        {
            var data = "";
            await DoWithClientAsync(uri, async client =>
            {
                data = await client.DownloadStringTaskAsync(uri);
            });

            return data;
        }

        private async Task DoWithClientAsync(Uri uri, Func<CookieAwareWebClient, Task> action)
        {
            try
            {
                // create cookie container with the session id
                var cookieContainer = new CookieContainer();
                cookieContainer.SetCookies(uri, sessionId);

                // start web client and do the action
                using (var client = new CookieAwareWebClient { CookieContainer = cookieContainer })
                {
                    await action(client);
                }
            }
            catch (WebException e)
            {
                errorMessage = e.Message;
            }
        }
    }
}