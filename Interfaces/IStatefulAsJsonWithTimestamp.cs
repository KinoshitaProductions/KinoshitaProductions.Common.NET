namespace KinoshitaProductions.Common.Interfaces;

public interface IStatefulAsJsonWithTimestamp : IStatefulAsJson
{
    DateTime Timestamp { get; set; } // used to track when was last updated or since when it's valid
}
