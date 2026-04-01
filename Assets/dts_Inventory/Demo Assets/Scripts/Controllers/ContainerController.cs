
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;


namespace dtsInventory
{
    [Serializable]
    public struct LootRoll
    {
        public ItemData itemdata;
        [Range(0, 1)] public float successChance;
        [Min(1)] public int minAmount;
        [Min(1)] public int maxAmount;

        LootRoll(ItemData itemData, float chance, int min, int max)
        {
            this.itemdata = itemData;
            this.successChance = Mathf.Clamp(chance, 0, 1);
            this.minAmount = min;
            this.maxAmount = max;

            //make sure the min/max is at least 1
            minAmount = Mathf.Max(minAmount, 1);
            maxAmount = Mathf.Max(maxAmount, 1);

            //swap the values if the min and max got mixed up
            if (minAmount > maxAmount)
            {
                int tempMin = minAmount;
                minAmount = maxAmount;
                maxAmount = tempMin;
            }
        }

        public override string ToString()
        {
            return itemdata.name + ": " + $"{successChance * 100}%";
        }
    }


    public interface IInteractable
    {
        GameObject GetGameObject();
        void TriggerInteraction();
        void EndInteraction();
        float InteractDistance();

    }

    public class ContainerController : MonoBehaviour, IInteractable
    {
        //Declarations
        [SerializeField] private GameObject _uiWindowPrefab;
        [SerializeField] private float _interactRadius = 1;
        [SerializeField] private bool _showLootRolls = false;
        [SerializeField] List<LootRoll> _lootTable = new List<LootRoll>();
        private InvWindow _invWindow;
        private Transform _containerUiParent;
        private IEnumerator _containerInitializer;
        private bool _contentsInitialized = false;


        public delegate void ContainerInteractionEvent();
        public event ContainerInteractionEvent OnContainerOpened;
        public event ContainerInteractionEvent OnContainerClosed;


        [Header("Debug")]
        [SerializeField] private bool _isDebugActive = false;
        [SerializeField] private bool _cmdOpenContainer = false;
        [SerializeField] private bool _cmdCloseContainer = false;
        [SerializeField] private bool _cmdRerollContainer = false;



        //Monobehaviours
        private void Update()
        {
            if (_isDebugActive)
                ListenForDebugCommands();
        }

        private void OnDestroy()
        {
            DestroyContainerUi();
        }



        //internals
        private IEnumerator InitializeContentsNextFrame()
        {
            yield return new WaitForEndOfFrame();
            InitializeContents();
            _containerInitializer = null;

        }
        private void CreateNewUi()
        {
            if (_invWindow != null)
                return;

            if (_uiWindowPrefab == null)
            {
                Debug.LogError("Missing ui prefab reference. Failed to create a new ui window");
                return;
            }

            GameObject newUi = Instantiate(_uiWindowPrefab);
            if (_containerUiParent == null && InvManagerHelper.GetParentUiTransformForContainers() != null)
                _containerUiParent = InvManagerHelper.GetParentUiTransformForContainers();

            //set the new Ui's parent
            if (_containerUiParent != null)
                newUi.GetComponent<RectTransform>().SetParent(_containerUiParent.transform,false);


            _invWindow = newUi.GetComponent<InvWindow>();
            InvManagerHelper.TrackNewInvWindow(_invWindow);

        }

        private void InitializeContents()
        {
            if (_invWindow == null)
                return;

            List<bool> lootRollResults = RollForLoot(_lootTable, _showLootRolls);


            //add all successfully-rolled items to the container
            for (int i =0; i < _lootTable.Count; i++)
            {
                
                if (lootRollResults[i] == true)
                {
                    int amountToAdd = UnityEngine.Random.Range(_lootTable[i].minAmount, _lootTable[i].maxAmount + 1);
                    
                    if (_showLootRolls)
                        Debug.Log($"Rolled Amount to add: {amountToAdd}");

                    _invWindow.GetItemGrid().AddItem(_lootTable[i].itemdata, amountToAdd);
                }
                    
            }

            _contentsInitialized = true;
        }
        private void DestroyContainerUi()
        {
            if (_invWindow == null)
                return;

            Destroy(_invWindow.gameObject);

        }






        //externals
        public void OpenContainer()
        {
            //be sure to initialize the window if it doesn't yet exist
            if (_invWindow == null)
            {
                CreateNewUi();
                if (_containerInitializer == null && !_contentsInitialized)
                {
                    _containerInitializer = InitializeContentsNextFrame();
                    StartCoroutine(_containerInitializer);
                }

            }

            _invWindow.OpenWindow();
            OnContainerOpened?.Invoke();


        }

        public void CloseContainer()
        {
            if (_invWindow == null)
                return;

            _invWindow.CloseWindow();
            OnContainerClosed?.Invoke();
        }
        public GameObject GetGameObject(){ return gameObject; }

        public void TriggerInteraction() { OpenContainer(); }
        public void EndInteraction() { CloseContainer(); }
        public float InteractDistance() { return _interactRadius; }
        

        public static List<bool> RollForLoot(List<LootRoll> lootTable, bool logRolls = false)
        {
            if (lootTable == null)
                return null;

            
            List<bool> results = new List<bool>();

            for (int i = 0; i < lootTable.Count; i++)
            {
                float rollResult = UnityEngine.Random.Range(0, 1.0f);
                float successThreshold = 1 - lootTable[i].successChance;

                if ( rollResult >= successThreshold)
                    results.Add(true);
                else results.Add(false);

                if (logRolls)
                    Debug.Log($"Roll Result for loot table entry [{lootTable[i]}]\n" +
                        $"Roll Result: {rollResult} \n" +
                        $"Needed Value: {successThreshold}\n"+
                        $"Did roll succeed?: [{results[i]}]");
            }


            return results;
            
        }


        //debug
        private void ListenForDebugCommands()
        {
            if (_cmdOpenContainer)
            {
                _cmdOpenContainer = false;
                OpenContainer();
            }

            if (_cmdCloseContainer)
            {
                _cmdCloseContainer = false;
                CloseContainer();
            }

            if (_cmdRerollContainer)
            {
                _cmdRerollContainer = false;
                Dictionary<HashSet<(int, int)>, ItemData> allStacks = _invWindow.GetItemGrid().GetAllStacks();


                
                //delete the contents of the container
                foreach (KeyValuePair<HashSet<(int, int)>, ItemData> entry in allStacks)
                {
                    //Debug.Log($"Deleting stack at position [{_invWindow.GetItemGrid().StringifyPositions(entry.Key)}]...");
                    _invWindow.GetItemGrid().DeleteStack(entry.Key.First());
                }

                InitializeContents();
                
            }
        }

        
    }
}


