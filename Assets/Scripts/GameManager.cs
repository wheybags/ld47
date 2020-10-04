using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public enum TileType
{
    Block,
    Floor,
    Die
}

public class GameManager : MonoBehaviour {
    #region References
    public Camera mainCamera;

    public Tilemap mainMap;
    public Tilemap shadowMap;

    public TileBase wallTopNormal;
    public TileBase wallTopBottom;
    public TileBase wallSide;
    public TileBase activeStartTile;
    public TileBase inactiveStartTile;
    public TileBase fruitTile;
    public TileBase floorTile;
    public TileBase pitTile;
    public TileBase voidTile;

    public TileBase shadowTL;
    public TileBase shadowL;
    public TileBase shadowT;
    public TileBase shadowTriangle;
    public TileBase shadowOuterCorner;
    
    public List<RobotBehavior> robots;
    public List<FruitSpawner> fruits;
    public List<GhostSpawn> spawns;
    public GameObject robotPrefab;

    public Text energyGui;
    public Text winText;

    public int maxMoves;
    #endregion

    #region Members
    private int _activeRobot;
    private Vector2Int _startTileIndex;
    private int _waitSteps = 0;
    private int _maxRobots = 0;
    public int _tick;
    private int _nextSpawnTick = 0;
    private float _nextAutoMove;
    private float autoDelay = 0.5f;

    bool complete = false;
    #endregion

    void Start()
    {
        winText.enabled = false;

        SetupShadowMap();

        robots = new List<RobotBehavior>();

        var robotGOs = GameObject.FindGameObjectsWithTag("Player");
        foreach (var robot in robotGOs) {
            var script = robot.GetComponent<RobotBehavior>();
            if (script) {
                robots.Add(script);
            }
        }

        mainMap.CompressBounds();
        var bounds = mainMap.cellBounds;
        // for (var x = bounds.min.x; x < bounds.max.x; x++) {
        //     for (var y = bounds.min.y; y < bounds.max.y; y++) {
        //         var tile = mainMap.GetTile(new Vector3Int(x, y, 0));
        //         if (tile == startTile) {
        //             _startTileIndex = new Vector2Int(x,y);
        //             break;
        //         }
        //     }
        // }

        for (var i = 0; i < fruits.Count; i++) {
            fruits[i].fruitType = i;
        }
        
        for (var i = 0; i < spawns.Count; i++) {
            spawns[i].fruitType = i;
        }
        
        // GameObject[] fruitGOs = GameObject.FindGameObjectsWithTag("Resource");
        //
        // foreach (var fruit in fruitGOs) {
        //     Debug.Log("added fruit");
        //     var spawner = fruit.GetComponent<FruitSpawner>();
        //     fruits.Add(spawner);
        // }
        
        _nextSpawnTick = 0;
        _activeRobot = -1;
        _maxRobots = 1;

        //Debug.DrawLine(new Vector3(0, 0, 0), bounds.min + new Vector3(1, 1), Color.green, 20, false);
        //Debug.DrawLine(new Vector3(0, 0, 0), bounds.max - new Vector3(1, 1), Color.green, 20, false);

        Vector2 centre = new Vector2(bounds.min.x + (bounds.max.x - bounds.min.x) / 2.0f, bounds.min.y + (bounds.max.y - bounds.min.y) / 2.0f);
        mainCamera.transform.position = new Vector3(centre.x, centre.y, mainCamera.transform.position.z);
    }
    
    void Update() {
        energyGui.text = "Level " + GetCurrentLevelNumber() + "\nEnergy: " + (maxMoves - _tick) + "/" + maxMoves;

        if (_activeRobot < 0 && Time.time > _nextAutoMove) {
            _nextAutoMove = Time.time + autoDelay;

            bool allFinished = robots.Count > 0;
            foreach (RobotBehavior robot in robots)
            {
                if (!robot.isFinished)
                {
                    allFinished = false;
                    break;
                }
            }

            if (allFinished)
            {
                autoDelay = Math.Max(autoDelay - 0.1f, 0.2f);
                winText.enabled = true;
                complete = true;
            }

            int newTick = _tick + 1;
            if (newTick > maxMoves || allFinished)
                newTick = 0;

            Resimulate(newTick, true );
        }

        if (robots.Count < _maxRobots && isCellBlockedByRobot(_startTileIndex) == false && robots.Count < fruits.Count) {
            SpawnRobot(robots.Count);
            SetControlledRobot(robots.Count -1);
            SetupFruits();
        }
    }

