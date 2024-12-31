using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Connection;

namespace TeamCitySharp.IntegrationTests
{
  [TestFixture]
  public class when_interacting_to_get_server_info
  {
    private ITeamCityClient m_client;
    private readonly string m_server;
    private readonly bool m_useSsl;
    private readonly string m_username;
    private readonly string m_password;


    public when_interacting_to_get_server_info()
    {
      m_server = Configuration.GetAppSetting("Server");
      bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
      m_username = Configuration.GetAppSetting("Username");
      m_password = Configuration.GetAppSetting("Password");
    }

    [SetUp]
    public void SetUp()
    {
      m_client = new TeamCityClient(m_server, m_useSsl);
      m_client.Connect(m_username, m_password);
    }

    [Test]
    public void it_throws_exception_when_no_url_passed()
    {
      Assert.Throws<ArgumentNullException>(() => new TeamCityClient(null));
    }

    [Test]
    public void it_throws_exception_when_no_connection_formed()
    {
      var client = new TeamCityClient(m_server, m_useSsl);

      Assert.Throws<ArgumentException>(() => client.ServerInformation.AllPlugins());

      //Assert: Exception
    }

    [Test]
    public void it_returns_server_info()
    {
      Server serverInfo = m_client.ServerInformation.ServerInfo();

      Assert.That(serverInfo, Is.Not.Null, "The server is not returning any information");
    }

    [Test, Ignore("Current user doesn't have the right to list plugins in tested instance.")]
    public void it_returns_all_server_plugins()
    {
      List<Plugin> plugins = m_client.ServerInformation.AllPlugins();

      Assert.That(plugins, Is.Not.Null, "Server is not returning a plugin list");
    }

    [Test]
    public void it_raises_exception_all_server_plugins_unauthorized_user()
    {
      try
      {
        m_client.ServerInformation.AllPlugins();
      }
      catch (HttpException e)
      {
        Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
      }
    }

    [Test]
    public void it_get_all_agents_version_7_0()
    {
      const string version = "7.0";
      var client = new TeamCityClient(m_server, m_useSsl);
      client.Connect(m_username, m_password);
      client.UseVersion(version);
      var agents = client.Agents.All();
      Assert.That(agents != null, "The server is not returning any information");
      foreach (var agent in agents)
      {
        Assert.That(version.Contains(agent.Href), Is.True);
        Assert.That(agent.TypeId, Is.Null);
        Assert.That(agent.WebUrl, Is.Null);
      }
    }
    
    [Test]
    public void it_get_all_agents_version_8_0()
    {
      const string version = "8.0";
      var client = new TeamCityClient(m_server, m_useSsl);
      client.Connect(m_username, m_password);
      client.UseVersion(version);
      var agents = client.Agents.All();
      Assert.That(agents != null, "The server is not returning any information");
      foreach (var agent in agents)
      {
        Assert.That(version.Contains(agent.Href), Is.True);
        Assert.That(agent.TypeId, Is.Not.Null);
        Assert.That(agent.WebUrl, Is.Null);
      }
    }
    [Test]
    public void it_get_all_agents_version_9_0()
    {
      const string version = "9.0";
      var client = new TeamCityClient(m_server, m_useSsl);
      client.Connect(m_username, m_password);
      client.UseVersion(version);
      var agents = client.Agents.All();
      Assert.That(agents != null, "The server is not returning any information");
      foreach (var agent in agents)
      {
        Assert.That(version.Contains(agent.Href), Is.True);
        Assert.That(agent.TypeId, Is.Not.Null);
        Assert.That(agent.WebUrl, Is.Null);
      }
    }
    [Test]
    public void it_get_all_agents_version_10_0()
    {
      const string version = "10.0";
      var client = new TeamCityClient(m_server, m_useSsl);
      client.Connect(m_username, m_password);
      client.UseVersion(version);
      var agents = client.Agents.All();
      Assert.That(agents, Is.Not.Null, "The server is not returning any information");
      foreach (var agent in agents)
      {
        Assert.That(agent.Href.Contains(version), Is.True);
        Assert.That(agent.TypeId, Is.Not.Null);
        Assert.That(agent.WebUrl, Is.Not.Null);
      }
    }
  }
}
