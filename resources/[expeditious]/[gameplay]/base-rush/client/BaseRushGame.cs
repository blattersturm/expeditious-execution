using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using NativeUI;
using static CitizenFX.Core.Native.API;

namespace BaseRush.Client
{
    public class BaseRushGame : BaseScript
    {
        private class Base
        {
            public Vector2 Min { get; }
            public Vector2 Max { get; }
            public Vector2 Center => (Max + Min) / 2;
            public Vector2 Size => (Max - Min);
            public float Radius => Size.Length();
            public int Index { get; set; }

            public Base(dynamic mapEntry)
            {
                float x1 = (float)mapEntry.p1[0];
                float y1 = (float)mapEntry.p1[1];

                float x2 = (float)mapEntry.p2[0];
                float y2 = (float)mapEntry.p2[1];

                Min = new Vector2(Math.Min(x1, x2), Math.Min(y1, y2));
                Max = new Vector2(Math.Max(x1, x2), Math.Max(y1, y2));
            }
        }

        public static string[] TeamNames = new string[] { "IAA", "FIB" };

        private List<Base> m_bases = new List<Base>();

        private bool m_roundEnded;

        public static bool RoundEnded { get; set; }

        public BaseRushGame()
        {
            var mm = MapManager.Instance.Value;
            mm.RegisterKeyDirective("base");

            Instance = this;

            m_timerBars.Add(m_team1Progress);
            m_timerBars.Add(m_team1Score);
            m_timerBars.Add(m_team2Progress);
            m_timerBars.Add(m_team2Score);
        }

        private VariableManager.VariableAPI m_vm;

        [EventHandler("br:setTeam")]
        public void SetTeam(int team)
        {
            SetPlayerTeam(PlayerId(), team);

            Debug.WriteLine($"im on team {team}");
        }

        [EventHandler("onClientMapStart")]
        public void OnClientMapStart()
        {
            m_bases.Clear();
            m_bases.AddRange(MapManager.Instance.Value.GetDirectives("base").Select(b => new Base(b)));

            RoundEnded = false;

            m_lastBaseIdx = -1;
            m_lastBaseReached = -1;

            for (int i = 0; i < m_bases.Count; i++)
            {
                m_bases[i].Index = i;
            }

            m_roundEnded = false;

            uint enemyHash = 0;
            uint player1Hash = 0;
            uint player2Hash = 0;
            AddRelationshipGroup("Escape_Enemy", ref enemyHash);
            AddRelationshipGroup("Escape_Player_Team1", ref player1Hash);
            AddRelationshipGroup("Escape_Player_Team2", ref player2Hash);

            SetRelationshipBetweenGroups(5, enemyHash, player1Hash);
            SetRelationshipBetweenGroups(5, player1Hash, enemyHash);

            SetRelationshipBetweenGroups(5, enemyHash, player2Hash);
            SetRelationshipBetweenGroups(5, player2Hash, enemyHash);

            SetRelationshipBetweenGroups(5, player2Hash, player1Hash);
            SetRelationshipBetweenGroups(5, player1Hash, player2Hash);

            SetRelationshipBetweenGroups(0, player1Hash, player1Hash);
            SetRelationshipBetweenGroups(0, player2Hash, player2Hash);

            SetPedRelationshipGroupHash(PlayerPedId(), (GetPlayerTeam(PlayerId()) == 0) ? player1Hash : player2Hash);

            RequestModel((uint)Game.GenerateHash("a_m_m_skater_01"));

            m_vm = VariableManager.API;

            TriggerServerEvent("br:getTeam");

            AddTextEntry("RUSH_GO_TO_BASE", "Go to the ~y~base~s~ and capture it!");
            AddTextEntry("RUSH_CAPTURE_BASE", "Capture the ~b~base~s~ at the marker!");
            AddTextEntry("RUSH_BASE", "Base");
            AddTextEntry("RUSH_ENEMY", "Enemy");

            RequestStreamedTextureDict("timerbars", false);
        }

        private int m_lastBaseIdx = -1;

        private int m_lastBaseReached = -1;

        private int m_baseBlip;

        private int m_remoteBlip;

        private Dictionary<int, bool> m_dead = new Dictionary<int, bool>();

        private Dictionary<int, int> m_blips = new Dictionary<int, int>();

        public static BaseRushGame Instance { get; private set; }

