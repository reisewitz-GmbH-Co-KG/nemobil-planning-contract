namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Ein geplanter Halt im Fahrplan. Trägt alles, was ein Aufrufer braucht, um den
    /// Halt ohne Kenntnis der Optimierung anzuwenden: Ort, Position, Art, Zeiten, Aufwände,
    /// Energiewirkung und die fachlichen Bezüge.</summary>
    public sealed class PlannedStop
    {
        public required GeoPoint Location { get; init; }

        /// <summary>Position des Halts innerhalb des Fahrplans (1-basiert wie geliefert).</summary>
        public int Order { get; init; }

        /// <summary>Art des Halts. Bestimmt, wie ein Aufrufer den Halt fachlich einordnet
        /// (siehe <see cref="PlannedStopKind"/>, insbesondere den Sonderfall
        /// <see cref="PlannedStopKind.Other"/>).</summary>
        public PlannedStopKind Kind { get; init; }

        /// <summary>Korrelationsschlüssel des Halts — stabil über Planungsläufe, damit
        /// Aufrufer bestehende Halte wiedererkennen können.</summary>
        public string? Key { get; init; }

        /// <summary>Ankunft am Halt inklusive Fahrzeit.</summary>
        public DateTime Arrival { get; init; }

        /// <summary>Beginn der Bedienung am Halt (Abfahrt minus Bedienzeit).</summary>
        public DateTime ServiceStart { get; init; }

        /// <summary>Abfahrt am Halt (Ende der Bedienung).</summary>
        public DateTime Departure { get; init; }

        /// <summary>Dauer der Bedienung am Halt in Sekunden.</summary>
        public int ServiceSeconds { get; init; }

        /// <summary>Fahrzeit von der vorherigen Position zu diesem Halt in Sekunden.</summary>
        public int DrivingSeconds { get; init; }

        /// <summary>Fahrstrecke von der vorherigen Position zu diesem Halt in Metern.</summary>
        public int DistanceMeters { get; init; }

        /// <summary>Zulässiges Zeitfenster des Halts, falls eines gilt.</summary>
        public TimeWindow? TimeWindow { get; init; }

        /// <summary>Auf der Strecke zu diesem Halt verbrauchte Energie in Wattstunden (Wh).</summary>
        public int ConsumedEnergy { get; init; }

        /// <summary>An diesem Halt geladene Energie in Wattstunden (Wh); 0 außerhalb eines
        /// Lade- oder Kupplungs-Halts.</summary>
        public int ChargedEnergy { get; init; }

        /// <summary>Restenergie des Fahrzeugs nach diesem Halt in Wattstunden (Wh). Ergibt sich
        /// vorwärts aus Verbrauch und Ladung: kein Anstieg ohne Lade- oder Konvoi-Halt (siehe die
        /// energetische Zusicherung des Vertrags).</summary>
        public int RemainingEnergy { get; init; }

        /// <summary>Referenz auf das Transfersegment, über das ein Konvoi-Halt bedient wird.</summary>
        public string? TransferSegmentId { get; init; }

        /// <summary>Schicht des Halts.
        /// <para><b>Nicht maßgeblich:</b> verbindlich ist allein
        /// <see cref="PlannedSchedule.VehicleScheduleId"/> am umgebenden Fahrplan — ein Fahrplan gilt
        /// für genau eine Schicht, und alle seine Halte gehören zu dieser. Dieses Feld wiederholt die
        /// Angabe nur je Halt und ist rein informativ; ein Aufrufer soll den Schicht-Bezug am
        /// Fahrplan lesen. Wer Fahrpläne erzeugt, setzt hier denselben Wert oder lässt das Feld
        /// leer.</para></summary>
        public string VehicleScheduleId { get; init; } = string.Empty;

        /// <summary>Am Halt benötigte bzw. hinterlegte Fähigkeiten (z. B. Rollstuhlgerechtigkeit),
        /// als numerische Kennungen wie vom Plattformzustand geführt.
        /// <para><b>Bekannte Lücke:</b> die Anforderungsseite der Anfrage führt Fähigkeiten als
        /// Bezeichner (<see cref="PlanningRequest.RequiredSkills"/> ist
        /// <c>IReadOnlyList&lt;string&gt;</c>), dieses Feld dagegen als Zahl. Der Vertrag liefert
        /// derzeit <b>keine</b> Zuordnung zwischen beiden — ein Aufrufer kann Anforderung und Halt
        /// nicht allein aus dem Vertrag korrelieren. Die Vereinheitlichung ist einer Folgephase
        /// vorbehalten.</para></summary>
        public IReadOnlyList<int> Skills { get; init; } = [];

        /// <summary>Kapazitätsangaben am Halt (z. B. Sitz-/Gepäckbedarf), benannt nach den
        /// fachlichen Schlüsseln des Plattformzustands.
        /// <para><b>Schlüssel-Konventionen</b> (damit das Feld ohne Kenntnis der Plattform lesbar ist):
        /// <list type="bullet">
        /// <item><description><c>ADULTS</c> — Anzahl erwachsener Fahrgäste.</description></item>
        /// <item><description><c>CHILDS</c> — Anzahl mitfahrender Kinder.</description></item>
        /// <item><description><c>LUGGAGE</c> — Gepäckstücke.</description></item>
        /// <item><description><c>REQUESTS</c> — <b>keine Personenzahl, sondern eine Belegungs-Restriktion:</b>
        /// belegt eine Fahrt, die Mitnahme erlaubt, den Wert <c>1</c>, eine Fahrt ohne Mitnahme
        /// dagegen die volle Kapazität (<c>100</c>). So blockiert eine Exklusivfahrt das Fahrzeug für
        /// weitere Anfragen, ohne dass der Vertrag ein eigenes Feld dafür braucht.</description></item>
        /// </list>
        /// Weitere Schlüssel sind zulässig; ein Aufrufer ignoriert, was er nicht kennt.</para>
        /// <para><b>Bei einem angefragten Ein-/Ausstieg (Pickup/Dropoff der eigenen Anfrage) ist
        /// dieses Feld NICHT verbindlich:</b> der Aufrufer kennt die Kapazitätsangaben der
        /// angefragten Fahrt bereits aus seiner eigenen Anfrage und muss sie beim
        /// Anwenden des Fahrplans daher selbst setzen, statt sich auf diesen Wert zu
        /// verlassen.</para></summary>
        public IReadOnlyDictionary<string, float> Quantities { get; init; } = new Dictionary<string, float>();

        /// <summary>Fachliche Priorität des Halts, wie sie der Aufrufer beim Erzeugen der Anfrage
        /// mitgegeben hat (z. B. aus der Buchung). Skala und Bedeutung legt der Aufrufer
        /// fest.</summary>
        public int Priority { get; init; }
    }
}
