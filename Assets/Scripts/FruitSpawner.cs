using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    #region References

    private GameManager _gameManager;
    private SpriteRenderer _renderer;
    #endregion
    
    #region Members
    public int fruitType = 0;
    private Vector2Int _cellIndex;
    private bool isStocked;
    #endregion
    
    
    // Start is called before the first frame update
    void Start() {
        _gameManager = FindObjectOfType<GameManager>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
        FixToTileCenter();
        RespawnFruit();
    }

    public void RespawnFruit() {
        _renderer.color = Color.white;
        isStocked = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool Harvest() {
        if (isStocked) {
            isStocked = false;
            _renderer.color = Color.clear;
            return true;
        }
        return false;
    }

    private void FixToTileCenter() {
        _cellIndex = _gameManager.GetCellIndexAtPosition(transform.position);
        transform.position = _gameManager.GetTileCenterPosition(_cellIndex);
        _gameManager.SetIndexToSpawner(_cellIndex);
    }
}
