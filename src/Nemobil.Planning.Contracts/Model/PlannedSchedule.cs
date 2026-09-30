namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Der Fahrplan eines Fahrzeugs, wie er nach Annahme des Vorschlags gilt —
    /// vollständig, nicht als Delta.</summary>
    public sealed class PlannedSchedule
    {
        public required string VehicleScheduleId { get; init; }
        public IReadOnlyList<PlannedStop> Stops { get; init; } = [];
    }
}
