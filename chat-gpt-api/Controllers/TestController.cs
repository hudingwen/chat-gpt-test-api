using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3;
using System.Collections.Generic;
using System.Net.Http.Headers;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Reflection.Emit;

namespace chat_gpt_api.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IMemoryCache memoryCache;

        public TestController(IConfiguration _configuration, IMemoryCache _memoryCache)
        {
            try
            {
                configuration = _configuration;
                memoryCache = _memoryCache;
                qq = configuration.GetValue<string>("qq").ToString();//你的QQ机器人
                OPENAPI_TOKEN = configuration.GetValue<string>("key").ToString();//输入自己的api-key
                sendUrl = configuration.GetValue<string>("url").ToString();//QQ推送地址
                token = configuration.GetValue<string>("token").ToString();//QQ推送地址token

                temperature = configuration.GetValue<float>("temperature");//准确度
                top = configuration.GetValue<float>("top");//情绪
                maxToken = configuration.GetValue<int>("maxToken");//最大消耗

                //QQ关键词监控

                monitorKey = configuration.GetValue<string>("monitorKey").ToString();//监控关键词
                monitorGroup = configuration.GetValue<string>("monitorGroup").ToString();//监控关键词
                wechatPushUrl = configuration.GetValue<string>("wechatPushUrl").ToString();//微信推送地址
                wechatTemplate = configuration.GetValue<string>("wechatTemplate").ToString();//微信推送模板ID
                wechatAccountID = configuration.GetValue<string>("wechatAccountID").ToString();//微信推送Code
                wechatCompanyCode = configuration.GetValue<string>("wechatCompanyCode").ToString();//微信推送Code
                wechatUserID = configuration.GetValue<string>("wechatUserID").ToString();//微信推送用户ID
                wechatQQurl = configuration.GetValue<string>("wechatQQurl").ToString();//微信QQ推送地址
                

            }
            catch (Exception ex)
            {
                Console.WriteLine("初始化系统失败");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private string qq { get; set; }
        private string OPENAPI_TOKEN { get; set; }
        private string sendUrl { get; set; }

        private string token { get; set; }
        private float temperature { get; set; }
        private float top { get; set; }
        private int maxToken { get; set; }


        private string monitorKey { get; set; }
        private string monitorGroup { get; set; }
        private string wechatPushUrl { get; set; }
        private string wechatTemplate { get; set; }
        private string wechatAccountID { get; set; }
        private string wechatCompanyCode { get; set; }
        private string wechatUserID { get; set; }
        private string wechatQQurl { get; set; }
        




        private Info userInfo { get; set; }
        /// <summary>
        /// 测试接口
        /// </summary>
        /// <returns></returns>
        [Route("test")]
        [HttpPost]
        public async void Test([FromBody] Info info)
        {
            try
            {
                userInfo = info;
                if ("group" == info.message_type)
                {
                    var flag = $"[CQ:at,qq={qq}]";
                    bool isCache = false;
                    if (info.raw_message.IndexOf(flag) >= 0 && !memoryCache.TryGetValue<bool>(info.message_id, out isCache))
                    {
                        memoryCache.Set(info.message_id, true);
                        Console.WriteLine("进入chatgpt");
                        Console.WriteLine(info.message);
                        var ls = info.raw_message.Split(new string[] { flag, " " }, StringSplitOptions.RemoveEmptyEntries);
                        string pro = string.Join(" ", ls);
                        Console.WriteLine(pro);

                        var chatHttp = HttpClientFactory.Create();
                        chatHttp.Timeout = TimeSpan.FromSeconds(1000); //设置超时1000秒
                        OpenAIService service = new OpenAIService(new OpenAiOptions() { ApiKey = OPENAPI_TOKEN }, chatHttp);

                        CompletionCreateRequest createRequest = new CompletionCreateRequest()
                        {
                            Prompt = pro,
                            Temperature = temperature,
                            TopP = top,
                            MaxTokens = maxToken,
                        };
                        CompletionCreateResponse res;
                        try
                        {
                            info.isOk = false;
                            Task.Run(() =>
                            {
                                CheckTask(pro);
                            });
                            res = await service.Completions.CreateCompletion(createRequest, Models.TextDavinciV3);
                        }
                        catch (Exception)
                        {
                            res = new CompletionCreateResponse();
                            res.Error = new Error();
                            res.Error.Type = "server_error";
                        }
                        finally
                        {
                            info.isOk = true;
                        }
                        var requestJson = string.Empty;
                        var sendMsg = string.Empty;
                        if (res.Successful)
                        {
                            sendMsg = res.Choices?.FirstOrDefault()?.Text;
                            if (string.IsNullOrWhiteSpace(sendMsg) && sendMsg.IndexOf("\n\n") == 0)
                            {
                                sendMsg = sendMsg.Substring(2, sendMsg.Length - 2);
                            }

                        }
                        else
                        {
                            //查询失败 
                            if ("server_error".Equals(res.Error.Type))
                            {
                                sendMsg = "ChatGPT服务器繁忙,请稍后再试!";
                            }
                            else
                            {
                                sendMsg = $"ChatGPT服务器繁忙,Type:{res.Error.Type},Code:{res.Error.Code},Message:{res.Error.Message}";
                            }

                        }
                        Console.WriteLine(sendMsg);
                        //推送QQ消息
                        Task.Run(() =>
                        {
                            SendQQMessage(sendMsg);
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("系统异常");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                userInfo.isOk = true;
            }

        }

        [Route("monitor")]
        [HttpPost]
        public async void Monitor([FromBody] Info info)
        {
            try
            {
                userInfo = info;
                if (!"group".Equals(info.message_type))
                    return;

                if (!monitorGroup.Split(",").ToList().Contains(info.group_id.ToString()))
                    return;

                var ls = monitorKey.Split(",").ToList();
                foreach (var item in ls)
                {
                    if (info.message.Contains(item))
                    {
                        Console.WriteLine(info.message);
                        //推送卡片消息
                        var groupInfo = await GetGroupInfo();
                        var groupUserinfo = await GetGroupUserInfo();

                        CardInfo cardInfo = new CardInfo();
                        cardInfo.info = new UserMsg() { id = wechatAccountID, companyCode = wechatCompanyCode, userID = wechatUserID };
                        cardInfo.cardMsg = new CardMsg() { template_id = wechatTemplate };
                        cardInfo.cardMsg.first = $"监控到群消息\n消息内容如下\n{info.message}\n来自群:{groupInfo.group_name}({groupInfo.group_id})\n来自人:{groupUserinfo.card}({groupUserinfo.user_id})";
                        var sendJson = Newtonsoft.Json.JsonConvert.SerializeObject(cardInfo);
                        using (HttpContent httpContent = new StringContent(sendJson))
                        {
                            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                            using var httpClient = new HttpClient();
                            var res = await httpClient.PostAsync(wechatPushUrl, httpContent).Result.Content.ReadAsStringAsync();
                            Console.WriteLine(res);
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("系统异常");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        /// <summary>
        /// 获取群信息
        /// </summary>
        private async Task<GroupInfo> GetGroupInfo()
        {
            string result = string.Empty;
            var sendJson = Newtonsoft.Json.JsonConvert.SerializeObject(userInfo);
            using (HttpContent httpContent = new StringContent(sendJson))
            {
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", token);
                //httpClient.Timeout = TimeSpan.FromSeconds(60);
                result = await httpClient.PostAsync(wechatQQurl + "/get_group_info", httpContent).Result.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<GroupInfo>(result);
            } 
        }
        /// <summary>
        /// 获取群成员信息
        /// </summary>
        private async Task<GroupUserInfo> GetGroupUserInfo()
        {
            string result = string.Empty;
            var sendJson = Newtonsoft.Json.JsonConvert.SerializeObject(userInfo);
            using (HttpContent httpContent = new StringContent(sendJson))
            {
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", token);
                //httpClient.Timeout = TimeSpan.FromSeconds(60);
                result = await httpClient.PostAsync(wechatQQurl + "/get_group_member_info", httpContent).Result.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<GroupUserInfo>(result);
            }  
        }

        private async Task<string> SendQQMessage(string sendMsg)
        {
            string requestJson;
            GrouInfo sendObj = new GrouInfo();
            sendObj.auto_escape = false;
            var tempMsg = $"[CQ:at,qq={userInfo.user_id}] \n{sendMsg}";
            sendObj.message = tempMsg;
            sendObj.group_id = userInfo.group_id;
            requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(sendObj);

            string result = string.Empty;
            using (HttpContent httpContent = new StringContent(requestJson))
            {
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", token);
                //httpClient.Timeout = TimeSpan.FromSeconds(60);
                result = await httpClient.PostAsync(sendUrl + "/send_group_msg", httpContent).Result.Content.ReadAsStringAsync();
            }
            Console.WriteLine(result);
            return result;
        }

        private async Task<bool> CheckTask(string pro)
        {

            DateTime startTime = DateTime.Now;
            while (!userInfo.isOk)
            {

                if (Convert.ToInt32(Math.Ceiling((DateTime.Now - startTime).TotalSeconds)) % 30 == 0)
                {
                    //每过30发出一个等待提示
                    await SendQQMessage($"您要询问的【{pro}】正在思考,请耐心等待一下!");
                }
                Thread.Sleep(1000);

            }
            return true;
        }
    }

    public class GroupInfo
    {
        public Int64 group_id;
        public string group_name;
        public string group_memo;
        public UInt32 group_create_time;
        public UInt32 group_level;
        public Int32 member_count;
        public Int32 max_member_count;
    }
    public class GroupUserInfo
    {
        public Int64 group_id;
        public Int64 user_id;
        public string nickname;
        public string card;
        public string sex;
        public Int32 age;
        public string area;
        public Int32 join_time;
        public Int32 last_sent_time;
        public string level;
        public string role;
        public bool unfriendly;
        public string title;
        public Int64 title_expire_time;
        public bool card_changeable;
        public Int64 shut_up_timestamp;
    }
    public class CardInfo
    {
        public UserMsg info;
        public CardMsg cardMsg;
    }
    public class UserMsg
    {
        public string id;
        public string companyCode;
        public string userID;
    }
    public class CardMsg
    {
        public string template_id;
        public string first;
        public string colorFirst;
        public string keyword1;
        public string color1;
        public string keyword2;
        public string color2;
        public string keyword3;
        public string color3;
        public string keyword4;
        public string color4;
        public string keyword5;
        public string color5;
        public string remark;
        public string colorRemark;
        public string url;
    }

    public class GrouInfo
    {
        public int group_id { get; set; }
        public string message { get; set; }
        public bool auto_escape { get; set; } = false;
    }
    public class Info
    {
        public int time { get; set; }
        public int self_id { get; set; }
        public string post_type { get; set; }
        public string message_type { get; set; }
        public string sub_type { get; set; }
        public int message_id { get; set; }
        public int user_id { get; set; }
        public string message { get; set; }
        public string raw_message { get; set; }
        public int font { get; set; }
        public int group_id { get; set; }
        public bool isOk { get; set; }
    }

}
