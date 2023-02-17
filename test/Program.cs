// See https://aka.ms/new-console-template for more information


var msg = await HttpHelper.SendHttpRequest("https://www.baidu.com/", "get", "", "");
Console.WriteLine(msg);