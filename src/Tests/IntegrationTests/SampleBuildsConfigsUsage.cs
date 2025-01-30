using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Xml.Serialization;
using NUnit.Framework;
using TeamCitySharp.ActionTypes;
using TeamCitySharp.Connection;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Fields;
using TeamCitySharp.Locators;
using File = System.IO.File;

namespace TeamCitySharp.IntegrationTests
{
  [TestFixture]
  public class when_interations_to_get_build_configuration_details
  {
    private ITeamCityClient m_client;
    private readonly string m_server;
    private readonly bool m_useSsl;
    private readonly string m_username;
    private readonly string m_password;
    private readonly string m_token;
    private readonly string m_goodBuildConfigId;
    private readonly string m_goodProjectId;
    private readonly string m_goodTemplateId;

    public when_interations_to_get_build_configuration_details()
    {
      m_server = Configuration.GetAppSetting("Server");
      bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
      m_username = Configuration.GetAppSetting("Username");
      m_password = Configuration.GetAppSetting("Password");
      m_token = Configuration.GetAppSetting("Token");
      m_goodBuildConfigId = Configuration.GetAppSetting("GoodBuildConfigId");
      m_goodProjectId = Configuration.GetAppSetting("GoodProjectId");
      m_goodTemplateId = Configuration.GetAppSetting("GoodTemplateId");
    }

    [SetUp]
    public void SetUp()
    {
      m_client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      m_client.Connect(m_username, m_password);
    }

    [Test]
    public void it_throws_exception_when_no_url_passed()
    {
      Assert.Throws<ArgumentNullException>(() => new TeamCityClient(null));
    }

    [Test]
    public void it_returns_all_build_types_with_access_token()
    {
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      client.ConnectWithAccessToken(m_token);
      var buildConfigs = client.BuildConfigs.All();
      Assert.That(buildConfigs.Any(), "No build types were found in this server");
    }

    [Test]
    public void it_throws_exception_when_no_connection_formed()
    {
      var client = new TeamCityClient(m_server, m_useSsl);

      Assert.Throws<ArgumentException>(() => client.BuildConfigs.All());

      //Assert: Exception
    }

    [Test]
    public void it_returns_all_build_types()
    {
      var buildConfigs = m_client.BuildConfigs.All();

      Assert.That(buildConfigs.Any(), "No build types were found in this server");
    }

    [Test]
    public void it_returns_build_config_details_by_configuration_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var buildConfig = m_client.BuildConfigs.ByConfigurationId(buildConfigId);

      Assert.That(buildConfig, Is.Not.Null, "Cannot find a build type for that buildId");
    }

    [Test]
    public void it_pauses_and_unpauses_configuration()
    {
      string buildConfigId = m_goodBuildConfigId;
      var buildLocator = BuildTypeLocator.WithId(buildConfigId);

      m_client.BuildConfigs.SetConfigurationPauseStatus(buildLocator, true);
      var status = m_client.BuildConfigs.GetConfigurationPauseStatus(buildLocator);
      Assert.That(status, Is.True, "Build not paused");

      m_client.BuildConfigs.SetConfigurationPauseStatus(buildLocator, false);
      status = m_client.BuildConfigs.GetConfigurationPauseStatus(buildLocator);
      Assert.That(status, Is.False, "Build not unpaused");
    }

    [Test]
    [Ignore("Not working - not throwing exception as expected")]
    public void it_throws_exception_pauses_configuration_forbidden()
    {
      string buildConfigId = m_goodBuildConfigId;
      var buildLocator = BuildTypeLocator.WithId(buildConfigId);
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      client.Connect(Configuration.GetAppSetting("NonAdminUser"), m_password);
      var e = Assert.Throws<HttpException>(() => client.BuildConfigs.SetConfigurationPauseStatus(buildLocator, true), "Expected that the user does not have permission to pause the build");
      Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void it_returns_build_config_details_by_configuration_name()
    {
      string buildConfigName = Configuration.GetAppSetting("NameOfBuildConfigWithTests");
      var buildConfig = m_client.BuildConfigs.ByConfigurationName(buildConfigName);

      Assert.That(buildConfig, Is.Not.Null, "Cannot find a build type for that buildName");
    }

    [Test]
    public void it_returns_build_configs_by_project_id()
    {
      string projectId = m_goodProjectId;
      var buildConfigs = m_client.BuildConfigs.ByProjectId(projectId);

      Assert.That(buildConfigs.Any(), "Cannot find a build type for that projectId");
    }

    [Test]
    public void it_returns_build_configs_by_project_name()
    {
      var projectName = Configuration.GetAppSetting("NameOfProjectWithBuildConfigs");
      var buildConfigs = m_client.BuildConfigs.ByProjectName(projectName);

      Assert.That(buildConfigs.Any(), "Cannot find a build type for that projectName");
    }

    [Test]
    public void it_returns_artifact_dependencies_by_build_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var artifactDependencies = m_client.BuildConfigs.GetArtifactDependencies(buildConfigId);

      Assert.That(artifactDependencies, Is.Not.Null,
        "Cannot find a Artifact dependencies for that buildConfigId");
    }
    
