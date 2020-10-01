using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;


namespace DeltaSphereTestApp.Helpers
{
    public static class FileHelper
    {
        public static string CreateTarGz(string tgzFilename, string directory)
        {
            var tempPath = Path.GetTempPath();
            var tarPath = Path.Combine(tempPath, $"{tgzFilename}.tar");

            using (var outStream = File.Create(tarPath))
            using (var gzoStream = new GZipOutputStream(outStream))
            using (var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
            {
                tarArchive.RootPath = Path.GetDirectoryName(directory);

                var tarEntry = TarEntry.CreateEntryFromFile(directory);
                tarEntry.Name = Path.GetFileName(directory);

                tarArchive.WriteEntry(tarEntry, true);
            }

            return tarPath;
        }

        /// <summary>
        /// Uploads the provided files to the selected address
        /// </summary>
        /// <param name="address">Address to upload to</param>
        /// <param name="files">Files to upload</param>
        /// <param name="values">Parameters to add to the upload</param>
        public static void UploadFiles(string address, IEnumerable<UploadFile> files, NameValueCollection values)
        {
            var request = WebRequest.Create(address);
            request.Method = "POST";
            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            boundary = "--" + boundary;

            using (var requestStream = request.GetRequestStream())
            {
                // Write the values
                foreach (string name in values.Keys)
                {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(s: $"Content-Disposition: form-data; name=\"{name}\"{Environment.NewLine}{Environment.NewLine}");
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(values[name] + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                // Write the files
                foreach (var file in files)
                {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes($"Content-Disposition: form-data; name=\"{file.Name}\"; filename=\"{file.Filename}\"{Environment.NewLine}");
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes($"Content-Type: {file.ContentType}{Environment.NewLine}{Environment.NewLine}");
                    requestStream.Write(buffer, 0, buffer.Length);
                    file.Stream.CopyTo(requestStream);
                    buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                var boundaryBuffer = Encoding.ASCII.GetBytes(boundary + "--");
                requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
            }

            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var stream = new MemoryStream())
            {
                responseStream?.CopyTo(stream);
                stream.ToArray();
            }
        }
    }
}