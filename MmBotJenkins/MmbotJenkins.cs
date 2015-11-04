using Devkoes.JenkinsManager.APIHandler.Managers;
using Devkoes.JenkinsManager.Model.Schema;
using MMBot.Scripts;
using MmBotJenkins.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MmBotJenkins
{
    public class MmbotJenkins : IMMBotScript
    {
        private string userName, apiKey, baseUrl, buildToken;

        private static void Main()
        {
            Task.Run(async () =>
        {
            MmbotJenkins bot = new MmbotJenkins();
            await bot.JenkinsList(null, null);
            await bot.JenkinsBuild(null, null);
        }).Wait();
        }

        public IEnumerable<string> GetHelp()
        {
            return new List<String>{
                "veyobot jenkins list - list jenkins jobs",
                "veyobot jenkins build <job name> - build the specified jenkins job",
            };
        }

        public void Register(MMBot.Robot robot)
        {
            SetupJenkins(robot);

            //robot.Hear("jenkins test", msg=> msg.Send("testing!!"));
            robot.Respond(@"jenkins test", msg => Task.Run(() => msg.Send("testing!!")).Wait());
            robot.Respond(@"jenkins build ([\w\.\-_ ]+)(, (.+))?/i", msg => Task.Run(() => JenkinsBuild(robot, msg)).Wait());
            //robot.Respond(@"/j(?:enkins)? b (\d+)/i", msg => JenkinsBuildById(msg));
            robot.Respond(@"jenkins list", msg => Task.Run(() => JenkinsList(robot, msg)).Wait());
            //robot.Respond(@"/j(?:enkins)? describe (.*)/i", msg => JenkinsDescribe(msg));
            //robot.Respond(@"/j(?:enkins)? last (.*)/i", msg => JenkinsLast(msg));
        }

        private void SetupJenkins(MMBot.Robot robot)
        {
            robot.Logger.Debug("Loading jenkins config values");
            userName = robot.GetConfigVariable("MMBOT_JENKINS_USERNAME") ?? userName;
            apiKey = robot.GetConfigVariable("MMBOT_JENKINS_APIKEY") ?? apiKey;
            baseUrl = robot.GetConfigVariable("MMBOT_JENKINS_BASEURL") ?? baseUrl;
            buildToken = robot.GetConfigVariable("MMBOT_JENKINS_BUILDTOKEN") ?? buildToken;
            robot.Logger.DebugFormat("Jenkins username - {0}", userName);
        }

        private void JenkinsLast(MMBot.IResponse<MMBot.TextMessage> msg)
        {

        }

        private void JenkinsDescribe(MMBot.IResponse<MMBot.TextMessage> msg)
        {
            throw new NotImplementedException();
        }

        private async Task JenkinsList(MMBot.Robot robot, MMBot.IResponse<MMBot.TextMessage> msg)
        {
            robot.Logger.Debug("JenkinsList called");
            var content = await GetList();

            if (content == null || content.jobs.Count == 0)
            {
                await WriteLine("Listing failed or no jobs exist. Please check the logs.", msg);
                return;
            }

            await WriteLine("{0,-20}{1,-20}", msg, "Name", "Status");

            foreach (var job in content.jobs)
            {
                await WriteLine("{0,-20}{1,-20}", msg, job.name, GetJobStatus(job.color));
            }
        }

        private async Task<ListResponse> GetList()
        {
            var content = await Get<ListResponse>("api/json");
            return content;
        }

        private string GetJobStatus(string color)
        {
            if (color == "red")
                return "FAIL";
            else if (color == "aborted")
                return "ABORTED";
            else if (color == "aborted_anime")
                return "CURRENTLY RUNNING";
            else if (color == "red_anime")
                return "CURRENTLY RUNNING";
            else if (color == "blue_anime")
                return "CURRENTLY RUNNING";
            else if (color == "disabled")
                return "DISABLED";
            else
                return "PASS";
        }

        private void JenkinsBuildById(MMBot.IResponse<MMBot.TextMessage> msg)
        {

        }

        private async Task JenkinsBuild(MMBot.Robot robot, MMBot.IResponse<MMBot.TextMessage> msg)
        {
            var jobName = msg == null ? "Test.Deploy" : msg.Match[0];

            ListResponse list = await GetList();
            if (!list.jobs.Any(x => x.name.Equals(jobName, StringComparison.InvariantCultureIgnoreCase)))
            {
                await WriteLine("No jobs found with the name {0}. Please use 'veyobot jenkins list' to list the existing jobs.");
                return;
            }

            var parameters = HttpUtility.UrlEncode("Are you sure?=yup ") + "&" + HttpUtility.UrlEncode("PublishRoles=All");
            var response = await Post<object>(String.Format("job/{0}/buildWithParameters?token={1}&{2}", jobName, buildToken, parameters));

            if (response != "")
            {
                await WriteLine("Jenkins returned an unexpected response. Please check the jenkins interface to see the status of the job.");
                return;
            }

            await WriteLine("Job submission sucessful. Build status messages will be sent to the #build slack channel.");
        }

        private async Task<T> Get<T>(string requestUri)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                                                               Convert.ToBase64String(
                                                                                                                      Encoding.ASCII.GetBytes(String.Format(
                                                                                                                                              "{0}:{1}", userName, apiKey))));
                    client.BaseAddress = new Uri(baseUrl);
                    var response = await (client.GetAsync(requestUri).ConfigureAwait(false));
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(responseString);
                }
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }

        private async Task WriteLine(String message, MMBot.IResponse<MMBot.TextMessage> msg = null, params object[] args)
        {
            if (msg == null)
            {
                Console.WriteLine(message, args);
            }
            else
            {
                await msg.SendFormat(message, args);
            }
        }

        private async Task<String> Post<T>(string requestUri)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response;
                    //client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                    //client.DefaultRequestHeaders.Add("Content-Length", "0");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                                                                   Convert.ToBase64String(
                                                                                                                          Encoding.ASCII.GetBytes(String.Format(
                                                                                                                                                  "{0}:{1}", userName, apiKey))));
                    client.BaseAddress = new Uri(baseUrl);
                    response = await (client.PostAsync(requestUri, null).ConfigureAwait(false));
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return String.Format("Post failed - {0} {1}", ex.Message, ex.StackTrace);
            }
        }
    }
}