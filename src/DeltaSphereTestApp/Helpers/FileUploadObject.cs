using System.Collections.Generic;

namespace DeltaSphereTestApp.Helpers
{
    public class FileUploadObject
    {
        public string Url { get; set; }

        public Dictionary<string, string> Fields { get; set; }

        public string RemotePath
        {
            get
            {
                return Fields.ContainsKey("key") 
                    ? Fields["key"]
                    : "";
            }
        }
    }
}