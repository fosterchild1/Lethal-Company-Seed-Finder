using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using SeedFinder;
using UnityEngine;
using static System.Random;

namespace SeedFinder
{
    [BepInPlugin(modGUID, "Seedfinder", "1.0.0")]
    public class SeedFinderBase : BaseUnityPlugin
    {
        private const string modGUID = "seedfinder.ye";
        private readonly Harmony harmony = new Harmony(modGUID);
        public static SeedFinderBase Instance;
        internal ManualLogSource logger;
        public class EnemySpawn
        {
            public string EnemyType { get; set; }
            public int amount { get; set; }
        }
        public class LevelScrap
        {
            public int rarity { get; set; }
            public bool twoHanded { get; set; }
            public string name { get; set; }
        }

        private List<LevelScrap> ExperimentationScrap = new List<LevelScrap>
        {
            new LevelScrap{rarity=80, twoHanded=true, name="Large axle"},
            new LevelScrap{rarity=90, twoHanded=true, name="V-type engine"},
            new LevelScrap{rarity=12, twoHanded=false, name="Plastic fish"},
            new LevelScrap{rarity=88, twoHanded=false, name="Metal sheet"},
            new LevelScrap{rarity=4, twoHanded=false, name="Laser pointer"},
            new LevelScrap{rarity=88, twoHanded=false, name="Big bolt"},
            new LevelScrap{rarity=19, twoHanded=true, name="Bottles"},
            new LevelScrap{rarity=3, twoHanded=false, name="Ring"},
            new LevelScrap{rarity=32, twoHanded=false, name="Steering wheel"},
            new LevelScrap{rarity=5, twoHanded=false, name="Cookie mold pan"},
            new LevelScrap{rarity=10, twoHanded=false, name="Egg Beater"},
            new LevelScrap{rarity=10, twoHanded=false, name="Jar of pickles"},
            new LevelScrap{rarity=32, twoHanded=false, name="Dust pan"},
            new LevelScrap{rarity=3, twoHanded=false, name="Airhorn"},
            new LevelScrap{rarity=3, twoHanded=false, name="Clown horn"},
            new LevelScrap{rarity=3, twoHanded=true, name="Cash register"},
            new LevelScrap{rarity=2, twoHanded=false, name="Candy"},
            new LevelScrap{rarity=1, twoHanded=false, name="Gold bar"},
            new LevelScrap{rarity=6, twoHanded=false, name="Yield Sign"},
            new LevelScrap{rarity=22, twoHanded=false, name="Homemade flashbang"},
            new LevelScrap{rarity=17, twoHanded=false, name="Gift"},
            new LevelScrap{rarity=42, twoHanded=false, name="Flask"},
            new LevelScrap{rarity=5, twoHanded=false, name="Easter egg"}
        };

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            harmony.PatchAll(typeof(SeedFinderBase));

            int targetItem = 16;

            logger.LogInfo("finder awake");
            for (int seed = 0; seed < 100000; seed++)
            {
              //  logger.LogInfo(seed.ToString());
                System.Random AnomalyRandom = new System.Random(seed + 5);
                int num = AnomalyRandom.Next(8, 12);

                if (AnomalyRandom.Next(0, 500) <= 25)
                {
                    //logger.LogInfo(string.Format("SSD Found: {0}", seed));
                    int num3 = AnomalyRandom.Next(0, ExperimentationScrap.Count);
                    bool flag = false;
                    for (int n = 0; n < 2; n++)
                    {
                        if (ExperimentationScrap[num3].rarity >= 5 && !ExperimentationScrap[num3].twoHanded)
                        {
                            flag = true;
                            break;
                        }
                        num3 = AnomalyRandom.Next(0, ExperimentationScrap.Count);
                    }
                    if (!flag && AnomalyRandom.Next(0, 100) < 60)
                    {
                        num3 = -1;
                    }
                    if (num3 == targetItem)
                    {
                        logger.LogInfo(string.Format("SSD Found: {0}", seed));
                    }
                }
            }
        }
    }
}