using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using TeamCitySharp.Connection;
using TeamCitySharp.Fields;
using TeamCitySharp.Locators;

namespace TeamCitySharp.IntegrationTests
{
  [TestFixture]
  public class when_interacting_to_get_build_status_info
  {
    private ITeamCityClient m_client;
    private readonly string m_server;
    private readonly bool m_useSsl;
    private readonly string m_username;
    private readonly string m_password;
    private readonly string m_goodBuildConfigId;

    public when_interacting_to_get_build_status_info()
    {
      m_server = Configuration.GetAppSetting("Server");
      bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
      m_username = Configuration.GetAppSetting("Username");
      m_password = Configuration.GetAppSetting("Password");
      m_goodBuildConfigId = Configuration.GetAppSetting("GoodBuildConfigId");
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
    public void it_throws_exception_when_no_connection_formed()
    {
      var client = new TeamCityClient(m_server, m_useSsl);

      const string buildConfigId = "Release Build";
      Assert.Throws<ArgumentException>(() => client.Builds.SuccessfulBuildsByBuildConfigId(buildConfigId));
    }

    [Test]
    public void it_returns_last_successful_build_by_build_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var build = m_client.Builds.LastSuccessfulBuildByBuildConfigId(buildConfigId);

      Assert.That(build, Is.Not.Null, "No successful builds have been found");
    }

    [Test]
    public void it_returns_last_successful_builds_by_build_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var buildDetails = m_client.Builds.SuccessfulBuildsByBuildConfigId(buildConfigId);

      Assert.That(buildDetails.Any(), "No successful builds have been found");
    }

    [Test]
    public void it_returns_last_failed_build_by_build_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var buildDetails = m_client.Builds.LastFailedBuildByBuildConfigId(buildConfigId);

