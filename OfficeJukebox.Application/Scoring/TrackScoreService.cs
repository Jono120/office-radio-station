using System.Text.Json;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;

namespace OfficeJukebox.Application.Scoring;

public interface ITrackScoreService
{
    Task<IReadOnlyList<TrackScore>> ComputeScoresAsync(CancellationToken cancellationToken = default);
}

public sealed class TrackScoreService(
    ITrackPlayRepository trackPlayRepository,
    ITrackScoreRepository trackScoreRepository,
    ITrackIdentityComparer identityComparer,
    ITimeProvider timeProvider) : ITrackScoreService
{
    private const string AutoplayUserPrefix = "autoplay";
    private const int RandomDivisor = 3;

    public async Task<IReadOnlyList<TrackScore>> ComputeScoresAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = new Dictionary<string, ScoreAggregate>(StringComparer.OrdinalIgnoreCase);
        // UTC: compared against UTC StartedAt values and persisted as ComputedAt.
        var now = timeProvider.UtcNow;

        foreach (var play in trackPlayRepository.GetAll())
        {
            var trackRef = identityComparer.FromTrackPlay(play);
            if (trackRef.IsEmpty)
            {
                continue;
            }

            var key = trackRef.ToString();
            if (!aggregates.TryGetValue(key, out var aggregate))
            {
                aggregate = new ScoreAggregate { Provider = trackRef.Provider, ExternalId = trackRef.ExternalId };
                aggregates[key] = aggregate;
            }

            var isAutoplay = play.User.StartsWith(AutoplayUserPrefix, StringComparison.OrdinalIgnoreCase);
            aggregate.RequestCount += isAutoplay ? 0 : 1;
            aggregate.AutoPlayCount += isAutoplay ? 1 : 0;
            aggregate.VetoCount += play.Vetoes.Count;
            aggregate.LikeCount += play.Likes.Count;
            if (!aggregate.Excluded)
            {
                aggregate.Excluded = play.Excluded;
            }

            var playedAt = play.StartedAt ?? DateTime.MinValue;
            if (playedAt > aggregate.LastPlayed)
            {
                aggregate.LastPlayed = playedAt;
                aggregate.Track = play.Track;
                aggregate.ExternalLink = play.ExternalLink ?? play.Track.Link;
            }
        }

        var random = Random.Shared;
        var scores = aggregates.Select(kvp =>
        {
            var value = kvp.Value;
            var rawScore = (double)(value.LikeCount - value.VetoCount) + 1;
            if (rawScore <= 0)
            {
                rawScore = 0.001;
            }

            var score = (int)Math.Round(rawScore * (random.NextDouble() / RandomDivisor) * 1000);
            var millisecondsSinceLastPlay = (now - value.LastPlayed).TotalMilliseconds;

            return new TrackScore
            {
                Provider = value.Provider,
                ExternalId = value.ExternalId,
                ExternalLink = value.ExternalLink,
                IsExcluded = value.Excluded,
                Score = score,
                MillisecondsSinceLastPlay = millisecondsSinceLastPlay,
                TrackJson = JsonSerializer.Serialize(value.Track),
                ComputedAt = now
            };
        }).ToList();

        await trackScoreRepository.ReplaceAllAsync(scores, cancellationToken);
        return scores;
    }

    private sealed class ScoreAggregate
    {
        public string Provider { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string ExternalLink { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public int VetoCount { get; set; }
        public int LikeCount { get; set; }
        public int AutoPlayCount { get; set; }
        public DateTime LastPlayed { get; set; } = DateTime.MinValue;
        public bool Excluded { get; set; }
        public Track Track { get; set; } = new();
    }
}
