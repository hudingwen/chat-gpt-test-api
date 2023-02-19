// See https://aka.ms/new-console-template for more information


//RegHelper regHelper = new RegHelper();
//regHelper.Start();

//抢红包

//using System.Text.RegularExpressions;

//var matchStr = "\\[CQ:redbag.+?\\]"; ;

//var targetStr = "[CQ:redbag,title=大吉大利]";

//Regex regex = new Regex(matchStr);
//Match match = regex.Match(targetStr);

//if (match.Success)
//{
//    Console.WriteLine("匹配成功");
//}
//else
//{
//    Console.WriteLine("匹配失败");
//}

//var tstr = "??????\nq\nw\n?\ne\n1231123\ntttt";
//var pstr = "^(\\?|\n|\\n|\\\n|\\\\n|q).+?";
//RegHelper regHelper = new RegHelper();
//var res = regHelper.Start(tstr, pstr, "");

var tstr = "??????\nq\nw\n?\ne\n1231123\ntttt";
tstr =  RegHelper.ReplaceStartWith(tstr, '?');
tstr = RegHelper.ReplaceStartWith(tstr, '\n');
Console.WriteLine(tstr);


//var tstr = "???\nwwww?q?q?1231123\ntttt";
//var pstr = "^\\?+?";
//RegHelper regHelper = new RegHelper();
//var res = regHelper.Start(tstr, pstr, "");
//Console.WriteLine(res);
