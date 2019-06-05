using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace BaseRush.Client
{
    public class PlayerBlips : BaseScript
    {
        /// <summary>
        /// Sets the correct blip sprite for the specific ped and blip.
        /// This is the (old) backup method for setting the sprite if the decorators version doesn't work.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="blip"></param>
        public static void SetCorrectBlipSprite(int ped, int blip)
        {
            if (IsPedInAnyVehicle(ped, false))
            {
                int vehicle = GetVehiclePedIsIn(ped, false);
                int blipSprite = BlipInfo.GetBlipSpriteForVehicle(vehicle);
                if (GetBlipSprite(blip) != blipSprite)
                {
                    SetBlipSprite(blip, blipSprite);
                }

            }
            else
            {
                SetBlipSprite(blip, 1);
            }
        }

        /// <summary>
        /// Returns the current or last vehicle of the current player.
        /// </summary>
        /// <param name="lastVehicle"></param>
        /// <returns></returns>
        public static Vehicle GetVehicle(bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return Game.PlayerPed.LastVehicle;
            }
            else
            {
                if (Game.PlayerPed.IsInVehicle())
                {
                    return Game.PlayerPed.CurrentVehicle;
                }
            }
            return null;
        }

        [Tick]
        public async Task ProcessPlayerBlips()
        {
            if (DecorIsRegisteredAsType("br_player_blip_sprite_id", 3))
            {
                int sprite = 1;
                if (IsPedInAnyVehicle(Game.PlayerPed.Handle, false))
                {
                    Vehicle veh = GetVehicle();
                    if (veh != null && veh.Exists())
                    {
                        sprite = BlipInfo.GetBlipSpriteForVehicle(veh.Handle);
                    }
                }
                try
                {
                    DecorSetInt(Game.PlayerPed.Handle, "br_player_blip_sprite_id", sprite);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(@"[CRITICAL] A critical bug in one of your scripts was detected. vMenu is unable to set or register a decorator's value because another resource has already registered 1.5k or more decorators. vMenu will NOT work as long as this bug in your other scripts is unsolved. Please fix your other scripts. This is *NOT* caused by or fixable by vMenu!!!");
                    Debug.WriteLine($"Error Location: {e.StackTrace}\nError info: {e.Message.ToString()}");
                    await Delay(1000);
                }

                bool enabled = true;

                foreach (Player p in Players)
                {
                    // continue only if this player is valid.
                    if (p != null && NetworkIsPlayerActive(p.Handle) && p.Character != null && p.Character.Exists())
                    {

                        //    
                        //else
                        //    SetBlipDisplay(blip, 3);

                        // if blips are enabled and the player has permisisons to use them.
                        if (enabled && GetPlayerTeam(p.Handle) == GetPlayerTeam(PlayerId()))
                        {
                            if (p != Game.Player)
                            {
                                int ped = p.Character.Handle;
                                int blip = GetBlipFromEntity(ped);

                                // if blip id is invalid.
                                if (blip < 1)
                                {
                                    blip = AddBlipForEntity(ped);
                                }
                                // only manage the blip for this player if the player is nearby
                                if (p.Character.Position.DistanceToSquared2D(Game.PlayerPed.Position) < 500000 || Game.IsPaused)
                                {
                                    // (re)set the blip color in case something changed it.
                                    SetBlipColour(blip, 0);

                                    // if the decorator exists on this player, use the decorator value to determine what the blip sprite should be.
                                    if (DecorExistOn(p.Character.Handle, "br_player_blip_sprite_id"))
                                    {
                                        int decorSprite = DecorGetInt(p.Character.Handle, "br_player_blip_sprite_id");
                                        // set the sprite according to the decorator value.
                                        SetBlipSprite(blip, decorSprite);

                                        // show heading on blip only if the player is on foot (blip sprite 1)
                                        ShowHeadingIndicatorOnBlip(blip, decorSprite == 1);

                                        // set the blip rotation if the player is not in a helicopter (sprite 422).
                                        if (decorSprite != 422)
                                        {
                                            SetBlipRotation(blip, (int)GetEntityHeading(ped));
                                        }
                                    }
                                    else // backup method for when the decorator value is not found.
                                    {
                                        // set the blip sprite using the backup method in case decorators failed.
                                        SetCorrectBlipSprite(ped, blip);

                                        // only show the heading indicator if the player is NOT in a vehicle.
                                        if (!IsPedInAnyVehicle(ped, false))
                                        {
                                            ShowHeadingIndicatorOnBlip(blip, true);
                                        }
                                        else
                                        {
                                            ShowHeadingIndicatorOnBlip(blip, false);

                                            // If the player is not in a helicopter, set the blip rotation.
                                            if (!p.Character.IsInHeli)
                                            {
                                                SetBlipRotation(blip, (int)GetEntityHeading(ped));
                                            }
                                        }
                                    }

                                    // set the player name.
                                    SetBlipNameToPlayerName(blip, p.Handle);

                                    // thanks lambda menu for hiding this great feature in their source code!
                                    // sets the blip category to 7, which makes the blips group under "Other Players:"
                                    SetBlipCategory(blip, 7);

                                    //N_0x75a16c3da34f1245(blip, false); // unknown

                                    // display on minimap and main map.
                                    SetBlipDisplay(blip, 6);
                                }
                                else
                                {
                                    // hide it from the minimap.
                                    SetBlipDisplay(blip, 3);
                                }
                            }
                        }
                        else // blips are not enabled.
                        {
                            if (!(p.Character.AttachedBlip == null || !p.Character.AttachedBlip.Exists()))
                            {
                                p.Character.AttachedBlip.Delete(); // remove player blip if it exists.
                            }
                        }
                    }
                }
            }
            else // decorator does not exist.
            {
                try
                {
                    DecorRegister("br_player_blip_sprite_id", 3);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(@"[CRITICAL] A critical bug in one of your scripts was detected. vMenu is unable to set or register a decorator's value because another resource has already registered 1.5k or more decorators. vMenu will NOT work as long as this bug in your other scripts is unsolved. Please fix your other scripts. This is *NOT* caused by or fixable by vMenu!!!");
                    Debug.WriteLine($"Error Location: {e.StackTrace}\nError info: {e.Message.ToString()}");
                    await Delay(1000);
                }
                while (!DecorIsRegisteredAsType("br_player_blip_sprite_id", 3))
                {
                    await Delay(0);
                }
            }
        }
    }

    public static class BlipInfo
    {
        public static int GetBlipSpriteForVehicle(int vehicle)
        {
            uint model = (uint)GetEntityModel(vehicle);
            Dictionary<uint, int> sprites = new Dictionary<uint, int>()
            {
                { (uint)GetHashKey("taxi"), 56 },
                //
                { (uint)GetHashKey("nightshark"), 225 },
                //
                { (uint)GetHashKey("rhino"), 421 },
                //
                { (uint)GetHashKey("lazer"), 424 },
                { (uint)GetHashKey("besra"), 424 },
                { (uint)GetHashKey("hydra"), 424 },
                //
                { (uint)GetHashKey("insurgent"), 426 },
                { (uint)GetHashKey("insurgent2"), 426 },
                { (uint)GetHashKey("insurgent3"), 426 },
                //
                { (uint)GetHashKey("limo2"), 460 },
                //
                { (uint)GetHashKey("blazer5"), 512 },
                //
                { (uint)GetHashKey("phantom2"), 528 },
                { (uint)GetHashKey("boxville5"), 529 },
                { (uint)GetHashKey("ruiner2"), 530 },
                { (uint)GetHashKey("dune4"), 531 },
                { (uint)GetHashKey("dune5"), 531 },
                { (uint)GetHashKey("wastelander"), 532 },
                { (uint)GetHashKey("voltic2"), 533 },
                { (uint)GetHashKey("technical2"), 534 },
                { (uint)GetHashKey("technical3"), 534 },
                { (uint)GetHashKey("technical"), 534 },
                //
                { (uint)GetHashKey("apc"), 558 },
                { (uint)GetHashKey("oppressor"), 559 },
                { (uint)GetHashKey("oppressor2"), 559 },
                { (uint)GetHashKey("halftrack"), 560 },
                { (uint)GetHashKey("dune3"), 561 },
                { (uint)GetHashKey("tampa3"), 562 },
                { (uint)GetHashKey("trailersmall2"), 563 },
                //
                { (uint)GetHashKey("alphaz1"), 572 },
                { (uint)GetHashKey("bombushka"), 573 },
                { (uint)GetHashKey("havok"), 574 },
                { (uint)GetHashKey("howard"), 575 },
                { (uint)GetHashKey("hunter"), 576 },
                { (uint)GetHashKey("microlight"), 577 },
                { (uint)GetHashKey("mogul"), 578 },
                { (uint)GetHashKey("molotok"), 579 },
                { (uint)GetHashKey("nokota"), 580 },
                { (uint)GetHashKey("pyro"), 581 },
                { (uint)GetHashKey("rogue"), 582 },
                { (uint)GetHashKey("starling"), 583 },
                { (uint)GetHashKey("seabreeze"), 584 },
                { (uint)GetHashKey("tula"), 585 },
                //
                { (uint)GetHashKey("avenger"), 589 },
                //
                { (uint)GetHashKey("stromberg"), 595 },
                { (uint)GetHashKey("deluxo"), 596 },
                { (uint)GetHashKey("thruster"), 597 },
                { (uint)GetHashKey("khanjali"), 598 },
                { (uint)GetHashKey("riot2"), 599 },
                { (uint)GetHashKey("volatol"), 600 },
                { (uint)GetHashKey("barrage"), 601 },
                { (uint)GetHashKey("akula"), 602 },
                { (uint)GetHashKey("chernobog"), 603 },
            };

            if (sprites.ContainsKey(model))
            {
                return sprites[model];
            }
            else if (IsThisModelABike(model))
            {
                return 348;
            }
            else if (IsThisModelABoat(model))
            {
                return 427;
            }
            else if (IsThisModelAHeli(model))
            {
                return 422;
            }
            else if (IsThisModelAPlane(model))
            {
                return 423;
            }
            return 225;
        }
    }
}