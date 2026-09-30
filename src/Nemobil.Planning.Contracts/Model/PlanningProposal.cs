namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Ein Planungsvorschlag (Zielfahrzeug + Fahrplan + relative Aufwandskennzahl).</summary>
    public sealed class PlanningProposal
    {
        /// <summary>Kennung des Fahrzeugs (der Ressource), auf das der Vorschlag bucht.
        /// Identifiziert das physische Betriebsmittel, nicht seinen Einsatztag.</summary>
        public required string VehicleId { get; init; }

        /// <summary>Kennung der Tour (Schicht), in die der Vorschlag den Halt einplant —
        /// also die Schicht des Fahrzeugs, nicht das Fahrzeug selbst. Ein Fahrzeug kann mehrere
        /// Touren haben; die Buchung bezieht sich immer auf genau eine.
        /// <para>Entspricht <see cref="PlannedSchedule.VehicleScheduleId"/> des Fahrplans unter
        /// <c>Schedules[0]</c>.</para></summary>
        public required string TourId { get; init; }

        /// <summary>Resultierende Fahrpläne. <c>Schedules[0]</c> ist der Fahrplan des Fahrzeugs,
        /// auf das gebucht wird; weitere Einträge betreffen mitgeplante Fahrzeuge (Konvoi).</summary>
        public IReadOnlyList<PlannedSchedule> Schedules { get; init; } = [];

        /// <summary>Korrelations-Id des Planungslaufs.</summary>
        public string TransactionId { get; init; } = string.Empty;

        /// <summary>Schlüssel des angefragten Einstiegs bzw. Ausstiegs innerhalb der Fahrpläne: der im
        /// Fahrplan früheste bzw. späteste der übergebenen <see cref="IPlanningItems.Items"/> — regulär der
        /// (erste) Einstieg und der (letzte) Ausstieg der Anfrage.
        /// <para><b>Faktisch erforderlich, technisch noch nullable:</b> siehe den Hinweis an
        /// <see cref="PickupWindow"/>.</para></summary>
        public string? RequestedPickupKey { get; init; }
        public string? RequestedDropoffKey { get; init; }

        /// <summary>Zugesagte Zeitfenster für Einstieg und Ausstieg.
        /// <para><b>Faktisch erforderlich, technisch noch nullable:</b> ein Vorschlag ohne
        /// zugesagte Zeitfenster und ohne die beiden Halt-Schlüssel
        /// (<see cref="RequestedPickupKey"/>/<see cref="RequestedDropoffKey"/>) lässt sich vom
        /// Aufrufer nicht in eine Buchung übersetzen — er hätte dem Fahrgast weder eine Zeit noch
        /// einen zuordenbaren Halt zu melden. Wer Vorschläge erzeugt, setzt die vier Felder daher
        /// zwingend; die Konformitätssuite prüft das (<c>[G3-Buchbarkeit]</c>).</para></summary>
        public TimeWindow? PickupWindow { get; init; }
        public TimeWindow? DropoffWindow { get; init; }

        /// <summary>Relative Aufwandskennzahl des Vorschlags: ein Maß dafür, wie gut die Anfrage in
        /// die bestehende Flottenplanung passt. <b>Kein Preis und keine absolute Größe</b> — nur
        /// innerhalb desselben Planungslaufs untereinander vergleichbar (niedriger = besser passend).
        /// Skala und Berechnung sind Sache der Implementierung und ausdrücklich nicht Teil des
        /// Vertrags; ein Aufrufer darf ausschließlich die Ordnung auswerten, nicht den Wert.</summary>
        public double Efficiency { get; init; }
        /// <summary>Streckendistanz des Fahrgasts (Einstieg bis Ausstieg) in Metern, nach dem
        /// Streckenmodell der Implementierung. Informativ: ein Aufrufer zeigt den Wert an oder wertet ihn
        /// aus, er ist aber keine Grundlage der Buchung. 0 bedeutet „nicht ermittelt“ — eine
        /// Implementierung, die den Wert nicht bestimmt, liefert 0 statt eines Schätzwerts.</summary>
        public double PassengerDistanceMeters { get; init; }
    }
}
