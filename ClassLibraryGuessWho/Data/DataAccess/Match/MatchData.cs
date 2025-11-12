using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Faults;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed class MatchData
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchData));

        private const string FAULT_CODE_MATCH_FULL = "MATCH_FULL";
        private const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DATABASE_COMMAND_TIMEOUT";
        private const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DATABASE_CONNECTION_FAILURE";
        private const string FAULT_CODE_MATCH_UNEXPECTED_ERROR = "MATCH_UNEXPECTED_ERROR";

        private const string FAULT_MESSAGE_MATCH_FULL =
            "The match is already full.";
        private const string FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT =
            "The server took too long to respond. Please try again.";
        private const string FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE =
            "The server could not connect to the database. Please try again later.";
        private const string FAULT_MESSAGE_MATCH_UNEXPECTED_ERROR =
            "An unexpected error occurred while working with the match. Please try again later.";

        private const byte HOST_SLOT_NUMBER = 1;
        private const byte GUEST_SLOT_NUMBER = 2;

        public MatchDto CreateMatch(CreateMatchArgs args)
        {
            try
            {
                using (var dataBaseContext = new GuessWhoDBEntities())
                using (var transaction = dataBaseContext.Database.BeginTransaction())
                {
                    var match = new MATCH
                    {
                        VISIBILITYID = args.Visibility,
                        STATUSID = args.MatchStatus,
                        MODE = args.Mode,
                        MATCHCODE = args.MatchCode,
                        CREATEDATUTC = args.CreateDate,
                        STARTTIME = null,
                        ENDTIME = null,
                        WINNERUSERID = null
                    };

                    dataBaseContext.MATCH.Add(match);

                    var matchPlayer = new MATCH_PLAYER
                    {
                        MATCHID = match.MATCHID,
                        USERID = args.UserProfileId,
                        SLOTNUMBER = HOST_SLOT_NUMBER,
                        ISWINNER = false,
                        JOINEDATUTC = args.CreateDate,
                        LEFTATUTC = null,
                        ISHOST = true,
                        ISREADY = false
                    };

                    dataBaseContext.MATCH_PLAYER.Add(matchPlayer);

                    dataBaseContext.SaveChanges();
                    transaction.Commit();

                    return new MatchDto
                    {
                        MatchId = match.MATCHID,
                        Code = match.MATCHCODE,
                        StatusId = match.STATUSID,
                        Mode = match.MODE,
                        VisibilityId = match.VISIBILITYID,
                        CreateAtUtc = match.CREATEDATUTC
                    };
                }
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database command timeout in MatchData.CreateMatch.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                    FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure in MatchData.CreateMatch.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                    FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected error in MatchData.CreateMatch.", ex);
                throw Faults.Create(
                    FAULT_CODE_MATCH_UNEXPECTED_ERROR,
                    FAULT_MESSAGE_MATCH_UNEXPECTED_ERROR);
            }
        }

        public MatchDto GetMatchByCode(JoinMatchArgs args)
        {
            try
            {
                using (var dataBaseContext = new GuessWhoDBEntities())
                using (var transaction = dataBaseContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    long? matchId = dataBaseContext.MATCH
                        .AsNoTracking()
                        .Where(m => m.MATCHCODE == args.MatchCode)
                        .Select(m => (long?)m.MATCHID)
                        .SingleOrDefault();

                    if (!matchId.HasValue)
                    {
                        return MatchDto.CreateInvalid(args.MatchCode);
                    }

                    long currentMatchId = matchId.Value;

                    bool isAlreadyPlayer = dataBaseContext.MATCH_PLAYER
                        .Any(p =>
                            p.MATCHID == currentMatchId &&
                            p.USERID == args.UserProfileId &&
                            p.LEFTATUTC == null);

                    if (isAlreadyPlayer)
                    {
                        var existingMatch = dataBaseContext.MATCH
                            .AsNoTracking()
                            .Single(m => m.MATCHID == currentMatchId);

                        transaction.Commit();

                        return MapToDto(existingMatch);
                    }

                    bool isSecondSlotFree = !dataBaseContext.MATCH_PLAYER
                        .Any(p =>
                            p.MATCHID == currentMatchId &&
                            p.LEFTATUTC == null &&
                            p.SLOTNUMBER == GUEST_SLOT_NUMBER);

                    if (!isSecondSlotFree)
                    {
                        string message = $"User {args.UserProfileId} tried to join a full match {currentMatchId} in MatchData.GetMatchByCode.";
                        Logger.Warn(message);
                        throw Faults.Create(
                            FAULT_CODE_MATCH_FULL,
                            FAULT_MESSAGE_MATCH_FULL);
                    }

                    dataBaseContext.MATCH_PLAYER.Add(new MATCH_PLAYER
                    {
                        MATCHID = currentMatchId,
                        USERID = args.UserProfileId,
                        SLOTNUMBER = GUEST_SLOT_NUMBER,
                        ISWINNER = false,
                        JOINEDATUTC = args.JoinedDate,
                        LEFTATUTC = null,
                        ISHOST = false,
                        ISREADY = false
                    });

                    try
                    {
                        dataBaseContext.SaveChanges();
                        transaction.Commit();
                    }
                    catch (DbUpdateException ex)
                    {
                        transaction.Rollback();
                        Logger.Warn("DbUpdateException while joining match; likely match is full. MatchData.GetMatchByCode.", ex);
                        throw Faults.Create(
                            FAULT_CODE_MATCH_FULL,
                            FAULT_MESSAGE_MATCH_FULL);
                    }

                    var match = dataBaseContext.MATCH
                        .AsNoTracking()
                        .Single(m => m.MATCHID == currentMatchId);

                    return MapToDto(match);
                }
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database command timeout in MatchData.GetMatchByCode.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                    FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure in MatchData.GetMatchByCode.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                    FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected error in MatchData.GetMatchByCode.", ex);
                throw Faults.Create(
                    FAULT_CODE_MATCH_UNEXPECTED_ERROR,
                    FAULT_MESSAGE_MATCH_UNEXPECTED_ERROR);
            }
        }

        public List<LobbyPlayerDto> GetMatchPlayers(long matchId)
        {
            try
            {
                using (var dataBaseContext = new GuessWhoDBEntities())
                {
                    return dataBaseContext.MATCH_PLAYER
                        .AsNoTracking()
                        .Where(mp => mp.MATCHID == matchId && mp.LEFTATUTC == null)
                        .OrderBy(p => p.SLOTNUMBER)
                        .Select(p => new LobbyPlayerDto
                        {
                            MatchId = p.MATCHID,
                            UserId = p.USERID,
                            DisplayName = dataBaseContext.USER_PROFILE
                                .Where(u => u.USERID == p.USERID)
                                .Select(u => u.DISPLAYNAME)
                                .FirstOrDefault(),
                            SlotNumber = (byte)p.SLOTNUMBER,
                            IsReady = p.ISREADY,
                            IsHost = p.ISHOST
                        })
                        .ToList();
                }
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database command timeout in MatchData.GetMatchPlayers.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                    FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure in MatchData.GetMatchPlayers.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                    FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected error in MatchData.GetMatchPlayers.", ex);
                throw Faults.Create(
                    FAULT_CODE_MATCH_UNEXPECTED_ERROR,
                    FAULT_MESSAGE_MATCH_UNEXPECTED_ERROR);
            }
        }

        private static MatchDto MapToDto(MATCH match)
        {
            if (match == null)
            {
                return MatchDto.CreateInvalid();
            }

            return new MatchDto
            {
                MatchId = match.MATCHID,
                Code = match.MATCHCODE,
                StatusId = match.STATUSID,
                Mode = match.MODE,
                VisibilityId = match.VISIBILITYID,
                CreateAtUtc = match.CREATEDATUTC
            };
        }
    }
}
