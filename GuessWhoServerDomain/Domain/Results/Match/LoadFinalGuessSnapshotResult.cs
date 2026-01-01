using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct LoadFinalGuessSnapshotResult(
        LoadFinalGuessSnapshotResultCode Code,
        FinalGuessSnapshot Snapshot)
    {
        public bool IsSuccess => Code == LoadFinalGuessSnapshotResultCode.Success;

        public static LoadFinalGuessSnapshotResult Success(FinalGuessSnapshot snapshot) =>
            new(LoadFinalGuessSnapshotResultCode.Success, snapshot);

        public static LoadFinalGuessSnapshotResult Fail(LoadFinalGuessSnapshotResultCode code) =>
            new(code, default);
    }
}
