using System;

namespace DeltaSphereTestApp.Entities
{
    public class Job
    {
        public string JobId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public string Progress { get; set; }

        public string Message { get; set; }

        public JobStatus JobStatus
        {
            get
            {
                if (Enum.TryParse(Status, true, out JobStatus status))
                {
                    return status;
                }

                throw new ArgumentException($"Could not parse {Status} to job status");
            }
        }
    }
}