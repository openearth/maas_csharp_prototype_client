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
        
        private readonly string sessionId;
        internal readonly Uri BaseUri;

        private string errorMessage;

        public DeltaSphereCaasApi(string serverAddress, string sessionId)
        {
            this.sessionId = sessionId;
            BaseUri = new Uri(serverAddress);
        }

        internal async Task<string> GetLoginName()
        {
            var loginNames = await GetStringFromAsync(new Uri(BaseUri, "me"));
            var userData = JsonConvert.DeserializeObject<User>(loginNames);
            return userData.Email;
        }

        internal async Task<IList<Process>> GetProcesses()
        {
            var modelJson = await GetStringFromAsync(new Uri(BaseUri, ProcessesUriId));
            return JsonConvert.DeserializeObject<List<Process>>(modelJson);
        }

        internal async Task<IList<Job>> GetJobsFor(Process process)
        {
            if (process == null)
            {
                errorMessage = "Could not retrieve jobs because no process has been selected";
                return null;
            }
            var jobsJson = await GetStringFromAsync(new Uri(BaseUri, $"{ProcessesUriId}/{process.Id}/{JobsUriId}"));
            return JsonConvert.DeserializeObject<List<Job>>(jobsJson);
        }

        internal async Task DeleteJob(string processId, string jobId)
        {
            var url = new Uri(BaseUri, $"{ProcessesUriId}/{processId}/{JobsUriId}/{jobId}");
            await DoWithClientAsync(url, async client =>
            {
                await Task.Run(() => client.UploadValues(url, "DELETE", new NameValueCollection()));
            });
        }

        internal async Task<IList<string>> GetFiles()
        {
            var filesJson = await GetStringFromAsync(new Uri(BaseUri, "files"));
            return JsonConvert.DeserializeObject<List<string>>(filesJson);
        }

        internal async Task UploadFiles(IEnumerable<string> filePaths, string processId, string jobName)
        {
            foreach (var filePath in filePaths)
            {
                var fileUploadUri = new Uri(BaseUri, $"files/{processId}/{jobName}/{Path.GetFileName(filePath)}");

                FileUploadObject generatedRemotePath = null;
                await DoWithClientAsync(fileUploadUri, async client =>
                {
                    var cloudPath = await client.UploadStringTaskAsync(fileUploadUri, "");
                    generatedRemotePath = JsonConvert.DeserializeObject<FileUploadObject>(cloudPath);
                });

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
            }
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