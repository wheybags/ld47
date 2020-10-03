using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour {
    #region References
    public Tilemap mainMap;
    public List<TileBase> tileIndexes;
    public List<RobotBehavior> robots;
    public GameObject robotPrefab;
    #endregion

    #region Members
    private int _activeRobot;
    private Vector2Int _startTileIndex;
    private int _waitSteps = 0;
    #endregion

    void Start() {
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
        for (var x = bounds.min.x; x < bounds.max.x; x++) {
            for (var y = bounds.min.y; y < bounds.max.y; y++) {
                var tile = mainMap.GetTile(new Vector3Int(x, y, 0));
                if (tile == tileIndexes[3]) {
                    _startTileIndex = new Vector2Int(x,y);
                    break;
                }
            }
        }
        SpawnRobot(0);
        _activeRobot = 0;
        SetControlledRobot(_activeRobot);
    }

    private void SpawnRobot(int fruitType) {
        var newRobot = GameObject.Instantiate(robotPrefab,
            mainMap.CellToWorld((Vector3Int) _startTileIndex), Quaternion.identity);
        var behavior = newRobot.GetComponent<RobotBehavior>(); 
        behavior.SetRequiredFruitType(fruitType);
        robots.Add(behavior);
    }

    public void SetControlledRobot(int robotIndex) {
        if (robots[robotIndex]) {
            foreach (var robot in robots) {
                robot.SetControlledState(false);
            }
            robots[robotIndex].SetControlledState(true);
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
    
    public void Resimulate(int steps) {
        foreach (var robot in robots) {
            robot.ResetSimulation();
        }
        
        GameObject[] fruits = GameObject.FindGameObjectsWithTag("Resource");
        foreach (var fruit in fruits) {
            fruit.GetComponent<FruitSpawner>().RespawnFruit();
        }

        bool stop = false;
        for (var s = 0; s < steps; s++) {
            foreach (var robot in robots) {
                bool thisBotStop = robot.StepSimulation();
                if (thisBotStop) {
                    stop = true;
                }
            }

            if (stop) {
                break;
            }
        }
    }
    
    public int GetCellTypeAtIndex(Vector2Int cellIndex) {
        TileBase tile = mainMap.GetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0));

        for (var i = 0; i < tileIndexes.Count; i++) {
            if (tileIndexes[i] == tile) {
                return i;
            }
        }
        
        return -1;
    }

    public Vector2Int GetCellIndexAtPosition(Vector3 position) {
        var cellIndex = mainMap.WorldToCell(position);
        return new Vector2Int(cellIndex.x,cellIndex.y);
    }
    
    public Vector3 GetTileCenterPosition(Vector2Int cellIndex) {
        return mainMap.GetCellCenterWorld(new Vector3Int(cellIndex.x,cellIndex.y,0));
    }
    
    public void OnMoveUp() {
        robots[_activeRobot].OnMoveUp();
    }

    public void OnMoveDown() {
        robots[_activeRobot].OnMoveDown();
    }

    public void OnMoveLeft() {
        robots[_activeRobot].OnMoveLeft();
    }

    public void OnMoveRight() {
        robots[_activeRobot].OnMoveRight();
    }
    
    void OnChangeRobot() {
        robots[_activeRobot].SetControlledState(false);
        SpawnRobot(_activeRobot+1);
        SelectNextRobot();
    }

    void OnUndo() {
        int step = robots[_activeRobot].OnUndo();
        Resimulate(step);
    }
    
    public void OnWait() {
        _waitSteps++;
        int step = robots[_activeRobot].OnUndo();
        robots[_activeRobot].isControlled = false;
        Resimulate(step+_waitSteps);
        robots[_activeRobot].isControlled = true;
    }

    public void SetIndexToSpawner(Vector2Int cellIndex) {
        mainMap.SetTile((Vector3Int) cellIndex, tileIndexes[4]);
    }
}
