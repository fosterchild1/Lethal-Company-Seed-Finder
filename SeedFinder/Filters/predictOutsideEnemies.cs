using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SeedFinder.predictOutsideEnemies
{
    public class predicter
    {
        public static SeedFinderBase Instance;
        private static void ResetEnemyTypesSpawnedCounts(SelectableLevel currentLevel)
        {
            EnemyAI[] array = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < currentLevel.Enemies.Count; i++)
            {
                currentLevel.Enemies[i].enemyType.numberSpawned = 0;
                for (int j = 0; j < array.Length; j++)
                {
                    if (array[j].enemyType == currentLevel.Enemies[i].enemyType)
                    {
                        currentLevel.Enemies[i].enemyType.numberSpawned++;
                    }
                }
            }
            for (int k = 0; k < currentLevel.OutsideEnemies.Count; k++)
            {
                currentLevel.OutsideEnemies[k].enemyType.numberSpawned = 0;
                for (int l = 0; l < array.Length; l++)
                {
                    if (array[l].enemyType == currentLevel.OutsideEnemies[k].enemyType)
                    {
                        currentLevel.OutsideEnemies[k].enemyType.numberSpawned++;
                    }
                }
            }
        }

        public static int GetRandomWeightedIndex(int[] weights, System.Random randomSeed = null)
        {
            if (weights == null || weights.Length == 0)
            {
                return -1;
            }
            int num = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] >= 0)
                {
                    num += weights[i];
                }
            }
            if (num <= 0)
            {
                return randomSeed.Next(0, weights.Length);
            }
            float num2 = (float)randomSeed.NextDouble();
            float num3 = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if ((float)weights[i] > 0f)
                {
                    num3 += (float)weights[i] / (float)num;
                    if (num3 >= num2)
                    {
                        return i;
                    }
                }
            }
            return randomSeed.Next(0, weights.Length);
        }


        public static Dictionary<string, int> predictAllOutsideEnemies(int seed, SelectableLevel currentLevel, int minOutsideEnemiesToSpawn, float currentMaxOutsidePower, TimeOfDay timeScript)
        {
           
            int i = 0;
            float num = 0f;
            bool flag = true;
            System.Random random = new System.Random(seed + 41);
            List<int> SpawnProbabilities = new List<int>();
            Dictionary<string, int> enemies = new Dictionary<string, int>();

            while (i < TimeOfDay.Instance.numberOfHours)
            {
                i += 2;
                float num2 = timeScript.lengthOfHours * (float)i;
                float num3 = currentLevel.outsideEnemySpawnChanceThroughDay.Evaluate(num2 / timeScript.totalTime);
                if (StartOfRound.Instance.isChallengeFile)
                {
                    num3 += 1f;
                }
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
                            if (-1 == k)
                            {
                                num7 = 100;
                            }
                            else if (enemyType.useNumberSpawnedFalloff)
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
                    if (num6 <= 0)
                    {
                        if (num >= currentMaxOutsidePower)
                        {
                        }
                    }
                    else
                    {
                        int randomWeightedIndex = GetRandomWeightedIndex(SpawnProbabilities.ToArray(), random);
                        EnemyType enemyType2 = currentLevel.OutsideEnemies[randomWeightedIndex].enemyType;
                        num += enemyType2.PowerLevel;
                        enemyType2.numberSpawned++;

                        string enemyname = enemyType2.enemyName;
                        if (!enemies.ContainsKey(enemyname))
                        {
                            enemies.Add(enemyname, 0);
                        }
                        enemies[enemyname]++;
                    }
                }
            }
            return enemies;
        }

    }
}
