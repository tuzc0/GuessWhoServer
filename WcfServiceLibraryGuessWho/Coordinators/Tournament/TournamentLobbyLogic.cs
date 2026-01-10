using GuessWhoCore.Contracts.Response;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Tournaments;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServices.Coordinators.Tournament;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuessWhoServices.Coordinators.Tournament
{
    public sealed class TournamentLobbyLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TournamentLobbyLogic));

        private const string CONTEXT_JOIN = nameof(TournamentLobbyLogic) + "." + nameof(JoinTournament);
        private const long INVALID_ID = 0;
        private const int MAX_PLAYERS = 4;
        private const int STATUS_ACTIVE = 2;

        private readonly IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory;
        private readonly ITournamentSubscriptionOperations tournamentSubscriptionOperations;

        public TournamentLobbyLogic(
            IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory,
            ITournamentSubscriptionOperations tournamentSubscriptionOperations)
        {
            this.guessWhoUnitOfWorkFactory = guessWhoUnitOfWorkFactory ??
                throw new ArgumentNullException(nameof(guessWhoUnitOfWorkFactory));

            this.tournamentSubscriptionOperations = tournamentSubscriptionOperations ??
                throw new ArgumentNullException(nameof(tournamentSubscriptionOperations));
        }

        public BasicResponse JoinTournament(long tournamentId, long userId)
        {
            try
            {
                using (IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create())
                {
                    using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
                    {
                        var playersBefore = unitOfWork.Tournaments.GetTournamentPlayers(tournamentId).ToList();

                        if (playersBefore.Count >= MAX_PLAYERS)
                        {
                            return new BasicResponse { Success = false, Code = "TOURNAMENT_FULL" };
                        }

                        bool added = unitOfWork.Tournaments.AddPlayerToTournament(tournamentId, userId);

                        if (!added)
                        {
                            transaction.Rollback();
                            return new BasicResponse { Success = false, Code = "DB_ERROR" };
                        }

                        unitOfWork.Flush();
                        transaction.Commit();

                        var updatedPlayers = unitOfWork.Tournaments.GetTournamentPlayers(tournamentId).ToList();
                        var playerDtos = updatedPlayers.Select(p => new TournamentPlayerDto
                        {
                            UserId = p.UserId,
                            DisplayName = p.DisplayName,
                            TournamentId = p.TournamentId
                        }).ToList();

                        if (updatedPlayers.Count == MAX_PLAYERS)
                        {
                            var (match1Id, match2Id) = StartTournamentMatches(unitOfWork, tournamentId, updatedPlayers);
                            tournamentSubscriptionOperations.NotifyTournamentStarted(tournamentId, match1Id, match2Id);
                        }
                        else
                        {
                            tournamentSubscriptionOperations.NotifyTournamentLobbyUpdated(tournamentId, playerDtos);
                        }

                        return new BasicResponse { Success = true, Code = "OK" };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{CONTEXT_JOIN}: Unexpected error.", ex);
                return new BasicResponse { Success = false, Code = "INTERNAL_ERROR" };
            }
        }

        private (long match1Id, long match2Id) StartTournamentMatches(IGuessWhoUnitOfWork unitOfWork, long tournamentId, List<TournamentPlayer> players)
        {
            try
            {
                var match1 = unitOfWork.Matches.CreateMatchClassic(new CreateMatchArgs
                {
                    UserProfileId = players[0].UserId,
                    MatchStatus = MatchStatus.Lobby,
                    Visibility = MatchVisibility.Private,
                    Mode = MatchMode.Classic,
                    CreateDate = DateTime.UtcNow,
                    MatchCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper()
                });

                unitOfWork.Matches.AddPlayerToPublicMatchById(match1.MatchId, players[1].UserId);

                var match2 = unitOfWork.Matches.CreateMatchClassic(new CreateMatchArgs
                {
                    UserProfileId = players[2].UserId,
                    MatchStatus = MatchStatus.Lobby,
                    Visibility = MatchVisibility.Private,
                    Mode = MatchMode.Classic,
                    CreateDate = DateTime.UtcNow,
                    MatchCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper()
                });

                unitOfWork.Matches.AddPlayerToPublicMatchById(match2.MatchId, players[3].UserId);

                if (match1.IsValid && match2.IsValid)
                {
                    unitOfWork.Tournaments.LinkMatchToTournament(tournamentId, match1.MatchId, false);
                    unitOfWork.Tournaments.LinkMatchToTournament(tournamentId, match2.MatchId, false);

                    unitOfWork.Tournaments.UpdateStatus(tournamentId, STATUS_ACTIVE);
                    unitOfWork.Flush();
                }

                return (match1.MatchId, match2.MatchId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error starting matches for tournament {tournamentId}", ex);
                throw;
            }
        }
        public BasicResponse HostTournament(long hostUserId, int turnSeconds)
        {
            try
            {
                long tournamentId;
                using (IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create())
                {
                    tournamentId = unitOfWork.Tournaments.CreateTournament(hostUserId, turnSeconds);
                    if (tournamentId <= INVALID_ID)
                    {
                        return new BasicResponse { Success = false, Code = "DB_ERROR" };
                    }
                    unitOfWork.Flush();
                }

                var joinResponse = JoinTournament(tournamentId, hostUserId);
                if (joinResponse.Success)
                {
                    joinResponse.Code = tournamentId.ToString();
                }

                return joinResponse;
            }
            catch (Exception ex)
            {
                Logger.Error($"TournamentLobbyLogic.HostTournament: Unexpected error.", ex);
                return new BasicResponse { Success = false, Code = "INTERNAL_ERROR" };
            }
        }
    }
}