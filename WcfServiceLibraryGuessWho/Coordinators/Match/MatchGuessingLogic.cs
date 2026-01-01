using ClassLibraryGuessWho.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using log4net;
using System;

namespace GuessWhoServerDomain.Domain.Logic.Match
{
    public sealed class MatchGuessingLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchGuessingLogic));

        private const long INVALID_ID = 0;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public MatchGuessingLogic(IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? 
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public FinalGuessResult GuessOpponentCharacter(long matchId, long guessingUserId, string guessedCharacterId)
        {
            if (matchId <= INVALID_ID)
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.MatchNotFound);
            }

            if (guessingUserId <= INVALID_ID)
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.InvalidUser);
            }

            string trimmedGuess = (guessedCharacterId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedGuess))
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.InvalidGuess);
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            LoadFinalGuessSnapshotResult snapshotResult =
                unitOfWork.MatchGuessing.LoadFinalGuessSnapshot(matchId, guessingUserId);

            if (!snapshotResult.IsSuccess)
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.MatchNotFound);
            }

            FinalGuessSnapshot snapshot = snapshotResult.Snapshot;

            if (snapshot.MatchStatusId != (int)MatchStatus.Active)
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.MatchNotInProgress);
            }

            if (snapshot.CurrentTurnUserId != guessingUserId)
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.NotYourTurn);
            }

            if (snapshot.OpponentUserId <= INVALID_ID)
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.OpponentNotInMatch);
            }

            if (string.IsNullOrWhiteSpace(snapshot.OpponentSecretCharacterId))
            {
                return FinalGuessResult.Fail(FinalGuessResultCode.OpponentSecretNotSelected);
            }

            string trimmedSecret = snapshot.OpponentSecretCharacterId.Trim();

            bool isCorrect = string.Equals(trimmedSecret, trimmedGuess, StringComparison.OrdinalIgnoreCase);

            long winnerUserId = isCorrect ? guessingUserId : snapshot.OpponentUserId;

            Logger.InfoFormat(
                "FinalGuess. MatchId={0} GuessingUserId={1} OpponentUserId={2} Guessed={3} Secret={4} IsCorrect={5}",
                matchId, guessingUserId, snapshot.OpponentUserId, trimmedGuess, trimmedSecret, isCorrect);

            using var transaction = unitOfWork.BeginTransaction();

            EndMatchResult endResult = unitOfWork.Matches.EndMatch(new EndMatchArgs
            {
                MatchId = matchId,
                WinnerUserId = winnerUserId
            });

            if (!endResult.IsSuccess)
            {
                transaction.Rollback();
                return FinalGuessResult.Fail(FinalGuessResultCode.EndMatchFailed);
            }

            unitOfWork.Flush();
            transaction.Commit();

            return FinalGuessResult.Success(isCorrect);
        }
    }
}
