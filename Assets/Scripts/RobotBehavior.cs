using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RobotBehavior : MonoBehaviour {

    #region References
    private GameManager _gameManager;
    private GameObject _carriedItemGO;
    private SpriteRenderer _carriedItemRenderer;
    private Animator _animator;
    private SpriteRenderer _renderer;
    #endregion

    #region Members
    public bool isControlled = false;
    public bool isBroken;
    public bool isFinished;
    public Vector2Int cellIndex { get; private set; }
    public Vector2Int previousCellIndex;
    public float lastMoveTime = 0;

    private Vector2Int _spawnIndex;
    private int _spawnWaitTicks;
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

    private Color[] tints =
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
        _carriedItemRenderer = _carriedItemGO.GetComponent<SpriteRenderer>();
        _gameManager = FindObjectOfType<GameManager>();
        _lastCommands = new List<Vector2Int>();
        cellIndex = _gameManager.GetCellIndexAtPosition(transform.position);
        _spawnIndex = cellIndex;
        ResetSimulation();
    }

    private void Update()
    {
        //Debug.DrawLine(new Vector3(0, 0, 0), _gameManager.GetTileCenterPosition(cellIndex), Color.red, 1f, false);

        //if (transform.position == _gameManager.GetTileCenterPosition(_spawnIndex) && _gameManager._tick != 0)
        //{
        //    Debug.DebugBreak();
        //}



        if (isBroken && _animator.speed > 0)
        {
            _animator.speed = 0;
            _renderer.flipY = true;
            _carriedItemRenderer.flipY = true;
        }
        if (!isBroken && _animator.speed <= 0)
        {
            _animator.speed = 1;
            _renderer.flipY = false;
            _carriedItemRenderer.flipY = false;
        }

        if (lastMoveLeftRight == LRDirection.Left && !_renderer.flipX)
        {
            _renderer.flipX = true;
            _carriedItemRenderer.flipX = true;
        }
        else if (lastMoveLeftRight == LRDirection.Right && _renderer.flipX)
        {
            _renderer.flipX = false;
            _carriedItemRenderer.flipX = false;
        }
        
        if (isControlled) {
            _renderer.material.color = new Color(2.0f, 2.0f, 1.0f, 1);
        }
        else {
            _renderer.material.color = Color.white;
        }
    }

    public void CustomOnPreRender()
    {
        if (lastMoveTime != -1)
        {
            const float movementTimeInSeconds = 0.1f;

            float timeSinceMove = Time.time - lastMoveTime;
            float alpha = timeSinceMove * 1 / movementTimeInSeconds;

            SetRenderPosition(Vector3.Lerp(_gameManager.GetTileCenterPosition(previousCellIndex), _gameManager.GetTileCenterPosition(cellIndex), alpha));

            if (alpha >= 1)
            {
                ApplyTileEffects();
                lastMoveTime = -1;
            }
        }
    }

    public void SetSpawnWait(int ticks) {
        _spawnWaitTicks = ticks;
    }
    
    public void SetControlledState(bool state) {
        isControlled = state;
    }

    private void SetCarryEmpty() {
        _isCarrying = false;
        _carriedItemRenderer.material.color = Color.clear;
    }

    private void SetCarryFull(FruitSpawner spawner) {
        _isCarrying = true;
        _harvestedFrom = spawner;
        _carriedItemRenderer.material.color = Color.white;
    }
    
    public void ResetSimulation() {
        SetRenderPosition(_gameManager.GetTileCenterPosition(_spawnIndex));
        cellIndex = _spawnIndex;
        previousCellIndex = _spawnIndex;
        lastMoveTime = -1;
        SetCarryEmpty();
        _commandIndex = -1;
        isBroken = false;
        isFinished = false;
        Move(Vector2Int.zero);
        lastMoveLeftRight = LRDirection.Right;
    }

    bool Move(Vector2Int direction) {
        if (direction.x > 0)
            lastMoveLeftRight = LRDirection.Right;
        else if (direction.x < 0)
            lastMoveLeftRight = LRDirection.Left;

        RobotBehavior otherRobot = _gameManager.isCellBlockedByRobot(cellIndex + direction);
        if (otherRobot && otherRobot != this)
        {
            isBroken = true;
            otherRobot.isBroken = true;
            return false;
        }

        previousCellIndex = cellIndex;
        cellIndex += direction;
        lastMoveTime = Time.time;

        return true;
    }
    
    void TryMove(Vector2Int direction) {
        if (isBroken == false && isControlled && lastMoveTime == -1 && _gameManager._tick < _gameManager.maxMoves) {
            TileType targetTileType = _gameManager.GetCellTypeAtIndex(cellIndex + direction);

            switch (targetTileType)
            {
                case TileType.Block: 
                {
                    //wall: add the movement to commands but make no movement
                    _lastCommands.Add(direction);
                    _gameManager.Resimulate(_lastCommands.Count + _spawnWaitTicks, false);
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
                    bool alive = Move(direction);
                    _lastCommands.Add(direction);
                    if (alive)
                        _gameManager.Resimulate(_lastCommands.Count  + _spawnWaitTicks, false);
                    break;
                }
            }
        }
    }

    private void ApplyTileEffects() {
        TileBase currentTile = _gameManager.GetCellAtIndex(cellIndex);
        TileType currentTileType = _gameManager.GetTileType(currentTile);

        if (currentTileType == TileType.Die)
            isBroken = true;

        if (currentTile == _gameManager.fruitTile)
        {
            // landing on a fruit spawner
            var collider = Physics2D.OverlapCircle(cellIndex, 0.5f);
            if (collider)
            {
                var spawner = collider.gameObject.GetComponent<FruitSpawner>();
                if (spawner && spawner.Harvest(_requiredFruitType))
                {
                    SetCarryFull(spawner);
                }
            }
        }

        if (currentTile == _gameManager.activeStartTile || currentTile == _gameManager.inactiveStartTile)
        {
            // returning to the start
            if (_isCarrying) {
                var collider = Physics2D.OverlapCircle(cellIndex, 0.5f);
                if (collider) {
                    var spawner = collider.gameObject.GetComponent<GhostSpawn>();
                    if (spawner && spawner.fruitType == _requiredFruitType) {
                        _harvestedFrom.RespawnFruit();
                        SetCarryEmpty();
                        isFinished = true;
                        if (isControlled) {
                            _gameManager.RelinquishControl(this);
                        }
                    }
                }
            }
        }
        
        if (currentTile == _gameManager.fruitTile)
        {
            // landing on a starting tile spawner
            var collider = Physics2D.OverlapCircle(cellIndex, 0.5f, LayerMask.NameToLayer("Resource"));
            if (collider)
            {
                var spawner = collider.gameObject.GetComponent<FruitSpawner>();
                if (spawner && spawner.Harvest(_requiredFruitType))
                {
                    SetCarryFull(spawner);
                }
            }
        }
    }

    private bool SimulatedMove(Vector2Int direction) {
        if (isBroken)
            return false;

        TileType targetTileType = _gameManager.GetCellTypeAtIndex(cellIndex + direction);

        switch (targetTileType)
        {
            case TileType.Block:
                previousCellIndex = cellIndex;
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

    public bool StepSimulation(int tick) {
        if (tick > _spawnWaitTicks && isFinished == false) {
            if (_lastCommands.Count > 0) {
                _commandIndex++;

                if (_lastCommands.Count > _commandIndex) {
                    return SimulatedMove(_lastCommands[_commandIndex]);
                }
                else {
                    isFinished = true;
                }
            }
        }

        return false;

    }

    public void ClearCommands() {
        _lastCommands = new List<Vector2Int>();
    }
    
    void SetRenderPosition(Vector3 pos)
    {
        transform.position = pos;
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

        return _lastCommands.Count + _spawnWaitTicks;
    }

}
