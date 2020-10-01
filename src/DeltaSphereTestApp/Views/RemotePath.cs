using System.IO;
using System.Runtime.InteropServices;

namespace DeltaSphereTestApp.Views
{
    public class RemotePath : IRemotePath
    {
        public const string RootName = "root";

        public string Name { get; set; }

        public virtual string FullPath
        {
            get
            {
                var parent = ParentFolder;
                var path = Name;
                
                while (parent != null && parent.Name != RootName)
                {
                    path = Path.Combine(parent.Name, path);
                    parent = parent.ParentFolder;
                }

                return path;
            }
        }

        public RemoteFolder ParentFolder { get; set; }
    }
}