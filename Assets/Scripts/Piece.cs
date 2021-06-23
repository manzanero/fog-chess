using System;
using System.Collections.Generic;
using OutlineEffect.OutlineEffect;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public GameObject body;
    public OutlineMesh outlineFocused;
    public OutlineMesh outlineSelected;
    public OutlineMesh outlineOpponent;
    public Draggable draggableBase;
    public Sprite spriteWhiteTower;
    public Sprite spriteWhiteHorse;
    public Sprite spriteWhiteBishop;
    public Sprite spriteWhiteKing;
    public Sprite spriteWhiteQueen;
    public Sprite spriteWhitePawn;
    public Sprite spriteBlackTower;
    public Sprite spriteBlackHorse;
    public Sprite spriteBlackBishop;
    public Sprite spriteBlackKing;
    public Sprite spriteBlackQueen;
    public Sprite spriteBlackPawn;

    public string color = "White";
    public string type = "Pawn";
    public bool IsMyColor => GameManager.Color == color;
    public Tile tile;
    public List<Tile> tilesInSight = new List<Tile>();

    private Camera _mainCamera;
    private bool _mouseOver;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _spriteRenderer = body.GetComponent<SpriteRenderer>();
        outlineFocused.enabled = true;
        outlineSelected.enabled = true;
        outlineOpponent.enabled = true;
    }

    private Piece Load(string colorInit, string typeInit)
    {
        name = $"{colorInit}{typeInit}";
        _spriteRenderer.sprite = colorInit switch
        {
            "White" when typeInit == "Tower" => spriteWhiteTower,
            "White" when typeInit == "Horse" => spriteWhiteHorse,
            "White" when typeInit == "Bishop" => spriteWhiteBishop,
            "White" when typeInit == "King" => spriteWhiteKing,
            "White" when typeInit == "Queen" => spriteWhiteQueen,
            "White" when typeInit == "Pawn" => spriteWhitePawn,
            "Black" when typeInit == "Tower" => spriteBlackTower,
            "Black" when typeInit == "Horse" => spriteBlackHorse,
            "Black" when typeInit == "Bishop" => spriteBlackBishop,
            "Black" when typeInit == "King" => spriteBlackKing,
            "Black" when typeInit == "Queen" => spriteBlackQueen,
            "Black" when typeInit == "Pawn" => spriteBlackPawn,
            _ => _spriteRenderer.sprite
        };
        return this;
    }

    private void Start()
    {
        outlineFocused.enabled = false;
        outlineSelected.enabled = false;
        outlineOpponent.enabled = false;
    }

    public bool Dragged => draggableBase.dragged;
    public bool MouseOver => draggableBase.mouseOver;

    private void FaceCamera()
    {
        var bodyCanvasTransform = body.transform;
        var mainCameraTransform = _mainCamera.transform;
        var mainCameraPosition = mainCameraTransform.position;
        var mainCameraRotation = mainCameraTransform.rotation;
        bodyCanvasTransform.LookAt(mainCameraPosition);
        bodyCanvasTransform.rotation = mainCameraRotation;
        var cameraForward = mainCameraTransform.forward;
        var cameraUp = mainCameraTransform.up;
        var screenForward = new Vector3(cameraForward.x, 0, cameraForward.z);
        screenForward = screenForward.magnitude < 0.001f ? cameraUp : screenForward.normalized;
        bodyCanvasTransform.localPosition =
            -Mathf.Sin(mainCameraRotation.eulerAngles.x * Mathf.Deg2Rad) * 0.5f * screenForward + 0.0625f * Vector3.up;
    }

    public bool Selected => GameManager.Instance.pieceSelected == this;

    private void OutlineSelected()
    {
        var focus = Selected || Dragged;
        outlineSelected.enabled = focus;
        if (IsMyColor) outlineFocused.enabled = MouseOver && !focus;
        else outlineOpponent.enabled = MouseOver && !focus;
    }

    private void Update()
    {
        if (MouseOver) GameManager.Instance.pieceOver = this;
        if (Dragged) GameManager.Instance.pieceDragged = this;

        FaceCamera();
        OutlineSelected();
    }

    private static bool IsInBounds(int x, int y) => x < 8 && x >= 0 && y < 8 && y >= 0;

    public List<Tile> RefreshTilesInSight()
    {
        tilesInSight.Clear();
        if (tile.capture) return tilesInSight;

        var board = GameManager.Instance.Tiles;

        var x = tile.x;
        var y = tile.y;
        if ((color == "White" || color == "Black") && type == "Tower")
        {
            for (var i = y + 1; i < 8; i++)
                tilesInSight.Add(board[x, i]);
            for (var i = x + 1; i < 8; i++)
                tilesInSight.Add(board[i, y]);
            for (var i = y - 1; i >= 0; i--)
                tilesInSight.Add(board[x, i]);
            for (var i = x - 1; i >= 0; i--)
                tilesInSight.Add(board[i, y]);
        }
        else if ((color == "White" || color == "Black") && type == "Horse")
        {
            for (var j = y - 2; j <= y + 2; j++)
            for (var i = x - 2; i <= x + 2; i++)
            {
                if (i == x && j == y) continue;
                if ((i == x - 2 || i == x + 2) && (j == y - 2 || j == y + 2)) continue;
                if (IsInBounds(i, j)) tilesInSight.Add(board[i, j]);
            }
        }
        else if ((color == "White" || color == "Black") && type == "Bishop")
        {
            for (var i = x + 1; i < 8; i++)
                if (IsInBounds(i, y + i - x)) tilesInSight.Add(board[i, y + i - x]);
            for (var i = x + 1; i < 8; i++)
                if (IsInBounds(i, y - i + x)) tilesInSight.Add(board[i, y - i + x]);
            for (var i = x - 1; i >= 0; i--)
                if (IsInBounds(i, y + i - x)) tilesInSight.Add(board[i, y + i - x]);
            for (var i = x - 1; i >= 0; i--)
                if (IsInBounds(i, y - i + x)) tilesInSight.Add(board[i, y - i + x]);
        }
        else if ((color == "White" || color == "Black") && type == "Queen")
        {
            for (var i = y + 1; i < 8; i++)
                tilesInSight.Add(board[x, i]);
            for (var i = x + 1; i < 8; i++)
                tilesInSight.Add(board[i, y]);
            for (var i = y - 1; i >= 0; i--)
                tilesInSight.Add(board[x, i]);
            for (var i = x - 1; i >= 0; i--)
                tilesInSight.Add(board[i, y]);
            for (var i = x + 1; i < 8; i++)
                if (IsInBounds(i, y + i - x)) tilesInSight.Add(board[i, y + i - x]);
            for (var i = x + 1; i < 8; i++)
                if (IsInBounds(i, y - i + x)) tilesInSight.Add(board[i, y - i + x]);
            for (var i = x - 1; i >= 0; i--)
                if (IsInBounds(i, y + i - x)) tilesInSight.Add(board[i, y + i - x]);
            for (var i = x - 1; i >= 0; i--)
                if (IsInBounds(i, y - i + x)) tilesInSight.Add(board[i, y - i + x]);
        }
        else if ((color == "White" || color == "Black") && type == "King")
        {
            if (IsInBounds(x, y + 1)) tilesInSight.Add(board[x, y + 1]);
            if (IsInBounds(x + 1, y + 1)) tilesInSight.Add(board[x + 1, y + 1]);
            if (IsInBounds(x + 1, y)) tilesInSight.Add(board[x + 1, y]);
            if (IsInBounds(x + 1, y - 1)) tilesInSight.Add(board[x + 1, y - 1]);
            if (IsInBounds(x, y - 1)) tilesInSight.Add(board[x, y - 1]);
            if (IsInBounds(x - 1, y - 1)) tilesInSight.Add(board[x - 1, y - 1]);
            if (IsInBounds(x - 1, y)) tilesInSight.Add(board[x - 1, y]);
            if (IsInBounds(x - 1, y + 1)) tilesInSight.Add(board[x - 1, y + 1]);
            if (!GameManager.Instance.kingDisrupted)
            {
                if (color == "White")
                {
                    tilesInSight.Add(board[2, 0]);
                    tilesInSight.Add(board[6, 0]);
                }
                else
                {
                    tilesInSight.Add(board[2, 7]);
                    tilesInSight.Add(board[6, 7]);
                }
            }
        }
        else if (color == "White" && type == "Pawn")
        {
            if (IsInBounds(x, y + 1)) tilesInSight.Add(board[x, y + 1]);
            if (IsInBounds(x + 1, y + 1)) tilesInSight.Add(board[x + 1, y + 1]);
            if (IsInBounds(x + 1, y)) tilesInSight.Add(board[x + 1, y]);
            if (IsInBounds(x - 1, y)) tilesInSight.Add(board[x - 1, y]);
            if (IsInBounds(x - 1, y + 1)) tilesInSight.Add(board[x - 1, y + 1]);
            if (y == 1) tilesInSight.Add(board[x, y + 2]);
        }
        else if (color == "Black" && type == "Pawn")
        {
            if (IsInBounds(x, y - 1)) tilesInSight.Add(board[x, y - 1]);
            if (IsInBounds(x + 1, y - 1)) tilesInSight.Add(board[x + 1, y - 1]);
            if (IsInBounds(x + 1, y)) tilesInSight.Add(board[x + 1, y]);
            if (IsInBounds(x - 1, y)) tilesInSight.Add(board[x - 1, y]);
            if (IsInBounds(x - 1, y - 1)) tilesInSight.Add(board[x - 1, y - 1]);
            if (y == 6) tilesInSight.Add(board[x, y - 2]);
        }

        return tilesInSight;
    }
}