    public void SetupFruits() {
        foreach (var fruit in fruits) {
            if (fruit) {
                fruit.Disappear();
            }
        }

        for (var r = 0; r < robots.Count; r++) {
            if (fruits[r]) {
                fruits[r].Appear();
            }
        }

        if (_activeRobot > -1) {
            fruits[_activeRobot].SetImportant();
        }
    }
    
    public bool isCellBlockedByRobot(Vector2Int cellIndex) {
        foreach (var robot in robots) {
            if (cellIndex == robot.cellIndex && robot.gameObject.activeInHierarchy) {
                return true;
            }
        }

        return false;
    }
    
    private void SetupShadowMap()
    {
        Func<int, int, TileBase> get = (int x, int y) =>
        {
            if (x > mainMap.cellBounds.max.x || x < mainMap.cellBounds.min.x || y > mainMap.cellBounds.max.y || y < mainMap.cellBounds.min.y)
                return null;

            return mainMap.GetTile(new Vector3Int(x, y, 0));
        };

        for (int y = mainMap.cellBounds.max.y - 1; y >= mainMap.cellBounds.min.y; y--)
        {
            for (int x = mainMap.cellBounds.min.x; x < mainMap.cellBounds.max.x; x++)
            {
                if (get(x, y) == wallSide && (get(x - 1, y) == wallTopBottom || get(x - 1, y) == wallTopNormal))
                    shadowMap.SetTile(new Vector3Int(x, y, 0), shadowTriangle);
                else if (GetTileType(get(x - 1, y)) == TileType.Block && GetTileType(get(x, y + 1)) == TileType.Block && GetTileType(get(x, y)) == TileType.Floor)
                    shadowMap.SetTile(new Vector3Int(x, y, 0), shadowTL);
                else if (get(x, y + 1) == wallSide && GetTileType(get(x, y)) == TileType.Floor)
                    shadowMap.SetTile(new Vector3Int(x, y, 0), shadowT);
                else if (GetTileType(get(x - 1, y)) == TileType.Block && GetTileType(get(x, y)) == TileType.Floor)
                    shadowMap.SetTile(new Vector3Int(x, y, 0), shadowL);

                if (y != mainMap.cellBounds.max.y - 1 &&
                    (shadowMap.GetTile(new Vector3Int(x - 1, y, 0)) == shadowT || (shadowMap.GetTile(new Vector3Int(x - 1, y, 0)) == shadowTL)) &&
                    shadowMap.GetTile(new Vector3Int(x, y + 1, 0)) == shadowL)
                {
                    shadowMap.SetTile(new Vector3Int(x, y, 0), shadowOuterCorner);
                }
            }
        }
    }

    private void SpawnRobot(int fruitType) {
        var newRobot = GameObject.Instantiate(robotPrefab,
            mainMap.CellToWorld((Vector3Int) spawns[fruitType].cellIndex), Quaternion.identity);
        var behavior = newRobot.GetComponent<RobotBehavior>();
        behavior.SetRequiredFruitType(fruitType);
        robots.Add(behavior);
    }

    public void SetControlledRobot(int robotIndex) {
        foreach (var robot in robots) {
            robot.SetControlledState(false);
        }
        if (robotIndex > -1 && robots[robotIndex]) {
            robots[robotIndex].SetControlledState(true);
        }

        foreach (var spawn in spawns) {
            spawn.DeactivateSpawner();
        }
        
        _activeRobot = robotIndex;
        if (_activeRobot > -1) {
            spawns[_activeRobot].ActivateSpawner();
        }
    }

    public void SelectNextRobot() {
        Debug.Log("change robot " + _activeRobot.ToString());
        _activeRobot++;
        if (_activeRobot == robots.Count) {
            _activeRobot -= robots.Count;
        }
        SetControlledRobot(_activeRobot);
    }

    private void ResetSimulation() {
        foreach (var robot in robots) {
            robot.ResetSimulation();
        }
        SetupFruits();
    }

