#nullable disable
using System.Net;
using System.Text.RegularExpressions;
using TidyUtility.Core.Extensions;

namespace TidyData.Sync;

public class DBSyncHttpStatusErrorException : Exception, IExceptionWithShortMessage
{
    public DBSyncHttpStatusErrorException(HttpResponseMessage response)
        : base(InitAndReturnMessage(response, out string shortMessage, out string responseBody))
    {
        this.HttpStatusErrorCode = response.StatusCode;
        this.ShortMessage = shortMessage;
        this.ResponseBody = responseBody;
    }

    private static string InitAndReturnMessage(HttpResponseMessage response, out string shortMessage, out string responseBody)
    {
        string tempMessage = $"Http request failed with status code: {response.StatusCode}({(int)response.StatusCode}).";
        
        try { responseBody = response.Content.ReadAsStringAsync().Result; }
        catch (Exception) { responseBody = "<Unable to Read HTTP Response!>"; }

        string title = GetTitle(responseBody);
        string h1 = GetH1(responseBody);

        shortMessage = title == null
            ? h1 == null
                ? tempMessage
                : $"{tempMessage}\n\n\"{h1}\""
            : h1 == null
                ? $"{tempMessage}\n\n\"{title}\""
                : $"{tempMessage}\n\n\"{title}\"\n\n\"{h1}\"";

        return $"{tempMessage} Response Body: \"{responseBody}\"";
    }

    private static string GetTitle(string file)
    {
        try
        {
            Match match = Regex.Match(file, @"<title.*>\s*(.+?)\s*</title>");
            if (match.Success)
                return match.Groups[1].Value;
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    private static string GetH1(string file)
    {
        try
        {
            Match match = Regex.Match(file, @"<h1.*>\s*(.+?)\s*<\/h1>");
            if (match.Success)
                return match.Groups[1].Value;
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    public HttpStatusCode HttpStatusErrorCode { get; }
    public string ShortMessage { get; }
    public string ResponseBody { get; }
}
