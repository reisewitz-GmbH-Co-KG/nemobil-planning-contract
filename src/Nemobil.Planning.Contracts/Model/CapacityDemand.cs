namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Kapazitätsbedarf einer Anfrage: Anzahl Erwachsene, Kinder und Gepäckstücke.</summary>
    public sealed class CapacityDemand
    {
        public int Adults { get; init; }
        public int Children { get; init; }
        public int Luggage { get; init; }
    }
}
