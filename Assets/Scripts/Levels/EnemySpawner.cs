using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using RPNEvaluator;

public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    private List<EnemyData> enemies;
    private List<LevelData> levels;

    private LevelData currentLevel;
    private int currentWave;

    void CreateLevelButtons()
    {
        float startY = 60f;
        float spacing = 70f;

        for (int i = 0; i < levels.Count; i++)
        {
            GameObject selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition = new Vector3(0, startY - i * spacing);

            MenuSelectorController controller = selector.GetComponent<MenuSelectorController>();
            controller.spawner = this;
            controller.SetLevel(levels[i].name);
        }
    }

    void LoadData()
    {
        Debug.Log("LoadData is running");
        TextAsset enemyFile = Resources.Load<TextAsset>("enemies");
        TextAsset levelFile = Resources.Load<TextAsset>("levels");

        if (enemyFile == null)
        {
            Debug.LogError("Could not load enemies.json from Resources.");
            enemies = new List<EnemyData>();
        }
        else
        {
            enemies = JsonConvert.DeserializeObject<List<EnemyData>>(enemyFile.text);
        }

        if (levelFile == null)
        {
            Debug.LogError("Could not load levels.json from Resources.");
            levels = new List<LevelData>();
        }
        else
        {
            levels = JsonConvert.DeserializeObject<List<LevelData>>(levelFile.text);
        }

        Debug.Log($"Loaded {enemies.Count} enemies.");
        Debug.Log($"Loaded {levels.Count} levels.");
    }

    int EvaluateExpression(string expression, int baseValue, int waveValue)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return baseValue;
        }

        Dictionary<string, int> variables = new Dictionary<string, int>();
        variables["base"] = baseValue;
        variables["wave"] = waveValue;

        return RPNEvaluator.RPNEvaluator.Evaluate(expression, variables);
    }

    EnemyData GetEnemyDataByName(string enemyName)
    {
        return enemies.Find(e => e.name == enemyName);
    }

    IEnumerator SpawnEnemy(SpawnData spawn)
    {
        EnemyData enemyData = GetEnemyDataByName(spawn.enemy);

        if (enemyData == null)
        {
            Debug.LogError($"Could not find enemy named {spawn.enemy}");
            yield break;
        }

        SpawnPoint spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        Vector2 offset = Random.insideUnitCircle * 1.8f;
        Vector3 initialPosition = spawnPoint.transform.position + new Vector3(offset.x, offset.y, 0);

        GameObject newEnemy = Instantiate(enemy, initialPosition, Quaternion.identity);

        int hpValue = EvaluateExpression(spawn.hp ?? "base", enemyData.hp, currentWave);
        int speedValue = EvaluateExpression(spawn.speed ?? "base", enemyData.speed, currentWave);

        newEnemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(enemyData.sprite);

        EnemyController en = newEnemy.GetComponent<EnemyController>();
        en.hp = new Hittable(hpValue, Hittable.Team.MONSTERS, newEnemy);
        en.speed = speedValue;

        GameManager.Instance.AddEnemy(newEnemy);

        yield return null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadData();

        CreateLevelButtons();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartLevel(string levelname)
    {
        currentLevel = levels.Find(level => level.name == levelname);
        currentWave = 1;

        if (currentLevel == null)
        {
            Debug.LogError($"Could not find level named {levelname}");
            return;
        }

        level_selector.gameObject.SetActive(false);
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();
        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        currentWave++;
        StartCoroutine(SpawnWave());
    }


    IEnumerator SpawnWave()
    {
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3;

        for (int i = 3; i > 0; i--)
        {
            yield return new WaitForSeconds(1);
            GameManager.Instance.countdown--;
        }

        GameManager.Instance.state = GameManager.GameState.INWAVE;

        foreach (SpawnData spawn in currentLevel.spawns)
        {
            EnemyData enemyData = GetEnemyDataByName(spawn.enemy);

            if (enemyData == null)
            {
                Debug.LogError($"Could not find enemy named {spawn.enemy}");
                continue;
            }

            int countValue = EvaluateExpression(spawn.count, 0, currentWave);
            int delayValue = EvaluateExpression(spawn.delay ?? "2", 0, currentWave);

            for (int i = 0; i < countValue; i++)
            {
                yield return SpawnEnemy(spawn);
                yield return new WaitForSeconds(delayValue);
            }
        }

        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
    }

}
