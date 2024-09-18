using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            ResetEnemyTypesSpawnedCounts(currentLevel);
            float currentOutsideEnemyPower = 0f;
            bool flag = true;
            System.Random OutsideEnemyRandom = new System.Random(seed + 41);

            List<int> SpawnProbabilities = new List<int>();
            Dictionary<string, int> enemies = new Dictionary<string, int>();

            // 2>4>6>8>10>12>14>16>18 end
            for (int currentHour = 2; currentHour < 19; currentHour += 2)
            {
                if (currentOutsideEnemyPower > currentMaxOutsidePower)
                {
                    break;
                }

                float num = 100f * (float)currentHour;
                float num2 = (float)((int)(currentLevel.outsideEnemySpawnChanceThroughDay.Evaluate(num / timeScript.totalTime) * 100f)) / 100f;
                float num3 = num2 + (float)Mathf.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 1.6f;
                int num4 = Mathf.Clamp(OutsideEnemyRandom.Next((int)(num3 - 3f), (int)(num2 + 3f)), minOutsideEnemiesToSpawn, 20);
                for (int j = 0; j < num4; j++)
                {
                    SpawnProbabilities.Clear();
                    int num5 = 0;
                    for (int k = 0; k < currentLevel.OutsideEnemies.Count; k++)
                    {
                        EnemyType enemyType = currentLevel.OutsideEnemies[k].enemyType;
                        if (flag)
                        {
                            enemyType.numberSpawned = 0;
                        }
                        if (enemyType.PowerLevel > currentMaxOutsidePower - currentOutsideEnemyPower || enemyType.numberSpawned >= enemyType.MaxCount || enemyType.spawningDisabled)
                        {
                            SpawnProbabilities.Add(0);
                        }
                        else
                        {
                            int num6;
                            if (enemyType.useNumberSpawnedFalloff)
                            {
                                num6 = (int)((float)currentLevel.OutsideEnemies[k].rarity * (enemyType.probabilityCurve.Evaluate(num / timeScript.totalTime) * enemyType.numberSpawnedFalloff.Evaluate((float)enemyType.numberSpawned / 10f)));
                            }
                            else
                            {
                                num6 = (int)((float)currentLevel.OutsideEnemies[k].rarity * enemyType.probabilityCurve.Evaluate(num / timeScript.totalTime));
                            }
                            SpawnProbabilities.Add(num6);
                            num5 += num6;
                        }
                    }
                    flag = false;
                    if (num5 <= 0)
                    {
                        break;
                    }
                    int randomWeightedIndex = GetRandomWeightedIndex(SpawnProbabilities.ToArray(), OutsideEnemyRandom);
                    EnemyType enemyType2 = currentLevel.OutsideEnemies[randomWeightedIndex].enemyType;

                    float spawnAmount = (float)Mathf.Max(enemyType2.spawnInGroupsOf, 1);
                    bool result = false;
                    for (int k = 0; (float)k < spawnAmount; k++)
                    {
                        if (enemyType2.PowerLevel > currentMaxOutsidePower - currentOutsideEnemyPower)
                        {
                            break;
                        }

                        string enemyname = enemyType2.enemyName;
                        if (!enemies.ContainsKey(enemyname))
                        {
                            enemies.Add(enemyname, 0);
                        }
                        enemies[enemyname]++;

                        currentOutsideEnemyPower += enemyType2.PowerLevel;
                        enemyType2.numberSpawned++;
                        result = true;
                    }
                    if (!result)
                    {
                        break;
                    }
                }
            }
            return enemies;
        }

    }
}
