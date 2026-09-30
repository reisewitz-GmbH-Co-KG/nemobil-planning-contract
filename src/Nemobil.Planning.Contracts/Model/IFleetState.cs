namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Der Flottenzustand, in den eine Anfrage eingeplant wird.
    /// <para><see cref="Vehicles"/> beschreibt jede einplanbare Schicht neutral und vollständig;
    /// <see cref="Tours"/> ist die kompakte Referenzliste derselben Schichten (z. B. für eine
    /// Reservierung). Beide Listen sind gleich lang, in derselben Reihenfolge, und es gilt
    /// <c>Tours[i].TourId == Vehicles[i].TourId</c>.</para>
    /// <para>Eine Implementierung dieser Schnittstelle darf daneben eine eigene, interne Repräsentation
    /// mitführen. Eine Planung, die nur den Vertrag kennt, arbeitet ausschließlich mit
    /// <see cref="Vehicles"/>. <see cref="FleetSnapshot"/> ist die offene Standard-Implementierung.</para></summary>
    public interface IFleetState
    {
        /// <summary>Referenzen auf die Schichten des Flottenzustands.</summary>
        IReadOnlyList<TourRef> Tours { get; }

        /// <summary>Neutraler Zustand je Schicht (siehe <see cref="VehicleState"/>).</summary>
        IReadOnlyList<VehicleState> Vehicles { get; }
    }

    /// <summary>Minimaler offener Bezug auf eine Tour des Flottenzustands.</summary>
    public readonly record struct TourRef(string TourId);
}
