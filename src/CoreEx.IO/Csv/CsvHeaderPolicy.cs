namespace CoreEx.IO.Csv;

/// <summary>Defines how named CSV mappings validate the column-name header.</summary>
public enum CsvHeaderPolicy
{
    /// <summary>Require mapped column names and allow additional header columns.</summary>
    AllowAdditionalColumns,

    /// <summary>Require the header to contain exactly the mapped column names, ignoring case and order.</summary>
    Exact
}
