using Newtonsoft.Json;

namespace Twiffer.Models
{
    public sealed class TwitterResponse
    {
        [JsonProperty("min_position")]
        public string MinPosition { get; set; }

        [JsonProperty("has_more_items")]
        public bool HasMoreItems { get; set; }

        [JsonProperty("items_html")]
        public string ItemsHtml { get; set; }

        [JsonProperty("new_latent_count")]
        public string NewLatentCount { get; set; }

        [JsonProperty("focused_refresh_interval")]
        public string FocusedRefreshInterval { get; set; }
    }
}