        public async Task<Vector3> GetRespawnPosition()
        {
            if (m_lastBaseReached == -1)
            {
                return Vector3.Zero;
            }

            var b = m_bases[m_lastBaseReached];

            // find a suitable vector
            Vector3 pos = Vector3.Zero;
            bool result = false;

            var tries = 0;
            var c = new Vector3(b.Center, 0.0f);

            AddNavmeshRequiredRegion(b.Center.X, b.Center.Y, b.Radius * 1.4f);

            while (!result)
            {
                // TODO: cases +0.5 is round?
                var vec = c + (RandomVectorXY() * (b.Radius * 1.4f));
                RequestCollisionAtCoord(vec.X, vec.Y, 100.0f);

                float z = 0.0f;

                if (vec.DistanceToSquared2D(c) > b.Radius)
                {
                    if (GetGroundZFor_3dCoord(vec.X, vec.Y, 1000f, ref z, false))
                    {
                        result = GetSafeCoordForPed(vec.X, vec.Y, z, false, ref pos, 16);
                    }
                }

                tries++;

                if (tries > 20)
                {
                    tries = 0;
                    await Delay(0);
                }
            }

            return pos;
        }

        public async Task<Vector3> GetInitialPosition(Vector3 initial)
        {
            // find a suitable vector
            Vector3 pos = Vector3.Zero;
            bool result = false;

            var tries = 0;
            var c = initial;

            AddNavmeshRequiredRegion(c.X, c.Y, 10.0f);

            Game.PlayerPed.Position = c;

            while (!result)
            {
                // TODO: cases +0.5 is round?
                var vec = c + (RandomVectorXY() * 10.0f);
                RequestCollisionAtCoord(vec.X, vec.Y, 100.0f);

                float z = 0.0f;

                if (GetGroundZFor_3dCoord(vec.X, vec.Y, 1000f, ref z, false))
                {
                    result = GetSafeCoordForPed(vec.X, vec.Y, z, false, ref pos, 16);
                }

                tries++;

                if (tries > 20)
                {
                    tries = 0;
                    await Delay(0);
                }
            }

            return pos;
        }

        private static Random ms_random = new Random();

        private static Vector3 RandomVectorXY()
        {
            Vector3 retval = new Vector3((float)(ms_random.NextDouble() - 0.5), (float)(ms_random.NextDouble() - 0.5), 0.0f);
            retval.Normalize();

            return retval;
        }

