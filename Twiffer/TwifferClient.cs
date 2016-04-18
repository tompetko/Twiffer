using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Twiffer.Converters;
using Twiffer.Extensions;
using Twiffer.Serializers;

namespace Twiffer.Models
{
    public sealed class SearchOptions
    {
        public DateTime Since { get; set; }
        public DateTime Until { get; set; }
        public string[] Accounts { get; set; }
        // string[] Keywords { get; set; }
    }

    public sealed class TwifferClient
    {
        private const string TimelineUrl = "https://twitter.com/i/search/timeline?";
        private const string DateFormat = "yyyy-MM-dd";

        private readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
        private ITweetSerializer tweetSerializer = new TweetCsvSerializer();
        private ITweetParser tweetParser = new TweetParser();

        public TwifferClient() { }

        public ITweetSerializer Serializer
        {
            get { return this.tweetSerializer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                this.tweetSerializer = value;
            }
        }

        public ITweetParser Parser
        {
            get { return this.tweetParser; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                this.tweetParser = value;
            }
        }

        public JsonSerializerSettings JsonSerializerSettings
        {
            get { return this.jsonSerializerSettings; }
        }

        public void ExportTweets(SearchOptions opts, StreamWriter writer, IProgress<int> progress, CancellationToken token)
        {
            if (opts == null)
                throw new ArgumentNullException("opts");

            if (writer == null)
                throw new ArgumentNullException("writer");

            if (progress == null)
                throw new ArgumentNullException("progress");

            var cursor = "";
            var lastCursor = "";
            var tweetsCount = 0;
            var hasMoreItems = false;
            var searchQuery = this.CreateSearchQuery(opts);

            do
            {
                if (token.IsCancellationRequested)
                    break;
                
                var uri = CreateUri(searchQuery, cursor);
                var webRequest = WebRequest.CreateHttp(uri);
                var jsonSerializerSettings = this.JsonSerializerSettings;
                var tweetSerializer = this.Serializer;
                var tweetParser = this.Parser;

                using (var response = webRequest.GetResponse())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var json = streamReader.ReadToEnd();
                        var twitterResponse = JsonConvert.DeserializeObject<TwitterResponse>(json, jsonSerializerSettings);
                        var tweets = tweetParser.Parse(twitterResponse);

                        //hasMoreItems = twitterResponse.HasMoreItems;
                        cursor = twitterResponse.MinPosition;
                        hasMoreItems = cursor != lastCursor;

                        foreach (var tweet in tweets)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            var tweetData = tweetSerializer.Serialize(tweet);
                            
                            tweetsCount++;

                            writer.WriteLine(tweetData);
                            progress.Report(tweetsCount);
                        }
                    }
                }

                lastCursor = cursor;
            }
            while (hasMoreItems);

            writer.Dispose();
        }

        private string CreateSearchQuery(SearchOptions opts)
        {
            if (opts == null)
                throw new ArgumentNullException("opts");

            var searchSince = opts.Since.ToString(DateFormat);
            var searchUntil = opts.Until.ToString(DateFormat);
            var accounts = opts.Accounts;

            var qBuilder = new StringBuilder();
            
            if (accounts != null)
            {
                var from = opts.Accounts
                    .Cast<string>()
                    .Select(accountName => string.Format("from:{0}", accountName))
                    .Aggregate((i, j) => i + ", OR " + j);

                qBuilder.Append(from);
            }

            qBuilder.Append(" since:" + searchSince);
            qBuilder.Append(" until:" + searchUntil);

            var q = qBuilder.ToString();

            return q;
        }

        private Uri CreateUri(string searchQuery, string cursor)
        {
            var query = new NameValueCollection();
            var uriBuilder = new UriBuilder(TimelineUrl);

            query.Add("include_available_feature", "1");
            query.Add("include_entities", "1");
            query.Add("max_position", cursor);
            query.Add("q", searchQuery);
            query.Add("src", "typd");

            uriBuilder.Query = query.ToQueryString();

            return uriBuilder.Uri;
        }
    }
}
