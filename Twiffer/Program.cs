using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Twiffer.Extensions;
using Twiffer.Models;

namespace Twiffer
{
    internal class Context
    {
        public string Query { get; private set; }
        public CancellationToken CancellationToken
        {
            get { return this.cancellationTokenSource.Token; }
        }

        private readonly CancellationTokenSource cancellationTokenSource;

        public Context(string q)
        {
            this.Query = q;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var twiffer = new TwifferClient();
            var accountNames = Settings.Default.SearchAccounts.ToArray();

            var searchOpts = new SearchOptions()
            {
                Since = Settings.Default.SearchSince,
                Until = Settings.Default.SearchUntil,
                Accounts = accountNames
            };

            var fileName = Settings.Default.OutputFileName;
            var progress = new Progress<int>(OnProgressUpdated);
            var cts = new CancellationTokenSource();

            //var fileStream = File.CreateText(fileName);
            var fileStream = new StreamWriter(
                new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write),
                Encoding.UTF8);

            ThreadStart action = () =>
                twiffer.ExportTweets(searchOpts, fileStream, progress, cts.Token);

            Thread work = new Thread(action);
            ConsoleCancelEventHandler consoleCancelHandler = null;

            Console.WriteLine("Search since: {0}", Settings.Default.SearchSince);
            Console.WriteLine("Search until: {0}", Settings.Default.SearchUntil);
            Console.WriteLine("Search in:    {0}", string.Join(", ", accountNames));

            Console.WriteLine("\nPress Enter to start or Esc to terminate.");
            
            var key = BlockTillKeyPress(ConsoleKey.Escape, ConsoleKey.Enter);
            if (key == ConsoleKey.Enter)
            {
                consoleCancelHandler = (o, e) =>
                {
                    e.Cancel = true;

                    if (work.IsAlive)
                        cts.Cancel();

                    fileStream.Dispose();
                    fileStream = File.CreateText(fileName);

                    Console.WriteLine("\nCancelling...");

                    Console.CancelKeyPress -= consoleCancelHandler;
                };

                Console.CancelKeyPress += consoleCancelHandler;

                Console.WriteLine("\nLoading...");

                work.Start();
                work.Join();
                
                Console.WriteLine("\nPress Enter to quit.");
                BlockTillKeyPress(ConsoleKey.Enter);
            }

            fileStream.Dispose();
        }

        private static void OnProgressUpdated(int tweetsCount)
        {
            Console.Write("\rWritten {0,6} tweets.", tweetsCount);
        }

        private static ConsoleKey BlockTillKeyPress(params ConsoleKey[] keys)
        {
            ConsoleKeyInfo k;

            do
            {
                k = Console.ReadKey();
            }
            while (!keys.Contains(k.Key));

            return k.Key;
        }

        /*static void Main(string[] args)
        {
            var consumerKey = Settings.Default.ConsumerKey;
            var consumerSecret = Settings.Default.ConsumerSecret;
            var userAccessToken = Settings.Default.UserAccessToken;
            var accessTokenSecret = Settings.Default.AccessTokenSecret;

            var searchSince = Settings.Default.SearchSince;
            var searchUntil = Settings.Default.SearchUntil;
            var searchKeywords = Settings.Default.SearchKeywords;
            var searchAccountNames = Settings.Default.SearchAccounts;
            //var searchAccounts = new List<IUser>();

            var exitSignal = new AutoResetEvent(false);

            // Set credentials
            Auth.SetUserCredentials(consumerKey, consumerSecret, userAccessToken, accessTokenSecret);
            
            // Get accounts ids
            foreach (var accountName in searchAccountNames)
            {
                var user = User.GetUserFromScreenName(accountName);
                searchAccounts.Add(user);
            }

            var result = new Dictionary<string, ITweet[]>();

            foreach (var accountName in searchAccountNames)
            {
                var timelineParams = new UserTimelineParameters()
                {
                    MaximumNumberOfTweetsToRetrieve = 200,
                    ExcludeReplies = true
                };

                timelineParams.AddCustomQueryParameter("since", "2014-02-15");
                timelineParams.AddCustomQueryParameter("until", "2014-03-30");

                var user = User.GetUserFromScreenName(accountName);

                var tweets = Timeline
                    .GetUserTimeline(accountName, timelineParams)
                    .ToArray();

                result[accountName] = tweets.ToArray();
            }
            
            // Search

            var searchTo = searchSince;
            var searchResult = new List<ITweet>();

            do
            {
                var searchParams = new TweetSearchParameters(searchQuery)
                {
                    MaximumNumberOfResults = int.MaxValue,
                    Since = searchSince,
                    Until = searchTo,
                    Filters = TweetSearchFilters.None
                };
            }
            while (searchTo <= searchUntil);

            var searchResult = Search.SearchTweets(searchParams);

            // Stream

            var stream = Stream.CreateFilteredStream();

            stream.AddCustomQueryParameter("since", "2014-02-15");
            stream.AddCustomQueryParameter("until", "2014-03-30");

            EventHandler<LimitReachedEventArgs> limitReachedHandler = null;
            EventHandler<MatchedTweetReceivedEventArgs> matchedTweetReceivedHandler = null;

            limitReachedHandler = (o, e) =>
                {
                    Console.WriteLine("Rate limit reached.");
                    stream.LimitReached -= limitReachedHandler;
                };

            matchedTweetReceivedHandler = (o, e) =>
                {
                };

            stream.NonMatchingTweetReceived += (o, e) =>
                {

                };

            stream.StreamStarted += (o, e) =>
                {

                };

            stream.LimitReached += limitReachedHandler;
            stream.MatchingTweetReceived += matchedTweetReceivedHandler;

            foreach (var keyword in searchKeywords)
                stream.AddTrack(keyword);

            foreach (var account in searchAccounts)
                stream.AddFollow(account);

            TaskEx.Run(() => stream.StartStreamMatchingAllConditionsAsync());
            
            exitSignal.WaitOne();
        }*/
    }
}
