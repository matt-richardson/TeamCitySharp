using System;
using System.Net;
using NUnit.Framework;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Fields;
using TeamCitySharp.Connection;

namespace TeamCitySharp.IntegrationTests
{
    [TestFixture]
    public class when_interacting_to_get_vcs_details
    {
        private ITeamCityClient m_client;
        private readonly string m_server;
        private readonly bool m_useSsl;
        private readonly string m_username;
        private readonly string m_password;
        private readonly string m_goodProjectId;

        public when_interacting_to_get_vcs_details()
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
            m_client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
            m_client.Connect(m_username, m_password);
        }

        [Test]
        public void it_returns_exception_when_no_host_specified()
        {
            Assert.Throws<ArgumentNullException>(() => new TeamCityClient(null));
        }


        [Test]
        public void it_returns_exception_when_no_connection_formed()
        {
            var client = new TeamCityClient(m_server, m_useSsl);
            Assert.Throws<ArgumentException>(() => client.VcsRoots.All());
        }

        [Test]
        public void it_returns_all_vcs_roots()
        {
            List<VcsRoot> vcsRoots = m_client.VcsRoots.All();

            Assert.That(vcsRoots.Any(), "No VCS Roots were found for the installation");
        }

        [Test]
        public void it_returns_vcs_details_when_passing_vcs_root_id()
        {
            string vcsRootId = Configuration.GetAppSetting("IdOfVcsRoot");
            VcsRoot rootDetails = m_client.VcsRoots.ById(vcsRootId);

            Assert.That(rootDetails, Is.Not.Null, "Cannot find the specific VCSRoot");
        }

        [Test]
        public void it_returns_correct_next_builds_with_filter()
        {
            VcsRootField vcsRootField = VcsRootField.WithFields(id: true, href: true, lastChecked: true, name: true,
                status: true, vcsName: true, version: true);
            VcsRootsField vcsRootsField = VcsRootsField.WithFields(vcsRootField);
            var vcsRoots = m_client.VcsRoots.GetFields(vcsRootsField.ToString()).All();

            Assert.That(vcsRoots, Is.Not.Null);
        }

        [Test]
        public void it_create_new_vsc()
        {
            VcsRoot createdVcsRoot = null;
            try
            {
                var project = m_client.Projects.ById(m_goodProjectId);

                VcsRoot vcsroot = new VcsRoot();
                vcsroot.Id = project.Id + "_vcsroot1_01";
                vcsroot.Name = project.Name + "_vcsroot1";
                vcsroot.VcsName = "jetbrains.git";
                vcsroot.Project = new Project();
                vcsroot.Project.Id = project.Id;

                Properties properties = new Properties();

                properties.Add("agentCleanFilesPolicy", "IGNORED_ONLY");
                vcsroot.Properties = properties;

                createdVcsRoot = m_client.VcsRoots.CreateVcsRoot(vcsroot, project.Id);

                m_client.VcsRoots.SetVcsRootValue(createdVcsRoot, VcsRootValue.Name, "TestChangeName");

                m_client.VcsRoots.SetConfigurationProperties(createdVcsRoot, "agentCleanFilesPolicy", "ALL_UNTRACKED");
                m_client.VcsRoots.SetConfigurationProperties(createdVcsRoot, "tt", "tt2");
            }
            finally
            {
                if (createdVcsRoot != null)
                {
                    m_client.VcsRoots.DeleteProperties(createdVcsRoot, "tt");
                    m_client.VcsRoots.DeleteVcsRoot(createdVcsRoot);
                }
            }
        }

        [Test]
        public void it_throws_exception_create_new_vsc_forbidden()
        {
            var client = new TeamCityClient(m_server, m_useSsl);
            client.Connect(Configuration.GetAppSetting("NonAdminUser"), m_password);
            var project = client.Projects.ById(m_goodProjectId);

            VcsRoot vcsroot = new VcsRoot();
            vcsroot.Id = project.Id + "_vcsroot1_01";
            vcsroot.Name = project.Name + "_vcsroot1";
            vcsroot.VcsName = "jetbrains.git";
            vcsroot.Project = new Project();
            vcsroot.Project.Id = project.Id;

            Properties properties = new Properties();

            properties.Add("agentCleanFilesPolicy", "IGNORED_ONLY");
            vcsroot.Properties = properties;

            var e = Assert.Throws<HttpException>(() => client.VcsRoots.CreateVcsRoot(vcsroot, project.Id));
            Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }
}