      Assert.That(buildDetails, Is.Not.Null, "No failed builds have been found");
    }

    [Test]
    public void it_returns_all_non_successful_builds_by_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var builds = m_client.Builds.FailedBuildsByBuildConfigId(buildConfigId);

      Assert.That(builds.Any(), "No failed builds have been found");
    }

    [Test]
    public void it_doesnt_throw_exceptions_when_searching_last_error_build_by_config_id()
    {
      m_client.Builds.LastErrorBuildByBuildConfigId(m_goodBuildConfigId);
    }

    [Test]
    public void it_returns_the_last_build_status_by_build_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var build = m_client.Builds.LastBuildByBuildConfigId(buildConfigId);

      Assert.That(build, Is.Not.Null, "No builds for this build config have been found");
    }

    [Test]
    public void it_returns_all_builds_by_build_config_id()
    {
      string buildConfigId = m_goodBuildConfigId;
      var builds = m_client.Builds.ByBuildConfigId(buildConfigId);

      Assert.That(builds.Any(), "No builds for this build configuration have been found");
    }

    [Test]
    public void it_returns_all_builds_by_build_config_id_and_tag()
    {
      string buildConfigId = m_goodBuildConfigId;
      const string tag = "Release";
      var builds = m_client.Builds.ByConfigIdAndTag(buildConfigId, tag);

      Assert.That(builds, Is.Not.Null, "No builds were found for this build id and Tag");
    }

    [Test]
    public void it_returns_all_builds_by_username()
    {
      string userName = m_username;
      var builds = m_client.Builds.ByUserName(userName);

      Assert.That(builds, Is.Not.Null, "No builds for this user have been found");
    }

    [Test]
    public void it_returns_all_non_successful_builds_by_username()
    {
      string userName = m_username;
      var builds = m_client.Builds.NonSuccessfulBuildsForUser(userName);

      Assert.That(builds, Is.Not.Null, "No non successful builds found for this user");
    }

    [Test]
    public void it_returns_all_non_successful_build_count_by_username()
    {
      string userName = m_username;
      var builds = m_client.Builds.NonSuccessfulBuildsForUser(userName);

      Assert.That(builds, Is.Not.Null, "No non successful builds found for this user");
    }

    [Test]
    public void it_returns_all_running_builds()
    {
      var builds = m_client.Builds.ByBuildLocator(BuildLocator.RunningBuilds());

      Assert.That(builds, Is.Not.Null, "There are currently no running builds");
    }

    [Test]
    public void it_returns_all_failed_builds_since_date()
    {
      var builds = m_client.Builds.AllBuildsOfStatusSinceDate(new DateTime(2024, 12, 30, 17, 25, 39, DateTimeKind.Utc), BuildStatus.FAILURE);

      Assert.That(builds, Is.Not.Null);
    }

    [Test]
    public void it_does_not_populate_the_status_text_field_of_the_build_object()
    {
      string buildConfigId = m_goodBuildConfigId;
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      client.Connect(m_username, m_password);

      var build =
        client.Builds.ByBuildLocator(BuildLocator.WithDimensions(BuildTypeLocator.WithId(buildConfigId),
          maxResults: 1));
      Assert.That(build.Count, Is.EqualTo(1));
      Assert.That(build[0].StatusText, Is.Null);
    }

    [Test]
    public void unknown_build_id_raises_exception()
    {
      const string buildId = "5726";
      var e = Assert.Throws<HttpException>(() => m_client.Builds.ById(buildId));
      Assert.That(e.ResponseStatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expects a 404 exception.");
    }

    [Test]
    public void it_returns_correct_next_builds()
    {
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      var buildId = Configuration.GetAppSetting("IdOfBuildWithSubsequentBuilds");
      client.Connect(m_username, m_password);

      var builds = client.Builds.NextBuilds(buildId, 10);

      foreach (var build in builds)
      {
        Console.WriteLine($"Build: {build}");
      }

      Assert.That(builds, Is.Not.Null);
      Assert.That(builds.Count, Is.EqualTo(int.Parse(Configuration.GetAppSetting("NumberOfSubsequentBuilds"))));
    }

    [Test]
    public void it_returns_correct_next_builds_with_filter()
    {
      var client = new TeamCityClient(m_server, m_useSsl, Configuration.GetWireMockClient);
      var buildId = Configuration.GetAppSetting("IdOfBuildWithSubsequentBuilds");
      client.Connect(m_username, m_password);

      BuildField buildField = BuildField.WithFields(id: true, number: true, finishDate: true);
      BuildsField buildsField = BuildsField.WithFields(buildField);
      var builds = client.Builds.GetFields(buildsField.ToString()).NextBuilds(buildId, 3);

      Assert.That(builds, Is.Not.Null);
      Assert.That(builds.Count, Is.EqualTo(3));
      int i = 0;
      foreach (var build in builds)
      {
        Assert.That(build.FinishDate, Is.Not.EqualTo(new DateTime()));
        Console.WriteLine("{0} => BuildId => {1} FinishDate => {2}", i, build.Id, build.FinishDate);
        i++;
      }
    }

    [Test]
    public void it_pins_and_unpins_by_config()
    {
      //todo: consider adding GETs to verify the pin
      m_client.Builds.PinBuildByBuildNumber(Configuration.GetAppSetting("IdOfBuildConfigWithTests"), Configuration.GetAppSetting("BuildNumberOfBuildToPin"), "Automated Comment");
      m_client.Builds.UnPinBuildByBuildNumber(Configuration.GetAppSetting("IdOfBuildConfigWithTests"), Configuration.GetAppSetting("BuildNumberOfBuildToPin"));
    }

    [Test]
    public void it_returns_first_build_artifacts_relatedIssues_statistics_no_field()
    {
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.ById(tempBuild.Id);

      Assert.That(build.Artifacts, Is.Not.Null, "No Artifacts");
      Assert.That(build.Artifacts.Href, Is.Not.Null, "No Artifacts href");
      Assert.That(build.RelatedIssues, Is.Not.Null, "No RelatedIssues");
      Assert.That(build.RelatedIssues.Href, Is.Not.Null, "No RelatedIssues href");
      Assert.That(build.Statistics, Is.Not.Null, "No Statistics");
      Assert.That(build.Statistics.Href, Is.Not.Null, "No Statistics href");
    }

    [Test]
    public void it_returns_first_build_types_builds_investigations_compatible_agents_field_null()
    {
      // Section 1
      var buildField = BuildField.WithFields(number: true, id: true);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build.Artifacts, Is.Null, "No Artifacts 1");
      Assert.That(build.RelatedIssues, Is.Null, "No RelatedIssues 1");
      Assert.That(build.Statistics, Is.Null, "No Statistics 1");

      // section 2
      var artifactsField = ArtifactsField.WithFields();
      var relatedIssuesField = RelatedIssuesField.WithFields();
      var statisticsField = StatisticsField.WithFields();
      buildField = BuildField.WithFields(id: true, artifacts: artifactsField, relatedIssues: relatedIssuesField,
        statistics: statisticsField);
      tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build.Artifacts, Is.Not.Null, "No Artifacts 2");
      Assert.That(build.Artifacts.Href, Is.Not.Null, "No Artifacts href 2");
      Assert.That(build.RelatedIssues, Is.Not.Null, "No RelatedIssues 2");
      Assert.That(build.RelatedIssues.Href, Is.Not.Null, "No RelatedIssues href 2");
      Assert.That(build.Statistics, Is.Not.Null, "No Statistics 2");
      Assert.That(build.Statistics.Href, Is.Null, "No Statistics href 2");

      // section 3
      statisticsField = StatisticsField.WithFields(href: true);
      artifactsField = ArtifactsField.WithFields(href: true);
      relatedIssuesField = RelatedIssuesField.WithFields(href: true);
      buildField = BuildField.WithFields(id: true, artifacts: artifactsField, relatedIssues: relatedIssuesField,
        statistics: statisticsField);
      tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build.Artifacts, Is.Not.Null, "No Artifacts 3");
      Assert.That(build.Artifacts.Href, Is.Not.Null, "No Artifacts href 3");
      Assert.That(build.RelatedIssues, Is.Not.Null, "No RelatedIssues 3");
      Assert.That(build.RelatedIssues.Href, Is.Not.Null, "No RelatedIssues href 3");
      Assert.That(build.Statistics, Is.Not.Null, "No Statistics 3");
      Assert.That(build.Statistics.Href, Is.Not.Null, "No Statistics href 3");
    }

    [Test]
    public void it_returns_full_build_field_1()
    {
      var buildField = BuildField.WithFields(id: true, taskId: true, buildTypeId: true, buildTypeInternalId: true,
        number: true, status: true, state: true, running: true, composite: true, failedToStart: true, personal: true,
        percentageComplete: true,
        branchName: true, defaultBranch: true, unspecifiedBranch: true, history: true, pinned: true, href: true,
        webUrl: true, queuePosition: true, limitedChangesCount: true, artifactsDirectory: true, statusText: true,
        startEstimate: true, waitReason: true,
        startDate: true, finishDate: true, queuedDate: true, settingsHash: true, currentSettingsHash: true,
        modificationId: true, chainModificationId: true, usedByOtherBuilds: true);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build.Id, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_2()
    {
      ItemsField itemsField = ItemsField.WithFields(item: true);
      BuildsField buildsField = BuildsField.WithFields();
      RelatedField relatedField = RelatedField.WithFields(builds: buildsField);
      RelatedIssuesField relatedIssuesField = RelatedIssuesField.WithFields(href: true);
      ArtifactDependenciesField artifactDependenciesField = ArtifactDependenciesField.WithFields();
      BuildArtifactDependenciesField buildArtifactDependenciesField = BuildArtifactDependenciesField.WithFields();
      BuildSnapshotDependenciesField buildSnapshotDependenciesField = BuildSnapshotDependenciesField.WithFields();
      DatasField datasField = DatasField.WithFields();
      StatisticsField statisticsField = StatisticsField.WithFields();
      EntriesField entriesField = EntriesField.WithFields();
      PropertiesField propertiesField = PropertiesField.WithFields();
      ArtifactsField artifactsField = ArtifactsField.WithFields(href: true);
      ProblemOccurrencesField problemOccurrences = ProblemOccurrencesField.WithFields();
      TestOccurrencesField testOccurrencesField = TestOccurrencesField.WithFields();
      AgentField agentField = AgentField.WithFields(id: true);
      CompatibleAgentsField compatibleAgentsField = CompatibleAgentsField.WithFields(agent: agentField, href: true);
      BuildField buildField1 = BuildField.WithFields(id: true);
      BuildChangeField buildChangeField = BuildChangeField.WithFields(nextBuild: buildField1, prevBuild: buildField1);
      BuildChangesField buildChangesField = BuildChangesField.WithFields(buildChange: buildChangeField);
      RevisionField revisionField = RevisionField.WithFields(version: true);
      RevisionsField revisionsField = RevisionsField.WithFields();
      LastChangesField lastChangesField = LastChangesField.WithFields();
      ChangesField changesField = ChangesField.WithFields();
      TriggeredField triggeredField = TriggeredField.WithFields(type: true);
      ProgressInfoField progressInfoField = ProgressInfoField.WithFields(currentStageText: true);
      TagsField tagsField = TagsField.WithFields();
      UserField userField = UserField.WithFields(id: true);
      CommentField commentField = CommentField.WithFields(text: true);
      BuildTypeField buildTypeField = BuildTypeField.WithFields(id: true);
      BuildTypeWrapperField buildTypeWrapperField = BuildTypeWrapperField.WithFields(buildType: buildTypeField);
      LinkField linkField = LinkField.WithFields(type: true);
      LinksField linksField = LinksField.WithFields(link: linkField);
      var buildField = BuildField.WithFields(links: linksField, buildType: buildTypeField, comment: commentField,
        tags: tagsField, pinInfo: commentField, user: userField, running_info: progressInfoField,
        canceledInfo: commentField, triggered: triggeredField, lastChanges: lastChangesField, changes: changesField,
        revisions: revisionsField, versionedSettingsRevision: revisionField,
        artifactDependencyChanges: buildChangesField, agent: agentField, compatibleAgents: compatibleAgentsField,
        testOccurrences: testOccurrencesField, problemOccurrences: problemOccurrences, artifacts: artifactsField,
        properties: propertiesField, resultingProperties: propertiesField, attributes: entriesField,
        statistics: statisticsField, metadata: datasField, snapshotDependencies: buildSnapshotDependenciesField,
        artifactDependencies: buildArtifactDependenciesField, customArtifactDependencies: artifactDependenciesField,
        statusChangeComment: commentField, relatedIssues: relatedIssuesField, replacementIds: itemsField,
        related: relatedField);

      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_resultingProperties()
    {
      var tempBuildConfig = m_client.BuildConfigs.All().First();

      PropertyField propertyField = PropertyField.WithFields(name:true,value:true);
      PropertiesField propertiesField = PropertiesField.WithFields(propertyField: propertyField);
      var buildField = BuildField.WithFields(resultingProperties:propertiesField);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_attributes()
    {
      var tempBuildConfig = m_client.BuildConfigs.All().First();

      EntryField entryField = EntryField.WithFields(name:true,value:true);
      EntriesField entriesField = EntriesField.WithFields(entry: entryField);
      var buildField = BuildField.WithFields( attributes: entriesField);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_trigger()
    {
      var tempBuildConfig = m_client.BuildConfigs.All().First();

      UserField userField =UserField.WithFields(id:true);
      BuildField buildField1 = BuildField.WithFields(id:true);
      BuildTypeField buildTypeField = BuildTypeField.WithFields(id: true);
      TriggeredField triggeredField = TriggeredField.WithFields(buildType: buildTypeField,type:true,date:true,details:true,user:userField,displayText:true,rawValue:true, build:buildField1);
      var buildField = BuildField.WithFields(triggered: triggeredField);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_BuildChanges()
    {
      var tempBuildConfig = m_client.BuildConfigs.All().First();

      BuildField buildField1 = BuildField.WithFields(id: true);
      BuildChangeField buildChangeField = BuildChangeField.WithFields(nextBuild: buildField1, prevBuild: buildField1);
      BuildChangesField buildChangesField = BuildChangesField.WithFields(buildChange: buildChangeField);
      var buildField = BuildField.WithFields(artifactDependencyChanges: buildChangesField);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_Links()
    {
      LinkField linkField = LinkField.WithFields(type:true,url:true, relativeUrl: true);
      LinksField linksField = LinksField.WithFields(link:linkField);
      var buildField = BuildField.WithFields(links:linksField);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_versionedSettingsRevision()
    {
      RevisionField revisionField = RevisionField.WithFields(version: true);
      var buildField = BuildField.WithFields(versionedSettingsRevision: revisionField);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_statistics_with_build()
    {
      PropertyField propertyField = PropertyField.WithFields(name: true, value: true);
      StatisticsField statisticsField = StatisticsField.WithFields(propertyField: propertyField,href:true, count:true);
      BuildField buildField = BuildField.WithFields(statistics: statisticsField);
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      var build = m_client.Builds.GetFields(buildField.ToString()).ById(tempBuild.Id);
      Assert.That(build.Statistics.Href, Is.Not.Null);
      Assert.That(build.Statistics.Count, Is.Not.Null);
      Assert.That(build.Statistics.Property, Is.Not.Null);
    }

    [Test]
    public void it_returns_full_build_field_statistics_without_build ()
    {
      var tempBuild = m_client.Builds.LastBuildByBuildConfigId(Configuration.GetAppSetting("IdOfBuildConfigWithTests"));
      PropertyField propertyField = PropertyField.WithFields(name: true, value: true);
      StatisticsField statisticsField = StatisticsField.WithFields(propertyField: propertyField, href: true, count: true);
      var stats = m_client.Statistics.GetFields(statisticsField.ToString()).GetByBuildId(tempBuild.Id);
      // By default teamcity return href null
      Assert.That(stats.Href, Is.Null);
      Assert.That(stats.Count, Is.Not.Null);
      Assert.That(stats.Property, Is.Not.Null);

    }
  }
}
