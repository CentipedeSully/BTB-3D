using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dtsInventory
{
    public class ChestSpawner : MonoBehaviour
    {
        //Declarations
        [SerializeField] private Transform _spawnContainer;
        [SerializeField] private GameObject _chestPrefab;
        [SerializeField] private LayerMask _spawnDenialObjects;
        [SerializeField] private Transform _spawnLocationParent;
        private List<Transform> _spawnAreas = new();
        private List<Transform> _openSpawnAreas = new();
        private Collider[] _overlapResults;
        private int _randomizedSpawnIndex;
        [Tooltip("The dimesion used to scan over each open spawn")]
        [SerializeField] private Vector3 _overlapCastArea = Vector3.zero;
        [Tooltip("The visual point to show a preview of the cast area. Used exclusively for previewing the cast-check area")]
        [SerializeField] private Vector3 _sampleCastAreaPosition = Vector3.zero;
        [SerializeField] private Color _sampleCastAreaColor = Color.magenta;
        [SerializeField] private float _spawnDelay = 1;
        [SerializeField] private bool _enableSpawning = false;
        [SerializeField] private bool _startSpawningAfterDelay = true;
        [SerializeField] private float _startDelay = 4f;
        private IEnumerator _spawnIterater;



        //monobehaviors
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _sampleCastAreaColor;
            Gizmos.DrawWireCube(_sampleCastAreaPosition, _overlapCastArea);
        }

        private void Start()
        {
            PopulateSpawnLocations();

            if (_startSpawningAfterDelay)
            {
                Debug.Log($"Starting spawns in {_startDelay} seconds");
                Invoke(nameof(StartSpawning), _startDelay);
            }
        }
        private void Update()
        {
            if (_enableSpawning == false && _spawnIterater != null)
                EndSpawning();
            else if (_enableSpawning && _spawnIterater == null)
                StartSpawning();
        }


        //internals
        private void StartSpawning()
        {
            if (_spawnAreas.Count <= 0)
            {
                Debug.LogWarning("Failed to start spawning. No spawn areas detected");
                return;
            }
            if (_spawnIterater == null)
            {
                Debug.Log("Enabling Spawning");
                _enableSpawning = true;
                _spawnIterater = IterateSpawning();
                StartCoroutine(_spawnIterater);
            }
        }

        private void PopulateSpawnLocations()
        {
            if (_spawnLocationParent == null)
            {
                Debug.LogWarning("Can't populate the spawn positions if there's no assigned parent that holds all the spawn transforms.");
                    return;
            }

            Debug.Log("Populating Spawns");
            _spawnAreas.Clear();
            for (int i = 0; i < _spawnLocationParent.childCount; i++)
                _spawnAreas.Add(_spawnLocationParent.GetChild(i));
        }

        private void EndSpawning()
        {
            if (_spawnIterater!= null)
            {
                Debug.Log("Disabling Spawning");
                StopCoroutine(_spawnIterater);
                _spawnIterater = null;
                Debug.Log("Spawning Cancelled");
            }
        }

        private IEnumerator IterateSpawning()
        {
            Debug.Log("Spawning Started");

            while (_enableSpawning)
            {
                _openSpawnAreas.Clear();

                foreach(Transform transform in _spawnAreas)
                {
                    if (IsSpawnOpen(transform))
                        _openSpawnAreas.Add(transform);
                }

                if (_openSpawnAreas.Count > 0)
                {
                    _randomizedSpawnIndex = Random.Range(0, _openSpawnAreas.Count);
                    SpawnChest(_openSpawnAreas[_randomizedSpawnIndex].position);
                }

                yield return new WaitForSeconds(_spawnDelay);
            }

            _spawnIterater = null;
            Debug.Log("Spawning Ended Naturally");
        }

        private bool IsSpawnOpen(Transform position)
        {
            if (position == null)
                return false;

            _overlapResults = Physics.OverlapBox(position.position, _overlapCastArea / 2, Quaternion.identity, _spawnDenialObjects);

            if (_overlapResults.Length > 0)
            {
                //Debug.Log($"Position [{position.name}] blocked]");
                return false;
            }

            else
            {
                //Debug.Log($"Position [{position.name}] open]");
                return true;
            }

        }
        private GameObject SpawnChest(Vector3 position)
        {
            GameObject newChest = Instantiate(_chestPrefab, position,Quaternion.identity, _spawnContainer);
            return newChest;
        }

        //externals



    }
}

