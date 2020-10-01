using System.Collections.Generic;
using System.Linq;

namespace DeltaSphereTestApp.Views
{
    public class RemoteFolder : RemotePath
    {
        public IList<RemoteFolder> SubFolders { get; set; } = new List<RemoteFolder>();

        public IList<RemotePath> Files { get; set; } = new List<RemotePath>();

        public IEnumerable<object> Items
        {
            get { return SubFolders.Concat(Files.OfType<object>()); }
        }

        public IEnumerable<IRemotePath> GetAllSubPathsRecursive()
        {
            foreach (var subFolder in SubFolders)
            {
                foreach (var path in subFolder.GetAllSubPathsRecursive())
                {
                    yield return path;
                }
                
            }

            foreach (var file in Files)
            {
                yield return file;
            }
        }
    }
}