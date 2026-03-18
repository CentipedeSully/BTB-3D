using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExpandUiWindowController : MonoBehaviour , IDragHandler
{
    //Declarations
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Text _screenTextObject;
    [SerializeField] private ContainerUpgradeController _upgradeController;
    [SerializeField] private InvGrid _playerInv;
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private Color _defaultColor = Color.white;
    [Tooltip("How the cell will represent itself in the upgrade preview if the player doesn't yet have it in his grid")]
    [SerializeField] private Color _additiveColor = Color.yellow;

    [Space(10)]
    [SerializeField] private GameObject _lv1Screen;
    [SerializeField] private GameObject _lv1UpgradePreviewPanel;
    [SerializeField] private RectTransform _lv1PreviewGrid;
    [SerializeField] private RectTransform _lv1CurrentGrid;
    [SerializeField] private Text _lv1PlayerPreviewGridSizeText;
    [SerializeField] private GameObject _lv1AlreadyGainedImg;

    [Space(10)]
    [SerializeField] private GameObject _lv2Screen;
    [SerializeField] private GameObject _lv2UpgradePreviewPanel;
    [SerializeField] private RectTransform _lv2PreviewGrid;
    [SerializeField] private RectTransform _lv2CurrentGrid;
    [SerializeField] private Text _lv2PlayerPreviewGridSizeText;
    [SerializeField] private GameObject _lv2AlreadyGainedImg;

    [Space(10)]
    [SerializeField] private GameObject _lv3Screen;
    [SerializeField] private GameObject _lv3UpgradePreviewPanel;
    [SerializeField] private RectTransform _lv3PreviewGrid;
    [SerializeField] private RectTransform _lv3CurrentGrid;
    [SerializeField] private Text _lv3PlayerPreviewGridSizeText;
    [SerializeField] private GameObject _lv3AlreadyGainedImg;
    private Vector2Int _pcInvDimesions;
    private Vector2Int _lastKnownPcDimensions;
    private int _currentScreen = 1;
    private string _lv1UpgradeText = "Upgrade 1";
    private string _lv2UpgradeText = "Upgrade 2";
    private string _lv3UpgradeText = "Upgrade 3";

    //monobehaviours
    private void Awake()
    {
        _screenTextObject.text = _lv1UpgradeText;
        
    }

    private void OnEnable()
    {
        UpdateEachUpgradesRelevance();
        DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
        RebuildPlayerInvPreviews();
        RecolorAllUpgradeGrids();
    }




    //internals
    private void RebuildPlayerInvPreviews()
    {
        //ignore all the work if the pc's inv hasnt changed
        if (_lastKnownPcDimensions == _pcInvDimesions)
            return;

        RebuildPlayerInvPreviewGrid(1);
        RebuildPlayerInvPreviewGrid(2);
        RebuildPlayerInvPreviewGrid(3);
    }
    private void RebuildPlayerInvPreviewGrid(int screen)
    {
        GameObject gridObject;
        Text textObject;
        switch (screen)
        {
            case 1:
                gridObject = _lv1CurrentGrid.gameObject;
                textObject = _lv1PlayerPreviewGridSizeText;
                break;

            case 2:
                gridObject = _lv2CurrentGrid.gameObject;
                textObject = _lv2PlayerPreviewGridSizeText;
                break;

            case 3:
                gridObject = _lv3CurrentGrid.gameObject;
                textObject = _lv3PlayerPreviewGridSizeText;
                break;

            //keep the default case simple. Just assume lv 1
            default:
                gridObject = _lv1CurrentGrid.gameObject;
                textObject = _lv1PlayerPreviewGridSizeText;
                break;
        }

        int childCount = gridObject.transform.childCount;
        int neededCells = _pcInvDimesions.x * _pcInvDimesions.y;
        //do nothing if we have exactly what we need already
        if (childCount == neededCells)
            return;

        //make more if we need more
        else if (childCount < neededCells)
        {
            while (childCount < neededCells)
            {
                GameObject newCellObject = Instantiate(_cellPrefab, gridObject.transform);
                neededCells--;
            }
        }

        //we need less. destroy all the excess
        else
        {
            int targetsDestroyed = 0;
            while (childCount > neededCells)
            {
                Destroy(gridObject.transform.GetChild(targetsDestroyed).gameObject);
                targetsDestroyed++;
                childCount--;
            }
        }


        GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>();
        Vector2 spacing = layout.spacing;
        Vector2 cellSize = layout.cellSize;
        int leftSpace = layout.padding.left;
        int rightSpace = layout.padding.right;
        int topSpace = layout.padding.top;
        int bottomSpace = layout.padding.bottom;

        float graphicWidth = _pcInvDimesions.x * cellSize.x + ((_pcInvDimesions.x - 1) * spacing.x) + leftSpace + rightSpace;
        float graphicHeight = _pcInvDimesions.y * cellSize.y + ((_pcInvDimesions.y - 1) * spacing.y) + topSpace + bottomSpace;

        //resize the parent to accomodate for the new dimensions
        gridObject.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(graphicWidth, graphicHeight);

        textObject.text = $"{_pcInvDimesions.x} x {_pcInvDimesions.y}";

    }

    private void UpdateEachUpgradesRelevance()
    {
        _lastKnownPcDimensions = _pcInvDimesions;
        _pcInvDimesions = _playerInv.ContainerSize();

        //ignore the call if nothing's changed
        if (_lastKnownPcDimensions == _pcInvDimesions)
            return;

        int x = _pcInvDimesions.x;
        int y = _pcInvDimesions.y;


        if (x < _upgradeController.Lv3Dimensions().x || y < _upgradeController.Lv3Dimensions().y)
            _lv3AlreadyGainedImg.SetActive(false);
        else _lv3AlreadyGainedImg.SetActive(true);


        if (x < _upgradeController.Lv2Dimensions().x || y < _upgradeController.Lv2Dimensions().y)
            _lv2AlreadyGainedImg.SetActive(false);
        else _lv2AlreadyGainedImg.SetActive(true);


        if (x < _upgradeController.Lv1Dimensions().x || y < _upgradeController.Lv1Dimensions().y)
            _lv1AlreadyGainedImg.SetActive(false);
        else _lv1AlreadyGainedImg.SetActive(true);
    }

    private void DisableConfirmBtnIfCurrentUpgradeIsIrrelevant()
    {
        switch (_currentScreen)
        {
            case 1:
                if (_lv1AlreadyGainedImg.activeSelf)
                    _confirmBtn.interactable = false;
                else _confirmBtn.interactable = true;
                break;


            case 2:
                if (_lv2AlreadyGainedImg.activeSelf)
                    _confirmBtn.interactable = false;
                else _confirmBtn.interactable = true;
                break;


            case 3:
                if (_lv3AlreadyGainedImg.activeSelf)
                    _confirmBtn.interactable = false;
                else _confirmBtn.interactable = true;
                break;


            default:
                break;
        }
    }
    private void RecolorUpgradePreviewCells(int screen)
    {
        GameObject gridObject;
        Vector2Int upgradeDimensions;
        switch (screen)
        {
            case 1:
                gridObject = _lv1PreviewGrid.gameObject;
                upgradeDimensions = _upgradeController.Lv1Dimensions();
                break;


            case 2:
                gridObject = _lv2PreviewGrid.gameObject;
                upgradeDimensions = _upgradeController.Lv2Dimensions();
                break;


            case 3:
                gridObject = _lv3PreviewGrid.gameObject;
                upgradeDimensions = _upgradeController.Lv3Dimensions();
                break;


            default:
                gridObject = _lv1PreviewGrid.gameObject;
                upgradeDimensions = _upgradeController.Lv1Dimensions();
                break;
        }
        int childIndex = 0;
        for (int y = 0; y < upgradeDimensions.y; y++)
        {
            for (int x = 0; x < upgradeDimensions.x; x++)
            {
                //recolor this cell if its out of the player's container bounds
                if (x > _pcInvDimesions.x -1 || y > _pcInvDimesions.y - 1)
                {
                    gridObject.transform.GetChild(childIndex).GetComponent<RawImage>().color = _additiveColor;
                }

                //otherwise, make sure the cell is the default color
                else 
                    gridObject.transform.GetChild(childIndex).GetComponent<RawImage>().color = _defaultColor;

                childIndex++;
            }
        }

    }
    private void RecolorAllUpgradeGrids()
    {
        //ignore all the work if the pc's inv hasnt changed
        if (_lastKnownPcDimensions == _pcInvDimesions)
            return;

        RecolorUpgradePreviewCells(1);
        RecolorUpgradePreviewCells(2);
        RecolorUpgradePreviewCells(3);
    }



    //externals
    public void GoToNextScreen()
    {
        switch (_currentScreen)
        {
            case 1:
                _currentScreen++;
                _lv1Screen.SetActive(false);
                _lv2Screen.SetActive(true);
                _screenTextObject.text = _lv2UpgradeText;

                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                break;


            case 2:
                _currentScreen++;
                _lv2Screen.SetActive(false);
                _lv3Screen.SetActive(true);
                _screenTextObject.text = _lv3UpgradeText;

                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                break;


            case 3:
                _currentScreen = 1;
                _lv3Screen.SetActive(false);
                _lv1Screen.SetActive(true);
                _screenTextObject.text = _lv1UpgradeText;

                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                break;


            default:
                break;
        }
    }

    public void GoToPrevScreen()
    {
        switch (_currentScreen)
        {
            case 1:
                _currentScreen = 3;
                _lv1Screen.SetActive(false);
                _lv3Screen.SetActive(true);
                _screenTextObject.text = _lv3UpgradeText;

                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                break;


            case 2:
                _currentScreen--;
                _lv2Screen.SetActive(false);
                _lv1Screen.SetActive(true);
                _screenTextObject.text = _lv1UpgradeText;

                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                break;


            case 3:
                _currentScreen--;
                _lv3Screen.SetActive(false);
                _lv2Screen.SetActive(true);
                _screenTextObject.text = _lv2UpgradeText;

                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                break;


            default:
                break;
        }
    }
    public void OpenWindow()
    {
        this.gameObject.SetActive(true);
    }

    public void CloseWindow()
    {
        this.gameObject.SetActive(false);
    }

    public void TriggerUpgrade()
    {
        switch (_currentScreen)
        {
            case 1:
                _upgradeController.UpgradeGridToLv1(_playerInv, _playerInv,true);
                UpdateEachUpgradesRelevance();
                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                RebuildPlayerInvPreviews();
                RecolorAllUpgradeGrids();
                break;


            case 2:
                _upgradeController.UpgradeGridToLv2(_playerInv, _playerInv, true);
                UpdateEachUpgradesRelevance();
                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                RebuildPlayerInvPreviews();
                RecolorAllUpgradeGrids();
                break;


            case 3:
                _upgradeController.UpgradeGridToLv3(_playerInv, _playerInv,true);
                UpdateEachUpgradesRelevance();
                DisableConfirmBtnIfCurrentUpgradeIsIrrelevant();
                RebuildPlayerInvPreviews();
                RecolorAllUpgradeGrids();
                break;


            default:
                break;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        GetComponent<RectTransform>().anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }
}
