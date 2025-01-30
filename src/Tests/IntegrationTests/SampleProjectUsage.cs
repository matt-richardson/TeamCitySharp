﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using TeamCitySharp.Connection;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Fields;
using TeamCitySharp.Locators;

namespace TeamCitySharp.IntegrationTests
{
  [TestFixture]
  public class when_interacting_to_get_project_details
  {
    private ITeamCityClient m_client;
    private readonly string m_server;
    private readonly bool m_useSsl;
    private readonly string m_username;
    private readonly string m_password;
    private readonly string m_goodBuildConfigId;
    private readonly string m_goodProjectId;


    public when_interacting_to_get_project_details()
    {
      m_server = Configuration.GetAppSetting("Server");
      bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
      m_username = Configuration.GetAppSetting("Username");
      m_password = Configuration.GetAppSetting("Password");
      m_goodBuildConfigId = Configuration.GetAppSetting("GoodBuildConfigId");
      m_goodProjectId = Configuration.GetAppSetting("GoodProjectId");
    }

    [SetUp]
    public void SetUp()
    {
      m_client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      m_client.Connect(m_username, m_password);
    }

    [Test]
    public void it_throws_exception_when_not_passing_url()
    {
      Assert.Throws<ArgumentNullException>(() => new TeamCityClient(null));
    }



    [Test]
    public void it_throws_exception_when_no_connection_formed()
    {
      var client = new TeamCityClient(m_server, m_useSsl);

      Assert.Throws<ArgumentException>(() => client.Projects.All());

      //Assert: Exception
    }

    [Test]
    public void it_returns_all_projects()
    {
      List<Project> projects = m_client.Projects.All();

      Assert.That(projects.Any(), "No projects were found for this server");
    }

    [Test]
    public void it_returns_project_details_when_passing_a_project_id()
    {
      string projectId = m_goodProjectId;
      Project projectDetails = m_client.Projects.ById(projectId);

      Assert.That(projectDetails, Is.Not.Null, "No details found for that specific project");
    }

    [Test]
    public void it_returns_project_details_when_passing_a_project_name()
    {
      string projectName = Configuration.GetAppSetting("NameOfProjectWithBuildConfigs");
      Project projectDetails = m_client.Projects.ByName(projectName);

      Assert.That(projectDetails, Is.Not.Null, "No details found for that specific project");
    }

    [Test]
    public void it_returns_project_details_when_passing_project()
    {
      var project = new Project {Id = m_goodProjectId};
      Project projectDetails = m_client.Projects.Details(project);

      Assert.That(!string.IsNullOrWhiteSpace(projectDetails.Id));
    }


    [Test]
    public void it_returns_project_details_when_creating_project()
    {
      var client = new TeamCityClient(m_server, httpClientFactory: Configuration.GetWireMockClient);
      client.Connect(m_username, m_password);
      var projectName = "SampleProjectName";
      try
      {
        var project = client.Projects.Create(projectName);

        Assert.That(project, Is.Not.Null);
        Assert.That(project.Name, Is.EqualTo(projectName));
      }
      finally
      {
        client.Projects.Delete(projectName);
      }
    }

    [Test]
    public void it_returns_projectFeatures_when_passing_a_project_id()
    {
      string projectId = "_Root";
      var projectFeature = m_client.Projects.GetProjectFeatures(projectId);
      Assert.That(projectFeature, Is.Not.Null, "No project feature found for that specific project");
    }

    [Test]
    public void it_returns_projectFeatures_when_passing_a_project_id_and_feature_id()
    {
      string projectId = "_Root";
      string featureId = "PROJECT_EXT_1";
      ProjectFeature projectFeature = m_client.Projects.GetProjectFeatureByProjectFeature(projectId, featureId);

      Assert.That(projectFeature, Is.Not.Null, "No project feature found for that specific project");
    }

    [Test]
    public void it_returns_projectFeatures_create_modify_delete()
    {
      string projectId = "_Root";
      ProjectFeature pf = new ProjectFeature
      {
        Id = "Test_TTT",
        Type = "ReportTab",
        Properties = new Properties
        {
          Property = new List<Property>
          {
            new Property {Name = "startPage", Value = "javadoc.zip!index.html"},
            new Property {Name = "title", Value = "javadoc.zip!index.html"},
            new Property {Name = "type", Value = "BuildReportTab"},
          }
        }
      };

      ProjectFeature projectFeature = m_client.Projects.CreateProjectFeature(projectId, pf);
      Assert.That(projectFeature, Is.Not.Null, "No project features found for that specific project");

      m_client.Projects.DeleteProjectFeature(projectId, projectFeature.Id);
    }

