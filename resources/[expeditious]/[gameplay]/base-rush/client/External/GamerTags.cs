using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace BaseRush.Client
{
    public class GamerTags : BaseScript
    {
        private Dictionary<Player, int> gamerTags = new Dictionary<Player, int>();

        /// <summary>
        /// Manages overhead player names.
        /// </summary>
        /// <returns></returns>
        [Tick]
        public async Task PlayerOverheadNamesControl()
        {
            await Delay(500);

            foreach (Player p in Players)
            {
                if (p != Game.Player)
                {
                    var dist = p.Character.Position.DistanceToSquared(Game.PlayerPed.Position);
                    var team = GetPlayerTeam(p.Handle);

                    if (team == -1)
                    {
                        continue;
                    }

                    var teamMate = team == GetPlayerTeam(PlayerId());
                    //Debug.WriteLine($"Dist: {dist}");
                    bool closeEnough = (teamMate) ? dist < 400f : IsPlayerFreeAimingAtEntity(PlayerId(), p.Character.Handle);
                    if (gamerTags.ContainsKey(p))
                    {
                        if (!closeEnough)
                        {
                            RemoveMpGamerTag(gamerTags[p]);
                            gamerTags.Remove(p);
                        }
                        else
                        {
                            gamerTags[p] = CreateMpGamerTag(p.Character.Handle, p.Name + $" [{BaseRushGame.TeamNames[team]}]", false, false, "", 0);
                        }
                    }
                    else if (closeEnough)
                    {
                        gamerTags.Add(p, CreateMpGamerTag(p.Character.Handle, p.Name + $" [{BaseRushGame.TeamNames[team]}]", false, false, "", 0));
                    }
                    if (closeEnough && gamerTags.ContainsKey(p))
                    {
                        SetMpGamerTagVisibility(gamerTags[p], 2, true); // healthArmor

                        if (p.WantedLevel > 0)
                        {
                            SetMpGamerTagVisibility(gamerTags[p], 7, true); // wantedStars
                            SetMpGamerTagWantedLevel(gamerTags[p], GetPlayerWantedLevel(p.Handle));
                        }
                        else
                        {
                            SetMpGamerTagVisibility(gamerTags[p], 7, false); // wantedStars
                        }

                        if (teamMate)
                        {
                            SetMpGamerTagVisibility(gamerTags[p], 10, true);
                            SetMpGamerTagColour(gamerTags[p], 0, 0);
                        }
                        else
                        {
                            SetMpGamerTagVisibility(gamerTags[p], 10, false);
                            SetMpGamerTagColour(gamerTags[p], 0, 6);
                            SetMpGamerTagVisibility(gamerTags[p], 2, false);
                        }
                    }
                }
            }
        }
    }
}