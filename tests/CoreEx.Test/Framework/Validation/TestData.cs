using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Framework.Validation
{
    public class TestDataBase
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class TestData : TestDataBase
    {
        [JsonPropertyName("datefrom")]
        public DateTime DateA { get; set; }

        [JsonPropertyName("dateto")]
        public DateTime? DateB { get; set; }

        public int CountA { get; set; }

        public int? CountB { get; set; }

        public decimal AmountA { get; set; }

        public decimal? AmountB { get; set; }

        public double DoubleA { get; set; }

        public double? DoubleB { get; set; }

        public bool SwitchA { get; set; }

        public bool? SwitchB { get; set; }

        public List<int>? Vals { get; set; }
    }
}