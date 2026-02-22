using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace dtsInventory
{
    public class LockAndReleaseOnInputFieldEvents : MonoBehaviour, ISelectHandler
    {
        public void OnSelect(BaseEventData eventData)
        {
            InvManagerHelper.SetInvSystemLock(true);
        }

        public void UnlockInvSystem()
        {
            InvManagerHelper.SetInvSystemLock(false);
            InvManagerHelper.IgnoreOtherConfirmCommandsUntilEndOfFrame();
        }
    }
}
