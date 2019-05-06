using System;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace AmbientWarClient
{
    public class BaseClient : BaseScript
    {
        private GangWarDataStore DataStore { get; set; }

        private GangWarZoneDisplay ZoneDisplay { get; set; }
        private GangWarListener Listener { get; set; }

        public BaseClient()
        {
            DataStore = new GangWarDataStore();
            ZoneDisplay = new GangWarZoneDisplay(DataStore);
            Listener = new GangWarListener(DataStore);

            BaseScript.RegisterScript(ZoneDisplay);
            BaseScript.RegisterScript(Listener);

            AddTextEntry("AMBIENT_GANG_LOST", "Enemy Gang: <C>Lost</C>");
            AddTextEntry("AMBIENT_GANG_MEXICAN", "Enemy Gang: <C>Vagos</C>");
            AddTextEntry("AMBIENT_GANG_FAMILY", "Enemy Gang: <C>Families</C>");

            EventHandlers["populationPedCreating"] += new Action<float, float, float, uint, dynamic>(PopulationPedCreating);
        }

        private void PopulationPedCreating(float x, float y, float z, uint model, dynamic setters)
        {
            // don't override any animal
            if (Animals.IsAnimal((int)model))
            {
                return;
            }

            // random chance
            if (GetRandomFloatInRange(0.0f, 1.0f) < 0.3f)
            {
                var zoneIdx = GetZoneAtCoords(x, y, z);

                if (DataStore.ZoneHashes.TryGetValue(zoneIdx, out var zoneName))
                {
                    if (DataStore.Zones.TryGetValue(zoneName, out var zoneInfo))
                    {
                        if (zoneInfo.Owner != null)
                        {
                            var newModel = DataStore.GetGangModel(zoneInfo.Owner);

                            RequestModel((uint)newModel);

                            try
                            {
                                setters.setModel((uint)newModel);
                            }
                            catch {}
                        }
                    }
                }
            }
        }
    }
}
