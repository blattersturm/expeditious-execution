using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace AmbientWarClient
{
    internal class GangWarListener : BaseScript
    {
        private GangWarDataStore dataStore;

        private Dictionary<int, Blip> blipHandles = new Dictionary<int, Blip>();

        private Dictionary<int, bool> deadPeds = new Dictionary<int, bool>();

        public GangWarListener()
        {
            // :(
        }

        public GangWarListener(GangWarDataStore dataStore)
        {
            this.dataStore = dataStore;

            this.Tick += OnTick;
        }

        public event Action<Ped> GangPedKilled;

        private async Task OnTick()
        {
            int ped = 0;
            int findHandle = FindFirstPed(ref ped);

            int count = 0;

            if (findHandle != -1)
            {
                try
                {
                    do
                    {
                        ProcessPed(ped);

                        if (count > 25)
                        {
                            await Delay(0);

                            count = 0;
                        }

                        count++;
                    } while (FindNextPed(findHandle, ref ped));
                }
                finally
                {
                    EndFindPed(findHandle);
                }
            }

            foreach (var blip in blipHandles.ToArray())
            {
                if (!DoesEntityExist(blip.Key))
                {
                    Debug.WriteLine($"Cleaning up blip for {blip.Key}");

                    blip.Value.Delete();
                    blipHandles.Remove(blip.Key);
                }
            }

            await Delay(100);
        }

        private void ProcessPed(int ped)
        {
            var pedObject = new Ped(ped);

            if (pedObject.IsAlive)
            {
                if (!this.blipHandles.ContainsKey(ped))
                {
                    var pedModel = GetEntityModel(ped);

                    if (dataStore.GetGangByModel(pedModel, out var gang))
                    {
                        var color = gang.Color;
                        var blip = new Blip(AddBlipForEntity(ped));

                        SetBlipColour(blip.Handle, (int)(255 | (color.B << 8) | (color.G << 16) | (color.R << 24)));
                        SetBlipAsShortRange(blip.Handle, true);
                        SetBlipDisplay(blip.Handle, (pedObject.IsInVehicle()) ? 0 : 2);
                        SetBlipScale(blip.Handle, 0.7f);
                        SetBlipNameFromTextFile(blip.Handle, gang.Identifier);
                        SetBlipCategory(blip.Handle, 4);

                        blipHandles[ped] = blip;

                        Debug.WriteLine($"Adding blip handle for {ped}");
                    }
                }

                if (NetworkGetEntityIsNetworked(ped))
                {
                    deadPeds[pedObject.NetworkId] = false;
                }
            }
            else
            {
                if (this.blipHandles.ContainsKey(ped))
                {
                    Debug.WriteLine($"Removing blip handle for {ped}");

                    var nid = pedObject.NetworkId;

                    if (!deadPeds.ContainsKey(nid) || !deadPeds[nid])
                    {
                        if (HasEntityBeenDamagedByEntity(ped, PlayerPedId(), true))
                        {
                            GangPedKilled?.Invoke(pedObject);

                            deadPeds[nid] = true;
                        }
                    }

                    blipHandles[ped].Delete();
                    blipHandles.Remove(ped);
                }
            }
        }
    }
}