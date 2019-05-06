using System;
using System.Collections.Generic;

using CitizenFX.Core;

namespace AmbientWarClient
{
    internal static class Animals
    {
        private static HashSet<int> animals = new HashSet<int>()
        {
            Game.GenerateHash("a_c_boar"),
            Game.GenerateHash("a_c_cat_01"),
            Game.GenerateHash("a_c_chickenhawk"),
            Game.GenerateHash("a_c_chimp"),
            Game.GenerateHash("a_c_chop"),
            Game.GenerateHash("a_c_cormorant"),
            Game.GenerateHash("a_c_cow"),
            Game.GenerateHash("a_c_coyote"),
            Game.GenerateHash("a_c_crow"),
            Game.GenerateHash("a_c_deer"),
            Game.GenerateHash("a_c_dolphin"),
            Game.GenerateHash("a_c_fish"),
            Game.GenerateHash("a_c_hen"),
            Game.GenerateHash("a_c_humpback"),
            Game.GenerateHash("a_c_husky"),
            Game.GenerateHash("a_c_killerwhale"),
            Game.GenerateHash("a_c_mtlion"),
            Game.GenerateHash("a_c_pig"),
            Game.GenerateHash("a_c_pigeon"),
            Game.GenerateHash("a_c_poodle"),
            Game.GenerateHash("a_c_rabbit_01"),
            Game.GenerateHash("a_c_rat"),
            Game.GenerateHash("a_c_retriever"),
            Game.GenerateHash("a_c_rhesus"),
            Game.GenerateHash("a_c_rottweiler"),
            Game.GenerateHash("a_c_seagull"),
            Game.GenerateHash("a_c_sharkhammer"),
            Game.GenerateHash("a_c_sharktiger"),
            Game.GenerateHash("a_c_shepherd"),
            Game.GenerateHash("a_c_westy"),
        };

        public static bool IsAnimal(int model)
        {
            return animals.Contains(model);
        }
    }
}