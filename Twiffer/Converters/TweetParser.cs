using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Twiffer.Extensions;
using Twiffer.Models;

namespace Twiffer.Converters
{
    public interface ITweetParser
    {
        Tweet[] Parse(TwitterResponse response);
    }

    internal sealed class TweetParser : ITweetParser
    {
        public Tweet[] Parse(TwitterResponse response)
        {
            if (response == null)
                throw new ArgumentNullException("response");

            var tweets = new List<Tweet>();
            var document = new HtmlDocument();
            var html = response.ItemsHtml;

            document.LoadHtml(html);

            var collection = document.DocumentNode
                .Descendants("li")
                .Where(d => d.Attributes.Contains("data-item-type"))
                .Where(d => d.Attributes["data-item-type"].Value == "tweet");

            foreach (var link in collection)
            {
                var tweetId = link.SelectSingleNode(".//div[contains(@class,'tweet')]")
                    .Attributes["data-tweet-id"]
                    .Value;
                    
                var accountName = link
                    .SelectSingleNode(".//span[contains(@class,'username')]")
                    .SelectSingleNode(".//b")
                    .InnerText;

                var message = link
                    .SelectSingleNode(".//p[contains(@class,'tweet-text')]")
                    .InnerText;

                var dateValue = link.SelectSingleNode(".//span[contains(@class,'_timestamp')]")
                    .Attributes["data-time"]
                    .Value;

                var date = long.Parse(dateValue).ToDateTimeFromEpoch();

                message = HttpUtility.HtmlDecode(message);

                var tweet = new Tweet()
                {
                    TweetId = tweetId,
                    AccountName = accountName,
                    Message = message,
                    Date = date
                };

                tweets.Add(tweet);
            }

            var result = tweets.ToArray();

            return result;
        }
    }
}
