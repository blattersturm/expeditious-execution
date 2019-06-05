using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

#if NOPE
namespace BaseRush.Client
{
    public class BaseRushTeleportTools : BaseScript
    {
        public class TeleportData
        {
            public float X {get; set;}
            public float Y {get; set;}
            public float Z {get; set;}
            public float Heading {get; set;}

            public string Name {get; set;}
        }

        private List<TeleportData> m_teleportData = new List<TeleportData>();

        [EventHandler("onClientResourceStart")]
        public void OnClientResourceStart(string resourceName)
        {
            if (resourceName != GetCurrentResourceName()) return;

            AddTextEntry("TP_HELP", "Teleport: ~a~ (press ~INPUT_VEH_FLY_PITCH_UP_ONLY~)");

            m_teleportData = JsonConvert.DeserializeObject<List<TeleportData>>(GetResourceKvpString("teleports") ?? "[]");
        }

        private void SaveTeleports()
        {
            SetResourceKvp("teleports", JsonConvert.SerializeObject(m_teleportData));
        }

        private int m_tpIndex;

        private Blip m_blip;

        [Tick]
        public Task OnTick()
        {
            if (m_teleportData.Count == 0)
            {
                return Task.FromResult(0);
            }

            int delta = 0;

            if (IsControlJustPressed(0, (int)Control.VehicleCinematicDownOnly) && !IsControlJustPressed(0, (int)Control.PhoneScrollForward))
            {
                delta = 1;
            }
            else if (IsControlJustPressed(0, (int)Control.VehicleCinematicUpOnly) && !IsControlJustPressed(0, (int)Control.PhoneScrollBackward))
            {
                delta = -1;
            }

            if (delta != 0)
            {
                m_tpIndex += delta;

                if (m_tpIndex >= m_teleportData.Count)
                {
                    m_tpIndex = 0;
                }
                else if (m_tpIndex < 0)
                {
                    m_tpIndex = m_teleportData.Count - 1;
                }

                m_blip?.Delete();

                var tp = m_teleportData[m_tpIndex];
                m_blip = World.CreateBlip(new Vector3(tp.X, tp.Y, tp.Z));
                m_blip.Sprite = BlipSprite.CaptureAmericanFlag;

                BeginTextCommandDisplayHelp("TP_HELP");
                AddTextComponentSubstringPlayerName(tp.Name);
                EndTextCommandDisplayHelp(0, false, true, 5000);
            }

            if (IsControlJustPressed(0, (int)Control.VehicleFlyPitchUpOnly))
            {
                var tp = m_teleportData[m_tpIndex];
                SetPedCoordsKeepVehicle(PlayerPedId(), tp.X, tp.Y, tp.Z);

                m_blip?.Delete();
            }

            return Task.FromResult(0);
        }

        [Command("tp_add")]
        public void TeleportSave(string[] args)
        {
            DoAdd(args.Length > 0 ? args[0] : "Teleport");
        }

        [Command("tp_del")]
        public void TeleportDelete(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int id))
            {
                m_teleportData.RemoveAt(id);

                m_blip?.Delete();
                m_tpIndex = 0;
            }
        }

        [Command("tp_go")]
        public void TeleportGo(string[] args)
        {
            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int id))
                {
                    var tp = m_teleportData[id];
                    SetPedCoordsKeepVehicle(PlayerPedId(), tp.X, tp.Y, tp.Z);
                }
                else
                {
                    var tp = m_teleportData.FirstOrDefault(a => a.Name.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));

                    if (tp != null)
                    {
                        SetPedCoordsKeepVehicle(PlayerPedId(), tp.X, tp.Y, tp.Z);
                    }
                }
            }
        }

        [Command("tp_list")]
        public void TeleportList()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < m_teleportData.Count; i++)
            {
                var tp = m_teleportData[i];
                sb.Append($"{i}: {tp.Name} ({tp.X:0.00}, {tp.Y:0.00}, {tp.Z:0.00} - {tp.Heading:0.0})\n");
            }

            TriggerEvent("chat:addMessage", new
            {
                args = new[] { sb.ToString() },
                multiline = true
            });

            Debug.WriteLine(sb.ToString());
        }

        private void DoAdd(string name)
        {
            var pos = LocalPlayer.Character.Position;

            m_teleportData.Add(new TeleportData()
            {
                X = pos.X,
                Y = pos.Y,
                Z = pos.Z - 1.0f,
                Heading = LocalPlayer.Character.Heading,
                Name = name
            });

            SaveTeleports();
        }
    }
}
#endif