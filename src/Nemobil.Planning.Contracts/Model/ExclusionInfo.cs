namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Neutraler Ausschlussgrund (Diagnose, kein interner Wert).</summary>
    public sealed class ExclusionInfo
    {
        public required string Reason { get; init; }
        public string? Detail { get; init; }
    }
}
