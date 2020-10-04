﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

public enum TileType
{
    Block,
    Floor,
    Die
}

public class GameManager : MonoBehaviour {
    #region References
    public Tilemap mainMap;
    public Tilemap shadowMap;

    public TileBase wallTopNormal;
    public TileBase wallTopBottom;
    public TileBase wallSide;
    public TileBase startTile;
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
    public GameObject robotPrefab;
    #endregion

    #region Members
    private int _activeRobot;
    private Vector2Int _startTileIndex;
    private int _waitSteps = 0;
    private int _tick;
    private int _nextSpawnTick = 0;
    private float _nextAutoMove;
    #endregion

    void Start()
    {
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
        for (var x = bounds.min.x; x < bounds.max.x; x++) {
            for (var y = bounds.min.y; y < bounds.max.y; y++) {
                var tile = mainMap.GetTile(new Vector3Int(x, y, 0));
                if (tile == startTile) {
                    _startTileIndex = new Vector2Int(x,y);
                    break;
                }
            }
        }
        _nextSpawnTick = 0;
        _activeRobot = -1;

    }
    
    void Update() {
        if (_activeRobot < 0 && Time.time > _nextAutoMove) {
            Debug.Log("automove");
            _nextAutoMove += 0.5f;
            Resimulate(_tick + 1 );
        }

        if (_tick >= _nextSpawnTick && isCellBlockedByRobot(_startTileIndex) == false) {
            SpawnRobot(0);
            SetControlledRobot(0);
            _nextSpawnTick += 10000;
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
                else if (get(x, y + 1) == wallSide)
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
            mainMap.CellToWorld((Vector3Int) _startTileIndex), Quaternion.identity);
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

        _activeRobot = robotIndex;
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
        _tick = steps;
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
        if (tile == startTile)
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
            Resimulate(step);

            robot.previousCellIndex = lastPosition;
            robot.lastMoveTime = Time.time;
        }
    }

    public void SetIndexToSpawner(Vector2Int cellIndex) {
        mainMap.SetTile((Vector3Int) cellIndex, fruitTile);
    }
}