        [Tick]
        public async Task OnTick()
        {
            if (m_bases.Count == 0)
            {
                return;
            }

            SetPedRelationshipGroupHash(PlayerPedId(), (uint)((GetPlayerTeam(PlayerId()) == 0) ? GetHashKey("Escape_Player_Team1") : GetHashKey("Escape_Player_Team2")));

            DeleteWatchedPeds();

            int currentBaseIdx = Convert.ToInt32(m_vm.get("currentBase") ?? 0);

            if (currentBaseIdx >= m_bases.Count)
            {
                if (m_lastBaseIdx != -1)
                {
                    CleanPeds(m_bases[m_lastBaseIdx]);

                    RemoveNavmeshRequiredRegions();

                    m_lastBaseIdx = -1;
                }

                RoundEnded = true;

                await EndRound();
                return;
            }

            // different base? create a blip
            var b = m_bases[currentBaseIdx];

            if (m_lastBaseIdx != currentBaseIdx)
            {
                if (m_lastBaseIdx != -1)
                {
                    CleanPeds(m_bases[m_lastBaseIdx]);

                    RemoveNavmeshRequiredRegions();
                }

                if (m_baseBlip != 0)
                {
                    RemoveBlip(ref m_baseBlip);
                }

                m_baseBlip = AddBlipForRadius(b.Center.X, b.Center.Y, 1.0f, b.Radius);
                SetBlipColour(m_baseBlip, 0x0099FF80);

                if (m_lastBaseIdx != -1 && NetworkIsHost())
                {
                    m_vm.set($"base:{m_lastBaseIdx}:peds", new int[0]);
                    m_vm.set($"base:{m_lastBaseIdx}:cap", new int[0]);
                }

                if (m_remoteBlip != 0)
                {
                    RemoveBlip(ref m_remoteBlip);
                }

                AddNavmeshRequiredRegion(b.Center.X, b.Center.Y, b.Radius * 1.3f);

                m_lastBaseIdx = currentBaseIdx;
            }

            var playerPed = PlayerPedId();
            var playerCoords = GetEntityCoords(playerPed, false);

            var rDist = Math.Max(b.Radius * 1.5f, 250f);

            bool shouldShowRemoteBlip;

            var responsiblePlayer = FindResponsiblePlayer(new Vector3(b.Center, 0.0f));
            var baseCapPoint = (Vector3)(m_vm.get($"base:{b.Index}:capPoint") ?? Vector3.Zero);

            if (playerCoords.DistanceToSquared2D(new Vector3(b.Center, 0.0f)) < (rDist * rDist))
            {
                m_lastBaseReached = b.Index;

                if (responsiblePlayer.Handle == LocalPlayer.Handle)
                {
                    CheckPeds(b);

                    if (baseCapPoint == Vector3.Zero)
                    {
                        float groundZ = 0.0f;
                        var baseCapPointFound = GetGroundZFor_3dCoord(b.Center.X, b.Center.Y, 1000.0f, ref groundZ, false);

                        if (baseCapPointFound)
                        {
                            Vector3 navPosition = Vector3.Zero;
                            var navPointFound = GetSafeCoordForPed(b.Center.X, b.Center.Y, groundZ, false, ref navPosition, 16); // 16 == flatGround

                            if (navPointFound)
                            {
                                baseCapPoint = navPosition + new Vector3(0.0f, 0.0f, 1.0f);

                                m_vm.set($"base:{b.Index}:capPoint", baseCapPoint);
                            }
                        }
                    }
                }

                if (baseCapPoint != Vector3.Zero)
                {
                    World.DrawMarker(MarkerType.ThickChevronUp, baseCapPoint, Vector3.Zero, new Vector3(180f, 0, 0), new Vector3(1, 1, 1), System.Drawing.Color.FromArgb(180, 0, 0x99, 0xFF), true, true);
                }

                BeginTextCommandPrint("RUSH_CAPTURE_BASE");
                EndTextCommandPrint(500, true);

                shouldShowRemoteBlip = false;
            }
            else
            {
                BeginTextCommandPrint("RUSH_GO_TO_BASE");
                EndTextCommandPrint(500, true);

                shouldShowRemoteBlip = true;
            }

            if (baseCapPoint != Vector3.Zero)
            {
                if (responsiblePlayer.Handle == LocalPlayer.Handle)
                {
                    ProcessBaseCapture(b, baseCapPoint);
                }
            }

            if (shouldShowRemoteBlip && m_remoteBlip == 0)
            {
                m_remoteBlip = AddBlipForCoord(b.Center.X, b.Center.Y, -100.0f);
                SetBlipColour(m_remoteBlip, 5);
                SetBlipDisplay(m_remoteBlip, 2);
                SetBlipRoute(m_remoteBlip, true);
                
                BeginTextCommandSetBlipName("RUSH_BASE");
                EndTextCommandSetBlipName(m_remoteBlip);
            }
            else if (!shouldShowRemoteBlip && m_remoteBlip != 0)
            {
                RemoveBlip(ref m_remoteBlip);
            }

            for (int team = 0; team < 2; team++)
            {
                DisplayBaseCapture(b, team);
            }

            UpdatePeds(b);

            m_timerBars.Draw();

            var myTeam = GetPlayerTeam(PlayerId());

            if (myTeam != -1)
            {
                new CitizenFX.Core.UI.Text(TeamNames[myTeam], new System.Drawing.PointF(1280f / 2f, 5f), 1.0f, Color.FromArgb(255, 255, 255, 255), CitizenFX.Core.UI.Font.ChaletLondon, CitizenFX.Core.UI.Alignment.Center, true, false).Draw();
            }
        }

