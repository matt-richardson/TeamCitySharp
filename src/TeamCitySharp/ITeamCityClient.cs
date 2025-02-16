using TeamCitySharp.ActionTypes;

namespace TeamCitySharp
{
  public interface ITeamCityClient
  {
    void Connect(string userName, string password);
    void ConnectWithAccessToken(string token);
    void UseVersion(string version);
    void UseUserAgent(string userAgent);
    void ConnectAsGuest();
    void DisableCache();
    void EnableCache();
    bool Authenticate(bool throwExceptionOnHttpError = true);

    IBuilds Builds { get; }
    IBuildQueue BuildQueue { get; }
    IBuildConfigs BuildConfigs { get; }
    IProjects Projects { get; }
    IServerInformation ServerInformation { get; }
    IUsers Users { get; }
    IAgents Agents { get; }
    IVcsRoots VcsRoots { get; }
    IChanges Changes { get; }
    IBuildArtifacts Artifacts { get; }
    IStatistics Statistics { get; }
    IBuildInvestigations Investigations { get; }
    ITests Tests { get; }
  }
}
