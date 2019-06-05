using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;

namespace BaseRush.Server
{
    public class BaseRushTeams : BaseScript
    {
        private Dictionary<string, int> m_teams = new Dictionary<string, int>();

        private Random r = new Random();

        [EventHandler("onPlayerJoining")]
        public void PlayerJoining([FromSource] Player player)
        {
            JoinOnTeam(player);
        }

        [EventHandler("onMapStart")]
        public async void MapStart()
        {
            m_teams.Clear();

            foreach (var player in Players.OrderBy(p => r.Next()))
            {
                JoinOnTeam(player);
            }

            await Delay(2500);

            rotatingMap = false;
        }

        [EventHandler("br:getTeam")]
        public void GetTeam([FromSource] Player player)
        {
            if (!m_teams.ContainsKey(player.Handle))
            {
                JoinOnTeam(player);
            }
        }

        private bool rotatingMap = false;

        [EventHandler("br:rotateMap")]
        public void RotateMap()
        {
            if (rotatingMap)
            {
                return;
            }

            rotatingMap = true;

            Exports["mapmanager"].roundEnded();
        }

        private void JoinOnTeam(Player player)
        {
            var teamToJoin = Enumerable.Range(0, 2).Select(a => new 
            {
                Team = a,
                Players = m_teams.Count(p => p.Value == a)
            }).OrderBy(a => a.Players).First().Team;

            m_teams[player.Handle] = teamToJoin;
            player.TriggerEvent("br:setTeam", teamToJoin);
        }

        [EventHandler("playerDropped")]
        public void PlayerDropped([FromSource] Player player, string reason)
        {
            m_teams.Remove(player.Handle);

            TriggerClientEvent("br:dropped", player.Name, reason);
        }
    }
}