
using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// VICTORY CONDITION
// ============================================================

public enum VictoryCondition
{
    DefeatAllEnemies,
    SurviveRounds,
    DefeatSpecificEnemy
}

// ============================================================
// DAY / NIGHT
// ============================================================

public enum DayNight
{
    Day,
    Night
}

// ============================================================
// ENCOUNTER DEFINITION
// ============================================================

[CreateAssetMenu(
    fileName = "EncounterDefinition",
    menuName = "Game/Encounter Definition"
)]
public class EncounterDefinition : ScriptableObject
{
    [Header("Encounter")]
    public string encounterName = "New Encounter";

    [TextArea(2, 5)]
    public string description;

    [Header("Map Node")]
    public Sprite mapNodeIcon;

    [Header("Biome")]
    public string biomeGameObjectName;

    [Header("Day / Night")]
    public DayNight dayNight = DayNight.Day;

    [Header("Music")]
    public AudioClip music;

    [Header("Grid")]
    [Min(1)]
    public int width = 11;

    [Min(1)]
    public int height = 11;

    public GridShapeType shape = GridShapeType.Box;

    [Header("Donut Settings")]
    [Min(0)]
    public int minRadius = 2;

    [Min(1)]
    public int maxRadius = 5;

    // ========================================================
    // TIDE SETTINGS
    // ========================================================

    [Header("Tide Settings")]
    [Tooltip("Enable tide movement and grid shrinking for this encounter.")]
    public bool useTide = false;

    // ========================================================
    // MISSION
    // ========================================================

    [Header("Mission")]
    public VictoryCondition victoryCondition =
        VictoryCondition.DefeatAllEnemies;

    [Header("Specific Target")]
    public string targetEnemyId;

    [Header("Survival")]
    [Min(1)]
    public int roundsToSurvive = 5;

    [Header("Obstacle Spawn")]
    public List<ObstacleSpawnData> obstacles =
        new List<ObstacleSpawnData>();

    [Header("Bush Spawn")]
    public List<ObstacleSpawnData> bushes =
        new List<ObstacleSpawnData>();

    [Header("Enemy Spawn")]
    public List<EnemySpawnData> enemies =
        new List<EnemySpawnData>();

    [Header("Boss Spawn")]
    public List<EnemySpawnData> bosses =
        new List<EnemySpawnData>();
}

// ============================================================
// OBSTACLE SPAWN DATA
// ============================================================

[Serializable]
public class ObstacleSpawnData
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Amount")]
    [Min(1)]
    public int amount = 1;
}

// ============================================================
// ENEMY SPAWN DATA
// ============================================================

[Serializable]
public class EnemySpawnData
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Character")]
    public CharacterSO character;

    [Header("Encounter ID")]
    public string enemyId;
}
