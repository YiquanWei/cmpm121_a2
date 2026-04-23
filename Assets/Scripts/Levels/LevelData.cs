using System;
using System.Collections.Generic;

[Serializable]
public class EnemyData
{
    public string name;
    public int sprite;
    public int hp;
    public int speed;
    public int damage;
}

[Serializable]
public class SpawnData
{
    public string enemy;
    public string count;
    public string hp;
    public string delay;
    public string damage;
    public string speed;
    public string location;
    public List<int> sequence;
}

[Serializable]
public class LevelData
{
    public string name;
    public int? waves;
    public List<SpawnData> spawns;
}