        private async Task EndRound()
        {
            HideHudAndRadarThisFrame();

            if (m_roundEnded)
            {
                return;
            }

            m_roundEnded = true;

            await Delay(500);

            var iaaScore = (int)(m_vm.get($"points:0") ?? 0);
            var fibScore = (int)(m_vm.get($"points:1") ?? 0);

            var winnerType = "DRAW";

            if (iaaScore > fibScore)
            {
                winnerType = (GetPlayerTeam(PlayerId()) == 0) ? "WINNER" : "LOSER";
            }
            else if (iaaScore < fibScore)
            {
                winnerType = (GetPlayerTeam(PlayerId()) == 1) ? "WINNER" : "LOSER";
            }

            FreezeEntityPosition(PlayerPedId(), true);

            CelebrationWinner.FIBScore = fibScore;
            CelebrationWinner.IAAScore = iaaScore;
            CelebrationWinner.TeamName = TeamNames[GetPlayerTeam(PlayerId())];
            CelebrationWinner.WinnerType = $"CELEB_{winnerType}";
            CelebrationWinner.OnComplete = async () =>
            {
                SwitchOutPlayer(PlayerPedId(), 0, 1);

                while (GetPlayerSwitchState() != 5)
                {
                    await Delay(0);
                }

                TriggerServerEvent("br:rotateMap");
            };

            CelebrationWinner.Enabled = true;


        }

        private TimerBarPool m_timerBars = new TimerBarPool();

        private BarTimerBar m_team1Progress = new BarTimerBar("");
        private BarTimerBar m_team2Progress = new BarTimerBar("");

        private TextTimerBar m_team1Score = new TextTimerBar("", "");
        private TextTimerBar m_team2Score = new TextTimerBar("", "");

        private void DisplayBaseCapture(Base b, int team)
        {
            BarTimerBar progressBar;
            TextTimerBar textBar;

            if (team == 0)
            {
                progressBar = m_team1Progress;
                progressBar.Label = TeamNames[team] + " Cap";

                textBar = m_team1Score;
                textBar.Label = TeamNames[team];
            }
            else
            {
                progressBar = m_team2Progress;
                progressBar.Label = TeamNames[team] + " Cap";

                textBar = m_team2Score;
                textBar.Label = TeamNames[team];
            }

            int count = m_vm.lcount($"base:{b.Index}:cap:{team}");
            progressBar.Percentage = Math.Min(count / 40f, 1f);

            var fgColor = Color.FromArgb(255, 255, 255, 255);
            var bgColor = Color.FromArgb(255, 100, 100, 100);

            bool lastContested = (bool)(m_vm.get($"base:{b.Index}:contested") ?? false);
            if (lastContested)
            {
                fgColor = Color.FromArgb(-65536);
                bgColor = Color.FromArgb(-7667712);
            }

            progressBar.BackgroundColor = bgColor;
            progressBar.ForegroundColor = fgColor;

            // not ToString? shoot me.
            textBar.Text = $"{(int)(m_vm.get($"points:{team}") ?? 0)}";

            //var t = new CitizenFX.Core.UI.Text($"cap {team}: {count}/20", new System.Drawing.PointF(0.05f, 0.05f + (team * 40f)), 1.0f, color);
            //t.Draw();
        }

        private void ProcessBaseCapture(Base b, Vector3 capPoint)
        {
            var netTime = GetNetworkTime();
            var numCapturing = new int[2];
            var totalCapturing = 0;

            foreach (var player in Players)
            {
                if (player.Character.Position.DistanceToSquared(capPoint) < 10f)
                {
                    numCapturing[GetPlayerTeam(player.Handle)]++;
                    totalCapturing++;
                }
            }

            bool lastContested = (bool)(m_vm.get($"base:{b.Index}:contested") ?? false);
            bool contested = false;

            for (int team = 0; team < 2; team++)
            {
                int count = m_vm.lcount($"base:{b.Index}:cap:{team}");

                if (numCapturing[team] == 0)
                {
                    if (count > 0)
                    {
                        var lastRemoveTime = (int)(m_vm.get($"base:{b.Index}:uncap:{team}") ?? 0);

                        if ((netTime - lastRemoveTime) > 1500)
                        {
                            m_vm.lrem($"base:{b.Index}:cap:{team}", 0);
                            m_vm.set($"base:{b.Index}:uncap:{team}", netTime);
                        }
                    }

                    continue;
                }

                if (totalCapturing != numCapturing[team])
                {
                    // contested!
                    contested = true;
                    continue;
                }

                var captureTime = Math.Max(200, 750 - (numCapturing[team] * 100));

                var lastCaptureTick = (count > 0) ? Convert.ToInt32(m_vm.lget($"base:{b.Index}:cap:{team}", count - 1)) : 0;

                if ((netTime - lastCaptureTick) > captureTime)
                {
                    m_vm.ladd($"base:{b.Index}:cap:{team}", netTime);
                }

                if (count >= 40)
                {
                    m_vm.set($"points:{team}", (int)(m_vm.get($"points:{team}") ?? 0) + 1);
                    m_vm.set("currentBase", b.Index + 1);
                }
            }

            if (contested != lastContested)
            {
                m_vm.set($"base:{b.Index}:contested", contested);
            }
        }

