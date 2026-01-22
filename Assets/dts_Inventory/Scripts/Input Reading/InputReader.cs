using mapPointer;
using UnityEngine;
using UnityEngine.Rendering;


namespace dtsInventory
{
    public class InputReader : MonoBehaviour
    {
        //Declarations
        [Header("Input Settings")]
        [SerializeField] private float _pointerClickDelay = 0.1f;
        private bool _isCoolingDown= false;

        [Header("Detected Pointer Inputs")]
        [SerializeField] private bool _lClick = false;
        [SerializeField] private bool _rClick = false;
        [SerializeField] private bool _mClick = false;


        [Header("Detected Directional Inputs")]
        [SerializeField] private float _horizontalInput = 0;
        [SerializeField] private float _verticalInput = 0;
        [SerializeField] private bool _lRotate = false;
        [SerializeField] private bool _rRotate = false;
        [SerializeField] private bool _confirm = false;
        [SerializeField] private bool _back = false;
        [SerializeField] private bool _alternativeInput = false;
        [SerializeField] private bool _alternativeInput2 = false;
        private InvInteracter _invInteracter;





        //monobehaviour
        private void Start()
        {
            _invInteracter = GetComponent<InvInteracter>();
        }
        private void Update()
        {
            
            ListenForPointerClicks();
            ListenForKeyboardCommands();

            if (_invInteracter!=null)
                ShareInputsWithInvInteracter();
        }



        //internals
        private void ListenForPointerClicks()
        {
            _lClick = Input.GetMouseButtonDown((int)MouseBtn.Left);
            _rClick = Input.GetMouseButtonDown((int)MouseBtn.Right);
            _mClick = Input.GetMouseButtonDown((int)MouseBtn.Middle);

            if (_lClick || _rClick || _mClick)
            {
                _isCoolingDown = true;
                //Debug.Log("Cooldown Started");
                Invoke(nameof(EndCooldown), _pointerClickDelay);
            }
        }
        private void ListenForKeyboardCommands()
        {
            _horizontalInput = Input.GetAxis("Horizontal");
            _verticalInput = Input.GetAxis("Vertical");

            _lRotate = Input.GetKeyDown(KeyCode.Q);
            _rRotate = Input.GetKeyDown(KeyCode.E);

            _confirm = Input.GetKeyDown(KeyCode.KeypadEnter);
            _back = Input.GetKeyDown(KeyCode.Escape);
            _alternativeInput = Input.GetKeyDown(KeyCode.LeftShift);
            _alternativeInput = Input.GetKeyDown(KeyCode.LeftControl);
        }

        private void EndCooldown() 
        { 
            _isCoolingDown = false; 
            //Debug.Log("Cooldown Over"); 
        }
        
        private void ShareInputsWithInvInteracter()
        {
            

            //respond to rotation commands
            if (_lRotate)
                _invInteracter.RotateHeldItemCounterClockwise();
            if (_rRotate)
                _invInteracter.RotateHeldItemClockwise();

            //Update any combo-button states
            //...

            //Update the InvInteracter's mouse Position
            _invInteracter.SetMousePosition(Input.mousePosition);

            //respond to pointer inputs
            _invInteracter.TriggerPointerInputs(_lClick, _rClick, _mClick);

            //respond to directional inputs
            //...

        }




        //externals



    }
}



