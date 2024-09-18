using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using SeedFinder;
using SeedFinder.getObjects;
using SeedFinder.Patches;
using SeedFinder.predictOutsideEnemies;
using UnityEngine;
using UnityEngine.AI;
using static System.Random;

namespace SeedFinder
{
    [BepInPlugin(modGUID, "Seedfinder", "1.0.0")]
    public class SeedFinderBase : BaseUnityPlugin
    {
        public const string modGUID = "seedfinder.ye";
        private readonly Harmony harmony = new Harmony(modGUID);
        public static SeedFinderBase Instance;
        internal ManualLogSource logger;


        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            harmony.PatchAll(typeof(SeedFinderBase));
            harmony.PatchAll(typeof(SeedFinderPatches));
            logger.LogInfo("finder awake");
        }
    }
}

namespace SeedFinder.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    public class SeedFinderPatches
    {
        private static ManualLogSource logger = SeedFinderBase.Instance.logger;

        private static SpawnableItemWithRarity CheckSSD(int seed, SelectableLevel level)
        {
            System.Random AnomalyRandom = new System.Random(seed + 5);
            AnomalyRandom.Next(8, 12);

            if (AnomalyRandom.Next(0, 500) <= 25)
            {
                //logger.LogInfo(string.Format("SSD Found: {0}", seed));
                int num3 = AnomalyRandom.Next(0, level.spawnableScrap.Count);
                bool flag = false;
                for (int n = 0; n < 2; n++)
                {
                    if (level.spawnableScrap[num3].rarity >= 5 && !level.spawnableScrap[num3].spawnableItem.twoHanded)
                    {
                        flag = true;
                        break;
                    }
                    num3 = AnomalyRandom.Next(0, level.spawnableScrap.Count);
                }
                if (!flag && AnomalyRandom.Next(0, 100) < 60)
                {
                    num3 = -1;
                }
                if (num3 != -1) 
                {
                    return level.spawnableScrap[num3];
                }
            }
            return null;
        }

        [HarmonyPatch("AdvanceHourAndSpawnNewBatchOfEnemies")]
        [HarmonyPostfix]
        static void Patch2(ref int ___currentHour)
        {
            logger.LogInfo(string.Format("ADVANCED HOUR, CURRENT HOUR: {0}", ___currentHour));
        }

        [HarmonyPatch("PredictAllOutsideEnemies")]
        [HarmonyPrefix]
        static void Patch(ref SelectableLevel ___currentLevel, ref NavMeshHit ___navHit, ref Transform[] ___shipSpawnPathPoints, 
            ref int ___minOutsideEnemiesToSpawn, ref TimeOfDay ___timeScript)
        {
            SelectableLevel level = ___currentLevel;
            NavMeshHit navHit = ___navHit;
            Transform[] shipSpawnPathPoints = ___shipSpawnPathPoints;
            int minOutsideEnemiesToSpawn = ___minOutsideEnemiesToSpawn;
            float currentMaxOutsidePower = (float)level.maxOutsideEnemyPowerCount;
            TimeOfDay timeScript = ___timeScript;

            int highest = 0;
            void checkSeed(int seed)
            {

                //  logger.LogInfo(seed.ToString());
               // SpawnableItemWithRarity item = CheckSSD(seed, level);
                //Dictionary<string, int> objects = outsideObjects.getOutsideObjects(seed, level, navHit, shipSpawnPathPoints);
                Dictionary<string, int> predictedEnemies = predicter.predictAllOutsideEnemies(seed, level, minOutsideEnemiesToSpawn, currentMaxOutsidePower, timeScript);
                int mechs;
                if (predictedEnemies.TryGetValue("RadMech", out mechs) && mechs >= highest)
                {
                    logger.LogInfo(string.Format("HIGH MECH AMOUNT ({0}): {1}", mechs, seed));
                    highest = mechs;
                }
            }
           
            for (int seed = 0; seed < 10000; seed++)
            {
                checkSeed(seed);
            }
        }
    }
}