        private void DeleteWatchedPeds()
        {
            var pedCount = m_vm.lcount("pedsToWatch");

            for (int i = pedCount - 1; i >= 0; i--)
            {
                bool delete = false;
                var netID = Convert.ToInt32(m_vm.lget("pedsToWatch", i));

                if (!NetworkDoesEntityExistWithNetworkId(netID))
                {
                    if (NetworkIsHost())
                    {
                        delete = true;
                    }
                }
                else if (NetworkHasControlOfNetworkId(netID))
                {
                    var ped = NetToPed(netID);
                    var coords = GetEntityCoords(ped, false);

                    if (!IsSphereVisibleToAnotherMachine(coords.X, coords.Y, coords.Z, 1.5f) && !IsSphereVisible(coords.X, coords.Y, coords.Z, 1.5f))
                    {
                        SetEntityAsMissionEntity(ped, true, false);
                        DeleteEntity(ref ped);
                        delete = true;
                    }
                }

                if (delete)
                {
                    m_vm.lrem("pedsToWatch", i);
                }
            }
        }

        private int m_lastPedUpdate;
        private int m_lastPedCheck;

        private const int PEDS_PER_FRAME = 3;

        private void UpdatePeds(Base b)
        {
            var idx = b.Index;

            int pedCount = m_vm.lcount($"base:{idx}:peds");

            var i = m_lastPedUpdate;

            for (int p = 0; p < PEDS_PER_FRAME; p++)
            {
                if (i >= pedCount)
                {
                    i = 0;
                }

                int ped = Convert.ToInt32(m_vm.lget($"base:{idx}:peds", i));

                if (NetworkDoesEntityExistWithNetworkId(ped))
                {
                    UpdatePed(b, ped);
                }
                else
                {
                    if (m_blips.ContainsKey(ped))
                    {
                        var blip = m_blips[ped];
                        RemoveBlip(ref blip);

                        m_blips.Remove(ped);
                    }
                }

                i++;
            }

            m_lastPedUpdate = i;
        }

        private void CleanPeds(Base b)
        {
            void CleanPed(int ped)
            {
                if (NetworkHasControlOfNetworkId(ped))
                {
                    int handle = NetToPed(ped);
                    SetEntityAsNoLongerNeeded(ref handle);

                    m_vm.ladd($"pedsToWatch", ped);
                }
            }

            var idx = b.Index;

            int pedCount = m_vm.lcount($"base:{idx}:peds");

            for (int i = 0; i < pedCount; i++)
            {
                int ped = Convert.ToInt32(m_vm.lget($"base:{idx}:peds", i));

                if (NetworkDoesEntityExistWithNetworkId(ped))
                {
                    CleanPed(ped);
                }
            }

            foreach (var blip in m_blips)
            {
                var blipBit = blip.Value;
                RemoveBlip(ref blipBit);
            }

            m_blips.Clear();
            m_dead.Clear();
        }

