using OutlineEffect.OutlineEffect;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public OutlineMesh outline;
    
    public int x;
    public int y;
    public bool capture;
    public string captureSide;
    public int order;
    
    public Piece piece;
    private MeshRenderer _renderer;

    private void Awake()
    {
        outline.enabled = true;
        _renderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        outline.enabled = false;
    }

    private void OnMouseEnter()
    {
        GameManager.Instance.tileOver = this;
        GameManager.Instance.pieceOver = piece;
    }

    public Vector3 Position()
    {
        return transform.position;
    }

    public void PlacePiece(Piece newPiece)
    {
        newPiece.transform.position = Position();
        newPiece.tile.piece = null;
        newPiece.tile = this;
        piece = newPiece;
        piece.gameObject.SetActive(_renderer.enabled);
    }

    public void Hide()
    {
        _renderer.enabled = false;
        if (piece) piece.gameObject.SetActive(false);
    }

    public void Show()
    {
        _renderer.enabled = true;
        if (piece) piece.gameObject.SetActive(true);
    }
}
