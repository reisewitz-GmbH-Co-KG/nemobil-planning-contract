namespace Nemobil.Planning.Reference
{
    /// <summary>Einstellungen der Referenz-Implementierung.</summary>
    public sealed class ReferenceEngineOptions
    {
        /// <summary>Anzahl Vorschläge, wenn die Anfrage keine Obergrenze nennt
        /// (<c>PlanningRequest.MaxProposals</c> ≤ 0).</summary>
        public int DefaultMaxProposals { get; init; } = 3;

        /// <summary>Energieverbrauch in Wh je Meter für Fahrzeuge, die keinen eigenen Wert führen
        /// (<c>VehicleState.ConsumptionWhPerMeter</c> = 0).</summary>
        public double DefaultConsumptionWhPerMeter { get; init; } = 0.15;
    }
}
