﻿namespace Interlude.Web.Server.API.Stats.Leaderboard

open NetCoreServer
open Percyqaz.Common
open Prelude.Data.User.Stats
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core

module MonthlyXP =

    open Stats.Leaderboard
    open Stats.Leaderboard.MonthlyXP

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let user_id, user = authorize headers

            let by_playtime = query_params.TryFind "sort" |> Option.bind Array.tryHead = Some "playtime"

            let month = Timestamp.now() |> timestamp_to_leaderboard_month

            let data =
                if by_playtime then
                    MonthlyStats.playtime_leaderboard month
                else
                    MonthlyStats.xp_leaderboard month

            let users =
                data |> Array.map (fun x -> x.UserId) |> User.by_ids |> Map.ofArray

            let mutable you : (int64 * XPLeaderboardEntry) option = None

            let mutable i = 0
            let result =
                data
                |> Array.map (fun l ->
                    i <- i + 1
                    let user = users.[l.UserId]
                    let entry =
                        {
                            Username = user.Username
                            Color = user.Color
                            XP = l.XP
                            Playtime = l.Playtime
                        }
                    if user_id = l.UserId then you <- Some (i, entry)
                    entry
                )

            match you with
            | Some _ -> ()
            | None ->
                match
                    if by_playtime then
                        MonthlyStats.playtime_rank user_id month
                    else
                        MonthlyStats.xp_rank user_id month
                with
                | Some rank ->
                    you <- Some (
                        rank.Rank,
                        {
                            Username = user.Username
                            Color = user.Color
                            XP = rank.XP
                            Playtime = rank.Playtime
                        }
                    )
                | None -> ()

            response.ReplyJson({ Leaderboard = result; You = you }: Response)
        }