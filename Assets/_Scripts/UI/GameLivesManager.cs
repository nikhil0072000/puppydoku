using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the Game-scene life counter visuals: instantiates one <see cref="LifeVisual"/>
/// per life (3 by default) under <see cref="container"/> and keeps them in sync with
/// the lives count. Referenced by <see cref="GameSceneUI"/>; gameplay code talks to
/// it through <see cref="SetLives"/> / <see cref="SpendLife"/>.
/// </summary>
public class GameLivesManager : MonoBehaviour
{
    [Tooltip("Prefab with a LifeVisual component (alive + lost visuals).")]
    [SerializeField] private LifeVisual lifeVisualPrefab;
    [Tooltip("Parent the life visuals are instantiated under. Defaults to this transform.")]
    [SerializeField] private Transform container;
    [Tooltip("How many life slots to create at startup.")]
    [Min(1)]
    [SerializeField] private int maxLives = 3;

    private readonly List<LifeVisual> _lives = new();
    private int _currentLives;

    /// <summary>Lives still shown as alive.</summary>
    public int CurrentLives => _currentLives;

    public int MaxLives => maxLives;

    private void Awake()
    {
        Build(maxLives);
    }

    /// <summary>(Re)creates the life slots. Called automatically with the inspector
    /// value; call again if a level ever uses a different life count.</summary>
    public void Build(int count)
    {
        maxLives = Mathf.Max(1, count);
        Transform parent = container != null ? container : transform;

        foreach (LifeVisual life in _lives)
            if (life != null) Destroy(life.gameObject);
        _lives.Clear();

        if (lifeVisualPrefab == null)
        {
            Debug.LogError("[GameLivesManager] No LifeVisual prefab assigned.", this);
            return;
        }

        for (int i = 0; i < maxLives; i++)
            _lives.Add(Instantiate(lifeVisualPrefab, parent));

        SetLives(maxLives);
    }

    /// <summary>Syncs all visuals to an absolute lives count (used on level load and revive).</summary>
    public void SetLives(int aliveCount)
    {
        _currentLives = Mathf.Clamp(aliveCount, 0, _lives.Count);
        for (int i = 0; i < _lives.Count; i++)
        {
            if (_lives[i] == null) continue;
            if (i < _currentLives) _lives[i].MarkAlive();
            else _lives[i].MarkLost();
        }
    }

    /// <summary>Marks the next available life as lost. No-op when none remain.</summary>
    public void SpendLife()
    {
        if (_currentLives <= 0) return;
        SetLives(_currentLives - 1);
    }
}
