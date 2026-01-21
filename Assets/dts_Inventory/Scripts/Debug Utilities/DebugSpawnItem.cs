using UnityEngine;

namespace dtsInventory
{
    public class DebugSpawnItem : MonoBehaviour
    {
        [SerializeField] private bool _spawnItem;
        [SerializeField] private bool _removeItem;
        [SerializeField] private InvWindow _targetInv;
        [SerializeField] private ItemData _specificItem;
        [SerializeField] private int _amount;





        private void Update()
        {
            if (_targetInv != null)
                ListenForCommands();
        }


        private void ListenForCommands()
        {
            //ensure logical errors are corrected
            if (_amount < 1)
                _amount = 1;


            if (_spawnItem)
            {
                _spawnItem = false;
                _targetInv.GetItemGrid().AddItem(_specificItem, _amount);
            }

            if (_removeItem)
            {
                _removeItem = false;
                _targetInv.GetItemGrid().RemoveItem(_specificItem, _amount);

            }

        }



    }
}
