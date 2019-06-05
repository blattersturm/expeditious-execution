using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace BaseRush.Client
{
    public static class Notify
    {
        public static void Custom(string message, bool blink = true, bool saveToBrief = true)
        {
            SetNotificationTextEntry("CELL_EMAIL_BCON"); // 10x ~a~
            foreach (string s in CitizenFX.Core.UI.Screen.StringToArray(message))
            {
                AddTextComponentSubstringPlayerName(s);
            }
            DrawNotification(blink, saveToBrief);
        }
    }

    public class Notifications : BaseScript
    {
        private string GetSafePlayerName(string name)
        {
            return name;
        }

        private HashSet<int> playerList = new HashSet<int>();

        private HashSet<int> deadPlayers = new HashSet<int>();


        /// <summary>
        /// Runs join/quit notification checks.
        /// </summary>
        /// <returns></returns>
        [Tick]
        public async Task JoinQuitNotifications()
        {
            PlayerList plist = Players;
            foreach (Player p in plist)
            {
                // new player joined.
                if (!playerList.Contains(p.Handle))
                {
                    var team = GetPlayerTeam(p.Handle);

                    if (team != -1)
                    {
                        Notify.Custom($"~g~<C>{GetSafePlayerName(p.Name)}</C>~s~ joined the {BaseRushGame.TeamNames[team]}.");
                        playerList.Add(p.Handle);
                        await Delay(0);
                    }
                }
            }

            playerList.RemoveWhere(a => !NetworkIsPlayerActive(a));
        }

        [EventHandler("br:dropped")]
        public void PlayerDropped(string name, string reason)
        {
            Notify.Custom($"~r~<C>{GetSafePlayerName(name)}</C>~s~ left. ~c~({reason})~s~");
        }

        /// <summary>
        /// Runs death notification checks.
        /// </summary>
        /// <returns></returns>
        [Tick]
        public async Task DeathNotifications()
        {
            PlayerList pl = Players;
            var tmpiterator = 0;
            foreach (Player p in pl)
            {
                tmpiterator++;
                await Delay(0);
                if (p.IsDead)
                {
                    if (deadPlayers.Contains(p.Handle)) { return; }
                    var killer = p.Character.GetKiller();
                    if (killer != null)
                    {
                        if (killer.Handle != p.Character.Handle)
                        {
                            if (killer.Exists())
                            {
                                if (killer.Model.IsPed)
                                {
                                    bool found = false;
                                    foreach (Player playerKiller in pl)
                                    {
                                        if (playerKiller.Character.Handle == killer.Handle)
                                        {
                                            Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~has been murdered by ~y~<C>{GetSafePlayerName(playerKiller.Name)}</C>~s~.");
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~has been murdered.");
                                    }
                                }
                                else if (killer.Model.IsVehicle)
                                {
                                    bool found = false;
                                    foreach (Player playerKiller in pl)
                                    {
                                        if (playerKiller.Character.IsInVehicle())
                                        {
                                            if (playerKiller.Character.CurrentVehicle.Handle == killer.Handle)
                                            {
                                                Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~has been murdered by ~y~<C>{GetSafePlayerName(playerKiller.Name)}</C>~s~.");
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!found)
                                    {
                                        Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~has been murdered.");
                                    }
                                }
                                else
                                {
                                    Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~has been murdered.");
                                }
                            }
                            else
                            {
                                Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~has been murdered.");
                            }
                        }
                        else
                        {
                            Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~committed suicide.");
                        }
                    }
                    else
                    {
                        Notify.Custom($"~o~<C>{GetSafePlayerName(p.Name)}</C> ~s~died.");
                    }
                    deadPlayers.Add(p.Handle);
                }
                else
                {
                    if (deadPlayers.Contains(p.Handle))
                    {
                        deadPlayers.Remove(p.Handle);
                    }
                }
            }
            await Delay(50);
        }
    }
}