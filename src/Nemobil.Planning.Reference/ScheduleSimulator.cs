using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Reference
{
    /// <summary>Ein Halt einer zu prüfenden Halt-Folge: entweder ein bestehender Halt des Fahrzeugs oder
    /// ein einzuplanender Halt der Anfrage.</summary>
    internal readonly record struct SequenceStop(PlannedStop? Existing, PlanningItem? Item)
    {
        public static SequenceStop Of(PlannedStop stop) => new(stop, null);

        public static SequenceStop Of(PlanningItem item) => new(null, item);

        public bool IsRequested => Item is not null;

        public GeoPoint Location => Existing?.Location ?? Item!.Location;

        public PlannedStopKind Kind => Existing?.Kind ?? Item!.Kind;

        public string? Key => Existing is not null ? Existing.Key : Item!.Key;

        public TimeWindow? Window => Existing is not null ? Existing.TimeWindow : Item!.TimeWindow;

        public int ServiceSeconds => Existing?.ServiceSeconds ?? Item!.ServiceSeconds;

        public IReadOnlyDictionary<string, float> Quantities => Existing?.Quantities ?? Item!.Quantities;

        public int ChargedEnergy => Existing?.ChargedEnergy ?? 0;
    }

    /// <summary>Zustand des Fahrzeugs am Beginn der veränderlichen Halt-Folge.</summary>
    internal sealed class StartState
    {
        public required DateTime Time { get; init; }

        public required GeoPoint? Position { get; init; }

        public required int EnergyWh { get; init; }

        public required IReadOnlyDictionary<string, float> Load { get; init; }

        public required int OtherPassengersOnboard { get; init; }
    }

    /// <summary>Ergebnis der Simulation einer Halt-Folge.</summary>
    internal sealed class SimulationResult
    {
        public required bool Feasible { get; init; }

        public string? FailureReason { get; init; }

        public IReadOnlyList<PlannedStop> Stops { get; init; } = [];

        public long TotalDistanceMeters { get; init; }

        public static SimulationResult Fail(string reason) => new() { Feasible = false, FailureReason = reason };
    }

    /// <summary>Rechnet eine Halt-Folge vorwärts durch: Fahrzeiten, Wartezeiten bis zum Zeitfenster,
    /// Bedienzeiten, Energie und Belegung — und prüft dabei jede harte Randbedingung.</summary>
    internal sealed class ScheduleSimulator
    {
        private readonly ITravelModel _travelModel;

        public ScheduleSimulator(ITravelModel travelModel)
        {
            _travelModel = travelModel;
        }

        public SimulationResult Simulate(
            VehicleState vehicle,
            StartState start,
            IReadOnlyList<SequenceStop> sequence,
            PlanningRequest request,
            double consumptionWhPerMeter,
            int firstOrder)
        {
            var time = start.Time;
            var position = start.Position;
            var energy = start.EnergyWh;
            var load = new Dictionary<string, float>(start.Load, StringComparer.Ordinal);
            var otherPassengers = start.OtherPassengersOnboard;

            var energyTracked = vehicle.EnergyCapacityWh > 0;
            var shiftEarliest = vehicle.Shift.Min.AddSeconds(-vehicle.Shift.ToleranceBefore);
            var shiftLatest = vehicle.Shift.Max.AddSeconds(vehicle.Shift.ToleranceAfter);

            var lastRequestedIndex = LastIndexOf(sequence, s => s.IsRequested);
            var ownRideActive = false;

            var stops = new List<PlannedStop>(sequence.Count);
            long totalDistance = 0;

            for (var i = 0; i < sequence.Count; i++)
            {
                var stop = sequence[i];
                var travel = position is { } from ? _travelModel.Estimate(from, stop.Location) : default;

                var arrival = time.AddSeconds(travel.DrivingSeconds);
                var serviceStart = arrival < shiftEarliest ? shiftEarliest : arrival;

                if (stop.Window is { } window)
                {
                    var earliest = window.Min.AddSeconds(-window.ToleranceBefore);
                    var latest = window.Max.AddSeconds(window.ToleranceAfter);

                    if (serviceStart < earliest)
                    {
                        serviceStart = earliest;
                    }

                    if (serviceStart > latest)
                    {
                        return SimulationResult.Fail(ReferenceExclusionReasons.TimeWindow);
                    }
                }

                var departure = serviceStart.AddSeconds(stop.ServiceSeconds);

                if (departure > shiftLatest)
                {
                    return SimulationResult.Fail(ReferenceExclusionReasons.Shift);
                }

                var consumed = (int)Math.Round(travel.DistanceMeters * consumptionWhPerMeter, MidpointRounding.AwayFromZero);
                var charged = stop.ChargedEnergy;

                if (energyTracked)
                {
                    energy = Math.Min(vehicle.EnergyCapacityWh, energy - consumed + charged);

                    if (energy < 0)
                    {
                        return SimulationResult.Fail(ReferenceExclusionReasons.Energy);
                    }
                }

                if (!request.AllowCarpooling)
                {
                    if (stop.IsRequested && stop.Kind == PlannedStopKind.Pickup && !ownRideActive)
                    {
                        if (otherPassengers > 0)
                        {
                            return SimulationResult.Fail(ReferenceExclusionReasons.Carpooling);
                        }

                        ownRideActive = true;
                    }
                    else if (!stop.IsRequested && stop.Kind == PlannedStopKind.Pickup && ownRideActive)
                    {
                        return SimulationResult.Fail(ReferenceExclusionReasons.Carpooling);
                    }
                }

                if (!ApplyLoad(stop, load, vehicle.Capacity))
                {
                    return SimulationResult.Fail(ReferenceExclusionReasons.Capacity);
                }

                if (!stop.IsRequested)
                {
                    otherPassengers += stop.Kind switch
                    {
                        PlannedStopKind.Pickup => 1,
                        PlannedStopKind.Dropoff => -1,
                        _ => 0,
                    };
                }

                if (i == lastRequestedIndex)
                {
                    ownRideActive = false;
                }

                stops.Add(BuildStop(vehicle, stop, firstOrder + i, arrival, serviceStart, departure, travel, consumed, charged, energyTracked ? energy : stop.Existing?.RemainingEnergy ?? 0));

                totalDistance += travel.DistanceMeters;
                time = departure;
                position = stop.Location;
            }

            return new SimulationResult { Feasible = true, Stops = stops, TotalDistanceMeters = totalDistance };
        }

        /// <summary>Belegt (Einstieg) bzw. gibt frei (Ausstieg) und prüft die Kapazitäten.</summary>
        private static bool ApplyLoad(SequenceStop stop, Dictionary<string, float> load, IReadOnlyDictionary<string, float> capacity)
        {
            var sign = stop.Kind switch
            {
                PlannedStopKind.Pickup => 1f,
                PlannedStopKind.Dropoff => -1f,
                _ => 0f,
            };

            if (sign == 0f)
            {
                return true;
            }

            foreach (var (key, amount) in stop.Quantities)
            {
                load[key] = load.GetValueOrDefault(key) + (sign * amount);

                if (capacity.TryGetValue(key, out var max) && load[key] > max + 1e-6f)
                {
                    return false;
                }
            }

            return true;
        }

        private static PlannedStop BuildStop(
            VehicleState vehicle,
            SequenceStop stop,
            int order,
            DateTime arrival,
            DateTime serviceStart,
            DateTime departure,
            TravelEstimate travel,
            int consumed,
            int charged,
            int remaining)
        {
            if (stop.Existing is { } existing)
            {
                return new PlannedStop
                {
                    Location = existing.Location,
                    Order = order,
                    Kind = existing.Kind,
                    Key = existing.Key,
                    Arrival = arrival,
                    ServiceStart = serviceStart,
                    Departure = departure,
                    ServiceSeconds = existing.ServiceSeconds,
                    DrivingSeconds = travel.DrivingSeconds,
                    DistanceMeters = travel.DistanceMeters,
                    TimeWindow = existing.TimeWindow,
                    ConsumedEnergy = consumed,
                    ChargedEnergy = charged,
                    RemainingEnergy = remaining,
                    TransferSegmentId = existing.TransferSegmentId,
                    VehicleScheduleId = vehicle.TourId,
                    Skills = existing.Skills,
                    Quantities = existing.Quantities,
                    Priority = existing.Priority,
                };
            }

            var item = stop.Item!;

            return new PlannedStop
            {
                Location = item.Location,
                Order = order,
                Kind = item.Kind,
                Key = item.Key,
                Arrival = arrival,
                ServiceStart = serviceStart,
                Departure = departure,
                ServiceSeconds = item.ServiceSeconds,
                DrivingSeconds = travel.DrivingSeconds,
                DistanceMeters = travel.DistanceMeters,
                TimeWindow = item.TimeWindow,
                ConsumedEnergy = consumed,
                ChargedEnergy = charged,
                RemainingEnergy = remaining,
                VehicleScheduleId = vehicle.TourId,
                Skills = item.Skills,
                Quantities = item.Quantities,
                Priority = item.Priority,
            };
        }

        private static int LastIndexOf(IReadOnlyList<SequenceStop> sequence, Func<SequenceStop, bool> predicate)
        {
            for (var i = sequence.Count - 1; i >= 0; i--)
            {
                if (predicate(sequence[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
