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
using SeedFinder.Patches;
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
        void Update()
        {
            logger.LogInfo("hi");
            logger.LogInfo(RoundManager.Instance.minOutsideEnemiesToSpawn);
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


        private static Dictionary<string, int> getOutsideObjects(int seed, SelectableLevel currentLevel, NavMeshHit navHit, Transform[] shipSpawnPathPoints)
        {
            float RandomNumberInRadius(float radius, System.Random randomSeed)
            {
                return ((float)randomSeed.NextDouble() - 0.5f) * radius;
            }
            Vector3 GetRandomNavMeshPositionInBoxPredictable(Vector3 pos, float radius = 10f, NavMeshHit navHit_ = default(NavMeshHit), System.Random randomSeed = null, int layerMask = -1)
            {
                float y = pos.y;
                float x = RandomNumberInRadius(radius, randomSeed);
                float y2 = RandomNumberInRadius(radius, randomSeed);
                float z = RandomNumberInRadius(radius, randomSeed);
                Vector3 vector = new Vector3(x, y2, z) + pos;
                vector.y = y;
                float num = Vector3.Distance(pos, vector);
                if (NavMesh.SamplePosition(vector, out navHit, num + 2f, layerMask))
                {
                    return navHit.position;
                }
                return pos;
            }
            Vector3 PositionEdgeCheck(Vector3 position, float width, NavMeshHit navHit_)
            {
                if (!NavMesh.FindClosestEdge(position, out navHit_, -1) || navHit_.distance >= width)
                {
                    return position;
                }
                Vector3 position2 = navHit_.position;
                Ray ray = new Ray(position2, position - position2);
                if (NavMesh.SamplePosition(ray.GetPoint(width + 0.5f), out navHit_, 10f, -1))
                {
                    position = navHit_.position;
                    return position;
                }
                return Vector3.zero;
            }


            System.Random random = new System.Random(seed + 2);
            GameObject[] outsideAINodes = (from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
                                   orderby Vector3.Distance(x.transform.position, Vector3.zero)
                                   select x).ToArray<GameObject>();
            NavMeshHit navMeshHit = default(NavMeshHit);

            int num2 = 0;
            List<Vector3> list = new List<Vector3>();
            Dictionary<string, int> objectlist = new Dictionary<string, int>();
            GameObject[] spawnDenialPoints = GameObject.FindGameObjectsWithTag("SpawnDenialPoint");
            if (currentLevel.spawnableOutsideObjects != null)
            {
                for (int j = 0; j < currentLevel.spawnableOutsideObjects.Length; j++)
                {
                    var outsideobject = currentLevel.spawnableOutsideObjects[j];
                    int width = outsideobject.spawnableObject.objectWidth;
                    double num3 = random.NextDouble();
                    int num = (int)currentLevel.spawnableOutsideObjects[j].randomAmount.Evaluate((float)num3);
                    if ((float)random.Next(0, 100) < 20f)
                    {
                        num *= 2;
                    }
                    int k = 0;
                    while (k < num)
                    {
                        int num4 = random.Next(0, outsideAINodes.Length);
                        Vector3 vector = GetRandomNavMeshPositionInBoxPredictable(outsideAINodes[num4].transform.position, 30f, navMeshHit, random, -1);
                        if (currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnableFloorTags == null)
                        {
                            goto IL_251;
                        }
                        bool flag = false;
                        RaycastHit raycastHit;
                        if (Physics.Raycast(vector + Vector3.up, Vector3.down, out raycastHit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                        {
                            for (int l = 0; l < currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnableFloorTags.Length; l++)
                            {
                                if (raycastHit.collider.transform.CompareTag(currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnableFloorTags[l]))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (flag)
                        {
                            goto IL_251;
                        }
                    IL_57F:
                        k++;
                        continue;
                    IL_251:
                        vector = PositionEdgeCheck(vector, (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth, navHit);
                        if (vector == Vector3.zero)
                        {
                            goto IL_57F;
                        }
                        bool flag2 = false;
                        for (int m = 0; m < shipSpawnPathPoints.Length; m++)
                        {
                            if (Vector3.Distance(shipSpawnPathPoints[m].transform.position, vector) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth + 6f)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            goto IL_57F;
                        }
                        for (int n = 0; n < spawnDenialPoints.Length; n++)
                        {
                            if (Vector3.Distance(spawnDenialPoints[n].transform.position, vector) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth + 6f)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            goto IL_57F;
                        }
                        if (Vector3.Distance(GameObject.FindGameObjectWithTag("ItemShipLandingNode").transform.position, vector) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth + 4f)
                        {
                            break;
                        }
                        if (!flag2)
                        {
                            if (width > 4)
                            {
                                flag2 = false;
                                for (int num5 = 0; num5 < list.Count; num5++)
                                {
                                    if (Vector3.Distance(vector, list[num5]) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth)
                                    {
                                        flag2 = true;
                                        break;
                                    }
                                }
                                if (flag2)
                                {
                                    goto IL_57F;
                                }
                            }
                            list.Add(vector);

                            string objname = currentLevel.spawnableOutsideObjects[j].spawnableObject.prefabToSpawn.name;
                            if (!objectlist.ContainsKey(objname))
                            {
                                objectlist.Add(objname, 1);
                            }
                            objectlist[objname]++;
                            num2++;

                            if (!currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnFacingAwayFromWall)
                            {
                                random.Next(0, 360);
                            }
                        }
                        goto IL_57F;
                    }
                }
            }
            return objectlist;
        }

        private static void ResetEnemyTypesSpawnedCounts(SelectableLevel currentLevel)
        {
            for (int k = 0; k < currentLevel.OutsideEnemies.Count; k++)
            {
                currentLevel.OutsideEnemies[k].enemyType.numberSpawned = 0;
            }
        }


        private static Dictionary<string, int> predictAllOutsideEnemies(int seed, SelectableLevel currentLevel, int minOutsideEnemiesToSpawn, int currentMaxOutsidePower, TimeOfDay timeScript)
        {
            ResetEnemyTypesSpawnedCounts(currentLevel);
            int GetRandomWeightedIndex(int[] weights, System.Random anomalyRandom)
            {
                if (weights == null || weights.Length == 0)
                {
                    Debug.Log("Could not get random weighted index; array is empty or null.");
                    return -1;
                }
                int number = 0;
                for (int i_ = 0; i_ < weights.Length; i_++)
                {
                    if (weights[i_] >= 0)
                    {
                        number += weights[i_];
                    }
                }
                if (number <= 0)
                {
                    return anomalyRandom.Next(0, weights.Length);
                }
                float number2 = (float)anomalyRandom.NextDouble();
                float number3 = 0f;
                for (int i_2 = 0; i_2 < weights.Length; i_2++)
                {
                    if ((float)weights[i_2] > 0f)
                    {
                        number3 += (float)weights[i_2] / (float)number;
                        if (number3 >= number2)
                        {
                            return i_2;
                        }
                    }
                }
                return anomalyRandom.Next(0, weights.Length);
            }

            int i = 0;
            float num = 0f;
            bool flag = true;
            System.Random random = new System.Random(seed + 41);
            new System.Random(seed + 21);

            List<int> SpawnProbabilities = new List<int>();
            Dictionary<string, int> enemies = new Dictionary<string, int>();

            while (i < TimeOfDay.Instance.numberOfHours)
            {
                i += 2;
                float num2 = 100f * (float)i;
                float num3 = currentLevel.outsideEnemySpawnChanceThroughDay.Evaluate(num2 / timeScript.totalTime);
                float num4 = num3 + (float)Mathf.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 1.6f;
                int num5 = Mathf.Clamp(random.Next((int)(num4 - 3f), (int)(num3 + 3f)), minOutsideEnemiesToSpawn, 20);
                for (int j = 0; j < num5; j++)
                {
                    SpawnProbabilities.Clear();
                    int num6 = 0;
                    for (int k = 0; k < currentLevel.OutsideEnemies.Count; k++)
                    {
                        EnemyType enemyType = currentLevel.OutsideEnemies[k].enemyType;
                        if (flag)
                        {
                            enemyType.numberSpawned = 0;
                        }
                        if (enemyType.PowerLevel > currentMaxOutsidePower - num || enemyType.numberSpawned >= enemyType.MaxCount || enemyType.spawningDisabled)
                        {
                            SpawnProbabilities.Add(0);
                        }
                        else
                        {
                            int num7;
                            if (enemyType.useNumberSpawnedFalloff)
                            {
                                num7 = (int)((float)currentLevel.OutsideEnemies[k].rarity * (enemyType.probabilityCurve.Evaluate(num2 / timeScript.totalTime) * enemyType.numberSpawnedFalloff.Evaluate((float)enemyType.numberSpawned / 10f)));
                            }
                            else
                            {
                                num7 = (int)((float)currentLevel.OutsideEnemies[k].rarity * enemyType.probabilityCurve.Evaluate(num2 / timeScript.totalTime));
                            }
                            SpawnProbabilities.Add(num7);
                            num6 += num7;
                        }
                    }
                    flag = false;
                    if (num6 > 0)
                    {
                        int randomWeightedIndex = GetRandomWeightedIndex(SpawnProbabilities.ToArray(), random);
                        EnemyType enemyType2 = currentLevel.OutsideEnemies[randomWeightedIndex].enemyType;

                        string enemyname = enemyType2.enemyName;
                        if (!enemies.ContainsKey(enemyname))
                        {
                            enemies.Add(enemyname, 0);
                        }
                        enemies[enemyname]++;
                        num += enemyType2.PowerLevel;
                        enemyType2.numberSpawned++;
                    }
                }
            }
            return enemies;
        }

        [HarmonyPatch("PredictAllOutsideEnemies")]
        [HarmonyPostfix]
        static void Patch(ref SelectableLevel ___currentLevel, ref NavMeshHit ___navHit, ref Transform[] ___shipSpawnPathPoints, 
            ref int ___minOutsideEnemiesToSpawn, ref TimeOfDay ___timeScript)
        {
            SelectableLevel level = ___currentLevel;
            NavMeshHit navHit = ___navHit;
            Transform[] shipSpawnPathPoints = ___shipSpawnPathPoints;
            int minOutsideEnemiesToSpawn = ___minOutsideEnemiesToSpawn;
            int currentMaxOutsidePower = level.maxOutsideEnemyPowerCount;
            TimeOfDay timeScript = ___timeScript;


            /*  foreach (var thing in level.spawnableScrap)
              {
                  logger.LogInfo(thing.spawnableItem.itemName);
              }*/
            Dictionary<string, int> outsideobjects = predictAllOutsideEnemies(1, level, minOutsideEnemiesToSpawn, currentMaxOutsidePower, timeScript);
            foreach (KeyValuePair<string, int> kvp in outsideobjects)
            {
                logger.LogInfo(kvp.Key);
                logger.LogInfo(kvp.Value);
            }
            logger.LogInfo(TimeOfDay.Instance.daysUntilDeadline);

            logger.LogInfo(string.Format("SEEDS: {0} {1}", RoundManager.Instance.playersManager.randomMapSeed, StartOfRound.Instance.randomMapSeed));

            int lowest = 3000;
            void checkSeed(int seed)
            {

                //  logger.LogInfo(seed.ToString());
               // SpawnableItemWithRarity item = CheckSSD(seed, level);
                //Dictionary<string, int> outsideObjects = getOutsideObjects(seed, level, navHit, shipSpawnPathPoints);
                Dictionary<string, int> predictedEnemies = predictAllOutsideEnemies(seed, level, minOutsideEnemiesToSpawn, currentMaxOutsidePower, timeScript);
                int mechs;
                if (predictedEnemies.TryGetValue("RadMech", out mechs) && mechs <= lowest)
                {
                    logger.LogInfo(string.Format("LOW MECH AMOUNT ({0}): {1}", mechs, seed));
                    lowest = mechs;
                }
            }
           
            for (int seed = 0; seed < 1; seed++)
            {
                checkSeed(seed);
            }
        }
    }
}