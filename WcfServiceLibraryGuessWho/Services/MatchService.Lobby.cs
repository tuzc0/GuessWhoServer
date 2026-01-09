using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Coordinators.InternalDtos;
using System.Collections.Generic;

namespace GuessWhoServices.Services
{
    public partial class MatchService
    {
        private const string CODE_INVALID_ARGS = "MATCH_INVALID_ARGS";
        private const string KEY_INVALID_ARGS = "Match.InvalidArgs";

        private const string CODE_SUBSCRIBE_FAIL = "MATCH_SUBSCRIBE_FAIL";
        private const string KEY_SUBSCRIBE_FAIL = "Match.SubscribeFailed";

        private const string CODE_UNSUBSCRIBE_FAIL = "MATCH_UNSUBSCRIBE_FAIL";
        private const string KEY_UNSUBSCRIBE_FAIL = "Match.UnsubscribeFailed";

        private const string INVALID_ARGS = "INVALID_ARGS";

        private const long INVALID_ID = 0;



        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            return ExecuteService(CONTEXT_CREATE_MATCH, () =>
            {
                EnsureRequestNotNull(request);

                if (request.ProfileId <= INVALID_ID)
                {
                    return new CreateMatchResponse
                    {
                        MatchId = INVALID_ID,
                        Code = INVALID_ARGS,
                        StatusId = 0,
                        VisibilityId = 0,
                        ModeId = 0,
                        CreateAtUtc = default,
                        HostProfileId = INVALID_ID
                    };
                }

                MatchSnapshot snapshot = 
                lifecycleLogic.CreateMatch(request.ProfileId, System.DateTime.UtcNow);

                if (!snapshot.IsValid)
                {
                    return new CreateMatchResponse
                    {
                        MatchId = INVALID_ID,
                        Code = INVALID_ARGS,
                        StatusId = 0,
                        VisibilityId = 0,
                        ModeId = 0,
                        CreateAtUtc = default,
                        HostProfileId = request.ProfileId
                    };
                }

                return new CreateMatchResponse
                {
                    MatchId = snapshot.MatchId,
                    Code = snapshot.MatchCode ?? string.Empty,
                    StatusId = snapshot.StatusId,
                    VisibilityId = snapshot.VisibilityId,
                    ModeId = snapshot.ModeId,
                    CreateAtUtc = snapshot.CreatedAtUtc,
                    HostProfileId = request.ProfileId
                };
            });
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            return ExecuteService(CONTEXT_JOIN_MATCH, () =>
            {
                EnsureRequestNotNull(request);

                if (request.UserId <= INVALID_ID)
                {
                    return FailJoin(CODE_INVALID_ARGS);
                }

                JoinLobbyLogicResult join = lobbyLogic.JoinMatch(
                    new JoinLobbyArgs
                    {
                        MatchCode = request.MatchCode,
                        UserId = request.UserId
                    });

                if (!join.Result.IsValid)
                {
                    return FailJoin(join.Result.Code.ToString());
                }

                var response = new JoinMatchResponse
                {
                    MatchId = join.Match.MatchId,
                    Code = join.Match.MatchCode ?? string.Empty,
                    StatusId = join.Match.StatusId,
                    Mode = join.Match.ModeId,
                    Visibility = join.Match.VisibilityId,
                    CreateAtUtc = join.Match.CreatedAtUtc,
                    HostUserId = join.HasHostPlayer ? join.HostPlayer.UserId : INVALID_ID,
                    Players = MapPlayers(join.Players)
                };

                if (join.HasJoinedPlayer)
                {
                    callbackDispatcher.Broadcast(join.Match.MatchId, callbackChanell => callbackChanell.OnPlayerJoined(MapPlayer(join.JoinedPlayer)));
                }

                return response;
            });
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            return ExecuteService(CONTEXT_LEAVE_MATCH, () =>
            {
                EnsureRequestNotNull(request);

                if (request.MatchId <= INVALID_ID || request.UserId <= INVALID_ID)
                {
                    return BasicFail(CODE_INVALID_ARGS, KEY_INVALID_ARGS);
                }

                LeaveLobbyLogicResult leaveMatch = lobbyLogic.LeaveMatch(request.MatchId, request.UserId);

                if (!leaveMatch.Result.IsSuccess)
                {
                    string code = leaveMatch.Result.Code.ToString();

                    return BasicFail(code, code);
                }

                callbackDispatcher.Broadcast(request.MatchId, callbackChannel => callbackChannel.OnPlayerLeft(
                    new LobbyPlayerDto
                    {
                        MatchId = request.MatchId,
                        UserId = request.UserId,
                        DisplayName = string.Empty,
                        AvatarId = string.Empty,
                        SlotNumber = 0, 
                        IsReady = false,
                        IsHost = leaveMatch.WasHost
                    }));

                return BasicOk();
            });
        }

        public BasicResponse SetMatchPrivate(SetMatchPrivateRequest request)
        {
            return ExecuteService(CONTEXT_SET_PRIVATE, () =>
            {
                EnsureRequestNotNull(request);

                if (request.MatchId <= INVALID_ID || request.UserId <= INVALID_ID)
                {
                    return BasicFail(CODE_INVALID_ARGS, KEY_INVALID_ARGS);
                }

                SetMatchPrivateResult result = lifecycleLogic.SetMatchPrivate(request.MatchId, request.UserId);

                if (!result.IsSuccess)
                {
                    string code = result.Code.ToString();
                    return BasicFail(code, code);
                }

                return BasicOk();
            });
        }

