using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using NUnit.Framework;
using TeamCitySharp.DomainEntities;

namespace TeamCitySharp.IntegrationTests
{
  [TestFixture]
  public class when_interacting_to_get_agent_details
  {
    private ITeamCityClient m_client;
    private readonly string m_server;
    private readonly bool m_useSsl;
    private readonly string m_username;
    private readonly string m_password;


    public when_interacting_to_get_agent_details()
    {
      m_server = Configuration.GetAppSetting("Server");
      bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
      m_username = Configuration.GetAppSetting("Username");
      m_password = Configuration.GetAppSetting("Password");
    }
    [SetUp]
    public void SetUp()
    {
      m_client = new TeamCityClient(m_server,m_useSsl, Configuration.GetWireMockClient);
      m_client.Connect(m_username,m_password);   
    }

    [Test]
    public void it_throws_exception_when_no_host()
    {
      Assert.Throws<ArgumentNullException>(() => new TeamCityClient(null));
    }

    [Test]
    public void it_throws_exception_when_no_client_connection_made()
    {
      var client = new TeamCityClient(m_server, m_useSsl);

      Assert.Throws<ArgumentException>(() => client.Agents.All());
    }

    [Test]
    public void it_returns_all_agents()
    {
      List<Agent> agents = m_client.Agents.All();

      Assert.That(agents.Any(), "No agents were found");
    }

    [Test]
    public void it_returns_last_build_status_for_agent()
    {
      var agent = m_client.Agents.All().FirstOrDefault();
      Assert.That(agent, Is.Not.Null, "No agents were found");

      Build lastBuild = m_client.Builds.LastBuildByAgent(agent.Name);

      Assert.That(lastBuild, Is.Not.Null,
        "No build information found for the last build on the specified agent");
    }
  }
}
