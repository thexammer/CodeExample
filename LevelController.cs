using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
	/// <summary>
	/// A grouping of enemies. Contains the parameters that determine what enemies spawn and when in this level. 
	/// A level is ended when either all enemies from it are killed or the timeLimit is reached.
	/// </summary>
    [System.Serializable]
    private class Level {
		/// <summary>
		/// The list of enemy types to spawn.
		/// </summary>
        [SerializeField] public List<EnemySpawnParams> spawnParams;
		/// <summary>
		/// The amount of time in which enemies are spawning in this level. Note that the amount of time in between each enemy spawn is spawnTime / totalNumberToSpawn.
		/// </summary>
        [SerializeField] public float spawnTime;
		/// <summary>
		/// The total amount of time a player can spend in this level before the next is started.
		/// </summary>
        [SerializeField] public float timeLimit;
		/// <summary>
		/// The number of spawns in this level. Clamped from sum of minSpawns to sum of maxSpawns in spawnParams.
		/// Make -1 for a random number between the min and max.
		/// </summary>
		[SerializeField] public int totalNumberToSpawn;
    }

	/// <summary>
	/// The parameters controlling the amount of enemies of a certain type that will spawn in this level.
	/// </summary>
    [System.Serializable]
    private class EnemySpawnParams
    {
		/// <summary>
		/// The type of enemy to spawn.
		/// </summary>
        [SerializeField] public EnemyType type;
		/// <summary>
		/// The maximum amount of this type that can spawn in this level.
		/// </summary>
        [SerializeField] public int minSpawn;
		/// <summary>
		/// The minimum amount of this type that can spawn in this level.
		/// </summary>
        [SerializeField] public int maxSpawn;
    }

	/// <summary>
	/// The levels for this arena run.
	/// </summary>
    [SerializeField] private List<Level> levels;
	
    private EnemyController enemyController;
	// Keeps track of the time passed in this level.
    private float levelTimeElapsed = 0f;
	// Keeps track of the time between spawns in this level.
    private float spawnTimeElapsed = 0f;
    private int currentLevel = 0;
	// A list of the exact enemies to be spawned in the current level.
    private List<EnemyType> levelSpawns = new List<EnemyType>();
    private int currentLevelSpawnNum = 0;

    // Start is called before the first frame update
    void Start()
    {
        enemyController = FindObjectOfType<EnemyController>();
        CreateLevelSpawns();
    }

    // Update is called once per frame
    void Update()
    {
		// Don't want to keep spawning enemies when the player dies.
        if (!PlayerController.IsDead()) {
            levelTimeElapsed += Time.deltaTime;
            spawnTimeElapsed += Time.deltaTime;

            if (currentLevel >= levels.Count)
            {
				// TODO: endgame behavior (win screen?)

				// TEMP: Repeat last level infinitely
				currentLevel--;
            }

			// Should we spawn the next enemy?
            if (spawnTimeElapsed >= levels[currentLevel].spawnTime / currentLevelSpawnNum 
                    && levelTimeElapsed < levels[currentLevel].spawnTime + 1)
            {
                SpawnRandomFromCurrentLevel();
                spawnTimeElapsed = 0f;
            }

			// Should we move on to the next level?
            if (levelTimeElapsed >= levels[currentLevel].timeLimit 
                    || (enemyController.EnemyCount < 1 && levelTimeElapsed > levels[currentLevel].spawnTime))
            {
                currentLevel++;
                levelTimeElapsed = 0f;
                spawnTimeElapsed = 0f;
                CreateLevelSpawns();
            }
        }
    }

	/// <summary>
	/// 
	/// </summary>
    private void CreateLevelSpawns()
    {
        levelSpawns.Clear();
        int sumMinSpawn = 0;
        int sumMaxSpawn = 0;
		// Contains the amount of spawns left between the max and min for each enemy spawn param.
		Dictionary<EnemySpawnParams, int> extraspawns = new Dictionary<EnemySpawnParams, int>();

        // Populate with the minimum spawns in this level
        foreach (EnemySpawnParams enemy in levels[currentLevel].spawnParams)
        {
            for (int i = 0; i < enemy.minSpawn; i++)
            {
                levelSpawns.Add(enemy.type);
            }
            sumMinSpawn += enemy.minSpawn;
            sumMaxSpawn += enemy.maxSpawn;
			if (enemy.maxSpawn - enemy.minSpawn > 0)
			{
				extraspawns.Add(enemy, enemy.maxSpawn - enemy.minSpawn);
			}
        }

		// Checks if we want a random number of spawns in this level.
		if (levels[currentLevel].totalNumberToSpawn == -1)
		{
			currentLevelSpawnNum = Random.Range(sumMinSpawn, sumMaxSpawn);
		}
		else
		{
			currentLevelSpawnNum = Mathf.Clamp(levels[currentLevel].totalNumberToSpawn, sumMinSpawn, sumMaxSpawn);
		}

        // Add extra spawns
        for (int i = 0; i < currentLevelSpawnNum - sumMinSpawn; i++)
        {
			// Grab an enemy to add from the current list of extra enemy spawns available
			List<EnemySpawnParams> extraSpawnParams = new List<EnemySpawnParams>(extraspawns.Keys);
			EnemySpawnParams chosenSpawnParam = extraSpawnParams[Random.Range(0, extraSpawnParams.Count)];

			levelSpawns.Add(chosenSpawnParam.type);

			// Decrease the amount of extra enemies spawns available by one for the enemy type just added
			extraspawns[chosenSpawnParam] -= 1;
			if (extraspawns[chosenSpawnParam] <= 0)
			{
				extraSpawnParams.Remove(chosenSpawnParam);
			}
        }
    }

	/// <summary>
	/// Spawns in a random enemy from the levelSpawns list.
	/// </summary>
    private void SpawnRandomFromCurrentLevel() {
        EnemyType enemyType = levelSpawns[Random.Range(0, levelSpawns.Count)];
        enemyController.SpawnEnemy(enemyType);
		levelSpawns.Remove(enemyType);
    }
}
