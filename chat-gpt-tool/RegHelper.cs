using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


public class RegHelper
{
    public static string ReplaceStartWith(string tStr, char pStr)
    {
        if (tStr.StartsWith(pStr))
        {
            tStr =  tStr.Substring(1, tStr.Length - 1);
            if (tStr.StartsWith(pStr))
            {
                return ReplaceStartWith(tStr, pStr);
            }
            else
            {
                return tStr;
            }
        }
        else
        {
            return tStr;
        }
    }

    public string Start(string tStr,string pStr= "\\[CQ:.+?\\]",string rStr=" ")
    {
        string nStr = ReplaceMatchingStr(tStr, pStr, rStr, true);  
        return nStr;
    }

    public string ReplaceMatchingStr(string targetStr, string patternStr, string replaceStr, bool isRecursion = true)
    {
        //targetStr: 待匹配字符串
        //patternStr: 正则表达式
        //isRecursion: 是否递归(查找所有/第一个符合表达式的字符串)

        //匹配表达式
        Regex regex = new Regex(patternStr);
        Match match = regex.Match(targetStr);

        //匹配结果
        return ReplaceMatchingStr(targetStr, match, replaceStr, isRecursion);
    }

    string ReplaceMatchingStr(string targetStr, Match match, string replaceStr, bool isRecursion)
    {
        //是否匹配成功
        if (match.Success)
        {
            //处理字符串
            targetStr = ReplaceStr(targetStr, match, replaceStr);

            //是否递归匹配
            if (isRecursion)
                targetStr = ReplaceMatchingStr(targetStr, match.NextMatch(), replaceStr, true);
        }
        return targetStr;
    }

    string ReplaceStr(string targetStr, Match match, string replaceStr)
    {
        //替换字符
        string newStr = targetStr.Replace(match.ToString(), replaceStr);

        //匹配结果开始字符下标
        //Debug.Log(match.Index);
        //匹配结果字符串长度
        //Debug.Log(match.Length);

        //Debug.Log(targetStr);
        //Debug.Log(newStr);

        return newStr;
    }
}
