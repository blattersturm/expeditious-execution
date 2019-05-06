using System;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace AmbientWarClient
{
    internal class GangWarZoneDisplay : BaseScript
    {
        private GangWarDataStore dataStore;
        private int overlayHandle;
        private bool firstTick;

        public GangWarZoneDisplay()
        {
            // :(
        }

        public GangWarZoneDisplay(GangWarDataStore dataStore)
        {
            this.dataStore = dataStore;

            Tick += this.OnTick;
        }

        private async Task OnTick()
        {
            if (!firstTick)
            {
                overlayHandle = AddMinimapOverlay("gang_areas.gfx");

                while (!HasMinimapOverlayLoaded(overlayHandle))
                {
                    await Delay(50);
                }

                foreach (GangInfo gang in this.dataStore.Gangs)
                {
                    AddGangColor(gang.Identifier, gang.Color.R, gang.Color.G, gang.Color.B);
                }

                foreach (var zoneInfo in this.dataStore.Zones)
                {
                    var zone = zoneInfo.Value;
                    var name = zoneInfo.Key;

                    AddGangArea(zone.X1, zone.Y1, zone.X2, zone.Y2, name);
                    SetGangAreaOwner(name, zone.Owner);
                }

                firstTick = true;
            }

            await Task.FromResult(0);
        }

        private void SetGangAreaOwner(string name, string owner)
        {
            CallMinimapScaleformFunction(overlayHandle, "SET_GANG_AREA_OWNER");
            PushScaleformMovieFunctionParameterString(name);
            PushScaleformMovieFunctionParameterString(owner);
            PopScaleformMovieFunctionVoid();
        }

        private void AddGangArea(float x1, float y1, float x2, float y2, string name)
        {
            CallMinimapScaleformFunction(overlayHandle, "ADD_GANG_AREA");
            PushScaleformMovieFunctionParameterFloat(x1);
            PushScaleformMovieFunctionParameterFloat(y1);
            PushScaleformMovieFunctionParameterFloat(x2);
            PushScaleformMovieFunctionParameterFloat(y2);
            PushScaleformMovieFunctionParameterString(name);
            PopScaleformMovieFunctionVoid();
        }

        private void AddGangColor(string identifier, byte r, byte g, byte b)
        {
            CallMinimapScaleformFunction(overlayHandle, "ADD_GANG_COLOR");
            PushScaleformMovieFunctionParameterString(identifier);
            PushScaleformMovieFunctionParameterInt(r);
            PushScaleformMovieFunctionParameterInt(g);
            PushScaleformMovieFunctionParameterInt(b);
            PopScaleformMovieFunctionVoid();
        }
    }
}