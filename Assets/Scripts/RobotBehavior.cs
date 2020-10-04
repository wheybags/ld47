﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RobotBehavior : MonoBehaviour {

    #region References
    private GameManager _gameManager;
    private GameObject _carriedItemGO;
    private Animator _animator;
    private SpriteRenderer _renderer;
    #endregion

    #region Members
    public bool isControlled = false;
    public bool isBroken;
    private Vector2Int _cellIndex;
    private Vector2Int _previousCellIndex;
    private float _lastMoveTime = 0;

    private Vector2Int _spawnIndex;
    private int _requiredFruitType;
    private bool _isCarrying;
    private FruitSpawner _harvestedFrom;
    [SerializeField] private List<Vector2Int> _lastCommands;
    private int _commandIndex;

    enum LRDirection
    {
        Left,
        Right
    }
    private LRDirection lastMoveLeftRight = LRDirection.Right;

    #endregion

    Color[] tints =
    {
        new Color(1.5f, 0.3f, 0.3f, 1),
        new Color(0.0f, 1.5f, 0.5f, 1),
        new Color(0.0f, 0.5f, 2.0f, 1),
        new Color(2.0f, 2.0f, 1.0f, 1),
        new Color(1.7f, 1.0f, 1.0f, 1),
    };
    
    void Start() {
        _animator = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
        _carriedItemGO = transform.Find("CarriedItem").gameObject;
        _gameManager = FindObjectOfType<GameManager>();
        _renderer.material.color = tints[Random.Range(0, tints.Length)];
        _lastCommands = new List<Vector2Int>();
        _cellIndex = _gameManager.GetCellIndexAtPosition(transform.position);
        _spawnIndex = _cellIndex;
        ResetSimulation();
    }

    private void Update()
    {
        if (_lastMoveTime != -1)
        {
            const float movementTimeInSeconds = 0.1f;

            float timeSinceMove = Time.time - _lastMoveTime;
            float alpha = timeSinceMove * 1 / movementTimeInSeconds;

            transform.position = Vector3.Lerp(_gameManager.GetTileCenterPosition(_previousCellIndex), _gameManager.GetTileCenterPosition(_cellIndex), alpha);

            if (alpha >= 1)
            {
                ApplyTileEffects();
                _lastMoveTime = -1;
            }
        }

        if (isBroken && _animator.speed > 0)
        {
            _animator.speed = 0;
            _renderer.flipY = true;
        }
        if (!isBroken && _animator.speed <= 0)
        {
            _animator.speed = 1;
            _renderer.flipY = false;
        }

        if (lastMoveLeftRight == LRDirection.Left && !_renderer.flipX)
            _renderer.flipX = true;
        else if (lastMoveLeftRight == LRDirection.Right && _renderer.flipX)
            _renderer.flipX = false;
    }

    public void SetControlledState(bool state) {
        isControlled = state;
    }

    private void SetCarryEmpty() {
        if (_isCarrying)
        {
            _isCarrying = false;
        }
        _carriedItemGO.SetActive(false);
    }

    private void SetCarryFull(FruitSpawner spawner) {
        _isCarrying = true;
        _harvestedFrom = spawner;
        _carriedItemGO.SetActive(true);
    }
    
    public void ResetSimulation() {
        transform.position = _gameManager.GetTileCenterPosition(_spawnIndex);
        _cellIndex = _spawnIndex;
        _previousCellIndex = _spawnIndex;
        _lastMoveTime = -1;
        SetCarryEmpty();
        _commandIndex = -1;
        isBroken = false;
        Move(Vector2Int.zero);
        lastMoveLeftRight = LRDirection.Right;
    }

    void Move(Vector2Int direction) {
        _previousCellIndex = _cellIndex;
        _cellIndex += direction;
        _lastMoveTime = Time.time;

        if (direction.x > 0)
            lastMoveLeftRight = LRDirection.Right;
        else if (direction.x < 0)
            lastMoveLeftRight = LRDirection.Left;
    }
    
    void TryMove(Vector2Int direction) {
        Debug.Log("uprobot");
        if (isBroken == false && isControlled && _lastMoveTime == -1) {
            TileType targetTileType = _gameManager.GetCellTypeAtIndex(_cellIndex + direction);

            switch (targetTileType)
            {
                case TileType.Block: 
                {
                    //wall: add the movement to commands but make no movement
                    _lastCommands.Add(direction);
                    _gameManager.Resimulate(_lastCommands.Count);
                    break;
                }
                case TileType.Die:
                {
                    //pit: add the movement to commands and make a movement, but stop the simulation
                    Move(direction);
                    _lastCommands.Add(direction);
                    break;
                }
                case TileType.Floor:
                {
                    //walkable: do the movement and add it to commands then run the simulation once
                    Move(direction);
                    _lastCommands.Add(direction);
                    _gameManager.Resimulate(_lastCommands.Count);
                    break;
                }
            }
        }
    }

    private void ApplyTileEffects() {
        TileBase currentTile = _gameManager.GetCellAtIndex(_cellIndex);
        TileType currentTileType = _gameManager.GetTileType(currentTile);

        if (currentTileType == TileType.Die)
            isBroken = true;

        if (currentTile == _gameManager.fruitTile)
        {
            // landing on a fruit spawner
            var collider = Physics2D.OverlapCircle(_cellIndex, 0.5f, LayerMask.NameToLayer("Resource"));
            if (collider)
            {
                var spawner = collider.gameObject.GetComponent<FruitSpawner>();
                if (spawner && spawner.Harvest())
                {
                    Debug.Log("harvested fruit");
                    SetCarryFull(spawner);
                }
            }
        }

        if (currentTile == _gameManager.startTile)
        {
            // returning to the start
            if (_isCarrying) {
                _harvestedFrom.RespawnFruit();
                SetCarryEmpty();
            }
        }
    }

    private bool SimulatedMove(Vector2Int direction) {
        if (isBroken)
            return false;

        TileType targetTileType = _gameManager.GetCellTypeAtIndex(_cellIndex + direction);

        switch (targetTileType)
        {
            case TileType.Block:
                _previousCellIndex = _cellIndex;
                return false;
            case TileType.Die:
                Move(direction);
                ApplyTileEffects();
                return true;
            case TileType.Floor:
                Move(direction);
                ApplyTileEffects();
                return false;
        }

        return false;
    }

    public bool StepSimulation() {
        if (_lastCommands.Count > 0) {
            _commandIndex++;
            if (_commandIndex == _lastCommands.Count) {
                _commandIndex -= _lastCommands.Count;
            }
            return SimulatedMove(_lastCommands[_commandIndex]);
        }
        return false;

    }

    public void SetRequiredFruitType(int fruitType) {
        _requiredFruitType = fruitType;
    }
    
    public void OnMoveUp() {
        TryMove(Vector2Int.up);
    }

    public void OnMoveDown() {
        TryMove(Vector2Int.down);
    }

    public void OnMoveLeft() {
        TryMove(Vector2Int.left);
    }

    public void OnMoveRight() {
        TryMove(Vector2Int.right);
    }
    
    public int OnUndo() {
        if (_lastCommands.Count > 0) {
            _lastCommands.RemoveAt(_lastCommands.Count - 1);
        }

        return _lastCommands.Count;
    }
}
