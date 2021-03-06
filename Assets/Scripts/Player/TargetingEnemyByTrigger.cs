﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetingEnemyByTrigger : MonoBehaviour
{
    public List<Transform> enemies;
    public bool DEBUG_ENEMY_FIND_STATUS, DEBUG_ENEMY_FIND_NAMES, DEBUG_ENEMY_TRIGGERING;
    
    private Transform _target, _player;
    private Vector3 _desiredMovementDirection;
    private float _currentDistance;
    private float _minDistance;
    private bool _isRotatingToEnemy = false;

    private Camera _cam;

    private void OnEnable()
    {
        EnemyEvents.current.enemyDeath += OnEnemyDeath;
        
        PlayerEvents.current.startCombatStrike += OnCombatStrikeStart;
        PlayerEvents.current.endCombatStrike += OnCombatStrikeEnd;
    }

    private void OnDisable()
    {
        EnemyEvents.current.enemyDeath -= OnEnemyDeath;
        
        PlayerEvents.current.startCombatStrike -= OnCombatStrikeStart;
        PlayerEvents.current.endCombatStrike -= OnCombatStrikeEnd;
    }

    private void Start()
    {
        enemies = new List<Transform>();
        _cam = Camera.main;

        _player = transform.parent;
    }

    private void LateUpdate()
    {
        if (!_isRotatingToEnemy) return;
        if (!_target) return;
        Rotate(_target.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.gameObject.CompareTag("Enemy")) return;
        
        //there are 2 colliders - 1 capsule and 1 trigger on each enemy, hence contains
        if (!enemies.Contains(other.transform))
        {
            if(DEBUG_ENEMY_TRIGGERING)
                print(other.gameObject.name + " entered");
            
            enemies.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(!other.gameObject.CompareTag("Enemy")) return;

        if (enemies.Contains(other.transform))
        {
            enemies.Remove(other.transform);
            if(DEBUG_ENEMY_TRIGGERING)
                print(other.gameObject.name + " exit");
        }
    }

    private bool FindTarget()
    {
        var noOfTargets = enemies.Count;

        if (noOfTargets == 1)
        {
            _target = enemies[0];
            if (DEBUG_ENEMY_FIND_STATUS)
                print("found 1 target named " + _target.name);
        }
        else if (noOfTargets == 0)
        {
            _target = null; 
            if (DEBUG_ENEMY_FIND_STATUS)
                print("No targets found for " + gameObject.name);
            return false;
        }
        else if (noOfTargets > 1)
        {
            _minDistance = 10000f;
            try
            {
                foreach (var enemy in enemies)
                {
                    //this is distance between two points, removed sqrt from formula because increased time complexity and sometimes give NaN
                    var enemyPos = enemy.position;
                    var playerPos = transform.position;
                    _currentDistance =
                        (enemyPos.x - playerPos.x) * (enemyPos.x - playerPos.x)
                        + (enemyPos.z - playerPos.z) * (enemyPos.z - playerPos.z);

                    if (DEBUG_ENEMY_FIND_NAMES)
                        print("current enemy = " + enemy.name + ", distance = " + _currentDistance);
                    if (_minDistance > _currentDistance)
                    {
                        //if v3.dot result is favorable, then
                        _minDistance = _currentDistance;
                        _target = enemy.transform;
                    }
                }
            }
            catch (Exception e)
            {
                print(e);
                return false;
            }
        }

        _isRotatingToEnemy = true;
        
        return true;
    }

    private void OnCombatStrikeStart()
    {
        //either let rotation recalculation happen when a combatstike starts
        //or everytime someone is near the player
        print("targeting success: " + FindTarget());
    }
    
    private void OnCombatStrikeEnd()
    {
        _isRotatingToEnemy = false;
        _target = null;
    }

    private void OnEnemyDeath(Transform enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);
    }
    
    public void FaceGraveToStealGold(Transform grave)
    {
        _target = grave;
        _isRotatingToEnemy = true;
    }

    public void StopFacingGrave()
    {
        _isRotatingToEnemy = false;
        _target = null;
    }

    private void Rotate(Vector3 position)
    {
        if (position.Equals(Vector3.zero))
            _desiredMovementDirection = _player.position - _cam.transform.position;
        else
            _desiredMovementDirection = position - _player.position;

        _desiredMovementDirection.y = 0f;

        _player.rotation = Quaternion.Slerp(_player.rotation, Quaternion.LookRotation(_desiredMovementDirection), 0.2f);
    }
}
