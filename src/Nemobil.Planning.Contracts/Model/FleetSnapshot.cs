namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Offene Standard-Implementierung von <see cref="IFleetState"/>: ein unveränderlicher
    /// Schnappschuss einer Flotte aus <see cref="VehicleState"/>-Einträgen.</summary>
    public sealed class FleetSnapshot : IFleetState
    {
        /// <summary>Erzeugt den Schnappschuss. Die Schicht-Kennungen (<see cref="VehicleState.TourId"/>)
        /// müssen eindeutig sein.</summary>
        /// <exception cref="ArgumentException">Eine <see cref="VehicleState.TourId"/> kommt mehrfach vor.</exception>
        public FleetSnapshot(IEnumerable<VehicleState> vehicles)
        {
            ArgumentNullException.ThrowIfNull(vehicles);

            var list = vehicles.ToList();
            var duplicate = list.GroupBy(v => v.TourId, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
            {
                throw new ArgumentException($"TourId '{duplicate.Key}' kommt mehrfach vor.", nameof(vehicles));
            }

            Vehicles = list.AsReadOnly();
            Tours = list.ConvertAll(v => new TourRef(v.TourId)).AsReadOnly();
        }

        /// <inheritdoc/>
        public IReadOnlyList<TourRef> Tours { get; }

        /// <inheritdoc/>
        public IReadOnlyList<VehicleState> Vehicles { get; }
    }
}