        public SearchPublicMatchResponse SearchPublicMatch(SearchPublicMatchRequest request)
        {
            return ExecuteService(CONTEXT_SEARCH_PUBLIC, () =>
            {
                EnsureRequestNotNull(request);

                string code = (request.MatchCode ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    return new SearchPublicMatchResponse { MatchId = INVALID_ID };
                }

                MatchSnapshot snapshot = lifecycleLogic.SearchPublicMatch(code);

                if (!snapshot.IsValid)
                {
                    return new SearchPublicMatchResponse { MatchId = INVALID_ID };
                }

                return new SearchPublicMatchResponse
                {
                    MatchId = snapshot.MatchId,
                    Code = snapshot.MatchCode ?? string.Empty,
                    StatusId = snapshot.StatusId,
                    VisibilityId = snapshot.VisibilityId,
                    ModeId = snapshot.ModeId,
                    CreateAtUtc = snapshot.CreatedAtUtc
                };
            });
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            return ExecuteService(CONTEXT_SET_READY, () =>
            {
                EnsureRequestNotNull(request);

                if (request.MatchId <= INVALID_ID || request.UserId <= INVALID_ID)
                {
                    return BasicFail(CODE_INVALID_ARGS, KEY_INVALID_ARGS);
                }

                ReadyLobbyLogicResult readyResult = lobbyLogic.SetPlayerReadyStatus(request.MatchId, request.UserId);

                if (!readyResult.Result.IsSuccess)
                {
                    string code = readyResult.Result.Code.ToString();

                    return BasicFail(code, code);
                }

                if (readyResult.HasReadyPlayer)
                {
                    callbackDispatcher.Broadcast(request.MatchId, callbackChannel => 
                    callbackChannel.OnReadyChanged(MapPlayer(readyResult.ReadyPlayer)));
                }

                return BasicOk();
            });
        }

        public BasicResponse SendMatchInvitation(SendMatchInvitationRequest request)
        {
            return ExecuteService(CONTEXT_SEND_INVITATION, () =>
            {
                EnsureRequestNotNull(request);

                bool hasEmail = !string.IsNullOrWhiteSpace(request.TargetEmail);
                bool hasTargetUser = request.TargetUserId > INVALID_ID;

                if ((!hasEmail && !hasTargetUser) || request.MatchId <= INVALID_ID)
                {
                    return BasicFail(CODE_INVALID_ARGS, KEY_INVALID_ARGS);
                }

                return lobbyLogic.SendInvitation(
                    request.MatchId,
                    request.InviterUserId,
                    request.TargetEmail,
                    request.TargetUserId);
            });
        }

        public BasicResponse SubscribeLobby(SubscribeLobbyRequest request)
        {
            return ExecuteService(CONTEXT_SUBSCRIBE, () =>
            {
                if (request == null || request.MatchId <= INVALID_ID || request.UserId <= INVALID_ID)
                {
                    return BasicFail(CODE_INVALID_ARGS, KEY_INVALID_ARGS);
                }

                bool subscribed = lobbyLogic.SubscribeLobby(new LobbySubscriptionArgs
                {
                    MatchId = request.MatchId,
                    UserId = request.UserId
                });

                return subscribed ? BasicOk() : BasicFail(CODE_SUBSCRIBE_FAIL, KEY_SUBSCRIBE_FAIL);
            });
        }

        public BasicResponse UnsubscribeLobby(UnsubscribeLobbyRequest request)
        {
            return ExecuteService(CONTEXT_UNSUBSCRIBE, () =>
            {
                if (request == null || request.MatchId <= INVALID_ID || request.UserId <= INVALID_ID)
                {
                    return BasicFail(CODE_INVALID_ARGS, KEY_INVALID_ARGS);
                }

                bool unsubscribed = lobbyLogic.UnsubscribeLobby(new LobbySubscriptionArgs
                {
                    MatchId = request.MatchId,
                    UserId = request.UserId
                });

                return unsubscribed ? BasicOk() : BasicFail(CODE_UNSUBSCRIBE_FAIL, KEY_UNSUBSCRIBE_FAIL);
            });
        }

        private static LobbyPlayerDto MapPlayer(LobbyPlayerSnapshot player)
        {
            return new LobbyPlayerDto
            {
                MatchId = player.MatchId,
                UserId = player.UserId,
                DisplayName = player.DisplayName,
                AvatarId = player.AvatarId,
                SlotNumber = player.SlotNumber,
                IsReady = player.IsReady,
                IsHost = player.IsHost,
            };
        }

        private static List<LobbyPlayerDto> MapPlayers(IReadOnlyList<LobbyPlayerSnapshot> players)
        {
            if (players == null || players.Count == 0)
            {
                return new List<LobbyPlayerDto>();
            }

            var result = new List<LobbyPlayerDto>(players.Count);

            for (int i = 0; i < players.Count; i++)
            {
                result.Add(MapPlayer(players[i]));
            }

            return result;
        }

        private static JoinMatchResponse FailJoin(string code)
        {
            string safe = code ?? string.Empty;

            return new JoinMatchResponse
            {
                MatchId = INVALID_ID,
                Code = safe,
                StatusId = 0,
                Mode = 0,
                Visibility = 0,
                CreateAtUtc = default,
                HostUserId = INVALID_ID,
                Players = new List<LobbyPlayerDto>()
            };
        }
    }
}
