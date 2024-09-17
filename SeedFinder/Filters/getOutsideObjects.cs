using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace SeedFinder.getObjects
{
    public class outsideObjects
    {
        public static SeedFinderBase Instance;
        public static Dictionary<string, int> getOutsideObjects(int seed, SelectableLevel currentLevel, NavMeshHit navHit, Transform[] shipSpawnPathPoints)
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
                                objectlist.Add(objname, 0);
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
    }
}
