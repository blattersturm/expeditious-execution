using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace BaseRush.Client
{
    public class BaseRushSpawn : BaseScript
    {
        private bool m_hasSpawned = true;
        private bool m_spawning;
        private bool m_firstSpawn = true;

        private List<SpawnPoint> m_spawns = new List<SpawnPoint>();

        private Random m_random = new Random();

        private class SpawnPoint
        {
            public Vector3 Position { get; }
            public float Heading { get; }

            public SpawnPoint(dynamic mapEntry)
            {
                Position = new Vector3((float)mapEntry.pos[0], (float)mapEntry.pos[1], (float)mapEntry.pos[2]);
                Heading = (float)mapEntry.heading;
            }
        }

        public BaseRushSpawn()
        {
            MapManager.Instance.Value.RegisterKeyDirective("spawn_team");
        }

        [EventHandler("onClientGameTypeStart")]
        public void GameTypeStart()
        {
            Exports["spawnmanager"].setAutoSpawn(false);

            // request all path nodes, everywhere
            N_0xf7b79a50b905a30d(-8192.0f, -8192.0f, 8192.0f, 8192.0f);
        }

        [EventHandler("onClientMapStop")]
        public void MapStop()
        {
            Game.PlayerPed.Position = new Vector3(0.0f, 0.0f, 0.0f);
            
            DisplayRadar(false);

            if (!BaseRushGame.RoundEnded)
            {
                DoScreenFadeOut(0);
            }
        }

        [EventHandler("onClientGameTypeStop")]
        public void GameTypeStop()
        {
            DoScreenFadeOut(0);
        }

        [EventHandler("onClientMapStart")]
        public void MapStart()
        {
            m_spawns.Clear();
            m_spawns.AddRange(MapManager.Instance.Value.GetDirectives("spawn_team").Select(b => new SpawnPoint(b)));

            m_spawning = false;
            m_hasSpawned = false;
            m_firstSpawn = true;
        }

        [Tick]
        public async Task OnTick()
        {
            //if (IsScreenFadedOut())
            {
                //Debug.WriteLine($"[SPAWN DEBUG] {m_hasSpawned} {m_spawning} {m_spawns.Count} {GetPlayerTeam(PlayerId())}");
            }

            if (!m_hasSpawned && !m_spawning && m_spawns.Count > 0 && GetPlayerTeam(PlayerId()) != -1)
            {
                if (BaseRushGame.RoundEnded)
                {
                    return;
                }

                m_spawning = true;

                //float x = -802.311f, y = 175.056f, z = 72.8446f;
                float x, y, z;

                var p = m_spawns[GetPlayerTeam(PlayerId())];
                x = p.Position.X;
                y = p.Position.Y;
                z = p.Position.Z;

                if (!m_firstSpawn)
                {
                    var pos = await BaseRushGame.Instance.GetRespawnPosition();

                    if (pos.X != 0.0f || pos.Y != 0.0f || pos.Z != 0.0f)
                    {
                        x = pos.X;
                        y = pos.Y;
                        z = pos.Z;
                    }
                }
                else
                {
                    Debug.WriteLine($"[SPAWN DEBUG] getting spawn point");

                    var pos = await BaseRushGame.Instance.GetInitialPosition(p.Position);

                    x = pos.X;
                    y = pos.Y;
                    z = pos.Z;
                }

                Debug.WriteLine($"[SPAWN DEBUG] calling SM");

                Exports["spawnmanager"].forceRespawn();

                Exports["spawnmanager"].spawnPlayer(new
                {
                    x, y, z,
                    model = (int)((GetPlayerTeam(PlayerId()) == 1) ? PedHash.FibSec01SMM : PedHash.CiaSec01SMM),
                    heading = p.Heading,
                    skipFade = m_firstSpawn
                }, new Action(async () =>
                {
                    Debug.WriteLine($"[SPAWN DEBUG] called SM");

                    m_spawning = false;
                    m_firstSpawn = false;

                    LocalPlayer.Character.Weapons.RemoveAll();

                    Debug.WriteLine($"[SPAWN DEBUG] switching IN?");

                    if (GetPlayerSwitchState() != 0 && GetPlayerSwitchState() != 12)
                    {
                        Debug.WriteLine($"[SPAWN DEBUG] waiting for switch IN");

                        SwitchInPlayer(PlayerPedId());

                        while (GetPlayerSwitchState() != 12)
                        {
                            await Delay(0);
                        }

                        Debug.WriteLine($"[SPAWN DEBUG] switched IN");
                    }

                    DisplayRadar(true);

                    SetMaxWantedLevel(0);

                    LocalPlayer.Character.Weapons.Give(WeaponHash.SniperRifle, 30, false, true);
                    LocalPlayer.Character.Weapons.Give(WeaponHash.AssaultRifleMk2, 600, false, true);
                    LocalPlayer.Character.Weapons.Give(WeaponHash.HeavyShotgun, 120, false, true);
                    LocalPlayer.Character.Weapons.Give(WeaponHash.MicroSMG, 600, false, true);
                    LocalPlayer.Character.Weapons.Give(WeaponHash.Grenade, 6, false, true);
                    LocalPlayer.Character.Weapons.Give(WeaponHash.Hatchet, 1, false, true);
                    LocalPlayer.Character.Weapons.Give(WeaponHash.Pistol, 1200, true, true);

                    var vehicleTypes = new[] { "issi4", "issi5", "issi6" };
                    var model = new Model(vehicleTypes[m_random.Next(vehicleTypes.Length)]);

                    await model.Request(5000);

                    var vehicle = await World.CreateVehicle(model, World.GetNextPositionOnStreet(new Vector3(x, y, z), true));

                    if (vehicle != null)
                    {
                        vehicle.NeedsToBeHotwired = false;
                        vehicle.PreviouslyOwnedByPlayer = true;

                        var vehicleBlip = vehicle.AttachBlip();
                        vehicleBlip.Sprite = BlipSprite.PersonalVehicleCar;
                        vehicleBlip.IsFlashing = true;
                        
                        vehicle.MarkAsNoLongerNeeded();

                        await Delay(5000);

                        vehicleBlip.Delete();
                    }
                }));

                m_hasSpawned = true;
            }

            if (IsEntityDead(PlayerPedId()) && !m_spawning)
            {
                m_hasSpawned = false;
            }
        }
    }
}