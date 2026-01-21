using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int _id;
    [SerializeField] private string _name;
    [SerializeField] private Faction _faction;

    [Header("Movement Settings")]
    private NavMeshAgent _agent;

    [Header("Attack Settings")]
    [SerializeField] private float _attackRange;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private float _atkCooldown;
    [SerializeField] private float _atkAnimBuildTime;
    //[SerializeField] private 
}
