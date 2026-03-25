using mapPointer;
using System.Collections.Generic;
using UnityEngine;


namespace dtsInventory
{
    /// <summary>
    /// Filters inputs
    /// </summary>
    public static class InputFilter
    {
        static List<GameObject> _objectDisallowingNonUiInput = new List<GameObject>();


        public static List<GameObject> GetAllObjectsDisallowingNonUiInput()
        {
            return _objectDisallowingNonUiInput;
        }

        public static void DisallowNonUiInput(GameObject source)
        {

            if (!_objectDisallowingNonUiInput.Contains(source))
                _objectDisallowingNonUiInput.Add(source);
        }
        public static void AllowNonUiInput(GameObject source)
        {
            if (_objectDisallowingNonUiInput.Contains(source))
                _objectDisallowingNonUiInput.Remove(source);
        }

        public static bool AllowNonUiInput() { return _objectDisallowingNonUiInput.Count == 0; }

    }

    public class InputReader : MonoBehaviour
    {
        //Declarations
        [Header("Input Settings")]
        [SerializeField] private float _pointerClickDelay = 0.1f;
        [SerializeField] private float _directionalMoveDelay = 0.02f;
        private bool _isCoolingDownPointer= false;
        private bool _isCoolingDownDirectional= false;

        [Header("Demo-related utilities")]
        [SerializeField] private MapPointer _mapPointer;
        [SerializeField] private CameraController _camControlller;

        [Header("Detected Pointer Inputs")]
        [SerializeField] private bool _pointerActivityDetected = false;
        [SerializeField] private bool _lClick = false;
        [SerializeField] private bool _rClick = false;
        [SerializeField] private bool _mClick = false;
        [SerializeField] private Vector2 _scrollDelta = Vector2.zero;
        [SerializeField] private Vector3 _pointerPosition = Vector3.negativeInfinity;
        [SerializeField] private Vector3 _LastPointerPosition = Vector3.negativeInfinity;


        [Header("Detected Directional Inputs")]
        [SerializeField] private bool _directionalActivityDetected = false;
        [SerializeField] private bool _leftCmd = false;
        [SerializeField] private bool _rightCmd = false;
        [SerializeField] private bool _upCmd = false;
        [SerializeField] private bool _downCmd = false;
        [SerializeField] private bool _inventoryCommand = false;
        [SerializeField] private bool _editInputFieldCommandBaseKey = false;
        [SerializeField] private bool _lRotate = false;
        [SerializeField] private bool _lRotateHold = false;
        [SerializeField] private bool _rRotate = false;
        [SerializeField] private bool _rRotateHold = false;
        [SerializeField] private bool _confirm = false;
        [SerializeField] private bool _back = false;
        [SerializeField] private bool _jumpWindow = false;
        [SerializeField] private bool _alternativeInput = false;
        [SerializeField] private bool _alternativeInput2 = false;
        [SerializeField] private bool _alternativeInput3 = false;
        private InvInteracter _invInteracter;
        private Vector2 _directionalInput;

        public delegate void DirectionalInputEvent(Vector2 directionalInput);
        public event DirectionalInputEvent OnDirectionalInputDetected;
        public delegate void PressedInputEvent();
        public event PressedInputEvent OnConfirmPressed;
        public event PressedInputEvent OnCancelPressed;



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

            if (InputFilter.AllowNonUiInput() == true)
            {
                if (Input.mousePresent)
                    ShareInputsWithMapPointer();

                ShareInputsWithCamController();
            }
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
                _scrollDelta = Input.mouseScrollDelta;

                //update our pointer activity status
                if (_LastPointerPosition != _pointerPosition || _lClick || _rClick || _mClick || _scrollDelta != Vector2.zero)
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
            _lRotateHold = Input.GetKey(KeyCode.Q);
            _rRotate = Input.GetKeyDown(KeyCode.E);
            _rRotateHold = Input.GetKey(KeyCode.E);

            _confirm = Input.GetKeyDown(KeyCode.Return);
            _back = Input.GetKeyDown(KeyCode.Escape);
            _jumpWindow = Input.GetKeyDown(KeyCode.Tab);
            _inventoryCommand = Input.GetKeyDown(KeyCode.I);
            _editInputFieldCommandBaseKey = Input.GetKeyDown(KeyCode.R);
            _alternativeInput = Input.GetKey(KeyCode.LeftShift);
            _alternativeInput2 = Input.GetKey(KeyCode.LeftControl);
            _alternativeInput3 = Input.GetKey(KeyCode.LeftAlt);

            //only read directional input to determine if keyboard is active
            //modifier/hotkeys could be used with the pointer, so don't change input mode for those
            if (_leftCmd || _rightCmd || _upCmd || _downCmd || _jumpWindow)
                _directionalActivityDetected = true;
            else _directionalActivityDetected= false;

            if (_leftCmd || _rightCmd || _upCmd || _downCmd)
            {
                _directionalInput = Vector2.zero;
                if (_leftCmd)
                    _directionalInput.x -= 1;
                if (_rightCmd)
                    _directionalInput.x += 1;
                if (_upCmd) 
                    _directionalInput.y += 1;
                if (_downCmd)
                    _directionalInput.y -= 1;

                OnDirectionalInputDetected?.Invoke(_directionalInput);
            }

            if (_confirm)
                OnConfirmPressed?.Invoke();
            if (_back)
                OnCancelPressed?.Invoke();

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

            if (_jumpWindow)
                _invInteracter.RespondToJumpWindowCommand();

            if (_editInputFieldCommandBaseKey && _alternativeInput)
                _invInteracter.RespondToChangeInputFieldCommand();


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

                if (_scrollDelta.y != 0)
                    _invInteracter.SetScrollInput(_scrollDelta);

                //respond to pointer inputs (not movement). Only one at a time per frame
                if (!_isCoolingDownPointer && _lClick || _rClick || _mClick)
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

        private void ShareInputsWithMapPointer()
        {
            if (_lClick)
                _mapPointer.RespondToLeftClick();
            else if (_rClick)
                _mapPointer.RespondToRightClick();
            else if (_mClick)
                _mapPointer.RespondToMiddleClick();
        }
        private void ShareInputsWithCamController()
        {
            //respond to rotation commands

            //default cam zoom to zero if no mouse is present
            if (Input.mousePresent == false)
                _camControlller.SetCamControls(_leftCmd, _rightCmd, _upCmd, _downCmd, _lRotateHold, _rRotateHold, Vector2.zero);

            //else provide all values as is
            else
                _camControlller.SetCamControls(_leftCmd, _rightCmd, _upCmd, _downCmd, _lRotateHold, _rRotateHold, _scrollDelta);
        }


        //externals
        public Vector2 CurrentPointerPosition() { return _pointerPosition; }
        public bool DoesPointerExist() { return Input.mousePresent; }

    }
}



