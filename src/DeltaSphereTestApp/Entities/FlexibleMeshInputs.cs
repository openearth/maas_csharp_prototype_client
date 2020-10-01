using Newtonsoft.Json;

namespace DeltaSphereTestApp.Entities
{
    public class FlexibleMeshInputs
    {
        [JsonProperty("type")]
        public string Type
        {
            get { return "FlexibleMeshInputs"; }
        }

        [JsonProperty("s3path")]
        public string S3Path { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }


    }
}