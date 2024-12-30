using System.Configuration;
using NUnit.Framework;
using System.Linq;

namespace TeamCitySharp.IntegrationTests
{
  [TestFixture]
  public class when_interacting_to_get_build_statistics
  {
    private ITeamCityClient m_client;
    private readonly string m_server;
    private readonly bool m_useSsl;
    private readonly string m_username;
    private readonly string m_password;
    private readonly string m_goodProjectId;


    public when_interacting_to_get_build_statistics()
    {
      m_server = Configuration.GetAppSetting("Server");
      bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
      m_username = Configuration.GetAppSetting("Username");
      m_password = Configuration.GetAppSetting("Password");
      m_goodProjectId = Configuration.GetAppSetting("GoodProjectId");
    }
    [SetUp]
    public void SetUp()
    {
      m_client = new TeamCityClient(m_server, m_useSsl);
      m_client.Connect(m_username, m_password);
    }

    [Test]
    public void it_returns_no_of_tests_from_last_successful_build()
    {
      var proj = m_client.Projects.ById(m_goodProjectId);
      var build = m_client.Builds.LastSuccessfulBuildByBuildConfigId(proj.BuildTypes.BuildType[0].Id);
      var stats = m_client.Statistics.GetByBuildId(build.Id);

      Assert.That(stats.Property.Any(property => property.Name.Equals("PassedTestCount")));
    }
  }
}
