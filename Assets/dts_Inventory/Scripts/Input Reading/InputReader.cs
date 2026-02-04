using mapPointer;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using static UnityEngine.EventSystems.StandaloneInputModule;


namespace dtsInventory
{
    public class InputReader : MonoBehaviour
    {
        //Declarations
        [Header("Input Settings")]
        [SerializeField] private float _pointerClickDelay = 0.1f;
        [SerializeField] private float _directionalMoveDelay = 0.02f;
        private bool _isCoolingDownPointer= false;
        private bool _isCoolingDownDirectional= false;

        [Header("Detected Pointer Inputs")]
        [SerializeField] private bool _pointerActivityDetected = false;
        [SerializeField] private bool _lClick = false;
        [SerializeField] private bool _rClick = false;
        [SerializeField] private bool _mClick = false;
        [SerializeField] private Vector3 _pointerPosition = Vector3.negativeInfinity;
        [SerializeField] private Vector3 _LastPointerPosition = Vector3.negativeInfinity;


        [Header("Detected Directional Inputs")]
        [SerializeField] private bool _directionalActivityDetected = false;
        [SerializeField] private bool _leftCmd = false;
        [SerializeField] private bool _rightCmd = false;
        [SerializeField] private bool _upCmd = false;
        [SerializeField] private bool _downCmd = false;
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
            _leftCmd = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            _rightCmd = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            _upCmd = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            _downCmd = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

            _lRotate = Input.GetKeyDown(KeyCode.Q);
            _rRotate = Input.GetKeyDown(KeyCode.E);

            _confirm = Input.GetKeyDown(KeyCode.Return);
            _back = Input.GetKeyDown(KeyCode.Escape);
            _inventoryCommand = Input.GetKeyDown(KeyCode.I);
            _alternativeInput = Input.GetKey(KeyCode.LeftShift);
            _alternativeInput2 = Input.GetKey(KeyCode.LeftControl);
            _alternativeInput3 = Input.GetKey(KeyCode.LeftAlt);

            //only read directional input to determine if keyboard is active
            //modifier/hotkeys could be used with the pointer, so don't change input mode for those
            if (_leftCmd || _rightCmd || _upCmd || _downCmd)
                _directionalActivityDetected = true;
            else _directionalActivityDetected= false;


        }

        private void EndPointerCooldown() 
        { 
            _isCoolingDownPointer = false; 
            //Debug.Log("Cooldown Over"); 
        }
        private void EndDirectionalCooldown()
        {
            _isCoolingDownDirectional = false;
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

            //respond to hotkey commands
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

                //respond to pointer inputs. Only one at a time per frame
                if (!_isCoolingDownPointer && _pointerActivityDetected)
                {
                    _isCoolingDownPointer = true;
                    Invoke(nameof(EndPointerCooldown), _pointerClickDelay);

                    if (_lClick)
                        _invInteracter.RespondToLeftClick();
                    else if (_rClick)
                        _invInteracter.RespondToRightClick();
                    else if (_mClick)
                        _invInteracter.RespondToMiddleClick();
                }
                
            }


            //respond to directional inputs
            if (_directionalActivityDetected)
                _invInteracter.SetInputMode(InputMode.Directional);


            if (!_isCoolingDownDirectional && _directionalActivityDetected)
            {
                _isCoolingDownDirectional = true;
                Invoke(nameof(EndDirectionalCooldown), _directionalMoveDelay);


                if (_leftCmd)
                    _invInteracter.RespondToLeftDirectionalCommand();
                else if (_rightCmd)
                    _invInteracter.RespondToRightDirectionalCommand();

                if (_downCmd)
                    _invInteracter.RespondToDownDirectionalCommand();
                else if (_upCmd)
                    _invInteracter.RespondToUpDirectionalCommand();
            }
            


        }




        //externals



    }
}