    [Test]
    public void it_returns_snapshot_dependencies_by_build_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var snapshotDependencies = m_client.BuildConfigs.GetSnapshotDependencies(buildConfigId);
      Assert.That(snapshotDependencies, Is.Not.Null,
        "Cannot find a snapshot dependencies for that buildConfigId");
    }

    [Test]
    public void it_create_build_config_step()
    {
      var bt = new BuildConfig();
      try
      {
        bt = m_client.BuildConfigs.CreateConfigurationByProjectId(m_goodProjectId,
          "testNewConfig1");


        var xml = "<step type=\"simpleRunner\">" +
                  "<properties>" +
                  "<property name=\"script.content\" value=\"@echo off&#xA;echo Step1&#xA;touch step1.txt\" />" +
                  "<property name=\"teamcity.step.mode\" value=\"default\" />" +
                  "<property name=\"use.custom.script\" value=\"true\" />" +
                  "</properties>" +
                  "</step>";
        m_client.BuildConfigs.PostRawBuildStep(BuildTypeLocator.WithId(bt.Id), xml);
        var newBt = m_client.BuildConfigs.ByConfigurationId(bt.Id);
        var currentStepBuild = newBt.Steps.Step[0];
        Assert.That(currentStepBuild.Type == "simpleRunner" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "script.content").Value ==
                    "@echo off\necho Step1\ntouch step1.txt" && 
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "teamcity.step.mode").Value ==
                    "default" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "use.custom.script").Value ==
                    "true");
      }
      finally
      {
        m_client.BuildConfigs.DeleteConfiguration(BuildTypeLocator.WithId(bt.Id));
      }
    }

    [Test]
    public void it_create_build_config_steps()
    {
      var bt = new BuildConfig();
      try
      {
        bt = m_client.BuildConfigs.CreateConfigurationByProjectId(m_goodProjectId,
          "testNewConfig2");


        const string xml = @"<steps>
                        <step name=""Test1"" type=""simpleRunner"">
                        <properties>
                          <property name=""script.content"" value=""@echo off&#xA;echo Step1&#xA;touch step1.txt"" />
                          <property name=""teamcity.step.mode"" value=""default"" />
                          <property name=""use.custom.script"" value=""true"" />
                        </properties>
                    </step>
                    <step name=""Test2"" type=""simpleRunner"">
                        <properties>
                          <property name=""script.content"" value=""@echo off&#xA;echo Step1&#xA;touch step2.txt"" />
                          <property name=""teamcity.step.mode"" value=""default"" />
                          <property name=""use.custom.script"" value=""true"" />
                        </properties>
                    </step>
                   </steps>";
        m_client.BuildConfigs.PutRawBuildStep(BuildTypeLocator.WithId(bt.Id), xml);
        var newBt = m_client.BuildConfigs.ByConfigurationId(bt.Id);
        var currentStepBuild = newBt.Steps.Step[0];
        Assert.That(currentStepBuild.Type == "simpleRunner" && currentStepBuild.Name=="Test1" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "script.content").Value ==
                    "@echo off\necho Step1\ntouch step1.txt" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "teamcity.step.mode").Value ==
                    "default" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "use.custom.script").Value ==
                    "true");
        currentStepBuild = newBt.Steps.Step[1];
        Assert.That(currentStepBuild.Type == "simpleRunner" && currentStepBuild.Name == "Test2" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "script.content").Value ==
                    "@echo off\necho Step1\ntouch step2.txt" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "teamcity.step.mode").Value ==
                    "default" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "use.custom.script").Value ==
                    "true");
      }
      finally
      {
        m_client.BuildConfigs.DeleteConfiguration(BuildTypeLocator.WithId(bt.Id));
      }
    }

    [Test]
    public void it_getraw_build_config_steps()
    {
      var bt = new BuildConfig();
      try
      {
        bt = m_client.BuildConfigs.CreateConfigurationByProjectId(m_goodProjectId,
          "testNewConfig3");


        const string xml = @"<steps>
                        <step name=""Test1"" type=""simpleRunner"">
                        <properties>
                          <property name=""script.content"" value=""@echo off&#xA;echo Step1&#xA;touch step1.txt"" />
                          <property name=""teamcity.step.mode"" value=""default"" />
                          <property name=""use.custom.script"" value=""true"" />
                        </properties>
                    </step>
                    <step name=""Test2"" type=""simpleRunner"">
                        <properties>
                          <property name=""script.content"" value=""@echo off&#xA;echo Step1&#xA;touch step2.txt"" />
                          <property name=""teamcity.step.mode"" value=""default"" />
                          <property name=""use.custom.script"" value=""true"" />
                        </properties>
                    </step>
                   </steps>";
        m_client.BuildConfigs.PutRawBuildStep(BuildTypeLocator.WithId(bt.Id), xml);
        var newSteps = m_client.BuildConfigs.GetRawBuildStep(BuildTypeLocator.WithId(bt.Id));
        var currentStepBuild = newSteps.Step[0];
        Assert.That(currentStepBuild.Type == "simpleRunner" && currentStepBuild.Name == "Test1" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "script.content").Value ==
                    "@echo off\necho Step1\ntouch step1.txt" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "teamcity.step.mode").Value ==
                    "default" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "use.custom.script").Value ==
                    "true");
        currentStepBuild = newSteps.Step[1];
        Assert.That(currentStepBuild.Type == "simpleRunner" && currentStepBuild.Name == "Test2" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "script.content").Value ==
                    "@echo off\necho Step1\ntouch step2.txt" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "teamcity.step.mode").Value ==
                    "default" &&
                    currentStepBuild.Properties.Property.FirstOrDefault(x => x.Name == "use.custom.script").Value ==
                    "true");
      }
      finally
      {
        m_client.BuildConfigs.DeleteConfiguration(BuildTypeLocator.WithId(bt.Id));
      }
    }


    [Test]
    [Ignore("Not working - not throwing exception as expected")]
    public void it_throws_exception_artifact_dependencies_by_build_config_id_forbidden()
    {
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      client.Connect(Configuration.GetAppSetting("NonAdminUser"), m_password);
      var e = Assert.Throws<HttpException>(() => client.BuildConfigs.GetArtifactDependencies(m_goodBuildConfigId));
      Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Ignore("Not working - not throwing exception as expected")]
    public void it_throws_exception_snapshot_dependencies_by_build_config_id_forbidden()
    {
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      client.Connect(Configuration.GetAppSetting("NonAdminUser"), m_password);
      var e = Assert.Throws<HttpException>(() =>   client.BuildConfigs.GetSnapshotDependencies(m_goodBuildConfigId));
      Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void it_throws_exception_create_build_config_forbidden()
    {
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      client.Connect(Configuration.GetAppSetting("NonAdminUser"), m_password);
      var e = Assert.Throws<HttpException>(() => client.BuildConfigs.CreateConfigurationByProjectId(m_goodProjectId, "testNewConfig4"));
      Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void it_modify_build_config()
    {
      const string depend = "TeamcityDashboardScenario_Test_TestWithCheckout";
      const string newDepend = "TeamcityDashboardScenario_Test_TestWithCheckoutWithDependencies";
      
      var projectName = "Project to test build config modification";
      var project = m_client.Projects.Create(projectName, m_goodProjectId);
      
      var buildConfig = m_client.BuildConfigs.CreateConfigurationByProjectId(project.Id, "testNewConfig5");
      var buildLocator = BuildTypeLocator.WithId(buildConfig.Id);
      var bt = new BuildTrigger
      {
        Id = "ttt1", Type = "buildDependencyTrigger", Properties = new Properties
        {
          Property = new List<Property>
          {
            new Property {Name = "afterSuccessfulBuildOnly", Value = "true"},
            new Property {Name = "dependsOn", Value = depend}
          }
        }
      };

      // Configure starting trigger
      m_client.BuildConfigs.SetTrigger(buildLocator, bt);

      var actualFirst = m_client.BuildConfigs.ByConfigurationId(buildConfig.Id);
      Assert.That(actualFirst.Triggers.Trigger[0].Type == "buildDependencyTrigger" &&
                  actualFirst.Triggers.Trigger[0].Properties.Property.FirstOrDefault(x => x.Name == "dependsOn")
                    .Value == depend);

      // Modify trigger
      m_client.BuildConfigs.ModifTrigger(buildConfig.Id, depend, newDepend);
      var actualTwo = m_client.BuildConfigs.ByConfigurationId(buildConfig.Id);
      Assert.That(actualTwo.Triggers.Trigger[0].Type == "buildDependencyTrigger" &&
                  actualTwo.Triggers.Trigger[0].Properties.Property.FirstOrDefault(x => x.Name == "dependsOn")
                    .Value == newDepend);
      var buildLocatorFinal = BuildTypeLocator.WithId(buildConfig.Id);

      //Cleanup 
      m_client.BuildConfigs.DeleteConfiguration(buildLocatorFinal);
      m_client.Projects.Delete(project.Name);
    }

    [Test]
    public void it_modify_artifact_dependencies()
    {
      const string depend = "TeamcityDashboardScenario_Test_TestWithCheckout";
      const string newDepend = "TeamcityDashboardScenario_Test_TestWithCheckoutWithDependencies";
      var buildLocatorFinal = new BuildTypeLocator();
      try
      {
        var buildConfig = m_client.BuildConfigs.CreateConfigurationByProjectId(m_goodProjectId, "testNewConfig6");
        buildLocatorFinal = BuildTypeLocator.WithId(buildConfig.Id);
        var artifactDependencies = new ArtifactDependencies
        {
          ArtifactDependency = new List<ArtifactDependency>
          {
            new ArtifactDependency
            {
              Id = "TTTT_100",
              Type = "artifact_dependency",
              SourceBuildType = new BuildConfig{Id = depend},
              Properties = new Properties
              {
                Property = new List<Property>
                {
                  new Property {Name = "cleanDestinationDirectory", Value = "false"},
                  new Property {Name = "pathRules", Value = "step1.txt"},
                  new Property {Name = "revisionName", Value = "lastSuccessful"},
                  new Property {Name = "revisionValue", Value = "latest.lastSuccessful"}
                }
              }
            }
          }
        };

        m_client.BuildConfigs.SetArtifactDependency(buildLocatorFinal, artifactDependencies.ArtifactDependency[0]);

        m_client.BuildConfigs.ModifArtifactDependencies(buildConfig.Id, depend, newDepend);

      }
      finally
      {
        //Cleanup 
        m_client.BuildConfigs.DeleteConfiguration(buildLocatorFinal);
      }
      
    }

    [Test]
    public void it_modify_snapshot_dependencies()
    {
      const string depend = "TeamcityDashboardScenario_Test_TestWithCheckout";
      const string newDepend = "TeamcityDashboardScenario_Test_TestWithCheckoutWithDependencies";
      var buildLocatorFinal = new BuildTypeLocator();

      var buildConfig = m_client.BuildConfigs.CreateConfigurationByProjectId(m_goodProjectId, "testNewConfig7");
      buildLocatorFinal = BuildTypeLocator.WithId(buildConfig.Id);
      var snapshotDependencies = new SnapshotDependencies
      {
        SnapshotDependency = new List<SnapshotDependency>
        {
          new SnapshotDependency
          {
            Id = "TTTT_100",
            Type = "snapshot_dependency",
            SourceBuildType = new BuildConfig{Id = depend},
            Properties = new Properties
            {
              Property = new List<Property>
              {
                new Property {Name = "run-build-if-dependency-failed", Value = "RUN_ADD_PROBLEM"},
                new Property {Name = "run-build-if-dependency-failed-to-start", Value = "MAKE_FAILED_TO_START"},
                new Property {Name = "run-build-on-the-same-agent", Value = "false"},
                new Property {Name = "take-started-build-with-same-revisions", Value = "true"},
                new Property {Name = "take-successful-builds-only", Value = "true"}
              }
            }
          }
        }
      };

      m_client.BuildConfigs.SetSnapshotDependency(buildLocatorFinal, snapshotDependencies.SnapshotDependency[0]);

      m_client.BuildConfigs.ModifSnapshotDependencies(buildConfig.Id, depend, newDepend);

      m_client.BuildConfigs.DeleteConfiguration(buildLocatorFinal);
    }

    [Test]
    public void it_returns_first_build_types_builds_investigations_compatible_agents_no_field()

    {
      var tempBuildConfig = m_client.BuildConfigs.All().First();
      var buildConfig = m_client.BuildConfigs.ByConfigurationId(tempBuildConfig.Id);
      Assert.That(buildConfig.Builds, Is.Not.Null, "No builds ");
      Assert.That(buildConfig.Builds.Href, Is.Not.Null, "No builds href");
      Assert.That(buildConfig.Investigations, Is.Not.Null, "No Investigations ");
      Assert.That(buildConfig.Investigations.Href, Is.Not.Null, "No Investigations href");
      Assert.That(buildConfig.CompatibleAgents, Is.Not.Null, "No CompatibleAgents ");
      Assert.That(buildConfig.CompatibleAgents.Href, Is.Not.Null, "No CompatibleAgents href");
    }

    [Test]
    public void it_returns_first_build_types_builds_investigations_compatible_agents_field_null()
    {
      var tempBuildConfigId =Configuration.GetAppSetting("IdOfBuildConfigWithTests");
      // Section 1
      var buildTypeField = BuildTypeField.WithFields(id:true);
      var buildConfig = m_client.BuildConfigs.GetFields(buildTypeField.ToString()).ByConfigurationId(tempBuildConfigId);
      Assert.That(buildConfig.Builds, Is.Null, "No builds 1");
      Assert.That(buildConfig.Investigations, Is.Null, "No Investigations 1");
      Assert.That(buildConfig.CompatibleAgents, Is.Null, "No CompatibleAgents 1");

      // section 2
      var buildsField = BuildsField.WithFields(count:true);
      var investigationsField = InvestigationsField.WithFields();
      var compatibleAgentsField = CompatibleAgentsField.WithFields();
      buildTypeField = BuildTypeField.WithFields(id: true, builds: buildsField,investigations: investigationsField, compatibleAgents:compatibleAgentsField);
      buildConfig = m_client.BuildConfigs.GetFields(buildTypeField.ToString()).ByConfigurationId(tempBuildConfigId);
      Assert.That(buildConfig.Builds, Is.Not.Null, "No builds 2");
      Assert.That(buildConfig.Builds.Href, Is.Null, "No builds href 2");
      Assert.That(buildConfig.Investigations, Is.Not.Null, "No Investigations 2");
      Assert.That(buildConfig.Investigations.Href, Is.Null, "No Investigations href 2");
      Assert.That(buildConfig.CompatibleAgents, Is.Not.Null, "No CompatibleAgents 2");
      Assert.That(buildConfig.CompatibleAgents.Href, Is.Not.Null, "No CompatibleAgents href 2");

      // section 3
      buildsField = BuildsField.WithFields(count: true,href:true);
      investigationsField = InvestigationsField.WithFields(href:true);
      compatibleAgentsField = CompatibleAgentsField.WithFields(href:true);
      buildTypeField = BuildTypeField.WithFields(id: true, builds: buildsField, investigations: investigationsField, compatibleAgents: compatibleAgentsField);
      buildConfig = m_client.BuildConfigs.GetFields(buildTypeField.ToString()).ByConfigurationId(tempBuildConfigId);
      Assert.That(buildConfig.Builds, Is.Not.Null, "No builds 3");
      Assert.That(buildConfig.Builds.Href, Is.Not.Null, "No builds href 3");
      Assert.That(buildConfig.Investigations, Is.Not.Null, "No Investigations 3");
      Assert.That(buildConfig.Investigations.Href, Is.Not.Null, "No Investigations href 3");
      Assert.That(buildConfig.CompatibleAgents, Is.Not.Null, "No CompatibleAgents 3");
      Assert.That(buildConfig.CompatibleAgents.Href, Is.Not.Null, "No CompatibleAgents href 3");
    }

    [Test]
    public void it_attaches_and_detaches_templates_from_build_config()
    {
      string buildConfigId = m_goodBuildConfigId;
      var buildLocator = BuildTypeLocator.WithId(buildConfigId);
      var buildConfig = new Template { Id = m_goodTemplateId };
      var buildConfigList = new List<Template>() { buildConfig };
      var templates = new Templates { BuildType = buildConfigList };
      m_client.BuildConfigs.AttachTemplates(buildLocator, templates);
      var templatesReceived = m_client.BuildConfigs.GetTemplates(buildLocator);
      Assert.That(templatesReceived.BuildType.Any(), "Templates not attached");
      
      var templatesField = m_client.BuildConfigs.ByConfigurationId(buildConfigId).Templates;
      Assert.That(templatesField, Is.Not.Null, "Templates property not retrieved correctly");
      
      m_client.BuildConfigs.DetachTemplates(buildLocator);

      templatesReceived = m_client.BuildConfigs.GetTemplates(buildLocator);
      Assert.That(!templatesReceived.BuildType.Any(), "Templates not detached");
    }

    [Test]
    public void it_downloads_build_configuration()
    {
      string buildConfigId = m_goodBuildConfigId;
      string directory = Directory.GetCurrentDirectory();
      string destination = Path.Combine(directory, "config.txt");
      if (File.Exists(destination))
        File.Delete(destination);
      
      var buildLocator = BuildTypeLocator.WithId(buildConfigId);
      m_client.BuildConfigs.DownloadConfiguration(buildLocator, tempfile => System.IO.File.Move(tempfile, destination));
      Assert.That(System.IO.File.Exists(destination), Is.True);
      Assert.That(new FileInfo(destination).Length > 0, Is.True);
    }

    [Test]
    public void it_creates_build_configuration()
    {
      var currentBuildId = "testId";
      var buildProject = new Project() { Id = m_goodProjectId };
      var parameters = new Parameters
        { Property = new List<Property>() { new Property() { Name = "category", Value = "test"} } };
      var buildConfig = new BuildConfig() { Id = currentBuildId, Name = "testNewConfig8", Project = buildProject, Parameters = parameters };

      try
      {
        buildConfig = m_client.BuildConfigs.CreateConfiguration(buildConfig);

        Assert.That(buildConfig.Id, Is.EqualTo(currentBuildId));
      }
      finally
      {
        m_client.BuildConfigs.DeleteConfiguration(BuildTypeLocator.WithId(currentBuildId));
      }
    }

    [Test]
    public void it_returns_branches()
    {
      string buildConfigId = Configuration.GetAppSetting("IdOfBuildConfigWithArtifactAndVcsRoot");
      var tempBuild = m_client.BuildConfigs.GetBranchesByBuildConfigurationId(buildConfigId);
      Assert.That(tempBuild.Count, Is.EqualTo(int.Parse(Configuration.GetAppSetting("NumberOfBranchesForBuildConfigWithArtifactAndVcsRoot"))));
    }

    [Test]
    public void it_returns_branches_history()
    {
      string buildConfigId = Configuration.GetAppSetting("IdOfBuildConfigWithArtifactAndVcsRoot");
      var tempBuild = m_client.BuildConfigs.GetBranchesByBuildConfigurationId(buildConfigId,BranchLocator.WithDimensions(BranchPolicy.ALL_BRANCHES));
      Assert.That(tempBuild.Count, Is.EqualTo(int.Parse(Configuration.GetAppSetting("NumberOfBranchesForBuildConfigWithArtifactAndVcsRoot"))));
    }

    [Test]
    public void it_returns_branches_history_with_field_default_but_active_not_fetched()
    {
      BranchField branchField = BranchField.WithFields(name:true, defaultValue:true);
      BranchesField branchesField = BranchesField.WithFields(branch: branchField);
      string buildConfigId = m_goodBuildConfigId;
      var tempBuild = m_client.BuildConfigs.GetFields(branchesField.ToString()).GetBranchesByBuildConfigurationId(buildConfigId, BranchLocator.WithDimensions(BranchPolicy.ALL_BRANCHES));
      var checkIfFieldWork = tempBuild.Branch.Single(x => x.Default);
      Assert.That(checkIfFieldWork.Active, Is.False);
    }

    [Test]
    public void it_returns_branches_history_with_field_default_active_fetched()
    {
      BranchField branchField = BranchField.WithFields(name: true, defaultValue: true, active:true);
      BranchesField branchesField = BranchesField.WithFields(branch: branchField);
      string buildConfigId = m_goodBuildConfigId;
      var tempBuild = m_client.BuildConfigs.GetFields(branchesField.ToString()).GetBranchesByBuildConfigurationId(buildConfigId, BranchLocator.WithDimensions(BranchPolicy.ALL_BRANCHES));
      var checkIfFieldWork = tempBuild.Branch.Single(x => x.Default);
      Assert.That(checkIfFieldWork.Active, Is.True);
    }

    #region private
    private string GetXml(object data)
    {
      XmlSerializer xsSubmit = new XmlSerializer(data.GetType());
      var ns = new XmlSerializerNamespaces();
      ns.Add("", "");

      XmlWriterSettings settings = new XmlWriterSettings();
      settings.OmitXmlDeclaration = true;
      using (var sww = new StringWriter())
      {
        using (XmlWriter writer = XmlWriter.Create(sww, settings))
        {
          xsSubmit.Serialize(writer, data,ns);
          return sww.ToString();
        }
      }
    }
#endregion

  }
}
