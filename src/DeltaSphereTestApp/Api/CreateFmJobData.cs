using DeltaSphereTestApp.Entities;

namespace DeltaSphereTestApp.Api
{
    public class CreateFmJobData
    {
        public string Title { get; set; }

        public string Description { get; set; }
        
        public string ProcessId { get; set; }

        public string RelativeS3Path { get; set; }

        public FlexibleMeshInputs CreateFlexibleMeshInputs()
        {
            return new FlexibleMeshInputs
            {
                Title = Title,
                Description = Description,
                S3Path = RelativeS3Path
            };
        }
    }
}