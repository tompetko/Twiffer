using System.Linq;
using Twiffer.Models;

namespace Twiffer.Serializers
{
    public interface ITweetSerializer
    {
        string Serialize(Tweet tweet);
    }

    internal sealed class TweetCsvSerializer : ITweetSerializer
    {
        public string Serialize(Tweet tweet)
        {
            if (tweet == null)
                throw new System.ArgumentNullException("tweet");

            var accountName = tweet.AccountName ?? string.Empty;
            var date = tweet.Date.ToString();
            var message = tweet.Message ?? string.Empty;

            var values = new string[] { accountName, date, message };
            var result = string.Join(";", values.Select(CSVHelper.Escape));

            return result;
        }
    }
}
