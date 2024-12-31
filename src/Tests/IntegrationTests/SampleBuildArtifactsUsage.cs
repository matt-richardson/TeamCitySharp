using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TeamCitySharp.IntegrationTests
{
  [TestFixture]
  public class test_build_artifact_download
  {
    private ITeamCityClient m_client;
    private readonly string m_server;
    private readonly bool m_useSsl;
    private readonly string m_username;
    private readonly string m_password;
    private readonly string m_token;
    private readonly string m_goodBuildConfigId;

    public test_build_artifact_download()
    {
      m_server = Configuration.GetAppSetting("Server");
      bool.TryParse(Configuration.GetAppSetting("UseSsl"), out m_useSsl);
      m_username = Configuration.GetAppSetting("Username");
      m_password = Configuration.GetAppSetting("Password");
      m_token = Configuration.GetAppSetting("Token");
      m_goodBuildConfigId = Configuration.GetAppSetting("GoodBuildConfigId");
    }

    [SetUp]
    public void SetUp()
    {
      m_client = new TeamCityClient(m_server, m_useSsl);
      m_client.Connect(m_username, m_password);
    }

    [Test]
    public void it_downloads_artifact()
    {
      string buildConfigId = m_goodBuildConfigId;
      var build = m_client.Builds.LastSuccessfulBuildByBuildConfigId(buildConfigId);
      var directartifact = m_client.Artifacts.ByBuildConfigId(build.BuildTypeId);
      var listFilesDownload = directartifact.Specification(build.Number).Download();
      Assert.That(listFilesDownload, Is.Not.Empty);
    }

    [Test, Ignore("You need to configure the token before to run this test")]
    public void it_downloads_artifact_with_access_token()
    {
      var buildConfigId = m_goodBuildConfigId;
      var token = m_token;
      var client = new TeamCityClient(m_server, m_useSsl);
      client.ConnectWithAccessToken(token);
      
      var build = client.Builds.LastSuccessfulBuildByBuildConfigId(buildConfigId);
      var directArtifact = client.Artifacts.ByBuildConfigId(build.BuildTypeId);
      var listFilesDownload = directArtifact.Specification(build.Number).Download();
      Assert.That(listFilesDownload, Is.Not.Empty);
    }

    [Test]
    public void it_download_no_artifacts_for_failed_builds()
    {
      string buildConfigId = m_goodBuildConfigId;
      var build = m_client.Builds.LastFailedBuildByBuildConfigId(buildConfigId);
      var directartifact = m_client.Artifacts.ByBuildConfigId(build.BuildTypeId);
      var listFilesDownload = directartifact.Specification(build.Number).Download();
      Assert.That(listFilesDownload, Is.Not.Empty);
    }

    [Test]
    public async Task it_download_artifact()
    {
      var buildConfigId = m_goodBuildConfigId;

      const string filename = "Outputs.zip";
      var expectedFile = Path.Combine(Path.GetTempPath(), "expectedFile.zip");
      var expectedUrl = $"http://{m_server}/repository/download/{m_goodBuildConfigId}/.lastSuccessful/{filename}";
      var artifact = m_client.Artifacts.ByBuildConfigId(buildConfigId);
      var file = artifact.LastSuccessful().DownloadFiltered(Path.GetTempPath(), new[] {filename}.ToList()).FirstOrDefault();
      Assert.That(file, Is.Not.Empty);
      await DownloadFile(expectedUrl, expectedFile);

      Assert.That(FileEquals(expectedFile, file), Is.True);
 
      if (File.Exists(file))
      {
        File.Delete(file);
      }

      if (File.Exists(expectedFile))
      {
        File.Delete(expectedFile);
      }
    }

    private async Task DownloadFile(string expectedUrl, string expectedFile)
    {
      var client = new HttpClient();
      var credentials = Encoding.ASCII.GetBytes($"{m_username}:{m_password}");
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

      using HttpResponseMessage response = await client.GetAsync(expectedUrl, HttpCompletionOption.ResponseHeadersRead);
      
      response.EnsureSuccessStatusCode();

      await using Stream contentStream = await response.Content.ReadAsStreamAsync();
      await using FileStream fileStream = new FileStream(expectedFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
      await contentStream.CopyToAsync(fileStream);
    }

    [Test]
    public async Task it_download_artifact_from_a_git_branch()
    {
      var buildConfigId = m_goodBuildConfigId;
      const string filename = "Outputs.zip";
      const string param = "branch=dev-2001";
      var expectedFile = Path.Combine(Path.GetTempPath(), "expectedFile.zip");
      var expectedUrl = $"http://{m_server}/repository/download/{m_goodBuildConfigId}/.lastSuccessful/{filename}?{param}";
      var artifact = m_client.Artifacts.ByBuildConfigId(buildConfigId, param);
      var file = artifact.LastSuccessful().DownloadFiltered(Path.GetTempPath(), new[] { filename }.ToList()).FirstOrDefault();
      Assert.That(file, Is.Not.Empty);
      await DownloadFile(expectedUrl, expectedFile);

      Assert.That(FileEquals(expectedFile, file), Is.True);
      if (File.Exists(file))
      {
        File.Delete(file);
      }

      if (File.Exists(expectedFile))
      {
        File.Delete(expectedFile);
      }

    }

    private static bool FileEquals(string path1, string path2)
    {
      byte[] file1 = File.ReadAllBytes(path1);
      byte[] file2 = File.ReadAllBytes(path2);
      if (file1.Length == file2.Length)
      {
        for (int i = 0; i < file1.Length; i++)
        {
          if (file1[i] != file2[i])
          {
            return false;
          }
        }
        return true;
      }
      return false;
    }
  }
}
