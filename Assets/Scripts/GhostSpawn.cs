using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostSpawn : MonoBehaviour
{
    #region References

    private GameManager _gameManager;
    private SpriteRenderer _renderer;
    #endregion
    
    #region Members
    public Vector2Int cellIndex;
    public int fruitType;
    #endregion
    
    void Start() {
        _gameManager = FindObjectOfType<GameManager>();
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sprite = null;
        FixToTileCenter();
    }

    public void ActivateSpawner() {
        _gameManager.SetIndexToActiveSpawner(cellIndex);
    }
    
    public void DeactivateSpawner() {
        _gameManager.SetIndexToInactiveSpawner(cellIndex);
    } 
    
    private void FixToTileCenter() {
        cellIndex = _gameManager.GetCellIndexAtPosition(transform.position);
        transform.position = _gameManager.GetTileCenterPosition(cellIndex);
        _gameManager.SetIndexToInactiveSpawner(cellIndex);
    }
}
