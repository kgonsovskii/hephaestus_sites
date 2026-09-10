namespace Sites.DataFtp;

public interface ISitesWebRootPathProvider
{
    /// <summary>
    /// FTP home: <c>hephaestus_sites_data/{profile}/</c>.
    /// Site files are under <c>wwwroot/{host}/</c> inside that folder.
    /// </summary>
    string WebRootFullPath { get; }
}
