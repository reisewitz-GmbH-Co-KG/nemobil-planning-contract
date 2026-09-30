using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Reference
{
    /// <summary>Einfaches Streckenmodell ohne Kartendaten: Luftlinie (Großkreis) mal Umwegfaktor, bei
    /// konstanter Durchschnittsgeschwindigkeit. Genügt für Tests und als Ausgangspunkt; für einen
    /// Betrieb gehört an diese Stelle ein Routing-Dienst.</summary>
    public sealed class StraightLineTravelModel : ITravelModel
    {
        private readonly double _metersPerSecond;
        private readonly double _detourFactor;

        /// <summary>Erzeugt das Modell.</summary>
        /// <param name="speedKmh">Durchschnittsgeschwindigkeit in km/h (größer 0).</param>
        /// <param name="detourFactor">Verhältnis Fahrstrecke zu Luftlinie (mindestens 1).</param>
        public StraightLineTravelModel(double speedKmh = 30, double detourFactor = 1.3)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speedKmh);
            ArgumentOutOfRangeException.ThrowIfLessThan(detourFactor, 1.0);

            _metersPerSecond = speedKmh / 3.6;
            _detourFactor = detourFactor;
        }

        /// <inheritdoc/>
        public TravelEstimate Estimate(GeoPoint from, GeoPoint to)
        {
            var meters = GeoMath.DistanceMeters(from, to) * _detourFactor;

            return new TravelEstimate(
                (int)Math.Round(meters, MidpointRounding.AwayFromZero),
                (int)Math.Ceiling(meters / _metersPerSecond));
        }
    }

    /// <summary>Geometrische Hilfsfunktionen auf WGS84-Koordinaten.</summary>
    public static class GeoMath
    {
        private const double EarthRadiusMeters = 6_371_008.8;

        /// <summary>Großkreis-Entfernung zweier Punkte in Metern (Haversine-Formel).</summary>
        public static double DistanceMeters(GeoPoint a, GeoPoint b)
        {
            var lat1 = DegreesToRadians(a.Lat);
            var lat2 = DegreesToRadians(b.Lat);
            var dLat = lat2 - lat1;
            var dLon = DegreesToRadians(b.Lon - a.Lon);

            var h = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
                    + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

            return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
