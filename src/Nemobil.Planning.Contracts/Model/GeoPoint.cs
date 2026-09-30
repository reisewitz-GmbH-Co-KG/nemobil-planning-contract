namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Geografischer Punkt (WGS84, Breite/Länge in Grad).</summary>
    public readonly record struct GeoPoint(double Lat, double Lon);
}
