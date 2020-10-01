namespace DeltaSphereTestApp.Views
{
    public interface IRemotePath
    {
        string Name { get; set; }

        string FullPath { get; }

        RemoteFolder ParentFolder { get; }
    }
}