using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    #region References

    private GameManager _gameManager;
    private SpriteRenderer _renderer;
    private Animator _animator;
    public AnimatorController importantSprite;
    public AnimatorController unimportantSprite;
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
        _animator = GetComponentInChildren<Animator>();
    }

    public void Appear() {
        Debug.Log("fruit appear");
        _renderer.material.color = new Color(1,1,1,1);
        SetUnimportant();
        FixToTileCenter();
        RespawnFruit();
        gameObject.SetActive(true);
    }
    
    public void Disappear() {
        Debug.Log("fruit disappear");
        SetUnimportant();
        _animator.runtimeAnimatorController = unimportantSprite;
        _renderer.material.color = Color.clear;
        gameObject.SetActive(false);
    }
    
    public void SetImportant() {
        Debug.Log("fruit important");
        _animator.runtimeAnimatorController = importantSprite;
    }

    public void SetUnimportant() {
        _animator.runtimeAnimatorController = unimportantSprite;
    }
    
    public void RespawnFruit() {
        _renderer.color = Color.white;
        isStocked = true;
    }

    public bool Harvest(int harvesterType) {
        if (isStocked && harvesterType == fruitType) {
            isStocked = false;
            Disappear();
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
