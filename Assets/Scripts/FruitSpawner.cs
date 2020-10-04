using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    #region References

    private GameManager _gameManager;
    private SpriteRenderer _renderer;
    public Sprite importantSprite;
    public Sprite unimportantSprite;
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
        Disappear();
    }

    public void Appear() {
        Debug.Log("fruit appear");
        SetUnimportant();
        FixToTileCenter();
        RespawnFruit();
    }
    
    public void Disappear() {
        Debug.Log("fruit disappear");
        SetUnimportant();
        //_renderer.sprite = null;
    }
    
    public void SetImportant() {
        Debug.Log("fruit important");
        //_renderer.sprite = importantSprite;
    }

    public void SetUnimportant() {
        //_renderer.sprite = unimportantSprite;
    }
    
    public void RespawnFruit() {
        _renderer.color = Color.white;
        isStocked = true;
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
