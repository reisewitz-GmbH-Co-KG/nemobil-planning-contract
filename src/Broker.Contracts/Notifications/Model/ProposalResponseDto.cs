using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Broker.Contracts.Notifications.Model
{
    public class ProposalResponseDto : NgsiLdBaseType
    {
        public string Id { get; set; }

        public GeoProperty PickupLocation { get; set; }

        public GeoProperty DropoffLocation { get; set; }

        public Property<string> PickupTime { get; set; }

        public Property<string> TargetTime { get; set; }

        public Property<string> ProposalReleaseTime { get; set; }

        public Property<string> User { get; set; }

        public Property<string> Request { get; set; }

        /// <summary>
        /// Bleibt bewusst leer — die monetäre Preisermittlung liegt nicht in unserem Scope,
        /// sondern beim Broker. Das Feld bleibt im Contract erhalten, damit der Broker den
        /// von ihm ermittelten Preis hier einsetzen kann.
        /// </summary>
        public Property<string> Costs { get; set; }

        /// <summary>
        /// Relative Aufwandskennzahl des Vorschlags: niedriger = besser in die bestehende Planung
        /// passend. Reines Ranking-Maß, kein Preis; die Berechnung ist Sache der Planung.
        /// </summary>
        public Property<int> Efficiency { get; set; }

        /// <summary>
        /// Gefahrene Strecke des Fahrgasts von Einstieg zu Ziel (Route Pickup -> Dropoff) in Kilometern.
        /// Datengrundlage für die Preisermittlung des Brokers. Property-Name bewusst exakt fixiert.
        /// </summary>
        [JsonPropertyName("proposalTripDistanceKM")]
        public Property<double> ProposalTripDistanceKm { get; set; }

        /// <summary>
        /// Diagnose-Grund des Vermittlungsergebnisses (z.B. "Successful", "NoValidCabs",
        /// "StartOutsideServiceArea"). Macht ein leeres Vorschlags-Ergebnis für den Empfänger
        /// nachvollziehbar.
        /// </summary>
        public Property<string> Status { get; set; }

        /// <summary>
        /// Diagnose-Grund bei leerem Ergebnis (kein passendes/erreichbares Cab): Zusammenfassung
        /// der Ausschlussgründe der Planung. Nur gesetzt, wenn keine Vorschläge erzeugt wurden.
        /// </summary>
        public Property<string> Reason { get; set; }

        /// <summary>
        /// True, wenn dieser Vorschlag auf einer Konvoifahrt (gekuppelte Fahrzeuge) basiert —
        /// erkennbar an Kuppel-/Abkuppel-Stops in der geplanten Tour. Erspart dem Broker, die
        /// Konvoi-Eigenschaft aus der Teilstrecken-Anzahl abzuleiten.
        /// </summary>
        public Property<bool> IsConvoyTrip { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProposalResponseDto"/> class.
        /// </summary>
        public ProposalResponseDto()
        {
            Type = "TripProposal";
        }
    }
}
