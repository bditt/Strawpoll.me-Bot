using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Strawpoll_Bot
{
    class Program
    {
        public List<string> proxylist;
        public int totalamountofproxies = 0;
        public int maxvoteamount = 0;
        public string voteid = "";
        public string votechoiceid = "";
        private Random random = new Random();
        private Captcha captcha = new Captcha();

        public async Task GetProxyList()
        {
            if (File.Exists("Proxies.txt"))
            {
                var alllines = File.ReadAllLines("Proxies.txt");
                proxylist = new List<string>(alllines.ToList<string>());
                foreach (var proxy in proxylist)
                {
                    totalamountofproxies++;
                }
            }
            else if (!File.Exists("Proxies.txt"))
            {
                File.Create("Proxies.txt");
            }
        }

        public async Task VoteRequest(string pollid, string option, string capkey, bool capprotected, int maxvoteamount, int currentvoteamount)
        {
            if (currentvoteamount <= maxvoteamount)
            {
                try
                {
                    string pollurl = "https://www.strawpoll.me/" + pollid;
                    HttpClient voteclient;
                    string proxy = "N/A";
                    proxy = proxylist[random.Next(totalamountofproxies)];
                    HttpClientHandler handler = new HttpClientHandler()
                    {
                        Proxy = new WebProxy(proxy),
                        UseProxy = true,
                    };
                    voteclient = new HttpClient(handler);
                    HttpResponseMessage getpage = await voteclient.GetAsync(pollurl);
                    var pagecontent = await getpage.Content.ReadAsStringAsync();
                    string[] separatingChars = { "<input id=\"field-security-token\" name=\"security-token\" type=\"hidden\" value=\"" };
                    string[] separatingChars2 = { "\" /><input id=\"field-authenticity-token\" name=\"" };
                    string[] separatingChars3 = { "\" type=\"hidden\" value=\"\" />" };
                    string[] splitcontentbase1 = pagecontent.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);
                    string[] splitcontentbase2 = splitcontentbase1[1].Split(separatingChars2, System.StringSplitOptions.RemoveEmptyEntries);
                    string[] splitcontentbase3 = splitcontentbase2[1].Split(separatingChars3, System.StringSplitOptions.RemoveEmptyEntries);
                    string sectoken = splitcontentbase2[0];
                    string authtoken = splitcontentbase3[0];
                    if (capprotected)
                    {
                        string capresponse = await captcha.SolveCaptcha(capkey, "6LeG8gQTAAAAACp5SKFqo0OOUoNhvrkH41M8Mrfz", pollurl);
                        var formcontent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("security-token", sectoken),
                            new KeyValuePair<string, string>(authtoken, ""),
                            new KeyValuePair<string, string>("g-recaptcha-response", capresponse),
                            new KeyValuePair<string, string>("options", option)
                        });

                        if (currentvoteamount < maxvoteamount)
                        {
                            HttpResponseMessage responsemsg = await voteclient.PostAsync(pollurl, formcontent);
                            var content = await responsemsg.Content.ReadAsStringAsync();
                            var jcontent = JObject.Parse(content);
                            if (jcontent["success"].ToString() != "success")
                            {
                                voteclient.Dispose();
                                await VoteRequest(pollid, option, capkey, capprotected, maxvoteamount, currentvoteamount);
                            }
                            else
                            {
                                voteclient.Dispose();
                            }
                        }
                    }
                    else
                    {
                        var formcontent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("security-token", sectoken),
                            new KeyValuePair<string, string>(authtoken, ""),
                            new KeyValuePair<string, string>("options", option)
                        });

                        if (currentvoteamount < maxvoteamount)
                        {
                            HttpResponseMessage responsemsg = await voteclient.PostAsync(pollurl, formcontent);
                            var content = await responsemsg.Content.ReadAsStringAsync();
                            var jcontent = JObject.Parse(content);
                            if (jcontent["success"].ToString() != "success")
                            {
                                voteclient.Dispose();
                                await VoteRequest(pollid, option, capkey, capprotected, maxvoteamount, currentvoteamount);
                            }
                            else
                            {
                                voteclient.Dispose();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    await VoteRequest(pollid, option, capkey, capprotected, maxvoteamount, currentvoteamount);
                }
            }
        }

        public async Task ShowHeader(int currentamount, int maxamount)
        {
            Console.Clear();
            Console.WriteLine("==================================================");
            Console.WriteLine("Amount Voted: {0}/{1}", currentamount, maxamount);
            Console.WriteLine("==================================================");
        }

        public async Task StartVoteBot()
        {
            Console.WriteLine("What is the poll id: ");
            string pollid = Console.ReadLine();
            Console.WriteLine("What is the choice id: ");
            string choiceid = Console.ReadLine();
            Console.WriteLine("How many votes: ");
            int maxvoteamount = Convert.ToInt32(Console.ReadLine()) + 1;
            Console.WriteLine("How many threads: ");
            int threadamount = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Captcha protected (yes or no): ");
            string capprotectedanswer = Console.ReadLine();
            int amountvoted = 0;
            bool capprotect = false;
            string capkey = "";
            if (capprotectedanswer == "yes")
            {
                capprotect = true;
                Console.WriteLine("What is your 2Captcha Key: ");
                capkey = Console.ReadLine();
            }
            int votedamount = 0;
            List<Task> tasks = new List<Task>();
            await ShowHeader(amountvoted, maxvoteamount);
            for (int i = 0; i < threadamount; i++)
            {
                tasks.Add(Task.Run(() => VoteRequest(pollid, choiceid, capkey, capprotect, maxvoteamount, votedamount)));
                votedamount++;
            }
            while (votedamount < maxvoteamount)
            {
                Task FinishedTask = await Task.WhenAny(tasks);
                if (FinishedTask.IsCompleted)
                {
                    tasks.Remove(FinishedTask);
                    amountvoted++;
                    await ShowHeader(amountvoted, maxvoteamount);
                }
                if (tasks.Count < threadamount)
                {
                    tasks.Add(Task.Run(() => VoteRequest(pollid, choiceid, capkey, capprotect, maxvoteamount, votedamount)));
                    votedamount++;
                }
            }
            while (tasks.Count > 0)
            {
                Task FinishedTask = await Task.WhenAny(tasks);
                if (FinishedTask.IsCompleted)
                {
                    tasks.Remove(FinishedTask);
                    amountvoted++;
                    await ShowHeader(amountvoted, maxvoteamount);
                }
            }
            Console.WriteLine("Voting done!");
        }

        static void Main(string[] args)
        {
            Program tbot = new Program();
            tbot.GetProxyList().Wait();
            tbot.StartVoteBot().Wait();
            Thread.Sleep(1000000);
        }
    }
}
