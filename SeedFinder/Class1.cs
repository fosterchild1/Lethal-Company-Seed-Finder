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
        private const string modGUID = "seedfinder.ye";
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
                    if (-1 == j)
                    {
                        num += 12;
                    }
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

        [HarmonyPatch("SetToCurrentLevelWeather")]
        [HarmonyPostfix]
        static void Patch(ref SelectableLevel ___currentLevel, ref NavMeshHit ___navHit, ref Transform[] ___shipSpawnPathPoints)
        {
            SelectableLevel level = ___currentLevel;
            NavMeshHit navHit = ___navHit;
            Transform[] shipSpawnPathPoints = ___shipSpawnPathPoints;


            /*  foreach (var thing in level.spawnableScrap)
              {
                  logger.LogInfo(thing.spawnableItem.itemName);
              }*/
            Dictionary<string, int> outsideobjects = getOutsideObjects(StartOfRound.Instance.randomMapSeed, level, ___navHit, ___shipSpawnPathPoints);
            foreach (KeyValuePair<string, int> kvp in outsideobjects)
            {
                logger.LogInfo(kvp.Key);
                logger.LogInfo(kvp.Value);
            }

            logger.LogInfo(string.Format("SEEDS: {0} {1}", RoundManager.Instance.playersManager.randomMapSeed, StartOfRound.Instance.randomMapSeed));

            int highestPumpkinAmount = 0;
            void checkSeed(int seed)
            {

                //  logger.LogInfo(seed.ToString());
               // SpawnableItemWithRarity item = CheckSSD(seed, level);
                Dictionary<string, int> outsideObjects = getOutsideObjects(seed, level, navHit, shipSpawnPathPoints);
                int pumpkins;
                if (outsideObjects.TryGetValue("GiantPumpkin", out pumpkins) && pumpkins >= highestPumpkinAmount)
                {
                    logger.LogInfo(string.Format("HIGH PUMPKIN AMOUNT ({0}): {1}", pumpkins, seed));
                    highestPumpkinAmount = pumpkins;
                }
            }
           
            for (int seed = 0; seed < 1000000; seed++)
            {
                checkSeed(seed);
            }
        }
    }
}