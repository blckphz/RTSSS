using System;
using System.Collections.Generic;
using UnityEngine;


// ============================================================
// ENCOUNTER DEFINITION
// ============================================================

[CreateAssetMenu(
    fileName = "EncounterDefinition",
    menuName = "Game/Encounter Definition"
)]
public class EncounterDefinition : ScriptableObject
{
    // ============================================================
    // ENCOUNTER
    // ============================================================

    [Header("Encounter")]
    public string encounterName = "New Encounter";

    [TextArea(2, 5)]
    public string description;


    // ============================================================
    // MAP NODE ICON
    // ============================================================

    [Header("Map Node")]
    [Tooltip(
        "The Sprite used for this encounter on the level map. " +
        "Leave empty to use the default icon from LevelMapManager."
    )]
    public Sprite mapNodeIcon;


    // ============================================================
    // GRID
    // ============================================================

    [Header("Grid")]
    [Min(1)]
    public int width = 11;

    [Min(1)]
    public int height = 11;

    public GridShapeType shape =
        GridShapeType.Box;


    // ============================================================
    // DONUT
    // ============================================================

    [Header("Donut Settings")]
    [Min(0)]
    public int minRadius = 2;

    [Min(1)]
    public int maxRadius = 5;


    // ============================================================
    // MISSION
    // ============================================================

    [Header("Mission")]
    public EncounterManager.VictoryCondition victoryCondition =
        EncounterManager.VictoryCondition.DefeatAllEnemies;


    [Header("Specific Target")]
    public string targetEnemyId;


    [Header("Survival")]
    [Min(1)]
    public int roundsToSurvive = 5;


    // ============================================================
    // ENEMIES
    // ============================================================

    [Header("Enemy Spawn")]
    public List<EnemySpawnData> enemies =
        new List<EnemySpawnData>();
}


// ============================================================
// MISSION TYPE
// ============================================================

public enum MissionType
{
    DefeatAllEnemies,

    SurviveRounds,

    KillTarget,

    DefeatAllEnemiesOrSurviveRounds,

    KillTargetOrSurviveRounds
}


// ============================================================
// ENEMY SPAWN DATA
// ============================================================

[Serializable]
public class EnemySpawnData
{
    public GameObject prefab;

    [Tooltip(
        "Unique ID for this specific encounter unit. " +
        "Example: boss_01"
    )]
    public string enemyId;
}