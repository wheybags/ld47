using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotBehavior : MonoBehaviour {

    #region References
    private GameManager _gameManager;
    private GameObject _carriedItemGO;
    #endregion

    #region Members
    public bool isControlled = false;
    public bool isBroken;
    private Vector2Int _cellIndex;
    private Vector2Int _spawnIndex;
    private int _requiredFruitType;
    private bool _isCarrying;
    private FruitSpawner _harvestedFrom;
    [SerializeField] private List<Vector2Int> _lastCommands;
    private int _commandIndex;
    #endregion
    
    void Start() {
        _carriedItemGO = transform.Find("CarriedItem").gameObject;
        _gameManager = FindObjectOfType<GameManager>();
        GetComponent<SpriteRenderer>().color = Random.ColorHSV(0f,1f,0.4f,0.5f,0.4f,0.5f);
        _lastCommands = new List<Vector2Int>();
        _cellIndex = _gameManager.GetCellIndexAtPosition(transform.position);
        _spawnIndex = _cellIndex;
        ResetSimulation();
    }

    public void SetControlledState(bool state) {
        isControlled = state;
    }

    private void SetCarryEmpty() {
        _isCarrying = false;
        _carriedItemGO.SetActive(false);
    }

    private void SetCarryFull(FruitSpawner spawner) {
        _isCarrying = true;
        _harvestedFrom = spawner;
        _carriedItemGO.SetActive(true);
    }
    
    public void ResetSimulation() {
        _cellIndex = _spawnIndex;
        SetCarryEmpty();
        _commandIndex = -1;
        isBroken = false;
        Move(Vector2Int.zero);
    }

    void Move(Vector2Int direction) {
        transform.position = _gameManager.GetTileCenterPosition(_cellIndex + direction);
        _cellIndex += direction;
        ApplyTileEffects();
    }
    
    void TryMove(Vector2Int direction) {
        Debug.Log("uprobot");
        if (isBroken == false && isControlled) {
            var destination = _gameManager.GetCellTypeAtIndex(_cellIndex + direction);
            if (destination == -1) {
                Debug.Log("found no tile");
                return;
            }

            if (destination <= 1) {
                //wall: add the movement to commands but make no movement
                _lastCommands.Add(direction);
                _gameManager.Resimulate(_lastCommands.Count);
            }
            else if (destination == 2) {
                //pit: add the movement to commands and make a movement, but stop the simulation
                Move(direction);
                _lastCommands.Add(direction);
            }
            else {
                //walkable: do the movement and add it to commands then run the simulation once
                Move(direction);
                _lastCommands.Add(direction);
                _gameManager.Resimulate(_lastCommands.Count);
            }
        }
    }

    private void ApplyTileEffects() {
        var currentTile = _gameManager.GetCellTypeAtIndex(_cellIndex);
        if (currentTile == 2) {
            //pit: set broken to true
            isBroken = true;
        } else if (currentTile == 4) {
            //landing on a fruit spawner
            var collider =
                Physics2D.OverlapCircle((Vector2) transform.position, 0.5f, LayerMask.NameToLayer("Resource"));
            if (collider) {
                var spawner = collider.gameObject.GetComponent<FruitSpawner>();
                if (spawner && spawner.Harvest()) {
                    Debug.Log("harvested fruit");
                    SetCarryFull(spawner);
                }
            }
        } else if (currentTile == 3) {
            //returning to the start
            if (_isCarrying) {
                _harvestedFrom.RespawnFruit();
                SetCarryEmpty();
            }
        }
    }

    private bool SimulatedMove(Vector2Int direction) {
        if (isBroken == false) {
            var destination = _gameManager.GetCellTypeAtIndex(_cellIndex + direction);
            if (destination == -1) {
                Debug.Log("found no tile");
                return false;
            }

            if (destination <= 1) {
                //wall: add the movement to commands but make no movement
                return false;
            }

            if (destination == 2) {
                //pit: make a movement, but stop the simulation
                Move(direction);
                return true;
            }

            //walkable: do the movement
            Move(direction);
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
