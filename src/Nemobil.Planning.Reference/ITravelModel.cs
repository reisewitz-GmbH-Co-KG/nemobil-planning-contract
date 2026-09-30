using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Reference
{
    /// <summary>Streckenmodell der Referenz-Implementierung: schätzt Strecke und Fahrzeit zwischen zwei
    /// Punkten. Austauschbar, z. B. gegen einen Routing-Dienst mit echtem Straßennetz.</summary>
    public interface ITravelModel
    {
        /// <summary>Schätzt Strecke und Fahrzeit von <paramref name="from"/> nach <paramref name="to"/>.</summary>
        TravelEstimate Estimate(GeoPoint from, GeoPoint to);
    }

    /// <summary>Ergebnis einer Streckenschätzung.</summary>
    /// <param name="DistanceMeters">Fahrstrecke in Metern.</param>
    /// <param name="DrivingSeconds">Fahrzeit in Sekunden.</param>
    public readonly record struct TravelEstimate(int DistanceMeters, int DrivingSeconds);
}
