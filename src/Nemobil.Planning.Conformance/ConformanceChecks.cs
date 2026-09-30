using System.Globalization;
using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Conformance
{
    /// <summary>Die Prüfregeln des Konformitäts-Testkatalogs, als wiederverwendbare Funktionen. Jede
    /// Funktion liefert die gefundenen Verstöße als lesbare Meldungen; eine leere Liste bedeutet
    /// „konform“.
    /// <para>Geprüft wird ausschließlich beobachtbares Verhalten gegen die Eingaben des Prüffalls —
    /// keine internen Erwartungswerte, keine konkrete Fahrzeugwahl, keine exakte Kennzahl.</para></summary>
    public static class ConformanceChecks
    {
        /// <summary>Zulässige Abweichung bei Zeitvergleichen (Rundung von Fahrzeiten).</summary>
        public static readonly TimeSpan TimeTolerance = TimeSpan.FromSeconds(60);

        /// <summary>Zulässige Abweichung bei der Energiebilanz je Halt (Rundung), in Wh.</summary>
        public const int EnergyToleranceWh = 1;

        private static readonly PlannedStopKind[] NotApplicableKinds = [PlannedStopKind.None, PlannedStopKind.Parking, PlannedStopKind.BusinessTrip];

        private static readonly PlannedStopKind[] PreservedKinds = [PlannedStopKind.Pickup, PlannedStopKind.Dropoff, PlannedStopKind.Depot];

        /// <summary>G1 — Eingang → Wirkung: die erwartete Wirkungsart des Prüffalls tritt ein.</summary>
        public static IReadOnlyList<string> CheckExpectation(ConformanceScenario scenario, PlanningResult result)
        {
            ArgumentNullException.ThrowIfNull(scenario);
            ArgumentNullException.ThrowIfNull(result);

            var violations = new List<string>();

            switch (scenario.Expectation)
            {
                case ScenarioExpectation.AtLeastOneProposal when result.Proposals.Count == 0:
                    violations.Add($"[G1] Erwartet: mindestens ein Vorschlag. Geliefert: keiner (Diagnose: '{result.Reason}').");
                    break;

                case ScenarioExpectation.NoProposal when result.Proposals.Count > 0:
                    violations.Add($"[G1] Erwartet: kein Vorschlag. Geliefert: {result.Proposals.Count}.");
                    break;

                case ScenarioExpectation.NoProposal when string.IsNullOrWhiteSpace(result.Reason):
                    violations.Add("[G1] Erwartet: leeres Ergebnis mit Diagnosegrund. Der Diagnosegrund fehlt.");
                    break;
            }

            return violations;
        }

        /// <summary>G2 — Invarianten jedes Vorschlags: Zeitfenster, Schicht, Kapazität, Energie,
        /// Reihenfolge-Vorgaben, Mitnahme-Regel, Konvoi-Paare.</summary>
        public static IReadOnlyList<string> CheckInvariants(ConformanceScenario scenario, PlanningResult result)
        {
            ArgumentNullException.ThrowIfNull(scenario);
            ArgumentNullException.ThrowIfNull(result);

            var context = new Context(scenario);
            var violations = new List<string>();

            foreach (var (proposal, p) in result.Proposals.Select((x, i) => (x, i + 1)))
            {
                foreach (var schedule in proposal.Schedules)
                {
                    var where = $"Vorschlag {p}, Fahrplan {schedule.VehicleScheduleId}";
                    var vehicle = context.Vehicle(schedule.VehicleScheduleId);

                    CheckTimeWindows(context, vehicle, schedule, where, violations);

                    if (vehicle is null)
                    {
                        continue;
                    }

                    CheckShift(vehicle, schedule, where, violations);
                    CheckSkills(context, vehicle, schedule, where, violations);
                    CheckCapacity(context, vehicle, schedule, where, violations);
                    CheckEnergyBounds(vehicle, schedule, where, violations);
                    CheckConvoyPairs(schedule, where, violations);
                }

                CheckPredecessors(context, proposal, $"Vorschlag {p}", violations);

                if (!scenario.Request.AllowCarpooling && proposal.Schedules.Count > 0)
                {
                    CheckNoCarpooling(context, proposal.Schedules[0], $"Vorschlag {p}", violations);
                }
            }

            return violations;
        }

        /// <summary>G3 — Vertrag: Obergrenze, Sortierbarkeit, Diagnose bei leerem Ergebnis und die
        /// Buchbarkeit jedes Vorschlags (Bezüge, Schlüssel, zugesagte Zeitfenster, anwendbare Halt-Arten).
        /// Die Seiteneffektfreiheit prüft <see cref="CheckInputsUnchanged"/>.</summary>
        public static IReadOnlyList<string> CheckContract(ConformanceScenario scenario, PlanningResult result)
        {
            ArgumentNullException.ThrowIfNull(scenario);
            ArgumentNullException.ThrowIfNull(result);

            var context = new Context(scenario);
            var violations = new List<string>();
            var request = scenario.Request;

            if (request.MaxProposals > 0 && result.Proposals.Count > request.MaxProposals)
            {
                violations.Add($"[G3-Obergrenze] {result.Proposals.Count} Vorschläge, angefragt höchstens {request.MaxProposals}.");
            }

            if (result.Proposals.Count == 0 && string.IsNullOrWhiteSpace(result.Reason))
            {
                violations.Add("[G3-Diagnose] Leeres Ergebnis ohne Diagnosegrund.");
            }

            if (result.Proposals.Count > 0 && !result.Successful)
            {
                violations.Add("[G3-Erfolg] Vorschläge geliefert, aber Successful = false.");
            }

            foreach (var (proposal, p) in result.Proposals.Select((x, i) => (x, i + 1)))
            {
                CheckProposal(context, proposal, $"Vorschlag {p}", violations);
            }

            return violations;
        }

        /// <summary>G3 — Seiteneffektfreiheit: die Eingaben sind nach dem Aufruf unverändert.</summary>
        /// <param name="snapshotBefore">Ergebnis von <see cref="Snapshot"/> vor dem Aufruf.</param>
        /// <param name="snapshotAfter">Ergebnis von <see cref="Snapshot"/> nach dem Aufruf.</param>
        public static IReadOnlyList<string> CheckInputsUnchanged(string snapshotBefore, string snapshotAfter) =>
            string.Equals(snapshotBefore, snapshotAfter, StringComparison.Ordinal)
                ? []
                : ["[G3-Seiteneffekt] Die Implementierung hat die übergebenen Eingaben verändert."];

        /// <summary>Serialisiert die Eingaben eines Aufrufs für <see cref="CheckInputsUnchanged"/>.</summary>
        public static string Snapshot(PlanningRequest request, IFleetState fleet, IPlanningItems items)
        {
            ArgumentNullException.ThrowIfNull(fleet);
            ArgumentNullException.ThrowIfNull(items);

            return System.Text.Json.JsonSerializer.Serialize(
                new { request, fleet.Tours, fleet.Vehicles, items.Items },
                ConformanceScenarios.SerializerOptions);
        }

        /// <summary>G4 — Zusicherungen je Fahrplan: wohlgeformt, energetisch konsistent, vollständig
        /// (inklusive unveränderter fixierter Halte).</summary>
        public static IReadOnlyList<string> CheckSchedules(ConformanceScenario scenario, PlanningResult result)
        {
            ArgumentNullException.ThrowIfNull(scenario);
            ArgumentNullException.ThrowIfNull(result);

            var context = new Context(scenario);
            var violations = new List<string>();

            foreach (var (proposal, p) in result.Proposals.Select((x, i) => (x, i + 1)))
            {
                foreach (var schedule in proposal.Schedules)
                {
                    var where = $"Vorschlag {p}, Fahrplan {schedule.VehicleScheduleId}";
                    var vehicle = context.Vehicle(schedule.VehicleScheduleId);

                    CheckWellFormed(schedule, where, violations);

                    if (vehicle is null)
                    {
                        continue;
                    }

                    CheckEnergyConsistency(vehicle, schedule, where, violations);
                    CheckCompleteness(vehicle, schedule, where, violations);
                    CheckFixedStops(context, vehicle, schedule, where, violations);
                }
            }

            return violations;
        }

        // ---- G2 --------------------------------------------------------------------------------

        private static void CheckTimeWindows(Context context, VehicleState? vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            foreach (var stop in schedule.Stops)
            {
                // Maßgeblich ist das Zeitfenster der Eingabe, nicht das im Ergebnis zurückgemeldete:
                // eine Implementierung darf ein Fenster nicht dadurch „einhalten“, dass sie es erweitert.
                var window = context.InputWindow(vehicle, stop.Key);

                if (window is not { } w || stop.Key is null)
                {
                    continue;
                }

                var earliest = w.Min.AddSeconds(-w.ToleranceBefore);
                var latest = w.Max.AddSeconds(w.ToleranceAfter);

                if (stop.ServiceStart < earliest - TimeTolerance || stop.ServiceStart > latest + TimeTolerance)
                {
                    violations.Add($"[G2-Zeitfenster] {where}, Halt {stop.Key}: Bedienbeginn {Fmt(stop.ServiceStart)} außerhalb [{Fmt(earliest)}, {Fmt(latest)}].");
                }
            }
        }

        private static void CheckShift(VehicleState vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            var earliest = vehicle.Shift.Min.AddSeconds(-vehicle.Shift.ToleranceBefore);
            var latest = vehicle.Shift.Max.AddSeconds(vehicle.Shift.ToleranceAfter);

            foreach (var stop in schedule.Stops)
            {
                if (stop.ServiceStart < earliest - TimeTolerance || stop.Departure > latest + TimeTolerance)
                {
                    violations.Add($"[G2-Schicht] {where}, Halt {stop.Key}: {Fmt(stop.ServiceStart)}–{Fmt(stop.Departure)} außerhalb der Schicht [{Fmt(earliest)}, {Fmt(latest)}].");
                }
            }
        }

        private static void CheckSkills(Context context, VehicleState vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            foreach (var stop in schedule.Stops)
            {
                var item = context.Item(stop.Key);

                foreach (var skill in item?.Skills.Where(s => !vehicle.Skills.Contains(s)) ?? [])
                {
                    violations.Add($"[G2-Fähigkeit] {where}, Halt {stop.Key}: benötigte Fähigkeit {skill} führt das Fahrzeug nicht.");
                }
            }
        }

        private static void CheckCapacity(Context context, VehicleState vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            var profile = LoadProfile(schedule.Stops, stop => context.InputQuantities(vehicle, stop));

            for (var i = 0; i < schedule.Stops.Count; i++)
            {
                foreach (var (key, max) in vehicle.Capacity)
                {
                    var load = profile[i].GetValueOrDefault(key);

                    if (load > max + 1e-6f)
                    {
                        violations.Add($"[G2-Kapazität] {where}, nach Halt {schedule.Stops[i].Key}: Belegung {key} = {load.ToString(CultureInfo.InvariantCulture)} > {max.ToString(CultureInfo.InvariantCulture)}.");
                    }
                }
            }
        }

        private static void CheckEnergyBounds(VehicleState vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            if (vehicle.EnergyCapacityWh <= 0)
            {
                return;
            }

            foreach (var stop in schedule.Stops)
            {
                if (stop.RemainingEnergy < 0 || stop.RemainingEnergy > vehicle.EnergyCapacityWh + EnergyToleranceWh)
                {
                    violations.Add($"[G2-Energie] {where}, Halt {stop.Key}: Restenergie {stop.RemainingEnergy} Wh außerhalb [0, {vehicle.EnergyCapacityWh}].");
                }
            }
        }

        private static void CheckConvoyPairs(PlannedSchedule schedule, string where, List<string> violations)
        {
            var open = new HashSet<string>(StringComparer.Ordinal);

            foreach (var stop in schedule.Stops)
            {
                var segment = stop.TransferSegmentId ?? string.Empty;

                if (stop.Kind == PlannedStopKind.Chaining)
                {
                    open.Add(segment);
                }
                else if (stop.Kind == PlannedStopKind.Unchaining && !open.Remove(segment))
                {
                    violations.Add($"[G2-Konvoi] {where}, Halt {stop.Key}: Entkoppeln ohne vorheriges Koppeln (Segment '{segment}').");
                }
            }

            foreach (var segment in open)
            {
                violations.Add($"[G2-Konvoi] {where}: Koppeln ohne späteres Entkoppeln (Segment '{segment}').");
            }
        }

        private static void CheckPredecessors(Context context, PlanningProposal proposal, string where, List<string> violations)
        {
            foreach (var schedule in proposal.Schedules)
            {
                var index = schedule.Stops
                    .Select((s, i) => (s.Key, i))
                    .Where(x => x.Key is not null)
                    .GroupBy(x => x.Key!, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().i, StringComparer.Ordinal);

                foreach (var item in context.Scenario.Items.Where(i => index.ContainsKey(i.Key)))
                {
                    foreach (var predecessor in item.Predecessors)
                    {
                        if (!index.TryGetValue(predecessor, out var predecessorIndex) || predecessorIndex >= index[item.Key])
                        {
                            violations.Add($"[G2-Reihenfolge] {where}: Halt {item.Key} liegt nicht hinter seinem Vorgänger {predecessor}.");
                        }
                    }
                }
            }
        }

        private static void CheckNoCarpooling(Context context, PlannedSchedule schedule, string where, List<string> violations)
        {
            var stops = schedule.Stops;
            var first = IndexOfFirst(stops, s => context.IsRequested(s.Key));
            var last = IndexOfLast(stops, s => context.IsRequested(s.Key));

            if (first < 0)
            {
                return;
            }

            // Mitfahrende zählen: Einstieg +1, Ausstieg -1 (ohne die eigene Anfrage). Wer schon vor dem
            // ersten Halt der Liste an Bord war, erscheint nur mit seinem Ausstieg.
            var running = 0;
            var baseline = 0;
            var counts = new int[stops.Count];

            for (var i = 0; i < stops.Count; i++)
            {
                if (!context.IsRequested(stops[i].Key))
                {
                    running += SignOf(stops[i].Kind);
                }

                baseline = Math.Max(baseline, -running);
                counts[i] = running;
            }

            var onboardAtPickup = (first == 0 ? 0 : counts[first - 1]) + baseline;

            if (onboardAtPickup > 0)
            {
                violations.Add($"[G2-Mitnahme] {where}: beim Einstieg {stops[first].Key} sind bereits {onboardAtPickup} weitere Fahrgäste an Bord, obwohl die Anfrage keine Mitnahme erlaubt.");
            }

            for (var i = first + 1; i < last; i++)
            {
                if (!context.IsRequested(stops[i].Key) && stops[i].Kind == PlannedStopKind.Pickup)
                {
                    violations.Add($"[G2-Mitnahme] {where}: während der Fahrt steigt {stops[i].Key} zu, obwohl die Anfrage keine Mitnahme erlaubt.");
                }
            }
        }

        // ---- G3 --------------------------------------------------------------------------------

        private static void CheckProposal(Context context, PlanningProposal proposal, string where, List<string> violations)
        {
            if (!double.IsFinite(proposal.Efficiency))
            {
                violations.Add($"[G3-Kennzahl] {where}: Aufwandskennzahl ist keine endliche Zahl.");
            }

            if (!double.IsFinite(proposal.PassengerDistanceMeters) || proposal.PassengerDistanceMeters < 0)
            {
                violations.Add($"[G3-Kennzahl] {where}: Fahrgast-Distanz {proposal.PassengerDistanceMeters} ist nicht ≥ 0.");
            }

            var vehicle = context.Vehicle(proposal.TourId);

            if (vehicle is null)
            {
                violations.Add($"[G3-Bezug] {where}: TourId '{proposal.TourId}' ist keine Schicht des Flottenzustands.");
            }
            else if (!string.Equals(vehicle.VehicleId, proposal.VehicleId, StringComparison.Ordinal))
            {
                violations.Add($"[G3-Bezug] {where}: VehicleId '{proposal.VehicleId}' gehört nicht zur Schicht '{proposal.TourId}' (erwartet '{vehicle.VehicleId}').");
            }

            if (proposal.Schedules.Count == 0)
            {
                violations.Add($"[G3-Fahrplan] {where}: kein Fahrplan geliefert.");
                return;
            }

            if (!string.Equals(proposal.Schedules[0].VehicleScheduleId, proposal.TourId, StringComparison.Ordinal))
            {
                violations.Add($"[G3-Fahrplan] {where}: Schedules[0] gehört zu '{proposal.Schedules[0].VehicleScheduleId}', nicht zur gebuchten Schicht '{proposal.TourId}'.");
            }

            foreach (var schedule in proposal.Schedules.Where(s => context.Vehicle(s.VehicleScheduleId) is null))
            {
                violations.Add($"[G3-Fahrplan] {where}: Fahrplan '{schedule.VehicleScheduleId}' gehört zu keiner Schicht des Flottenzustands.");
            }

            foreach (var stop in proposal.Schedules.SelectMany(s => s.Stops).Where(s => NotApplicableKinds.Contains(s.Kind)))
            {
                violations.Add($"[G3-Halt-Art] {where}, Halt {stop.Key}: Art {stop.Kind} kann der Aufrufer nicht anwenden.");
            }

            var occurrences = proposal.Schedules
                .SelectMany(s => s.Stops.Select(stop => (Schedule: s, Stop: stop)))
                .Where(x => context.IsRequested(x.Stop.Key))
                .GroupBy(x => x.Stop.Key!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            foreach (var item in context.Scenario.Items)
            {
                var count = occurrences.TryGetValue(item.Key, out var found) ? found.Count : 0;

                if (count != 1)
                {
                    violations.Add($"[G3-Buchbarkeit] {where}: angefragter Halt {item.Key} kommt {count}-mal statt genau einmal vor.");
                }
            }

            CheckRequestedStop(proposal.RequestedPickupKey, proposal.PickupWindow, "Einstieg", occurrences, where, violations);
            CheckRequestedStop(proposal.RequestedDropoffKey, proposal.DropoffWindow, "Ausstieg", occurrences, where, violations);

            if (proposal.RequestedPickupKey is { } pickupKey && proposal.RequestedDropoffKey is { } dropoffKey
                && occurrences.TryGetValue(pickupKey, out var pickups) && occurrences.TryGetValue(dropoffKey, out var dropoffs)
                && ReferenceEquals(pickups[0].Schedule, dropoffs[0].Schedule)
                && pickups[0].Stop.ServiceStart > dropoffs[0].Stop.ServiceStart)
            {
                violations.Add($"[G3-Buchbarkeit] {where}: der gemeldete Einstieg liegt nach dem gemeldeten Ausstieg.");
            }
        }

        private static void CheckRequestedStop(
            string? key,
            TimeWindow? promised,
            string label,
            Dictionary<string, List<(PlannedSchedule Schedule, PlannedStop Stop)>> occurrences,
            string where,
            List<string> violations)
        {
            if (key is null)
            {
                violations.Add($"[G3-Buchbarkeit] {where}: Schlüssel für den {label} fehlt.");
                return;
            }

            if (!occurrences.TryGetValue(key, out var found))
            {
                violations.Add($"[G3-Buchbarkeit] {where}: gemeldeter {label} '{key}' ist kein angefragter Halt der Fahrpläne.");
                return;
            }

            if (promised is not { } window)
            {
                violations.Add($"[G3-Buchbarkeit] {where}: zugesagtes Zeitfenster für den {label} fehlt.");
                return;
            }

            var serviceStart = found[0].Stop.ServiceStart;
            var earliest = window.Min.AddSeconds(-window.ToleranceBefore);
            var latest = window.Max.AddSeconds(window.ToleranceAfter);

            if (serviceStart < earliest - TimeTolerance || serviceStart > latest + TimeTolerance)
            {
                violations.Add($"[G3-Buchbarkeit] {where}: geplanter {label} {Fmt(serviceStart)} liegt außerhalb des zugesagten Fensters [{Fmt(earliest)}, {Fmt(latest)}].");
            }
        }

        // ---- G4 --------------------------------------------------------------------------------

        private static void CheckWellFormed(PlannedSchedule schedule, string where, List<string> violations)
        {
            var stops = schedule.Stops;

            foreach (var duplicate in stops.Where(s => s.Key is not null).GroupBy(s => s.Key!, StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                violations.Add($"[G4-Wohlgeformt] {where}: Schlüssel {duplicate.Key} kommt {duplicate.Count()}-mal vor.");
            }

            for (var i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];

                if (stop.Arrival > stop.ServiceStart || stop.ServiceStart > stop.Departure)
                {
                    violations.Add($"[G4-Wohlgeformt] {where}, Halt {stop.Key}: Ankunft {Fmt(stop.Arrival)}, Bedienbeginn {Fmt(stop.ServiceStart)}, Abfahrt {Fmt(stop.Departure)} nicht in dieser Reihenfolge.");
                }

                if (stop.DistanceMeters < 0 || stop.DrivingSeconds < 0 || stop.ServiceSeconds < 0)
                {
                    violations.Add($"[G4-Wohlgeformt] {where}, Halt {stop.Key}: negative Strecke, Fahr- oder Bedienzeit.");
                }

                if (stop.Kind == PlannedStopKind.Depot && i != 0 && i != stops.Count - 1)
                {
                    violations.Add($"[G4-Wohlgeformt] {where}, Halt {stop.Key}: Depot-Halt an Position {i + 1} statt am Anfang oder Ende.");
                }

                if (i == 0)
                {
                    continue;
                }

                var previous = stops[i - 1];

                if (stop.Order <= previous.Order)
                {
                    violations.Add($"[G4-Wohlgeformt] {where}, Halt {stop.Key}: Position {stop.Order} nicht größer als die des Vorgängers ({previous.Order}).");
                }

                if (stop.Arrival + TimeTolerance < previous.Departure.AddSeconds(stop.DrivingSeconds))
                {
                    violations.Add($"[G4-Wohlgeformt] {where}, Halt {stop.Key}: Ankunft {Fmt(stop.Arrival)} vor Abfahrt am Vorgänger ({Fmt(previous.Departure)}) plus Fahrzeit ({stop.DrivingSeconds} s).");
                }
            }
        }

        private static void CheckEnergyConsistency(VehicleState vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            if (vehicle.EnergyCapacityWh <= 0)
            {
                return;
            }

            var stops = schedule.Stops;

            for (var i = 1; i < stops.Count; i++)
            {
                var previous = stops[i - 1].RemainingEnergy;
                var stop = stops[i];
                var withoutCharge = previous - stop.ConsumedEnergy;
                var chargingStop = stop.ChargedEnergy > 0 || stop.Kind is PlannedStopKind.Charging or PlannedStopKind.Chaining or PlannedStopKind.Unchaining;

                if (!chargingStop && Math.Abs(stop.RemainingEnergy - withoutCharge) > EnergyToleranceWh)
                {
                    violations.Add($"[G4-Energie] {where}, Halt {stop.Key}: Restenergie {stop.RemainingEnergy} Wh, erwartet {withoutCharge} Wh (Vorgänger {previous} Wh minus Verbrauch {stop.ConsumedEnergy} Wh).");
                }

                if (chargingStop && (stop.RemainingEnergy > withoutCharge + stop.ChargedEnergy + EnergyToleranceWh || stop.RemainingEnergy < withoutCharge - EnergyToleranceWh))
                {
                    violations.Add($"[G4-Energie] {where}, Halt {stop.Key}: Restenergie {stop.RemainingEnergy} Wh passt nicht zu Vorgänger {previous} Wh, Verbrauch {stop.ConsumedEnergy} Wh und Ladung {stop.ChargedEnergy} Wh.");
                }
            }
        }

        private static void CheckCompleteness(VehicleState vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            var keys = schedule.Stops.Where(s => s.Key is not null).Select(s => s.Key!).ToHashSet(StringComparer.Ordinal);

            foreach (var stop in vehicle.Stops.Where(s => s.Key is not null && PreservedKinds.Contains(s.Kind)))
            {
                if (!keys.Contains(stop.Key!))
                {
                    violations.Add($"[G4-Vollständig] {where}: bestehender Halt {stop.Key} ({stop.Kind}) fehlt im gelieferten Fahrplan.");
                }
            }
        }

        private static void CheckFixedStops(Context context, VehicleState vehicle, PlannedSchedule schedule, string where, List<string> violations)
        {
            var fixedCount = FixedStopCount(vehicle);

            for (var i = 0; i < fixedCount; i++)
            {
                var expected = vehicle.Stops[i];

                if (i >= schedule.Stops.Count)
                {
                    violations.Add($"[G4-Fixiert] {where}: fixierter Halt {expected.Key} fehlt am Anfang des Fahrplans.");
                    continue;
                }

                var actual = schedule.Stops[i];

                if (!string.Equals(actual.Key, expected.Key, StringComparison.Ordinal))
                {
                    violations.Add($"[G4-Fixiert] {where}: an Position {i + 1} steht {actual.Key}, erwartet der fixierte Halt {expected.Key}.");
                }
                else if (actual.Arrival != expected.Arrival || actual.Departure != expected.Departure)
                {
                    violations.Add($"[G4-Fixiert] {where}: fixierter Halt {expected.Key} wurde zeitlich verändert.");
                }
            }

            var firstRequested = IndexOfFirst(schedule.Stops, s => context.IsRequested(s.Key));

            if (firstRequested >= 0 && firstRequested < fixedCount)
            {
                violations.Add($"[G4-Fixiert] {where}: angefragter Halt {schedule.Stops[firstRequested].Key} steht vor einem fixierten Halt.");
            }
        }

        // ---- Hilfen ----------------------------------------------------------------------------

        /// <summary>Anzahl der unveränderlichen Halte am Anfang des Ist-Fahrplans eines Fahrzeugs, nach
        /// den Regeln von <see cref="VehicleState.LastFixedStopKey"/> und <see cref="VehicleState.AvailableFrom"/>.</summary>
        public static int FixedStopCount(VehicleState vehicle)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            var last = -1;

            for (var i = 0; i < vehicle.Stops.Count; i++)
            {
                var stop = vehicle.Stops[i];

                if ((vehicle.LastFixedStopKey is not null && string.Equals(stop.Key, vehicle.LastFixedStopKey, StringComparison.Ordinal))
                    || (vehicle.AvailableFrom is { } availableFrom && stop.Departure < availableFrom))
                {
                    last = i;
                }
            }

            return last + 1;
        }

        /// <summary>Belegung nach jedem Halt (Einstieg belegt, Ausstieg gibt frei). Fahrgäste, die schon
        /// vor dem ersten Halt an Bord waren und nur mit ihrem Ausstieg erscheinen, werden als
        /// Anfangsbelegung mitgezählt.</summary>
        private static List<Dictionary<string, float>> LoadProfile(IReadOnlyList<PlannedStop> stops, Func<PlannedStop, IReadOnlyDictionary<string, float>> quantities)
        {
            var running = new Dictionary<string, float>(StringComparer.Ordinal);
            var baseline = new Dictionary<string, float>(StringComparer.Ordinal);
            var snapshots = new List<Dictionary<string, float>>(stops.Count);

            foreach (var stop in stops)
            {
                var sign = SignOf(stop.Kind);

                foreach (var (key, amount) in quantities(stop))
                {
                    running[key] = running.GetValueOrDefault(key) + (sign * amount);
                    baseline[key] = Math.Max(baseline.GetValueOrDefault(key), -running[key]);
                }

                snapshots.Add(new Dictionary<string, float>(running, StringComparer.Ordinal));
            }

            foreach (var snapshot in snapshots)
            {
                foreach (var (key, offset) in baseline)
                {
                    snapshot[key] = snapshot.GetValueOrDefault(key) + offset;
                }
            }

            return snapshots;
        }

        private static int SignOf(PlannedStopKind kind) => kind switch
        {
            PlannedStopKind.Pickup => 1,
            PlannedStopKind.Dropoff => -1,
            _ => 0,
        };

        private static int IndexOfFirst(IReadOnlyList<PlannedStop> stops, Func<PlannedStop, bool> predicate)
        {
            for (var i = 0; i < stops.Count; i++)
            {
                if (predicate(stops[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOfLast(IReadOnlyList<PlannedStop> stops, Func<PlannedStop, bool> predicate)
        {
            for (var i = stops.Count - 1; i >= 0; i--)
            {
                if (predicate(stops[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string Fmt(DateTime time) => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

        /// <summary>Nachschlagehilfe über die Eingaben eines Prüffalls.</summary>
        private sealed class Context
        {
            private readonly Dictionary<string, VehicleState> _vehicles;
            private readonly Dictionary<string, PlanningItem> _items;

            public Context(ConformanceScenario scenario)
            {
                Scenario = scenario;
                _vehicles = scenario.Vehicles.ToDictionary(v => v.TourId, StringComparer.Ordinal);
                _items = scenario.Items.ToDictionary(i => i.Key, StringComparer.Ordinal);
            }

            public ConformanceScenario Scenario { get; }

            public VehicleState? Vehicle(string tourId) => _vehicles.GetValueOrDefault(tourId);

            public bool IsRequested(string? key) => key is not null && _items.ContainsKey(key);

            public PlanningItem? Item(string? key) => key is null ? null : _items.GetValueOrDefault(key);

            public TimeWindow? InputWindow(VehicleState? vehicle, string? key)
            {
                if (key is null)
                {
                    return null;
                }

                if (_items.TryGetValue(key, out var item))
                {
                    return item.TimeWindow;
                }

                return vehicle?.Stops.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.Ordinal))?.TimeWindow;
            }

            public IReadOnlyDictionary<string, float> InputQuantities(VehicleState vehicle, PlannedStop stop)
            {
                if (stop.Key is not null)
                {
                    if (_items.TryGetValue(stop.Key, out var item))
                    {
                        return item.Quantities;
                    }

                    var existing = vehicle.Stops.FirstOrDefault(s => string.Equals(s.Key, stop.Key, StringComparison.Ordinal));

                    if (existing is not null)
                    {
                        return existing.Quantities;
                    }
                }

                return stop.Quantities;
            }
        }
    }
}
