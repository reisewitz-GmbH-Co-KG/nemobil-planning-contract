using Nemobil.Planning.Conformance;
using Nemobil.Planning.Contracts;
using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Reference.Tests
{
    /// <summary>Gegenprobe der Konformitätssuite: jede Prüfregel muss einen gezielt eingebauten Verstoß
    /// erkennen. Ohne diese Tests könnte eine Regel still wirkungslos sein und die Suite trotzdem grün
    /// melden. Ausgangspunkt ist jeweils das (konforme) Ergebnis der Referenz, an dem genau eine Stelle
    /// verfälscht wird.</summary>
    public class ConformanceChecksTests
    {
        private static async Task<(ConformanceScenario Scenario, PlanningResult Result)> ReferenceRunAsync(string scenarioId)
        {
            var scenario = ConformanceScenarios.Get(scenarioId);
            var result = await new ReferencePlanningEngine().PlanAsync(scenario.Request, scenario.CreateFleet(), scenario.CreateItems(), CancellationToken.None);

            return (scenario, result);
        }

        private static IReadOnlyList<string> AllChecks(ConformanceScenario scenario, PlanningResult result) =>
            ConformanceChecks.CheckExpectation(scenario, result)
                .Concat(ConformanceChecks.CheckInvariants(scenario, result))
                .Concat(ConformanceChecks.CheckContract(scenario, result))
                .Concat(ConformanceChecks.CheckSchedules(scenario, result))
                .ToList();

        private static void AssertFlagged(IReadOnlyList<string> violations, string rule) =>
            Assert.True(violations.Any(v => v.StartsWith(rule, StringComparison.Ordinal)), $"Erwartet: Verstoß {rule}. Gemeldet: {string.Join(" | ", violations)}");

        private static PlanningResult WithFirstSchedule(PlanningResult result, Func<IReadOnlyList<PlannedStop>, IReadOnlyList<PlannedStop>> change, Func<PlanningProposal, PlanningProposal>? proposalChange = null)
        {
            var proposal = result.Proposals[0];
            var changed = Copy(proposal, [new PlannedSchedule { VehicleScheduleId = proposal.Schedules[0].VehicleScheduleId, Stops = change(proposal.Schedules[0].Stops) }]);

            return new PlanningResult
            {
                Successful = result.Successful,
                Proposals = [proposalChange?.Invoke(changed) ?? changed, .. result.Proposals.Skip(1)],
                Exclusions = result.Exclusions,
                Reason = result.Reason,
            };
        }

        private static PlanningProposal Copy(PlanningProposal p, IReadOnlyList<PlannedSchedule>? schedules = null, TimeWindow? pickupWindow = null, bool clearPickupWindow = false, string? tourId = null) => new()
        {
            VehicleId = p.VehicleId,
            TourId = tourId ?? p.TourId,
            Schedules = schedules ?? p.Schedules,
            TransactionId = p.TransactionId,
            RequestedPickupKey = p.RequestedPickupKey,
            RequestedDropoffKey = p.RequestedDropoffKey,
            PickupWindow = clearPickupWindow ? null : pickupWindow ?? p.PickupWindow,
            DropoffWindow = p.DropoffWindow,
            Efficiency = p.Efficiency,
            PassengerDistanceMeters = p.PassengerDistanceMeters,
        };

        private static PlannedStop Copy(
            PlannedStop s,
            int? order = null,
            string? key = null,
            PlannedStopKind? kind = null,
            DateTime? arrival = null,
            DateTime? serviceStart = null,
            DateTime? departure = null,
            int? remaining = null) => new()
        {
            Location = s.Location,
            Order = order ?? s.Order,
            Kind = kind ?? s.Kind,
            Key = key ?? s.Key,
            Arrival = arrival ?? s.Arrival,
            ServiceStart = serviceStart ?? s.ServiceStart,
            Departure = departure ?? s.Departure,
            ServiceSeconds = s.ServiceSeconds,
            DrivingSeconds = s.DrivingSeconds,
            DistanceMeters = s.DistanceMeters,
            TimeWindow = s.TimeWindow,
            ConsumedEnergy = s.ConsumedEnergy,
            ChargedEnergy = s.ChargedEnergy,
            RemainingEnergy = remaining ?? s.RemainingEnergy,
            TransferSegmentId = s.TransferSegmentId,
            VehicleScheduleId = s.VehicleScheduleId,
            Skills = s.Skills,
            Quantities = s.Quantities,
            Priority = s.Priority,
        };

        private static PlannedStop FromItem(PlanningItem item, int order, string tourId) => new()
        {
            Location = item.Location,
            Order = order,
            Kind = item.Kind,
            Key = item.Key,
            TimeWindow = item.TimeWindow,
            ServiceSeconds = item.ServiceSeconds,
            Quantities = item.Quantities,
            VehicleScheduleId = tourId,
        };

        private static IReadOnlyList<PlannedStop> Renumber(IEnumerable<PlannedStop> stops) =>
            stops.Select((s, i) => Copy(s, order: i + 1)).ToList();

        [Fact]
        public async Task Referenz_IstInAllenPrueffaellenKonform()
        {
            foreach (var id in ConformanceScenarios.Ids)
            {
                var (scenario, result) = await ReferenceRunAsync(id);

                Assert.Empty(AllChecks(scenario, result));
            }
        }

        [Fact]
        public async Task G1_ErkenntFehlendenVorschlag()
        {
            var (scenario, _) = await ReferenceRunAsync("G1-01");

            AssertFlagged(ConformanceChecks.CheckExpectation(scenario, new PlanningResult { Successful = true, Reason = "x" }), "[G1]");
        }

        [Fact]
        public async Task G2_ErkenntVerletztesZeitfenster()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");
            var late = scenario.Items[0].TimeWindow!.Value.Max.AddMinutes(10);

            var broken = WithFirstSchedule(result, stops => stops
                .Select(s => s.Key == "g1-01_Pickup" ? Copy(s, arrival: late, serviceStart: late, departure: late.AddSeconds(s.ServiceSeconds)) : s)
                .ToList());

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, broken), "[G2-Zeitfenster]");
        }

        [Fact]
        public async Task G2_ErkenntUeberbuchteKapazitaet()
        {
            // Fahrzeug 1 befoerdert schon drei Erwachsene; die Anfrage bringt zwei weitere bei vier Plaetzen.
            var (scenario, result) = await ReferenceRunAsync("G2-02");
            var vehicle = scenario.Vehicles.Single(v => v.TourId == "tour-1");
            var stops = vehicle.Stops;
            var pickup = FromItem(scenario.Items[0], 0, vehicle.TourId);
            var dropoff = FromItem(scenario.Items[1], 0, vehicle.TourId);

            var overbooked = Renumber([stops[0], stops[1], pickup, stops[2], dropoff, stops[3]]);
            var proposal = new PlanningProposal
            {
                VehicleId = vehicle.VehicleId,
                TourId = vehicle.TourId,
                Schedules = [new PlannedSchedule { VehicleScheduleId = vehicle.TourId, Stops = overbooked }],
            };

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, new PlanningResult { Successful = true, Proposals = [proposal] }), "[G2-Kapazität]");
        }

        [Fact]
        public async Task G2_ErkenntMitnahmeTrotzVerbot()
        {
            var (scenario, _) = await ReferenceRunAsync("G1-05");
            var vehicle = scenario.Vehicles.Single(v => v.TourId == "tour-a");
            var stops = vehicle.Stops;

            var shared = Renumber([stops[0], stops[1], FromItem(scenario.Items[0], 0, vehicle.TourId), FromItem(scenario.Items[1], 0, vehicle.TourId), stops[2], stops[3]]);
            var proposal = new PlanningProposal
            {
                VehicleId = vehicle.VehicleId,
                TourId = vehicle.TourId,
                Schedules = [new PlannedSchedule { VehicleScheduleId = vehicle.TourId, Stops = shared }],
            };

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, new PlanningResult { Successful = true, Proposals = [proposal] }), "[G2-Mitnahme]");
        }

        [Fact]
        public async Task G2_ErkenntVerletzteVorgaengerReihenfolge()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => Renumber([stops[0], stops[2], stops[1], stops[3]]));

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, broken), "[G2-Reihenfolge]");
        }

        [Fact]
        public async Task G2_ErkenntNegativeRestenergie()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => stops.Select((s, i) => i == 2 ? Copy(s, remaining: -5) : s).ToList());

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, broken), "[G2-Energie]");
        }

        [Fact]
        public async Task G3_ErkenntUeberschritteneObergrenze()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-02");

            var tooMany = new PlanningResult { Successful = true, Proposals = [.. result.Proposals, .. result.Proposals] };

            AssertFlagged(ConformanceChecks.CheckContract(scenario, tooMany), "[G3-Obergrenze]");
        }

        [Fact]
        public async Task G3_ErkenntLeeresErgebnisOhneDiagnose()
        {
            var (scenario, _) = await ReferenceRunAsync("G1-03");

            AssertFlagged(ConformanceChecks.CheckContract(scenario, new PlanningResult { Successful = true }), "[G3-Diagnose]");
        }

        [Fact]
        public async Task G3_ErkenntFehlendesZugesagtesZeitfenster()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => stops, p => Copy(p, clearPickupWindow: true));

            AssertFlagged(ConformanceChecks.CheckContract(scenario, broken), "[G3-Buchbarkeit]");
        }

        [Fact]
        public async Task G3_ErkenntFremdeSchicht()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => stops, p => Copy(p, tourId: "gibt-es-nicht"));

            AssertFlagged(ConformanceChecks.CheckContract(scenario, broken), "[G3-Bezug]");
        }

        [Fact]
        public async Task G3_ErkenntNichtAnwendbareHaltArt()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => Renumber([.. stops.Take(3), Copy(stops[2], key: "parken", kind: PlannedStopKind.Parking), .. stops.Skip(3)]));

            AssertFlagged(ConformanceChecks.CheckContract(scenario, broken), "[G3-Halt-Art]");
        }

        [Fact]
        public async Task G3_ErkenntVeraenderteEingaben()
        {
            var scenario = ConformanceScenarios.Get("G1-01");
            var fleet = scenario.CreateFleet();
            var items = scenario.CreateItems();

            var before = ConformanceChecks.Snapshot(scenario.Request, fleet, items);
            await new MutatingEngine().PlanAsync(scenario.Request, fleet, items, CancellationToken.None);
            var after = ConformanceChecks.Snapshot(scenario.Request, fleet, items);

            AssertFlagged(ConformanceChecks.CheckInputsUnchanged(before, after), "[G3-Seiteneffekt]");
        }

        [Fact]
        public async Task G4_ErkenntDoppeltenSchluessel()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => Renumber([.. stops.Take(3), Copy(stops[2], key: stops[1].Key), .. stops.Skip(3)]));

            AssertFlagged(ConformanceChecks.CheckSchedules(scenario, broken), "[G4-Wohlgeformt]");
        }

        [Fact]
        public async Task G4_ErkenntInkonsistenteEnergiebilanz()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => stops.Select((s, i) => i == 2 ? Copy(s, remaining: s.RemainingEnergy + 500) : s).ToList());

            AssertFlagged(ConformanceChecks.CheckSchedules(scenario, broken), "[G4-Energie]");
        }

        [Fact]
        public async Task G4_ErkenntUnvollstaendigenFahrplan()
        {
            var (scenario, result) = await ReferenceRunAsync("G4-01");

            var broken = WithFirstSchedule(result, stops => Renumber(stops.Where(s => s.Key is not ("z1_Pickup" or "z1_Dropoff"))));

            AssertFlagged(ConformanceChecks.CheckSchedules(scenario, broken), "[G4-Vollständig]");
        }

        [Fact]
        public async Task G4_ErkenntVeraenderteFixierteHalte()
        {
            var (scenario, result) = await ReferenceRunAsync("G4-02");

            var broken = WithFirstSchedule(result, stops => stops.Select((s, i) => i == 1 ? Copy(s, departure: s.Departure.AddMinutes(1)) : s).ToList());

            AssertFlagged(ConformanceChecks.CheckSchedules(scenario, broken), "[G4-Fixiert]");
        }

        [Fact]
        public void FixierteHalte_ZaehlenBisZumSchluesselUndNachZeit()
        {
            var vehicle = ConformanceScenarios.Get("G4-02").Vehicles[0];

            Assert.Equal(2, ConformanceChecks.FixedStopCount(vehicle));
        }

        [Fact]
        public void Prueffaelle_LassenSichAufEinenAnderenTagVerschieben()
        {
            var nominal = ConformanceScenarios.Get("G1-01");
            var shifted = ConformanceScenarios.Get("G1-01", new DateOnly(2031, 3, 17));

            var offset = new DateTime(2031, 3, 17, 0, 0, 0, DateTimeKind.Utc) - new DateTime(2030, 1, 7, 0, 0, 0, DateTimeKind.Utc);

            Assert.Equal(nominal.Request.Pickup!.Value.Min + offset, shifted.Request.Pickup!.Value.Min);
            Assert.Equal(nominal.Vehicles[0].Shift.Max + offset, shifted.Vehicles[0].Shift.Max);
            Assert.Equal(nominal.Vehicles[0].Stops[0].Arrival + offset, shifted.Vehicles[0].Stops[0].Arrival);
            Assert.Equal(nominal.Request.ToleratedDelayAfter, shifted.Request.ToleratedDelayAfter);
        }

        [Fact]
        public void Prueffaelle_SindVollstaendigUndEindeutig()
        {
            var scenarios = ConformanceScenarios.All();

            Assert.Equal(14, scenarios.Count);
            Assert.All(scenarios, s => Assert.False(string.IsNullOrWhiteSpace(s.Description)));
            Assert.Equal(scenarios.Count, scenarios.Select(s => s.Id).Distinct().Count());
        }

        [Fact]
        public async Task G1_ErkenntUnerwartetenVorschlag()
        {
            var (_, reachable) = await ReferenceRunAsync("G1-01");
            var unreachable = ConformanceScenarios.Get("G1-03");

            AssertFlagged(ConformanceChecks.CheckExpectation(unreachable, reachable), "[G1]");
        }

        [Fact]
        public async Task G2_ErkenntHaltAusserhalbDerSchicht()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");
            var late = scenario.Vehicles[0].Shift.Max.AddHours(1);

            var broken = WithFirstSchedule(result, stops => stops
                .Select((s, i) => i == stops.Count - 1 ? Copy(s, arrival: late, serviceStart: late, departure: late) : s)
                .ToList());

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, broken), "[G2-Schicht]");
        }

        [Fact]
        public async Task G2_ErkenntFehlendeFaehigkeit()
        {
            var (scenario, _) = await ReferenceRunAsync("G2-05");
            var vehicle = scenario.Vehicles.Single(v => v.TourId == "tour-1");
            var stops = vehicle.Stops;

            var withoutSkill = Renumber([stops[0], FromItem(scenario.Items[0], 0, vehicle.TourId), FromItem(scenario.Items[1], 0, vehicle.TourId), stops[1]]);
            var proposal = new PlanningProposal
            {
                VehicleId = vehicle.VehicleId,
                TourId = vehicle.TourId,
                Schedules = [new PlannedSchedule { VehicleScheduleId = vehicle.TourId, Stops = withoutSkill }],
            };

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, new PlanningResult { Successful = true, Proposals = [proposal] }), "[G2-Fähigkeit]");
        }

        [Fact]
        public async Task G2_ErkenntUngepaartesKoppeln()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = WithFirstSchedule(result, stops => Renumber([.. stops.Take(2), Copy(stops[1], key: "koppeln", kind: PlannedStopKind.Chaining), .. stops.Skip(2)]));

            AssertFlagged(ConformanceChecks.CheckInvariants(scenario, broken), "[G2-Konvoi]");
        }

        [Fact]
        public async Task G3_ErkenntVorschlaegeOhneErfolgsmeldung()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            AssertFlagged(ConformanceChecks.CheckContract(scenario, new PlanningResult { Successful = false, Proposals = result.Proposals }), "[G3-Erfolg]");
        }

        [Fact]
        public async Task G3_ErkenntUngueltigeKennzahl()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");
            var p = result.Proposals[0];

            var broken = new PlanningProposal
            {
                VehicleId = p.VehicleId,
                TourId = p.TourId,
                Schedules = p.Schedules,
                RequestedPickupKey = p.RequestedPickupKey,
                RequestedDropoffKey = p.RequestedDropoffKey,
                PickupWindow = p.PickupWindow,
                DropoffWindow = p.DropoffWindow,
                Efficiency = double.NaN,
                PassengerDistanceMeters = -1,
            };

            AssertFlagged(ConformanceChecks.CheckContract(scenario, new PlanningResult { Successful = true, Proposals = [broken] }), "[G3-Kennzahl]");
        }

        [Fact]
        public async Task G3_ErkenntFehlendenFahrplan()
        {
            var (scenario, result) = await ReferenceRunAsync("G1-01");

            var broken = new PlanningResult { Successful = true, Proposals = [Copy(result.Proposals[0], schedules: [])] };

            AssertFlagged(ConformanceChecks.CheckContract(scenario, broken), "[G3-Fahrplan]");
        }

        /// <summary>Verändert eine Eingabe, bevor es an die Referenz delegiert.</summary>
        private sealed class MutatingEngine : IPlanningEngine
        {
            public Task<PlanningResult> PlanAsync(PlanningRequest request, IFleetState fleet, IPlanningItems items, CancellationToken cancellationToken)
            {
                if (fleet.Vehicles[0].Capacity is Dictionary<string, float> capacity)
                {
                    capacity["ADULTS"] = 99;
                }

                return new ReferencePlanningEngine().PlanAsync(request, fleet, items, cancellationToken);
            }
        }
    }
}
