using Nemobil.Planning.Contracts;
using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Reference
{
    /// <summary>Offene Referenz-Implementierung von <see cref="IPlanningEngine"/>.
    /// <para><b>Was:</b> plant eine Anfrage in den übergebenen Flottenzustand ein, indem sie für jedes
    /// Fahrzeug die günstigste zulässige Einfügeposition der angefragten Halte in den bestehenden
    /// Fahrplan sucht („cheapest insertion“, ein Standardverfahren der Tourenplanung). Zulässig ist eine
    /// Position, wenn danach alle Zeitfenster, die Schicht, die Kapazitäten, die Energie und die
    /// Mitnahme-Regel eingehalten sind. Aufwandskennzahl ist die zusätzliche Fahrstrecke in Metern.</para>
    /// <para><b>Warum:</b> das offene Repository soll ohne die gekapselte Optimierung lauffähig und
    /// prüfbar sein. Die Referenz zeigt, wie eine Implementierung den Vertrag bedient, und ist die
    /// Gegenprobe der Konformitätssuite.</para>
    /// <para><b>Bewusst einfach:</b> keine Konvoibildung, kein Einplanen von Ladehalten, keine
    /// Umplanung anderer Fahrzeuge, keine Optimierung über mehrere Anfragen. Mehr als zwei angefragte
    /// Halte werden nacheinander eingefügt statt gemeinsam. Sie ist kein Abbild des in der Plattform
    /// eingesetzten Verfahrens.</para></summary>
    public sealed class ReferencePlanningEngine : IPlanningEngine
    {
        private readonly ScheduleSimulator _simulator;
        private readonly ITravelModel _travelModel;
        private readonly ReferenceEngineOptions _options;

        /// <summary>Erzeugt die Referenz-Implementierung.</summary>
        /// <param name="travelModel">Streckenmodell; Standard ist <see cref="StraightLineTravelModel"/>.</param>
        /// <param name="options">Einstellungen; Standard ist <see cref="ReferenceEngineOptions"/>.</param>
        public ReferencePlanningEngine(ITravelModel? travelModel = null, ReferenceEngineOptions? options = null)
        {
            _travelModel = travelModel ?? new StraightLineTravelModel();
            _options = options ?? new ReferenceEngineOptions();
            _simulator = new ScheduleSimulator(_travelModel);
        }

        /// <inheritdoc/>
        public Task<PlanningResult> PlanAsync(PlanningRequest request, IFleetState fleet, IPlanningItems items, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(fleet);
            ArgumentNullException.ThrowIfNull(items);

            return Task.FromResult(Plan(request, fleet.Vehicles, items.Items, cancellationToken));
        }

        private PlanningResult Plan(PlanningRequest request, IReadOnlyList<VehicleState> vehicles, IReadOnlyList<PlanningItem> items, CancellationToken cancellationToken)
        {
            if (!TryOrderItems(items, out var orderedItems, out var inputProblem))
            {
                return new PlanningResult
                {
                    Successful = false,
                    Reason = $"{ReferenceExclusionReasons.InvalidInput}: {inputProblem}",
                };
            }

            if (vehicles.Count == 0)
            {
                return new PlanningResult { Successful = true, Reason = ReferenceExclusionReasons.NoVehicles };
            }

            var candidates = new List<Candidate>();
            var exclusions = new List<ExclusionInfo>();

            foreach (var vehicle in vehicles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outcome = EvaluateVehicle(request, vehicle, orderedItems);

                if (outcome.Candidate is { } candidate)
                {
                    candidates.Add(candidate);
                }
                else
                {
                    exclusions.Add(new ExclusionInfo { Reason = outcome.Reason!, Detail = $"Tour {vehicle.TourId}" });
                }
            }

            var maxProposals = request.MaxProposals > 0 ? request.MaxProposals : _options.DefaultMaxProposals;

            var proposals = candidates
                .OrderBy(c => c.AddedDistanceMeters)
                .ThenBy(c => c.Vehicle.VehicleId, StringComparer.Ordinal)
                .ThenBy(c => c.Vehicle.TourId, StringComparer.Ordinal)
                .Take(maxProposals)
                .Select(c => ToProposal(request, c, orderedItems))
                .ToList();

            return new PlanningResult
            {
                Successful = true,
                Proposals = proposals,
                Exclusions = exclusions,
                Reason = proposals.Count == 0 ? Summarize(exclusions) : null,
            };
        }

        private VehicleOutcome EvaluateVehicle(PlanningRequest request, VehicleState vehicle, IReadOnlyList<PlanningItem> items)
        {
            var requiredSkills = items.SelectMany(i => i.Skills).Distinct().ToList();

            if (requiredSkills.Exists(skill => !vehicle.Skills.Contains(skill)))
            {
                return VehicleOutcome.Excluded(ReferenceExclusionReasons.MissingSkills);
            }

            var stops = vehicle.Stops;
            var fixedCount = FixedStopCount(vehicle);
            var prefix = stops.Take(fixedCount).ToList();
            var tail = stops.Skip(fixedCount).ToList();

            var position = vehicle.CurrentLocation
                           ?? (prefix.Count > 0 ? prefix[^1].Location : (GeoPoint?)null)
                           ?? (tail.Count > 0 && tail[0].Kind == PlannedStopKind.Depot ? tail[0].Location : (GeoPoint?)null);

            if (position is null)
            {
                return VehicleOutcome.Excluded(ReferenceExclusionReasons.UnknownPosition);
            }

            if (request.MaxSearchRadiusMeters > 0
                && GeoMath.DistanceMeters(position.Value, items[0].Location) > request.MaxSearchRadiusMeters)
            {
                return VehicleOutcome.Excluded(ReferenceExclusionReasons.OutOfRange);
            }

            var start = BuildStartState(vehicle, stops, prefix, position.Value);
            var consumption = vehicle.ConsumptionWhPerMeter > 0 ? vehicle.ConsumptionWhPerMeter : _options.DefaultConsumptionWhPerMeter;
            var firstOrder = prefix.Count + 1;

            var baseline = _simulator.Simulate(vehicle, start, tail.ConvertAll(SequenceStop.Of), request, consumption, firstOrder);

            if (!baseline.Feasible)
            {
                return VehicleOutcome.Excluded(ReferenceExclusionReasons.ExistingScheduleInfeasible);
            }

            var (minSlot, maxSlot) = InsertionSlots(prefix.Count, tail);
            var failures = new Dictionary<string, int>(StringComparer.Ordinal);

            SimulationResult? best = items.Count <= 2
                ? InsertExhaustively(vehicle, start, tail, items, request, consumption, firstOrder, minSlot, maxSlot, failures)
                : InsertSequentially(vehicle, start, tail, items, request, consumption, firstOrder, minSlot, maxSlot, failures);

            if (best is null)
            {
                var dominant = failures.Count == 0
                    ? ReferenceExclusionReasons.TimeWindow
                    : failures.OrderByDescending(f => f.Value).ThenBy(f => f.Key, StringComparer.Ordinal).First().Key;

                return VehicleOutcome.Excluded(dominant);
            }

            var schedule = prefix.Select((s, i) => Renumber(s, i + 1, vehicle.TourId)).Concat(best.Stops).ToList();

            return new VehicleOutcome(
                new Candidate(vehicle, schedule, best.TotalDistanceMeters - baseline.TotalDistanceMeters),
                null);
        }

        /// <summary>Alle Einfügepositionen für ein oder zwei Halte (Reihenfolge wie übergeben).</summary>
        private SimulationResult? InsertExhaustively(
            VehicleState vehicle, StartState start, List<PlannedStop> tail, IReadOnlyList<PlanningItem> items,
            PlanningRequest request, double consumption, int firstOrder, int minSlot, int maxSlot, Dictionary<string, int> failures)
        {
            SimulationResult? best = null;

            for (var i = minSlot; i <= maxSlot; i++)
            {
                if (items.Count == 1)
                {
                    best = Better(best, Try(Insert(tail, (i, items[0]))));
                    continue;
                }

                for (var j = i; j <= maxSlot; j++)
                {
                    best = Better(best, Try(Insert(tail, (i, items[0]), (j, items[1]))));
                }
            }

            return best;

            SimulationResult? Try(List<SequenceStop> sequence)
            {
                var result = _simulator.Simulate(vehicle, start, sequence, request, consumption, firstOrder);

                if (!result.Feasible)
                {
                    failures[result.FailureReason!] = failures.GetValueOrDefault(result.FailureReason!) + 1;
                    return null;
                }

                return result;
            }
        }

        /// <summary>Mehr als zwei Halte: nacheinander, jeweils an der günstigsten zulässigen Position hinter
        /// dem zuvor eingefügten Halt. Zwischenstände werden vollständig geprüft; ein Einstieg ohne seinen
        /// Ausstieg belegt dabei Kapazität bis zum Ende der Folge, ist also eher zu streng als zu lax.</summary>
        private SimulationResult? InsertSequentially(
            VehicleState vehicle, StartState start, List<PlannedStop> tail, IReadOnlyList<PlanningItem> items,
            PlanningRequest request, double consumption, int firstOrder, int minSlot, int maxSlot, Dictionary<string, int> failures)
        {
            var sequence = tail.ConvertAll(SequenceStop.Of);
            var lowerBound = minSlot;
            var upperBound = maxSlot;
            SimulationResult? current = null;

            foreach (var item in items)
            {
                SimulationResult? bestForItem = null;
                var bestPosition = -1;

                for (var p = lowerBound; p <= upperBound; p++)
                {
                    var candidate = new List<SequenceStop>(sequence);
                    candidate.Insert(p, SequenceStop.Of(item));

                    var result = _simulator.Simulate(vehicle, start, candidate, request, consumption, firstOrder);

                    if (!result.Feasible)
                    {
                        failures[result.FailureReason!] = failures.GetValueOrDefault(result.FailureReason!) + 1;
                        continue;
                    }

                    if (bestForItem is null || result.TotalDistanceMeters < bestForItem.TotalDistanceMeters)
                    {
                        bestForItem = result;
                        bestPosition = p;
                    }
                }

                if (bestForItem is null)
                {
                    return null;
                }

                sequence.Insert(bestPosition, SequenceStop.Of(item));
                lowerBound = bestPosition + 1;
                upperBound++;
                current = bestForItem;
            }

            return current;
        }

        private static List<SequenceStop> Insert(List<PlannedStop> tail, params (int Slot, PlanningItem Item)[] insertions)
        {
            var sequence = new List<SequenceStop>(tail.Count + insertions.Length);
            var next = 0;

            for (var position = 0; position <= tail.Count; position++)
            {
                while (next < insertions.Length && insertions[next].Slot == position)
                {
                    sequence.Add(SequenceStop.Of(insertions[next].Item));
                    next++;
                }

                if (position < tail.Count)
                {
                    sequence.Add(SequenceStop.Of(tail[position]));
                }
            }

            return sequence;
        }

        private static SimulationResult? Better(SimulationResult? best, SimulationResult? candidate)
        {
            if (candidate is null)
            {
                return best;
            }

            return best is null || candidate.TotalDistanceMeters < best.TotalDistanceMeters ? candidate : best;
        }

        /// <summary>Erlaubte Einfügepositionen in der veränderlichen Folge: nicht vor einem noch nicht
        /// angefahrenen Depot-Start, nicht hinter dem Depot-Ende.</summary>
        private static (int Min, int Max) InsertionSlots(int prefixCount, List<PlannedStop> tail)
        {
            var min = prefixCount == 0 && tail.Count > 0 && tail[0].Kind == PlannedStopKind.Depot ? 1 : 0;
            var max = tail.Count > min && tail[^1].Kind == PlannedStopKind.Depot ? tail.Count - 1 : tail.Count;

            return (min, max);
        }

        /// <summary>Anzahl der unveränderlichen Halte am Anfang des Ist-Fahrplans.</summary>
        private static int FixedStopCount(VehicleState vehicle)
        {
            var stops = vehicle.Stops;
            var byKey = -1;

            if (vehicle.LastFixedStopKey is { } key)
            {
                for (var i = 0; i < stops.Count; i++)
                {
                    if (string.Equals(stops[i].Key, key, StringComparison.Ordinal))
                    {
                        byKey = i;
                    }
                }
            }

            var byTime = -1;

            if (vehicle.AvailableFrom is { } availableFrom)
            {
                for (var i = 0; i < stops.Count; i++)
                {
                    if (stops[i].Departure < availableFrom)
                    {
                        byTime = i;
                    }
                }
            }

            return Math.Max(byKey, byTime) + 1;
        }

        private static StartState BuildStartState(VehicleState vehicle, IReadOnlyList<PlannedStop> allStops, List<PlannedStop> prefix, GeoPoint position)
        {
            // Wer vor Beginn der Liste schon zugestiegen ist, taucht nur mit seinem Ausstieg auf. Die
            // laufende Belegung wird deshalb so angehoben, dass sie nie negativ wird.
            var baseline = new Dictionary<string, float>(StringComparer.Ordinal);
            var running = new Dictionary<string, float>(StringComparer.Ordinal);
            var passengersRunning = 0;
            var passengersBaseline = 0;

            foreach (var stop in allStops)
            {
                var sign = SignOf(stop.Kind);

                if (sign == 0)
                {
                    continue;
                }

                passengersRunning += sign;
                passengersBaseline = Math.Max(passengersBaseline, -passengersRunning);

                foreach (var (key, amount) in stop.Quantities)
                {
                    running[key] = running.GetValueOrDefault(key) + (sign * amount);
                    baseline[key] = Math.Max(baseline.GetValueOrDefault(key), -running[key]);
                }
            }

            var load = new Dictionary<string, float>(baseline, StringComparer.Ordinal);
            var passengers = passengersBaseline;

            foreach (var stop in prefix)
            {
                var sign = SignOf(stop.Kind);
                passengers += sign;

                foreach (var (key, amount) in stop.Quantities)
                {
                    load[key] = load.GetValueOrDefault(key) + (sign * amount);
                }
            }

            var time = vehicle.Shift.Min;

            if (vehicle.AvailableFrom is { } availableFrom && availableFrom > time)
            {
                time = availableFrom;
            }

            if (prefix.Count > 0 && prefix[^1].Departure > time)
            {
                time = prefix[^1].Departure;
            }

            return new StartState
            {
                Time = time,
                Position = position,
                EnergyWh = prefix.Count > 0 ? prefix[^1].RemainingEnergy : vehicle.CurrentEnergyWh,
                Load = load,
                OtherPassengersOnboard = passengers,
            };
        }

        private static int SignOf(PlannedStopKind kind) => kind switch
        {
            PlannedStopKind.Pickup => 1,
            PlannedStopKind.Dropoff => -1,
            _ => 0,
        };

        private static PlannedStop Renumber(PlannedStop stop, int order, string tourId) => new()
        {
            Location = stop.Location,
            Order = order,
            Kind = stop.Kind,
            Key = stop.Key,
            Arrival = stop.Arrival,
            ServiceStart = stop.ServiceStart,
            Departure = stop.Departure,
            ServiceSeconds = stop.ServiceSeconds,
            DrivingSeconds = stop.DrivingSeconds,
            DistanceMeters = stop.DistanceMeters,
            TimeWindow = stop.TimeWindow,
            ConsumedEnergy = stop.ConsumedEnergy,
            ChargedEnergy = stop.ChargedEnergy,
            RemainingEnergy = stop.RemainingEnergy,
            TransferSegmentId = stop.TransferSegmentId,
            VehicleScheduleId = tourId,
            Skills = stop.Skills,
            Quantities = stop.Quantities,
            Priority = stop.Priority,
        };

        private static PlanningProposal ToProposal(PlanningRequest request, Candidate candidate, IReadOnlyList<PlanningItem> items)
        {
            var requestedKeys = items.Select(i => i.Key).ToHashSet(StringComparer.Ordinal);
            var schedule = candidate.Schedule;

            var first = schedule.First(s => s.Key is not null && requestedKeys.Contains(s.Key));
            var last = schedule.Last(s => s.Key is not null && requestedKeys.Contains(s.Key));
            var firstIndex = schedule.IndexOf(first);
            var lastIndex = schedule.IndexOf(last);

            var passengerDistance = schedule.Skip(firstIndex + 1).Take(lastIndex - firstIndex).Sum(s => (double)s.DistanceMeters);

            return new PlanningProposal
            {
                VehicleId = candidate.Vehicle.VehicleId,
                TourId = candidate.Vehicle.TourId,
                Schedules = [new PlannedSchedule { VehicleScheduleId = candidate.Vehicle.TourId, Stops = schedule }],
                TransactionId = request.CorrelationId,
                RequestedPickupKey = first.Key,
                RequestedDropoffKey = last.Key,
                PickupWindow = first.TimeWindow ?? PromisedWindow(first, request),
                DropoffWindow = last.TimeWindow ?? PromisedWindow(last, request),
                Efficiency = candidate.AddedDistanceMeters,
                PassengerDistanceMeters = passengerDistance,
            };
        }

        private static TimeWindow PromisedWindow(PlannedStop stop, PlanningRequest request) =>
            new(stop.ServiceStart.Subtract(request.ToleratedDelayBefore), stop.ServiceStart.Add(request.ToleratedDelayAfter));

        /// <summary>Bringt die Halte in eine Reihenfolge, die alle <see cref="PlanningItem.Predecessors"/>
        /// einhält (stabil: bei freier Wahl gilt die übergebene Reihenfolge).</summary>
        private static bool TryOrderItems(IReadOnlyList<PlanningItem> items, out List<PlanningItem> ordered, out string? problem)
        {
            ordered = [];
            problem = null;

            if (items.Count == 0)
            {
                problem = "keine einzuplanenden Halte";
                return false;
            }

            var unsupported = items.FirstOrDefault(i => i.Kind is not (PlannedStopKind.Pickup or PlannedStopKind.Dropoff));

            if (unsupported is not null)
            {
                problem = $"Halt '{unsupported.Key}' hat die Art {unsupported.Kind}; unterstützt sind Pickup und Dropoff";
                return false;
            }

            var keys = items.Select(i => i.Key).ToHashSet(StringComparer.Ordinal);
            var unknown = items.SelectMany(i => i.Predecessors.Select(p => (i.Key, Predecessor: p))).FirstOrDefault(x => !keys.Contains(x.Predecessor));

            if (unknown.Predecessor is not null)
            {
                problem = $"Halt '{unknown.Key}' nennt den unbekannten Vorgänger '{unknown.Predecessor}'";
                return false;
            }

            var placed = new HashSet<string>(StringComparer.Ordinal);
            var remaining = items.ToList();

            while (remaining.Count > 0)
            {
                var next = remaining.Find(i => i.Predecessors.All(placed.Contains));

                if (next is null)
                {
                    problem = "die Vorgänger-Angaben enthalten einen Zyklus";
                    return false;
                }

                ordered.Add(next);
                placed.Add(next.Key);
                remaining.Remove(next);
            }

            return true;
        }

        private static string Summarize(List<ExclusionInfo> exclusions)
        {
            var summary = exclusions
                .GroupBy(e => e.Reason, StringComparer.Ordinal)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => $"{g.Key}×{g.Count()}");

            return $"{ReferenceExclusionReasons.NoFeasibleVehicle}: {string.Join(", ", summary)}";
        }

        private sealed record Candidate(VehicleState Vehicle, List<PlannedStop> Schedule, long AddedDistanceMeters);

        private sealed record VehicleOutcome(Candidate? Candidate, string? Reason)
        {
            public static VehicleOutcome Excluded(string reason) => new(null, reason);
        }
    }
}