    [Test]
    [Ignore("Not working - not throwing exception as expected")]
    public void it_refuses_projectFeatures_create_modify_delete_when_unauthorized()
    {
      string projectId = "_Root";
      ProjectFeature pf = new ProjectFeature
      {
        Id = "Test_TTT",
        Type = "ReportTab",
        Properties = new Properties
        {
          Property = new List<Property>
          {
            new Property {Name = "startPage", Value = "javadoc.zip!index.html"},
            new Property {Name = "title", Value = "javadoc.zip!index.html"},
            new Property {Name = "type", Value = "BuildReportTab"},
          }
        }
      };

      ProjectFeature projectFeature = m_client.Projects.CreateProjectFeature(projectId, pf);
      var e = Assert.Throws<HttpException>(() => m_client.Projects.DeleteProjectFeature(projectId, projectFeature.Id));
      Assert.That(e.ResponseStatusCode == HttpStatusCode.Forbidden,
        "Creating a project feature should fail with unauthorized http exception.");
    }

    [Test]
    public void it_returns_projectFeatures_field()
    {
      string projectId = "_Root";
      string featureId = "PROJECT_EXT_1";

      PropertyField propertyField = PropertyField.WithFields(name: true, value: true, inherited: true);
      PropertiesField propertiesField = PropertiesField.WithFields(propertyField: propertyField);
      ProjectFeatureField projectFeatureField =
        ProjectFeatureField.WithFields(type: true, properties: propertiesField);

      ProjectFeature projectFeature = m_client.Projects.GetFields(projectFeatureField.ToString())
        .GetProjectFeatureByProjectFeature(projectId, featureId);

      Assert.That(projectFeature, Is.Not.Null, "No project feature found for that specific project");
      Assert.That(projectFeature.Type, Is.Not.Null, "Bad Value type");
      Assert.That(projectFeature.Properties, Is.Not.Null, "Bad Value type");
      Assert.That(projectFeature.Href, Is.Null, "Bad Value type");
      Assert.That(projectFeature.Id, Is.Null, "Bad Value type");
    }

    [Test]
    [Ignore("Not working - not throwing exception as expected")]
    public void it_faces_exceptions_projectFeatures_field_when_unauthorized()
    {
      string projectId = "_Root";
      string featureId = "PROJECT_EXT_1";

      PropertyField propertyField = PropertyField.WithFields(name: true, value: true, inherited: true);
      PropertiesField propertiesField = PropertiesField.WithFields(propertyField: propertyField);
      ProjectFeatureField projectFeatureField =
      ProjectFeatureField.WithFields(type: true, properties: propertiesField);

      var e = Assert.Throws<HttpException>(() => m_client.Projects.GetFields(projectFeatureField.ToString())
        .GetProjectFeatureByProjectFeature(projectId, featureId));
      Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void it_returns_branches()
    {
      string projectId = m_goodProjectId;
      var tempBuild = m_client.Projects.GetBranchesByBuildProjectId(projectId);
      Assert.That(tempBuild.Count > 0, Is.True);
    }

    [Test]
    public void it_returns_branches_history()
    {
      string projectId = Configuration.GetAppSetting("IdOfProjectWithQueuedBuilds");
      var expected = int.Parse(Configuration.GetAppSetting("NumberOfBranchesForProjectWithArtifactAndVcsRoot"));
      var tempBuild = m_client.Projects.GetBranchesByBuildProjectId(projectId,
        BranchLocator.WithDimensions(BranchPolicy.ALL_BRANCHES));
      Assert.That(tempBuild.Count, Is.EqualTo(expected));
    }

    [Test]
    public void it_returns_branches_history_with_field_Default_but_active_not_fetched()
    {
      BranchField branchField = BranchField.WithFields(name: true, defaultValue: true);
      BranchesField branchesField = BranchesField.WithFields(branch: branchField);
      string projectId = m_goodProjectId;
      var tempBuild = m_client.Projects.GetFields(branchesField.ToString())
        .GetBranchesByBuildProjectId(projectId, BranchLocator.WithDimensions(BranchPolicy.ALL_BRANCHES));
      var checkIfFieldWork = tempBuild.Branch.Single(x => x.Default);
      Assert.That(checkIfFieldWork.Active, Is.False);

    }

    [Test]
    public void it_returns_branches_history_with_field_Default_active_fetched()
    {
      BranchField branchField = BranchField.WithFields(name: true, defaultValue: true, active: true);
      BranchesField branchesField = BranchesField.WithFields(branch: branchField);
      string projectId = m_goodProjectId;
      var tempBuild = m_client.Projects.GetFields(branchesField.ToString())
        .GetBranchesByBuildProjectId(projectId, BranchLocator.WithDimensions(BranchPolicy.ALL_BRANCHES));
      var checkIfFieldWork = tempBuild.Branch.Single(x => x.Default);
      Assert.That(checkIfFieldWork.Active, Is.True);
    }
  }
}
