using System;

namespace Twiffer.Models
{
    public sealed class Tweet
    {
        public string TweetId { get; set; }
        public string AccountName { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }
}
