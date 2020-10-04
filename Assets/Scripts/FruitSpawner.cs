using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    #region References

    private GameManager _gameManager;
    private SpriteRenderer _renderer;
    private Animator _animator;
    public RuntimeAnimatorController importantSprite;
    public RuntimeAnimatorController unimportantSprite;
    #endregion

    #region Members
    public int fruitType = 0;
    private Vector2Int _cellIndex;
    private bool isStocked;
    bool important = false;
    #endregion


    // Start is called before the first frame update
    void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponentInChildren<Animator>();
        FixToTileCenter();
    }

    public void Appear()
    {
        SetUnimportant();
        RespawnFruit();
    }

    public void Disappear()
    {
        SetUnimportant();
    }

    public void SetImportant()
    {
        important = true;
    }

    public void SetUnimportant()
    {
        important = false;
    }

    public void RespawnFruit()
    {
        isStocked = true;
    }

    public bool Harvest(int harvesterType)
    {
        if (isStocked && harvesterType == fruitType)
        {
            isStocked = false;
            Disappear();
            return true;
        }
        return false;
    }

    private void FixToTileCenter()
    {
        _cellIndex = _gameManager.GetCellIndexAtPosition(transform.position);
        transform.position = _gameManager.GetTileCenterPosition(_cellIndex);
        _gameManager.SetIndexToFruitSpawner(_cellIndex);
    }

    public void CustomOnPreRender()
    {
        _animator.runtimeAnimatorController = important ? importantSprite : unimportantSprite;
        _renderer.color = isStocked ? Color.white : Color.clear;
    }
}
