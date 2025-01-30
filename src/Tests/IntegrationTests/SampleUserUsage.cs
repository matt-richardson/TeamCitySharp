using System;
using System.Net;
using NUnit.Framework;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using TeamCitySharp.Connection;
using TeamCitySharp.DomainEntities;

namespace TeamCitySharp.IntegrationTests
{
    [TestFixture]
    public class when_interacting_to_get_user_information
    {
        private ITeamCityClient m_client;
        private readonly string m_server;
        private readonly bool m_useSsl;
        private readonly string m_username;
        private readonly string m_password;


        public when_interacting_to_get_user_information()
        {
            m_server = Configuration.GetAppSetting("Server");
            bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
            m_username = Configuration.GetAppSetting("Username");
            m_password = Configuration.GetAppSetting("Password");
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
        public void it_returns_exception_when_no_connection_made()
        {
            var client = new TeamCityClient(m_server, m_useSsl);

            Assert.Throws<ArgumentException>(() => client.Users.All());
        }

        [Test]
        [Ignore("Not working - not throwing exception as expected")]
        public void user_operation_throws_exception_for_unauthorized_user()
        {
            var e = Assert.Throws<HttpException>(() => m_client.Users.All());
            Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public void it_returns_all_user_groups()
        {
            List<Group> groups = m_client.Users.AllUserGroups();

            Assert.That(groups.Any(), "No user groups were found");
        }

        [Test]
        public void it_returns_all_users_by_user_group_name()
        {
            string userGroupName = "ALL_USERS_GROUP";
            List<User> users = m_client.Users.AllUsersByUserGroup(userGroupName);

            Assert.That(users.Any(), "No users were found for this group");
        }

        [Test]
        public void it_returns_all_roles_by_user_group_name()
        {
            string userGroupName = "ALL_USERS_GROUP";
            List<Role> roles = m_client.Users.AllUserRolesByUserGroup(userGroupName);

            Assert.That(roles.Any(), "No roles were found for that userGroup");
        }

        [Test]
        public void it_returns_all_users()
        {
            List<User> users = m_client.Users.All();

            Assert.That(users.Any(), "No users found for this server");
        }

        [Test]
        [Ignore("Needs an enterprise license")]
        public void it_returns_all_user_roles_by_user_name()
        {
            string userName = Configuration.GetAppSetting("NameOfUserWithRoles");
            List<Role> roles = m_client.Users.AllRolesByUserName(userName);

            Assert.That(roles.Any(), "No roles found for this user");
        }

        [Test]
        public void it_returns_all_user_groups_by_user_group_name()
        {
            string userName = m_username;
            List<Group> groups = m_client.Users.AllGroupsByUserName(userName);

            Assert.That(groups.Any(), "This user is not a member of any groups");
        }

        [Test]
        public void it_returns_user_details_by_user()
        {
            string userName = m_username;
            User details = m_client.Users.Details(userName);

            Assert.That(details.Email, Is.EqualTo(Configuration.GetAppSetting("UserEmail")), "Incorrect email address");
        }

        [Test]
        public void it_should_throw_exception_when_forbidden_status_code_returned()
        {
            var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
            client.ConnectAsGuest();

            Assert.Throws<HttpException>(() => client.Users.All());
        }
    }
}