    public void Resimulate(int steps, bool animate) {
        Vector2Int[] lastPositions = new Vector2Int[robots.Count];
        for (int i = 0; i < robots.Count; i++)
        {
            lastPositions[i] = robots[i].cellIndex;
        }

        ResetSimulation();
        _tick = steps;
        
        foreach (var fruit in fruits) {
            fruit.RespawnFruit();
        }

        bool stop = false;
        for (var s = 0; s < steps; s++) {
            foreach (var robot in robots) {

                bool thisBotStop = robot.StepSimulation(s + 1);

                if (thisBotStop) {
                    stop = true;
                }
            }

            if (stop) {
                break;
            }
        }


        for (int i = 0; i < robots.Count; i++)
        {
            if (animate || robots[i].isFinished)
            {
                robots[i].previousCellIndex = lastPositions[i];
                robots[i].lastMoveTime = Time.time;
            }
        }
    }

    public TileBase GetCellAtIndex(Vector2Int cellIndex)
    {
        return mainMap.GetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0));
    }

    public TileType GetCellTypeAtIndex(Vector2Int cellIndex)
    {
        TileBase tile = GetCellAtIndex(cellIndex);
        return GetTileType(tile);
    }

    public TileType GetTileType(TileBase tile)
    {
        if (tile == null)
            return TileType.Floor;
        if (tile == wallTopNormal)
            return TileType.Block;
        if (tile == wallTopBottom)
            return TileType.Block;
        if (tile == wallSide)
            return TileType.Block;
        if (tile == inactiveStartTile)
            return TileType.Floor;
        if (tile == activeStartTile)
            return TileType.Floor;
        if (tile == fruitTile)
            return TileType.Floor;
        if (tile == floorTile)
            return TileType.Floor;
        if (tile == pitTile)
            return TileType.Die;
        if (tile == voidTile)
            return TileType.Floor;

        throw new System.Exception("unregistered tile encountered");
    }

    public Vector2Int GetCellIndexAtPosition(Vector3 position) {
        var cellIndex = mainMap.WorldToCell(position);
        return new Vector2Int(cellIndex.x,cellIndex.y);
    }
    
    public Vector3 GetTileCenterPosition(Vector2Int cellIndex) {
        return mainMap.GetCellCenterWorld(new Vector3Int(cellIndex.x,cellIndex.y,0));
    }
    
    public void RelinquishControl(RobotBehavior robot) {
        if (_activeRobot > -1 && robot == robots[_activeRobot]) {
            SetControlledRobot(-1);
            _maxRobots++;
            _tick = -1;
            _nextAutoMove = Time.time + 0.1f;
        }
    }
    
    public void OnMoveUp() {
        if (_activeRobot > -1) {
            robots[_activeRobot].OnMoveUp();
        }
    }

    public void OnMoveDown() {
        if (_activeRobot > -1) {
            robots[_activeRobot].OnMoveDown();
        }
    }

    public void OnMoveLeft() {
        if (_activeRobot > -1) {
            robots[_activeRobot].OnMoveLeft();
        }
    }

    public void OnMoveRight() {
        if (_activeRobot > -1) {
            robots[_activeRobot].OnMoveRight();
        }
    }

    void OnUndo() {
        if (_activeRobot > -1) {
            RobotBehavior robot = robots[_activeRobot];

            Vector2Int lastPosition = robot.cellIndex;

            int step = robot.OnUndo();
            Resimulate(step, true);
        }
    }

    void OnRestartLevel()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    int GetCurrentLevelNumber()
    {
        Scene scene = SceneManager.GetActiveScene();
        return int.Parse(scene.name.Replace("Level_", ""));
    }

    void OnNextLevel()
    {
        if (complete)
        {
            int number = GetCurrentLevelNumber() + 1;
            SceneManager.LoadScene("Level_" + number.ToString("D2"));
        }
    }

    public void SetIndexToActiveSpawner(Vector2Int cellIndex) {
         mainMap.SetTile((Vector3Int) cellIndex, activeStartTile);
     }
    
    public void SetIndexToInactiveSpawner(Vector2Int cellIndex) {
        mainMap.SetTile((Vector3Int) cellIndex, inactiveStartTile);
    }
    
    public void SetIndexToFruitSpawner(Vector2Int cellIndex) {
        mainMap.SetTile((Vector3Int) cellIndex, fruitTile);
    }
}
