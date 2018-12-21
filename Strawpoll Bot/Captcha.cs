using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Strawpoll_Bot
{
    class Captcha
    {
        private static readonly HttpClient client = new HttpClient();
        private static Random random = new Random();
        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task<string> GetSolvedCaptcha(string captchakey, string id)
        {
            var newrequesturl = "http://2captcha.com/res.php?key=" + captchakey + "&action=get&id=" + id;
            var newresponse = await client.GetAsync(newrequesturl);
            string newresponseString = await newresponse.Content.ReadAsStringAsync();
            //Console.WriteLine("Response String: " + newresponseString);
            if (newresponseString == "CAPCHA_NOT_READY")
            {
                await Task.Delay(5000);
                return await GetSolvedCaptcha(captchakey, id);
            }
            else
            {
                string responsestring = newresponseString.Split('|')[1];
                return responsestring;
            }
        }

        public async Task<string> SolveCaptcha(string captchakey, string sitekey, string currenturl)
        {
            var requesturl = "http://2captcha.com/in.php?key=" + captchakey + "&method=userrecaptcha&googlekey=" + sitekey + "&invisible=1&pageurl=" + currenturl;
            var response = await client.GetAsync(requesturl);
            string responseString = await response.Content.ReadAsStringAsync();
            Thread.Sleep(15000);
            string solvedcaptchaid = responseString.Split('|')[1];
            string solvedresponse = await GetSolvedCaptcha(captchakey, solvedcaptchaid);
            return solvedresponse;
        }
    }
}
