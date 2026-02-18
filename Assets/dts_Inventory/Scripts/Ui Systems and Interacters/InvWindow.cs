using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace dtsInventory
{
    public class InvWindow : MonoBehaviour, IDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform _headerRectTransform;
        [SerializeField] private RectTransform _descRectTransform;
        [SerializeField] private RectTransform _gridAreaRectTransform;
        [SerializeField] private RectTransform _actualGridRectTransform;
        [SerializeField] private RectTransform _spritesContainerTransform;

        [SerializeField] private Text _itemDescription;
        [SerializeField] private Text _itemName;
        [SerializeField] private InvGrid _itemGrid;

        [SerializeField] private Text _containerNameText;
        [SerializeField] private InputField _containerInputField;

        [Header("Customization")]
        [SerializeField] string _containerName;

        private RectTransform _rectTransform;
        private Canvas _canvas;

        public delegate void InvWindowEvent(InvWindow window);
        public event InvWindowEvent OnWindowOpened;
        public event InvWindowEvent OnWindowClosed;


        //monobehaviours
        private void OnDestroy()
        {
            //ensure all destroyed windows are removed and unsubscribed
            InvManagerHelper.UnTrackInvWindow(this);
        }



        //internals
        private void Awake()
        {
            
            _rectTransform = GetComponent<RectTransform>();
            _containerNameText.text = _containerName;
            _containerInputField.text = _containerName;
        }

        private void Start()
        {
            float gridWidth = _actualGridRectTransform.sizeDelta.x;
            float headerHeight = _headerRectTransform.sizeDelta.y;
            float descHeight = _descRectTransform.sizeDelta.y;
            float gridHeight = _actualGridRectTransform.sizeDelta.y;
            _gridAreaRectTransform.sizeDelta = new Vector2(gridWidth, gridHeight);
            _rectTransform.sizeDelta = new Vector2(gridWidth, headerHeight + descHeight + gridHeight);
            _spritesContainerTransform.localPosition = _actualGridRectTransform.localPosition;

            _canvas = CanvasReferenceHelper.GetCanvas();

            InvManagerHelper.TrackNewInvWindow(this);
            InvManagerHelper.ParentInvWindowToInventoryUisContainer(this);
        }




        //externals
        public InvGrid GetItemGrid() { return _itemGrid; }
        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;

            if (ContextWindowHelper.IsContextWindowShowing())
            {
                //if the context window is bound to this window,
                //then move the context window this this window
                if (ContextWindowHelper.CurrentlyBoundWindow() == this)
                    ContextWindowHelper.MoveWindow(eventData.delta);
            }
        }
        public void SetItemDescription(string newDescription) { _itemDescription.text = newDescription; }
        public void SetItemName(string itemName) { _itemName.text = itemName; }
        public bool IsWindowOpen() { return gameObject.activeSelf; }
        public void CloseWindow()
        {
            if (gameObject.activeSelf)
            {
                //close the context window if its bound to this window
                if (ContextWindowHelper.IsContextWindowShowing())
                {
                    if (ContextWindowHelper.CurrentlyBoundWindow() == this)
                    {
                        ContextWindowHelper.HideContextWindow();
                        _itemGrid.ForceImmediateUndarken();
                    }
                }
                //Debug.Log($"Firing [{gameObject.name}]'s OnClose Window Event now...");
                OnWindowClosed?.Invoke(this);
                gameObject.SetActive(false);
                

            }
                
        }
        public void OpenWindow()
        {
            if (!gameObject.activeSelf)
            {

                gameObject.SetActive(true);
                //Debug.Log($"Firing [{gameObject.name}]'s OnOpen Window Event now...");
                OnWindowOpened?.Invoke(this);
            }
        }
        public string ContainerName() { return _containerName; }
        public void RenameContainer(string newName)
        {
            if (newName == "")
                return;
            
            _containerName = newName;
            _containerNameText.text = _containerName;
            _containerInputField.text = _containerName;
        }
        public void RenameContainerViaInput()
        {
            _containerName = _containerNameText.text;
        }


        public void DarkenGrid() { _itemGrid.DarkenGrid(); }
        public void UndarkenGrid() { _itemGrid.UndarkenGrid(); }
    }
}