        private void UpdatePed(Base b, int netID)
        {
            var ped = NetToPed(netID);
            var pedKey = $"base:{b.Index}:peds:{netID}";

            void UpdatePedControl()
            {
                bool dead = (bool)(m_vm.hget(pedKey, "dead") ?? false);

                if (dead)
                {
                    return;
                }

                // 342, 351, 424
                // 351 == CTaskThreatResponse
                if (GetScriptTaskStatus(ped, 0x42cc4f21) == 7 && !GetIsTaskActive(ped, 351))
                {
                    SetPedRelationshipGroupHash(ped, (uint)Game.GenerateHash("Escape_Enemy"));
                    SetEntityCanBeDamagedByRelationshipGroup(ped, false, Game.GenerateHash("Escape_Enemy"));

                    SetPedAccuracy(ped, 10);

                    var coords = GetEntityCoords(ped, false);
                    TaskCombatHatedTargetsInArea(ped, coords[0], coords[1], coords[2], 45f, 0);
                }

                if (IsEntityDead(ped))
                {
                    if (!dead)
                    {
                        m_vm.hset(pedKey, "dead", true);
                        
                        m_vm.ladd($"pedsToWatch", netID);
                    }

                    //SetEntityAsMissionEntity(ped, true, false);
                    //DeleteEntity(ref ped);
                    SetEntityAsNoLongerNeeded(ref ped);
                }
            };

            if (DoesEntityExist(ped))
            {
                if (NetworkHasControlOfEntity(ped))
                {
                    UpdatePedControl();
                }

                var dead = this.m_dead.ContainsKey(netID);

                if (dead)
                {
                    if ((bool)(m_vm.hget(pedKey, "dead") ?? false))
                    {
                        return;
                    }

                    this.m_dead.Remove(netID);
                }

                if (!IsEntityDead(ped) && !this.m_blips.ContainsKey(netID))
                {
                    var blip = AddBlipForEntity(ped);
                    SetBlipColour(blip, 1);
                    SetBlipDisplay(blip, 2);
                    SetBlipScale(blip, 0.6f);
                    SetBlipAsShortRange(blip, true);
                    SetBlipCategory(blip, 3);

                    BeginTextCommandSetBlipName("RUSH_ENEMY");
                    EndTextCommandSetBlipName(blip);

                    this.m_blips[netID] = blip;
                }
                else if (IsEntityDead(ped))
                {
                    if (m_blips.ContainsKey(netID))
                    {
                        var blip = m_blips[netID];
                        RemoveBlip(ref blip);

                        this.m_blips.Remove(netID);
                    }

                    this.m_dead[netID] = true;
                }
            }
            else
            {
                if (this.m_blips.ContainsKey(netID))
                {
                    var blip = m_blips[netID];
                    RemoveBlip(ref blip);

                    this.m_blips.Remove(netID);
                }
            }
        }

        private void CheckPeds(Base b)
        {
            if ((GetGameTimer() - m_lastPedCheck) < 500)
            {
                return;
            }

            m_lastPedCheck = GetGameTimer();

            var idx = b.Index;
            int pedCount = m_vm.lcount($"base:{idx}:peds");

            float needPeds = Math.Min(b.Radius * 0.2f, 20);
            float baseNeedPeds = needPeds;

            for (int i = 0; i < pedCount; i++)
            {
                int ped = Convert.ToInt32(m_vm.lget($"base:{idx}:peds", i));
                var pedKey = $"base:{idx}:peds:{ped}";

                bool dead = (bool)(m_vm.hget(pedKey, "dead") ?? false);

                if (!dead)
                {
                    needPeds -= 1;
                }
                else
                {
                    needPeds -= baseNeedPeds * 0.015f;
                }
            }

            var needPedsInt = (int)Math.Ceiling(needPeds);

            for (int p = 0; p < needPedsInt; p++)
            {
                CreateEnemyPed(b);
            }
        }

        private void CreateEnemyPed(Base b)
        {
            var result = false;
            Vector3 pos = Vector3.Zero;

            var tries = 0;

            while (!result)
            {
                // TODO: cases +0.5 is round?
                var x = GetRandomFloatInRange(b.Min.X, b.Max.X);
                var y = GetRandomFloatInRange(b.Min.Y, b.Max.Y);

                float z = 0.0f;

                if (GetGroundZFor_3dCoord(x, y, 1000f, ref z, false))
                {
                    result = GetSafeCoordForPed(x, y, z, false, ref pos, 16);
                }

                tries++;

                if (tries > 20)
                {
                    return;
                }
            }

            var ped = CreatePed(4, (uint)Game.GenerateHash("a_m_m_skater_01"), pos.X, pos.Y, pos.Z - 1.0f, GetRandomFloatInRange(0, 359.5f), true, false);
            var netID = NetworkGetNetworkIdFromEntity(ped);

            GiveWeaponToPed(ped, (uint)Game.GenerateHash("WEAPON_MICROSMG"), 500, false, true);

            try
            {
                SetCurrentPedWeapon(ped, (uint)Game.GenerateHash("WEAPON_MICROSMG"), true);
            }
            catch {}

            m_vm.ladd($"base:{b.Index}:peds", netID);
            m_vm.hset($"base:{b.Index}:peds:{netID}", "dead", false);
        }

        private Player FindResponsiblePlayer(Vector3 position)
        {
            return Players.OrderBy(p => p.Character.Position.DistanceToSquared2D(position)).FirstOrDefault();
        }
    }
}