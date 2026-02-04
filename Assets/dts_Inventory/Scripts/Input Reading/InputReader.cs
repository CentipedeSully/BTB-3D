using mapPointer;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.EventSystems.StandaloneInputModule;


namespace dtsInventory
{
    public class InputReader : MonoBehaviour
    {
        //Declarations
        [Header("Input Settings")]
        [SerializeField] private float _pointerClickDelay = 0.1f;
        private bool _isCoolingDown= false;

        [Header("Detected Pointer Inputs")]
        [SerializeField] private bool _pointerActivityDetected = false;
        [SerializeField] private bool _lClick = false;
        [SerializeField] private bool _rClick = false;
        [SerializeField] private bool _mClick = false;
        [SerializeField] private Vector3 _pointerPosition = Vector3.negativeInfinity;
        [SerializeField] private Vector3 _LastPointerPosition = Vector3.negativeInfinity;


        [Header("Detected Directional Inputs")]
        [SerializeField] private bool _keyboardActivityDetected = false;
        [SerializeField] private float _horizontalInput = 0;
        [SerializeField] private float _verticalInput = 0;
        [SerializeField] private bool _inventoryCommand = false;
        [SerializeField] private bool _lRotate = false;
        [SerializeField] private bool _rRotate = false;
        [SerializeField] private bool _confirm = false;
        [SerializeField] private bool _back = false;
        [SerializeField] private bool _alternativeInput = false;
        [SerializeField] private bool _alternativeInput2 = false;
        [SerializeField] private bool _alternativeInput3 = false;
        private InvInteracter _invInteracter;





        //monobehaviour
        private void Start()
        {
            _invInteracter = GetComponent<InvInteracter>();
        }
        private void Update()
        {
            
            ListenForPointerInput();
            ListenForKeyboardCommands();

            if (_invInteracter!=null)
                ShareInputsWithInvInteracter();
        }



        //internals
        private void ListenForPointerInput()
        {
            if (Input.mousePresent)
            {
                //Track last Mouse Position
                _LastPointerPosition = _pointerPosition;
                _pointerPosition = Input.mousePosition;


                _lClick = Input.GetMouseButtonDown((int)MouseBtn.Left);
                _rClick = Input.GetMouseButtonDown((int)MouseBtn.Right);
                _mClick = Input.GetMouseButtonDown((int)MouseBtn.Middle);

                //cooldown any clicks, to prevent accidental multiclicks
                if (_lClick || _rClick || _mClick)
                {
                    _isCoolingDown = true;
                    //Debug.Log("Cooldown Started");
                    Invoke(nameof(EndCooldown), _pointerClickDelay);
                }

                //update our pointer activity status
                if (_LastPointerPosition != _pointerPosition || _lClick || _rClick || _mClick)
                    _pointerActivityDetected = true;
                else
                    _pointerActivityDetected = false;
            }

            //update the pointer activity status if the mouse is lost
            else if (_pointerActivityDetected == true)
                _pointerActivityDetected = false;
        }

        private void ListenForKeyboardCommands()
        {
            _horizontalInput = Input.GetAxis("Horizontal");
            _verticalInput = Input.GetAxis("Vertical");

            _lRotate = Input.GetKeyDown(KeyCode.Q);
            _rRotate = Input.GetKeyDown(KeyCode.E);

            _confirm = Input.GetKeyDown(KeyCode.KeypadEnter);
            _back = Input.GetKeyDown(KeyCode.Escape);
            _inventoryCommand = Input.GetKeyDown(KeyCode.I);
            _alternativeInput = Input.GetKey(KeyCode.LeftShift);
            _alternativeInput2 = Input.GetKey(KeyCode.LeftControl);
            _alternativeInput3 = Input.GetKey(KeyCode.LeftAlt);

            //only read directional input to determine if keyboard is active
            //modifier/hotkeys could be used with the pointer, so don't change input mode for those
            if (_horizontalInput != 0 || _verticalInput != 0)
                _keyboardActivityDetected = true;
            else _keyboardActivityDetected= false;


        }

        private void EndCooldown() 
        { 
            _isCoolingDown = false; 
            //Debug.Log("Cooldown Over"); 
        }
        
        private void ShareInputsWithInvInteracter()
        {
            //respond to the inventory command
            if (_inventoryCommand)
                _invInteracter.ToggleInventoryWindow();

            //respond to rotation commands
            if (_lRotate)
                _invInteracter.RotateHeldItemCounterClockwise();
            if (_rRotate)
                _invInteracter.RotateHeldItemClockwise();

            //Update any combo-button states
            _invInteracter.SetAlternateInputs(_alternativeInput, _alternativeInput2,_alternativeInput3);

            if (_back)
                _invInteracter.RespondToBackInput();

            if (_confirm)
                _invInteracter.RespondToConfirm();


            if (Input.mousePresent)
            {
                if (_pointerActivityDetected)
                    _invInteracter.SetInputMode(InputMode.Pointer);

                //Update the invInteracter if the pointer's position changed
                if (_pointerPosition != _LastPointerPosition)
                {
                    //Update the InvInteracter's mouse Position
                    _invInteracter.SetMousePosition(_pointerPosition);
                }

                //respond to pointer inputs
                _invInteracter.TriggerPointerCommands(_lClick, _rClick, _mClick);
            }


            //respond to directional inputs
            if (_keyboardActivityDetected)
                _invInteracter.SetInputMode(InputMode.Directional);

            _invInteracter.TriggerDirectionalCommands(_horizontalInput, _verticalInput);


        }




        //externals



    }
}



